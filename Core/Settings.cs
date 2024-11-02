using System.Collections.Generic;
using System.Linq;

namespace Emuratch.Core;

public class Settings
{
	public bool alwaysRefresh = true;
	public bool isTurbowarp = false;
	public bool isCompiled = false;

	public static Settings LoadSettings(string text)
	{
		string[] splits = text.Split("\n");
		List<string> splitlist = splits.ToList();

		foreach (var item in splitlist.Where((selected) => selected[0] == '#'))
		{
			splitlist.Remove(item);
		};

		return new()
		{
			alwaysRefresh = splits[0] == "true"
		};
	}

	public string SaveSettings()
	{
		return $"{alwaysRefresh}\n{isTurbowarp}\n{isCompiled}";
	}
}
