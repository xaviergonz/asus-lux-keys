using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using AsusLuxKeys.Configuration;
using AsusLuxKeys.Hardware;
using AsusLuxKeys.Rules;

namespace AsusLuxKeys.UI;

public sealed class OptionsForm : Form
{
    private readonly CheckBox _enabled = new() { Text = "Enabled", AutoSize = true, FlatStyle = FlatStyle.System };
    private readonly CheckBox _runOnStartup = new() { Text = "Run on startup", AutoSize = true, FlatStyle = FlatStyle.System };
    private readonly Label _colorValue = new()
    {
        AutoSize = false,
        Anchor = AnchorStyles.Left,
        BorderStyle = BorderStyle.FixedSingle,
        Padding = new Padding(8, 0, 8, 0),
        MinimumSize = new Size(140, 34),
        Size = new Size(140, 34),
        TextAlign = ContentAlignment.MiddleLeft
    };
    private readonly Label _currentLumensValue = new() { AutoSize = true, Anchor = AnchorStyles.Left };
    private readonly Button _colorButton = CreateButton("...", width: 56);
    private readonly DataGridView _rulesGrid = new();
    private readonly BindingList<RuleRow> _rows = [];
    private readonly Func<double?> _currentLumens;
    private readonly bool _showColorOptions;
    private readonly System.Windows.Forms.Timer _lumensTimer;

    private bool _loading;
    private string _color;

    public OptionsForm(AppSettings settings, Func<double?> currentLumens, bool showColorOptions, bool runOnStartup, Icon icon)
    {
        Text = $"{AppInfo.DisplayName} Options";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(720, 520);
        Size = new Size(800, 600);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        TopMost = true;
        Font = SystemFonts.MessageBoxFont;
        BackColor = SystemColors.Control;
        Icon = (Icon)icon.Clone();

        _color = settings.Color;
        _currentLumens = currentLumens;
        _showColorOptions = showColorOptions;
        _runOnStartup.Checked = runOnStartup;
        _lumensTimer = new System.Windows.Forms.Timer { Interval = AppTiming.ReconcileIntervalMilliseconds };
        _lumensTimer.Tick += (_, _) => UpdateCurrentLumens();

        BuildLayout();
        LoadSettings(settings);
        UpdateCurrentLumens();
        _lumensTimer.Start();
    }

