using Emuratch.Core.Scratch;
using Newtonsoft.Json;

namespace Emuratch.Core.Turbowarp;

public class Configuration
{

	public struct RuntimeOptions
	{
		public RuntimeOptions() { }

		public string maxClones = "";
		public bool miscLimits = true;
		public bool fencing = true;
	}

	public readonly int framerate = 30;
	public RuntimeOptions runtimeOptions;
	public bool interpolation = false;
	public bool hq = false;
	public readonly uint width = 480;
	public readonly uint height = 360;

	/// <summary>
	/// Tries to parse Configuration instance from Turbowarp's comment
	/// </summary>
	/// <returns>return process is successfully done or not</returns>
	public static bool TryParse(string text, out Configuration? parsedconfig)
	{
		string json = text.Split('\n')[2].Replace(" // _twconfig_", "");
		parsedconfig = JsonConvert.DeserializeObject<Configuration>(json);

		return parsedconfig != null;
	}

	public void ApplyConfig(ref Project project)
	{
		project.width = width;
		project.height = height;
	}
}
