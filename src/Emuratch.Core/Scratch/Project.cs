using Emuratch.Core.Render;
using Emuratch.Core.Turbowarp;
using Emuratch.Core.vm;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Emuratch.Core.Scratch;

public class Project
{
	private Project()
	{
		stage = new(this);
	}

	public bool debug;

	public const uint defaultWidth = 480;
	public const uint defaultHeight = 360;

	public IRunner? runner;

	public Sprite[] sprites = Array.Empty<Sprite>();
	public List<Sprite> clones = [];

	public Comment[] comments = Array.Empty<Comment>();
	public Meta meta = new();
	
	public Sprite stage;

	public uint width = defaultWidth;
	public uint height = defaultHeight;

	public bool isTurbowarp = false;
	public Monitor[] monitors = [];

	public Configuration config = new();

	public string GetAbsolutePath(string relative) => $"{projectpath}{Path.DirectorySeparatorChar}{relative}";
	string projectpath = "";

	internal static string loadingpath = "";
	public static Project? LoadProject(string jsonpath, Type runner, Type render)
	{
		string json = File.ReadAllText(jsonpath);
		loadingpath = jsonpath;
		Project project = new();

		JObject parsed = JObject.Parse(json);

		project.projectpath = Path.GetDirectoryName(jsonpath) ?? "";
		if (string.IsNullOrEmpty(project.projectpath)) return null;

		//Import Monitor
		Monitor[]? monitorArray = parsed["monitors"]?.ToObject<Monitor[]>();
		if (monitorArray == null) return null;
		project.monitors = monitorArray;

		//Import sprites
		Sprite[]? spritesArray = parsed["targets"]?.ToObject<Sprite[]>();
		if (spritesArray == null) return null;

		List<Sprite> spritesList = spritesArray.ToList();
		project.stage = spritesList[0];
		spritesList.RemoveAt(0);
		spritesList.ForEach(x => x.project = project);
		spritesArray = spritesList.ToArray();

		project.sprites = spritesArray;

		//Import meta
		Meta? meta = parsed["meta"]?.ToObject<Meta>();
		if (meta == null) return null;

		project.meta = meta;

		foreach (var comment in project.comments)
		{
			if (comment.text.Contains("// _twconfig_"))
			{
				if (Configuration.TryParse(comment.text, out var config))
				{
					project.config = config ?? new();
				}
			}
		}

		project.runner = (IRunner?)Activator.CreateInstance(runner, project, (IRender?)Activator.CreateInstance(render, project));

		if (project.runner == null) throw new NullReferenceException();
		if (project.runner.render == null) throw new NullReferenceException();

		project.runner.fps = project.config.framerate;

		return project;
	}
}