    public event EventHandler<OptionsSavedEventArgs>? SettingsSaved;

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            RowCount = _showColorOptions ? 6 : 5,
            ColumnCount = 1
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        if (_showColorOptions)
        {
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var lumensRow = CreateValueRow("Current lumens", _currentLumensValue);

        _rulesGrid.AutoGenerateColumns = false;
        _rulesGrid.AllowUserToAddRows = false;
        _rulesGrid.AllowUserToDeleteRows = false;
        _rulesGrid.BackgroundColor = SystemColors.Window;
        _rulesGrid.BorderStyle = BorderStyle.FixedSingle;
        _rulesGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        _rulesGrid.EditMode = DataGridViewEditMode.EditOnEnter;
        _rulesGrid.RowHeadersVisible = false;
        _rulesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _rulesGrid.MultiSelect = false;
        _rulesGrid.Dock = DockStyle.Fill;
        _rulesGrid.CellEndEdit += (_, e) => _rulesGrid.Rows[e.RowIndex].ErrorText = "";
        _rulesGrid.CellValidating += RulesGrid_CellValidating;
        _rulesGrid.CellValueChanged += (_, _) => SaveIfValid();
        _rulesGrid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_rulesGrid.IsCurrentCellDirty)
            {
                _rulesGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        };
        _rulesGrid.DataError += (_, e) => e.ThrowException = false;
        _rulesGrid.DataSource = _rows;
        _rulesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(RuleRow.Lumens),
            HeaderText = "Lumens",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });
        _rulesGrid.Columns.Add(new DataGridViewComboBoxColumn
        {
            DataPropertyName = nameof(RuleRow.Brightness),
            HeaderText = "Keyboard brightness",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            DataSource = Enum.GetValues<KeyboardBrightness>()
                .Select(BrightnessOption.FromBrightness)
                .ToList(),
            DisplayMember = nameof(BrightnessOption.Text),
            ValueMember = nameof(BrightnessOption.Value)
        });

        var listButtons = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            Dock = DockStyle.Fill
        };
        var add = CreateButton("Add");
        var remove = CreateButton("Remove");
        add.Click += (_, _) => AddRule();
        remove.Click += (_, _) => RemoveSelectedRule();
        listButtons.Controls.Add(add);
        listButtons.Controls.Add(remove);

        root.Controls.Add(_enabled, 0, 0);
        var row = 1;
        if (_showColorOptions)
        {
            root.Controls.Add(CreateColorRow(), 0, row++);
        }

        root.Controls.Add(lumensRow, 0, row++);
        root.Controls.Add(_rulesGrid, 0, row++);
        root.Controls.Add(listButtons, 0, row++);
        root.Controls.Add(_runOnStartup, 0, row++);
        Controls.Add(root);

        _enabled.CheckedChanged += (_, _) => SaveIfValid();
        _runOnStartup.CheckedChanged += (_, _) => SaveIfValid();
        _rows.ListChanged += (_, _) => SaveIfValid();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _lumensTimer.Dispose();
            Icon?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void LoadSettings(AppSettings settings)
    {
        _loading = true;
        _enabled.Checked = settings.Enabled;
        _color = SettingsStore.ToHex(SettingsStore.ParseColor(settings.Color));
        if (_showColorOptions)
        {
            UpdateColorLabel();
        }
        _rows.Clear();

        foreach (var rule in BrightnessRuleEngine.Normalize(settings.Rules))
        {
            _rows.Add(new RuleRow
            {
                Lumens = BrightnessRuleEngine.FormatLumens(rule.Lumens),
                Brightness = rule.Brightness
            });
        }
        _loading = false;
    }

    private void PickColor()
    {
        using var dialog = new ColorDialog
        {
            FullOpen = true,
            Color = SettingsStore.ParseColor(_color)
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _color = SettingsStore.ToHex(dialog.Color);
            UpdateColorLabel();
            SaveIfValid();
        }
    }

    private void UpdateColorLabel()
    {
        _colorValue.Text = _color;
        _colorValue.BackColor = SettingsStore.ParseColor(_color);
        _colorValue.ForeColor = _colorValue.BackColor.GetBrightness() < 0.45 ? Color.White : Color.Black;
    }

    private void UpdateCurrentLumens()
    {
        var lumens = _currentLumens();
        _currentLumensValue.Text = lumens is null
            ? "Unavailable"
            : lumens.Value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private void AddRule()
    {
        _rows.Add(new RuleRow { Lumens = "Infinity", Brightness = KeyboardBrightness.High });
        _rulesGrid.CurrentCell = _rulesGrid.Rows[^1].Cells[0];
    }

    private void RemoveSelectedRule()
    {
        if (_rulesGrid.CurrentRow?.DataBoundItem is RuleRow row)
        {
            _rows.Remove(row);
        }
    }

    private void SaveIfValid()
    {
        if (_loading)
        {
            return;
        }

        if (!_rulesGrid.EndEdit())
        {
            return;
        }

        if (!TryReadRules(out var rules))
        {
            return;
        }

        SettingsSaved?.Invoke(this, new OptionsSavedEventArgs
        {
            Settings = new AppSettings
            {
                Enabled = _enabled.Checked,
                Color = _color,
                Rules = BrightnessRuleEngine.Normalize(rules)
            },
            RunOnStartup = _runOnStartup.Checked
        });
    }

    private void RulesGrid_CellValidating(object? sender, DataGridViewCellValidatingEventArgs e)
    {
        if (_rulesGrid.Columns[e.ColumnIndex].DataPropertyName != nameof(RuleRow.Lumens))
        {
            return;
        }

        var value = Convert.ToString(e.FormattedValue, CultureInfo.CurrentCulture);
        if (BrightnessRuleEngine.TryParseLumens(value, out _))
        {
            return;
        }

        _rulesGrid.Rows[e.RowIndex].ErrorText = $"Invalid lumens value: {value}";
        e.Cancel = true;
    }

    private bool TryReadRules(out List<BrightnessRule> rules)
    {
        rules = [];

        foreach (var row in _rows)
        {
            if (!BrightnessRuleEngine.TryParseLumens(row.Lumens, out var lumens))
            {
                return false;
            }

            if (!Enum.IsDefined(row.Brightness))
            {
                return false;
            }

            rules.Add(new BrightnessRule
            {
                Lumens = lumens,
                Brightness = row.Brightness
            });
        }

        return true;
    }

    private sealed class RuleRow
    {
        public string Lumens { get; set; } = "Infinity";

        public KeyboardBrightness Brightness { get; set; } = KeyboardBrightness.High;
    }

    private sealed class BrightnessOption
    {
        public required KeyboardBrightness Value { get; init; }

        public required string Text { get; init; }

        public static BrightnessOption FromBrightness(KeyboardBrightness brightness)
        {
            return new BrightnessOption
            {
                Value = brightness,
                Text = brightness.ToDisplayText()
            };
        }
    }

    private static TableLayoutPanel CreateValueRow(string label, Control value)
    {
        var row = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 8, 0, 4)
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.Controls.Add(new Label
        {
            Text = label,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 0, 24, 0)
        }, 0, 0);
        row.Controls.Add(value, 1, 0);
        return row;
    }

    private TableLayoutPanel CreateColorRow()
    {
        var row = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 4,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 8, 0, 8)
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.Controls.Add(new Label
        {
            Text = "Color",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 0, 24, 0)
        }, 0, 0);
        _colorValue.Margin = new Padding(0, 0, 8, 0);
        row.Controls.Add(_colorValue, 1, 0);
        row.Controls.Add(_colorButton, 2, 0);
        _colorButton.Click += (_, _) => PickColor();
        return row;
    }

    private static Button CreateButton(string text, int width = 112)
    {
        return new Button
        {
            Text = text,
            AutoSize = false,
            FlatStyle = FlatStyle.System,
            Margin = new Padding(6),
            MinimumSize = new Size(width, 42),
            Padding = new Padding(12, 6, 12, 6),
            Size = new Size(width, 42),
            UseVisualStyleBackColor = true
        };
    }
}
