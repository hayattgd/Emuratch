using Emuratch.Core.Turbowarp;
using Emuratch.Core.Crossplatform;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Emuratch.Core.Scratch;

public class Project : Unloadable
{
	public const uint defaultWidth = 480;
	public const uint defaultHeight = 360;

	public Sprite[] sprites = Array.Empty<Sprite>();
	public Comment[] comments = Array.Empty<Comment>();
	public Meta meta = new();
	
	public Sprite stage { get => sprites[0]; }

	public uint width = defaultWidth;
	public uint height = defaultHeight;

	public bool isTurbowarp = false;

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

			//Import sprites
			Sprite[]? spritesArray = parsed["targets"]?.ToObject<Sprite[]>();
			if (spritesArray == null) return false;

			List<Sprite> spritesList = spritesArray.ToList();
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
			DialogServiceFactory.CreateDialogService().ShowMessageDialog("Error while loading project : " + ex);
			return false;
		}
	}
}
