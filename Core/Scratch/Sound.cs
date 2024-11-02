using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Raylib_cs;
using System.Collections.Generic;
using System;
using System.IO;

namespace Emuratch.Core.Scratch;

public class Sound
{
    public string name = "";
    public Raylib_cs.Sound sound;
}

public class SoundConverter : JsonConverter<Sound[]>
{
	public override void WriteJson(JsonWriter writer, Sound[]? value, JsonSerializer serializer)
	{
		throw new NotImplementedException();
	}

	public override Sound[]? ReadJson(JsonReader reader, Type objectType, Sound[]? existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		var obj = JToken.Load(reader);

		List<Sound> sounds = new() { };

		foreach (var item in obj)
		{
			sounds.Add(new()
			{
				name = item["name"]?.ToString() ?? "",
				sound = Raylib.LoadSound(item["md5ext"]?.ToString() ?? "")
			});
		}

		return sounds.ToArray();
	}

	public override bool CanWrite => false;
}
