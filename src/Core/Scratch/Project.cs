using Emuratch.Core.Turbowarp;
using Emuratch.Core.Crossplatform;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Emuratch.Core.Scratch;

public class Project : Unloadable
{
	private Project()
	{
		stage = new();
	}

	public const uint defaultWidth = 480;
	public const uint defaultHeight = 360;

	public Sprite[] sprites = Array.Empty<Sprite>();
	public List<Sprite> clones = [];

	public Comment[] comments = Array.Empty<Comment>();
	public Meta meta = new();
	
	public Sprite stage;

	public uint width = defaultWidth;
	public uint height = defaultHeight;

	public bool isTurbowarp = false;
	public Monitor[] monitors = [];

	public void Unload()
	{
		foreach (var sprite in sprites)
		{
			foreach (var item in sprite)
			{
				item.Unload();
			}
		}
	}

	public static bool LoadProject(string json, out Project project)
	{
		project = new();
		Configuration.Config = new();

		try
		{
			JObject parsed = JObject.Parse(json);

			//Import Monitor
			Monitor[]? monitorArray = parsed["monitors"]?.ToObject<Monitor[]>();
			if (monitorArray == null) return false;
			project.monitors = monitorArray;

			//Import sprites
			Sprite[]? spritesArray = parsed["targets"]?.ToObject<Sprite[]>();
			if (spritesArray == null) return false;

			List<Sprite> spritesList = spritesArray.ToList();
			project.stage = spritesList[0];
			spritesList.RemoveAt(0);
			spritesArray = spritesList.ToArray();

			project.sprites = spritesArray;

			//Import meta
			Meta? meta = parsed["meta"]?.ToObject<Meta>();
			if (meta == null) return false;

			project.meta = meta;

			return true;
		}
		catch (Exception ex)
		{
			DialogServiceFactory.CreateDialogService().ShowMessageDialog("Error while loading project : " + ex.Message);
			return false;
		}
	}
}
