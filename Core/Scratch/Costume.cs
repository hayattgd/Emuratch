using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Raylib_cs;
using System.Collections.Generic;
using System;
using Svg;
using System.IO;

namespace Emuratch.Core.Scratch;

public class Costume : Unloadable
{
    public string name = "";
	public int bitmapResolution = 1;
    public string dataFormat = "";
    public float rotationCenterX = 240;
    public float rotationCenterY = 180;
    public Image image = new();
	public Texture2D texture = new();

	public void Unload()
	{
		Raylib.UnloadImage(image);
		Raylib.UnloadTexture(texture);
	}
}

public class CostumeConverter : JsonConverter<Costume[]>
{
	public override void WriteJson(JsonWriter writer, Costume[]? value, JsonSerializer serializer)
	{
		throw new NotImplementedException();
	}

	public override Costume[]? ReadJson(JsonReader reader, Type objectType, Costume[]? existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		var obj = JToken.Load(reader);

		List<Costume> costumes = new() { };

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

			string imagepath = $"{item["assetId"]?.ToString()}.{costume.dataFormat}" ?? "";

			if (costume.dataFormat != "svg")
			{
				costume.image = Raylib.LoadImage(imagepath);
			}
			else
			{
				//Convert .svg to .png
				string pngpath = Path.ChangeExtension(imagepath, ".png");

				if (!File.Exists(pngpath))
				{
					var svg = SvgDocument.Open(imagepath);
					using var bitmap = svg.Draw();
					bitmap?.Save(pngpath);
				}

				costume.image = Raylib.LoadImage(pngpath);
			}

			costume.texture = Raylib.LoadTextureFromImage(costume.image);

			costumes.Add(costume);
		}

		return costumes.ToArray();
	}

	public override bool CanWrite => false;
}
