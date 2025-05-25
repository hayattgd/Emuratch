using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System;
using System.Drawing;
using Svg;
using System.IO;

namespace Emuratch.Core.Scratch;

public class Costume
{
	public string name = "";
	public int bitmapResolution = 1;
	public float rotationCenterX;
	public float rotationCenterY;

	public string assetId = "";
	public string dataFormat = "";

	public float Width = 0;
	public float Height = 0;
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
			string dataformat = item["dataFormat"]?.ToString() ?? "";
			string imagepath = $"{Path.GetDirectoryName(Project.loadingpath)}{Path.DirectorySeparatorChar}{item["assetId"]}.{dataformat}";
			float width = 0;
			float height = 0;
			if (dataformat == "png")
			{
				Image img = Image.FromFile(imagepath);
				width = img.Width;
				height = img.Height;
				img.Dispose();
			}
			else if (dataformat == "svg")
			{
				var svg = SvgDocument.Open(imagepath);
				width = svg.Width.Value;
				height = svg.Height.Value;
			}

			Costume costume = new()
			{
				name = item["name"]?.ToString() ?? "",
				bitmapResolution = (int)(item["bitmapResolution"] ?? 0),
				assetId = item["assetId"]?.ToString() ?? "",
				dataFormat = dataformat,
				rotationCenterX = (int)(item["rotationCenterX"] ?? 0),
				rotationCenterY = (int)(item["rotationCenterY"] ?? 0),
				Width = width,
				Height = height
			};

			costumes.Add(costume);
		}

		return costumes.ToArray();
	}

	public override bool CanWrite => false;
}
