using Emuratch.Core.vm;
using Raylib_cs;

namespace Emuratch.Core;

public class Sprite
{
    public bool isStage = false;

    public string name = "";
    public Variable[] variables = new Variable[] { };
    public Block[] blocks = new Block[] { };
    public int currentCostume = 0;
    public Costume[] costumes = new Costume[] { };
    public Sound[] sounds = new Sound[] { };
    public float volume = 100;
    public int layoutOrder = 1;
    public bool visible = true;
    public float x = 0;
    public float y = 0;
    public float size = 100;
    public float direction = 90;
    public bool draggable = false;
    public string rotationStyle = "all around";
}
