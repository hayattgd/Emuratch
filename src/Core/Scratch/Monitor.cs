using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Emuratch.Core.Scratch;

[JsonConverter(typeof(MonitorConverter))]
public class Monitor
{
	public enum Mode
	{
		normal, //default is a keyword sadly
		large,
		slider,
		list
	}

	public static Mode ToMode(string str)
	{
		if (str == "default") return Mode.normal;
		return Enum.Parse<Mode>(str);
	}

	public Mode mode = Mode.normal;
	public string id = string.Empty;
	public string name = string.Empty;
	public string sprname = string.Empty;
	public Vector2 pos = new(5, 5);
	public Vector2 size = new(100, 200);
	public Vector2 sliderrange = new(0, 100);
	public bool visible = true;

	public string DisplayName => $"{(sprname == "" ? "" : sprname + " : ")}{name}";
}

public class MonitorConverter : JsonConverter<List<Monitor>>
{
	public override void WriteJson(JsonWriter writer, List<Monitor>? value, JsonSerializer serializer)
	{
		throw new NotImplementedException();
	}

	public override List<Monitor> ReadJson(JsonReader reader, Type objectType, List<Monitor>? existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		var obj = JObject.Load(reader);

		List<Monitor> list = new();

		foreach (var item in obj.Properties())
		{
			list.Add(new()
			{
				id = item.Value["id"]?.ToString() ?? "",
				name = item.Value["params"]?[0]?.ToString() ?? "my variable",
				sprname = item.Value["spriteName"]?.ToString() ?? string.Empty,
				mode = Monitor.ToMode(item.Value["mode"]?.ToString() ?? "default"),
				pos = new(item.Value["x"]?.ToObject<int>() ?? 2, item.Value["y"]?.ToObject<int>() ?? 2),
				size = new(item.Value["width"]?.ToObject<int>() ?? 100, item.Value["height"]?.ToObject<int>() ?? 200),
				sliderrange = new(item.Value["sliderMin"]?.ToObject<float>() ?? 0f, item.Value["sliderMax"]?.ToObject<float>() ?? 0f),
				visible = item.Value["visible"]?.ToObject<bool>() ?? true
			});
		}

		return list;
	}
}