using System;
using Raylib_cs;

namespace Emuratch.UI;

public static class Program
{
	public static readonly Application app = new();

	[STAThread]
	public static int Main()
	{
		app.Initialize();

		while (!Raylib.WindowShouldClose())
		{
			app.OnUpdate();
		}

		return app.Unload();
	}
}