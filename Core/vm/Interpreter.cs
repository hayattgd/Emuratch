using Raylib_cs;

namespace Emuratch.Core.vm;

public class Interpreter
{
    public static List<Action<Sprite, List<object>>> Operations = new()
    {
        //motion_movesteps
        (spr, arg) => {
            spr.x += (float)arg[0] * MathF.Sin(spr.direction);
            spr.x += (float)arg[0] * MathF.Cos(spr.direction);
        },

        //motion_turnright
        (spr, arg) => {
            spr.direction += (float)arg[0];
        },

        //motion_turnleft
        (spr, arg) => {
            spr.direction -= (float)arg[0];
        },

        //motion_goto
        (spr, arg) => {
            spr.x = ((Sprite)arg[0]).x;
            spr.y = ((Sprite)arg[0]).y;
        },

        //motion_gotoxy
        (spr, arg) => {
            spr.x = (float)arg[0];
            spr.y = (float)arg[1];
        },

        //motion_glideto
        (spr, arg) => {
            spr.x = (float)arg[0];
            spr.y = (float)arg[1];
        }
    };

    public Sprite sprite;

    public Interpreter(Sprite spr)
    {
        sprite = spr;
    }
}
