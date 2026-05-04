# AsusLuxKeys

AsusLuxKeys is a small Windows tray utility that adjusts ASUS keyboard backlight brightness from the ambient light sensor.

It is intended for ASUS laptops where you want the keyboard to dim or brighten automatically as room lighting changes. The app uses static keyboard lighting only, with optional static color control on devices that support it.

## Features

- Tray app with an Options window and Quit command.
- Opens Options on left click and the tray menu on right click.
- Automatically maps ambient light readings to keyboard brightness.
- Supports ASUS keyboard brightness levels: `0%`, `33%`, `66%`, and `100%`.
- Optional static keyboard color when supported by the device.
- Shows the current ambient light reading while editing rules.
- Optional Run on startup setting.
- Saves settings between runs.

## Requirements

- Windows 10/11.
- .NET 10 SDK if building from source.
- Compatible ASUS laptop keyboard lighting hardware.
- A Windows ambient light sensor.

If required hardware support is missing, the app exits with an error instead of running silently in the tray.

## Usage

Run the app, then open `Options` from the tray icon.

The rule table maps lux thresholds to keyboard brightness. Rules are evaluated from lowest lumens upward: the first threshold greater than the current light reading wins.

Example:

| Lumens   | Keyboard brightness |
| -------- | ------------------- |
| 5        | 66%                 |
| 10       | 33%                 |
| Infinity | 0%                  |

Means:

- `0 <= lux < 5`: `66%`
- `5 <= lux < 10`: `33%`
- `10 <= lux`: `0%`

If there are no rules, or no rule matches, brightness is `0%`.

Options auto-save after valid changes. If a lumens value is incomplete or invalid while editing, that change is not saved until it becomes valid.

## Development Commands

Restore dependencies:

```powershell
dotnet restore AsusLuxKeys.slnx
```

Build:

```powershell
dotnet build AsusLuxKeys.slnx
```

Run the tray app:

```powershell
dotnet run --project src\AsusLuxKeys\AsusLuxKeys.csproj
```

Run unit tests:

```powershell
dotnet test AsusLuxKeys.slnx
```

Run unit tests without rebuilding:

```powershell
dotnet test AsusLuxKeys.slnx --no-build
```

Check formatting:

```powershell
dotnet format AsusLuxKeys.slnx --verify-no-changes
```

## Publishing a Release

GitHub Releases are created automatically when a version tag is pushed.

Use a `vMAJOR.MINOR.PATCH` tag:

```powershell
git tag v1.0.0
git push origin v1.0.0
```

The release workflow builds the Release version, publishes the `win-x64` app, zips it as `AsusLuxKeys-win-x64.zip`, and attaches it to the GitHub Release.
