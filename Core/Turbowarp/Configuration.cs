﻿using Emuratch.Core.Scratch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Emuratch.Core.Turbowarp;

public class Configuration
{
	private static Configuration? config;

	public struct RuntimeOptions
	{
		public RuntimeOptions() { }

		public string maxClones = "";
		public bool miscLimits = true;
		public bool fencing = true;
	}

	public int framerate = 30;
	public RuntimeOptions runtimeOptions;
	public bool interpolation = false;
	public bool hq = false;
	public uint width = 480;
	public uint height = 360;

	public static Configuration? Config { get => config; set => config = value; }

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

	public static void ApplyConfig(ref Project project)
	{
		project.width = Config?.width ?? Project.defaultWidth;
		project.height = Config?.height ?? Project.defaultHeight;
	}
}
