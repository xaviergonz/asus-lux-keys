using System.Drawing;

namespace KeyboardAmbientLight.Hardware;

public readonly record struct KeyboardLightState(Color? Color, KeyboardBrightness Brightness);
