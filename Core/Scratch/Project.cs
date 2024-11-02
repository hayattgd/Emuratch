using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Emuratch.Core.Scratch;

public class Project
{
    public Sprite background = new();
    public Sprite[] sprites = Array.Empty<Sprite>();
	public Comment[] comments = Array.Empty<Comment>();
    public Meta meta = new();

	public int width = 480;
	public int height = 360;

	public static bool LoadProject(string json, out Project project)
    {
        project = new();

        try
        {
            JObject parsed = JObject.Parse(json);

            //Import sprites
            Sprite[]? spritesArray = parsed["targets"]?.ToObject<Sprite[]>();
            if (spritesArray == null) return false;

            project.background = spritesArray[0];

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
			MessageBox.Show("Error while loading project : " + ex.ToString(), "Error");
			return false;
        }
    }
}
