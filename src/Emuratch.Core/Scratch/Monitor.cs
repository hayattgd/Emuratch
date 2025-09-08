using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
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

public class MonitorConverter : JsonConverter<Monitor>
{
	public override void WriteJson(JsonWriter writer, Monitor? value, JsonSerializer serializer)
	{
		throw new NotImplementedException();
	}

	public override Monitor ReadJson(JsonReader reader, Type objectType, Monitor? existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		var obj = JToken.Load(reader);

		return new()
		{
			id = obj["id"]?.ToString() ?? "",
			name = obj["params"]?["VARIABLE"]?.ToString() ?? "my variable",
			sprname = obj["spriteName"]?.ToString() ?? string.Empty,
			mode = Monitor.ToMode(obj["mode"]?.ToString() ?? "default"),
			pos = new(obj["x"]?.ToObject<int>() ?? 2, obj["y"]?.ToObject<int>() ?? 2),
			size = new(obj["width"]?.ToObject<int>() ?? 100, obj["height"]?.ToObject<int>() ?? 200),
			sliderrange = new(obj["sliderMin"]?.ToObject<float>() ?? 0f, obj["sliderMax"]?.ToObject<float>() ?? 0f),
			visible = obj["visible"]?.ToObject<bool>() ?? true
		};
	}
}