using System;
using Raylib_cs;
using Emuratch.Core;

namespace Emuratch;

public static class Program
{
	public static Application app = new();

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