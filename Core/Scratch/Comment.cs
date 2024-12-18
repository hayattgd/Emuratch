using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Emuratch.Core.Turbowarp;

namespace Emuratch.Core.Scratch;

public class Comment
{
	public string text = "";
}

public class CommentConverter : JsonConverter<Comment[]>
{
	public override void WriteJson(JsonWriter writer, Comment[]? value, JsonSerializer serializer)
	{
		throw new NotImplementedException();
	}

	public override Comment[] ReadJson(JsonReader reader, Type objectType, Comment[]? existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		var obj = JToken.Load(reader);

		List<Comment> comments = new();

		foreach (var item in obj.Values())
		{
			string text = item["text"]?.ToString() ?? "";

			if (text.Contains("// _twconfig_"))
			{
				Configuration.TryParse(text, out var config);
				Configuration.Config = config ?? new();
			}

			comments.Add(new()
			{
				text = text
			});
		}

		return comments.ToArray();
	}

	public override bool CanWrite => false;
}