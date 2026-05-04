using KeyboardAmbientLight.UI;

namespace KeyboardAmbientLight;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var icon = AppIconFactory.CreateIcon();
        if (icon is null)
        {
            MessageBox.Show(
                "The application icon asset could not be found or loaded. Keyboard Ambient Light will now exit.",
                "Keyboard Ambient Light",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        var lightSensor = new Ambient.LightSensorService();
        if (!lightSensor.IsAvailable)
        {
            MessageBox.Show(
                "No ambient light sensor was found. Keyboard Ambient Light will now exit.",
                "Keyboard Ambient Light",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            icon.Dispose();
            lightSensor.Dispose();
            return;
        }

        var keyboard = new Hardware.AsusStaticKeyboardLightController();
        if (!keyboard.CanSetBrightness)
        {
            MessageBox.Show(
                "No supported ASUS keyboard brightness controller was found. Keyboard Ambient Light will now exit.",
                "Keyboard Ambient Light",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            icon.Dispose();
            keyboard.Dispose();
            lightSensor.Dispose();
            return;
        }

        Application.Run(new Tray.KeyboardAmbientLightAppContext(lightSensor, keyboard, icon));
    }
}
