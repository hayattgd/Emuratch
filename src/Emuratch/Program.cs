using System;
using System.IO;
using System.Linq;
using Raylib_cs;

namespace Emuratch;

public static class Program
{
	public static readonly Application app = new();

	[STAThread]
	public static int Main(string[] args)
	{
		app.Initialize();

		if (args.Length > 0)
		{
			var path = args[0];
			if (args[0][0] == '.')
			{
				path = Path.Combine(Directory.GetCurrentDirectory(), path.Substring(2));
			}
			Console.WriteLine($"Loading {path}...");
			app.LoadProject(path);
		}
		
		while (!Raylib.WindowShouldClose())
		{
			app.OnUpdate();
		}

		return app.Unload();
	}
}