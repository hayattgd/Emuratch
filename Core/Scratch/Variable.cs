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

public class VariableConverter : JsonConverter<Variable[]>
{
	public override void WriteJson(JsonWriter writer, Variable[]? value, JsonSerializer serializer)
	{
		throw new NotImplementedException();
	}

	public override Variable[] ReadJson(JsonReader reader, Type objectType, Variable[]? existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		var obj = JObject.Load(reader).Values();

		List<Variable> variables = new();

		foreach (var item in obj)
		{
			Console.WriteLine(item.Path);
			variables.Add(new()
			{
				name = item[0]?.ToString() ?? "",
				value = item[1] ?? ""
			});
		}

		return variables.ToArray();
	}

	public override bool CanWrite => false;
}
