using Emuratch.Core.Project;
using System.IO;
using Raylib_cs;

namespace Emuratch;

public class Program
{
    public static int Main()
    {
		bool success = Project.LoadProject(File.ReadAllText(@"S:\Workspace\Csharp\raylib\Emuratch\Project\project.json"), out Project _);

		Raylib.InitWindow(800, 600, "Emuratch");

		while (!Raylib.WindowShouldClose())
		{
			Raylib.BeginDrawing();

			Raylib.ClearBackground(Color.White);

			Raylib.DrawText(success.ToString(), 16, 16, 8, Color.Black);

			Raylib.EndDrawing();
		}

		Raylib.CloseWindow();
		return 0;
    }
}