using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
			comments.Add(new()
			{
				text = item["text"]?.ToString() ?? ""
			});
		}

		return comments.ToArray();
	}

	public override bool CanWrite => false;
}