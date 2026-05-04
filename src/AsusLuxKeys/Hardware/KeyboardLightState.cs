using System.Drawing;

namespace AsusLuxKeys.Hardware;

public readonly record struct KeyboardLightState(Color? Color, KeyboardBrightness Brightness);
