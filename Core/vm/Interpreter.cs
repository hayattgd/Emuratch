using Emuratch.Core.Project;
using System.Collections.Generic;
using System;

namespace Emuratch.Core.vm;

public class Interpreter : Executer
{
    public readonly static List<Action<Sprite, List<object>>> Operations = new()
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
        },
    };

    public Sprite Sprite {  get; set; }

    public Interpreter(Sprite spr)
    {
        Sprite = spr;
    }

    public void Execute(Block block)
    {
        int operationIndex = Block.opcodes.IndexOf(block.opcode);
        if (operationIndex == -1) return;

        Operations[operationIndex](Sprite, block.fields);
        if (block.next != null)
        {
            Execute(block.next);
        }
    }
}
