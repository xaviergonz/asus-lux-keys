namespace KeyboardAmbientLight;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var lightSensor = new Ambient.LightSensorService();
        if (!lightSensor.IsAvailable)
        {
            MessageBox.Show(
                "No ambient light sensor was found. Keyboard Ambient Light will now exit.",
                "Keyboard Ambient Light",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
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
            keyboard.Dispose();
            lightSensor.Dispose();
            return;
        }

        Application.Run(new Tray.KeyboardAmbientLightAppContext(lightSensor, keyboard));
    }
}
