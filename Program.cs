using Raylib_cs;

namespace Emurach;

public class Program
{
    public static int Main(string[] args)
    {
        Raylib.InitWindow(800, 600, "Emuratch");

        while (!Raylib.WindowShouldClose())
        {
            Raylib.BeginDrawing();

            Raylib.ClearBackground(Color.White);

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
        return 0;
    }
}