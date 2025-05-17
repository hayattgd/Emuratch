using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Raylib_cs;
using System.Collections.Generic;
using System;
using Svg;
using System.IO;
using System.Runtime.InteropServices;

namespace Emuratch.Core.Scratch;

public class Costume : Unloadable
{
	public string name = "";
	public int bitmapResolution = 1;
	public string dataFormat = "";
	public float rotationCenterX;
	public float rotationCenterY;
	public Image image;
	public Texture2D texture;

	public IntPtr colors;

	public Color GetColor(int x, int y)
	{
		if (x > image.Width || x < 0) return Color.Blank;
		if (y > image.Height || y < 0) return Color.Blank;
		
		Color color;
		unsafe
		{
			color = ((Color*)colors)[y * image.Width + x];
		}
		return color;
	}

	public void Unload()
	{
		Raylib.UnloadImage(image);
		Raylib.UnloadTexture(texture);
		Marshal.FreeCoTaskMem(colors);
	}
}

public class CostumeConverter : JsonConverter<Costume[]>
{
	public override void WriteJson(JsonWriter writer, Costume[]? value, JsonSerializer serializer)
	{
		throw new NotImplementedException();
	}

	public override Costume[] ReadJson(JsonReader reader, Type objectType, Costume[]? existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		var obj = JToken.Load(reader);

		List<Costume> costumes = new();

		foreach (var item in obj)
		{
			Costume costume = new()
			{
				name = item["name"]?.ToString() ?? "",
				bitmapResolution = (int)(item["bitmapResolution"] ?? 0),
				dataFormat = item["dataFormat"]?.ToString() ?? "",
				rotationCenterX = (int)(item["rotationCenterX"] ?? 0),
				rotationCenterY = (int)(item["rotationCenterY"] ?? 0)
			};

			string imagepath = $"{item["assetId"]}.{costume.dataFormat}";

			if (costume.dataFormat != "svg")
			{
				costume.image = Raylib.LoadImage(imagepath);
			}
			else
			{
				if (File.Exists(Program.app.GetAbsolutePath(imagepath)))
				{
					//Convert .svg to .png
					string pngpath = Path.ChangeExtension(imagepath, ".png");

					if (!File.Exists(Program.app.GetAbsolutePath(pngpath)))
					{
						var svg = SvgDocument.Open(Program.app.GetAbsolutePath(imagepath));
						using var bitmap = svg.Draw();
						bitmap?.Save(Program.app.GetAbsolutePath(pngpath));
					}
					costume.image = Raylib.LoadImage(pngpath);
				}
				else
				{
					costume.image = Raylib.GenImageColor(32, 32, new Color(255, 0, 255, 255));
				}
			}

			costume.texture = Raylib.LoadTextureFromImage(costume.image);

			unsafe
			{
				costume.colors = (IntPtr)Raylib.LoadImageColors(costume.image);
			}

			costumes.Add(costume);
		}

		return costumes.ToArray();
	}

	public override bool CanWrite => false;
}
