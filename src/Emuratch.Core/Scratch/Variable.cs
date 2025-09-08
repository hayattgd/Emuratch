using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System;

namespace Emuratch.Core.Scratch;

public class Variable
{
	public string name = "my variable";
	public object value = 0;
}

public class VariableConverter : JsonConverter<Dictionary<string, Variable>>
{
	public override void WriteJson(JsonWriter writer, Dictionary<string, Variable>? value, JsonSerializer serializer)
	{
		throw new NotImplementedException();
	}

	public override Dictionary<string, Variable> ReadJson(JsonReader reader, Type objectType, Dictionary<string, Variable>? existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		var obj = JObject.Load(reader);

		Dictionary<string, Variable> variables = new();

		foreach (var item in obj)
		{
			if (item.Value == null) continue;
			variables.Add(item.Key, new()
			{
				name = item.Value[0]?.ToString() ?? "",
				value = item.Value[1] ?? ""
			});
		}

		return variables;
	}

	public override bool CanWrite => false;
}