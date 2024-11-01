using Newtonsoft.Json;

namespace Emuratch.Core.Turbowarp;

public class Configuration
{
    public struct RuntimeOptions
    {
        public RuntimeOptions() { }

        public int maxClones = 300;
        public bool miscLimits = true;
        public bool fencing = true;
    }

    public int framerate = 30;
    public RuntimeOptions runtimeOptions;
    public bool interpolation = false;
    public bool hq = false;
    public int width = 480;
    public int height = 360;

    /// <summary>
    /// Tries to parse Configuration instance from Turbowarp's comment
    /// </summary>
    /// <returns>return process is successfully done or not</returns>
    public static bool TryParse(string text, out Configuration? config)
    {
        string json = text.Split('\n')[2].Replace(" // _twconfig_", "");
        config = JsonConvert.DeserializeObject<Configuration>(json);

        return config != null;
    }
}
