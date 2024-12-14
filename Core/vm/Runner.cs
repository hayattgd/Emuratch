using Emuratch.Core.Scratch;
using Raylib_cs;
using System.Collections.Generic;
using System;
using System.Numerics;

namespace Emuratch.Core.vm;

public interface Runner
{
	public static int[] FPS
	{
		get => new int[]
		{
			2,
			5,
			15,
			30,
			45,
			60,
			90,
			120,
			240,
			480,
			960
		};
	}

	public Project project { get; protected internal set; }

	public bool TAS { get; set; }
	public bool paused { get; set; }

	public int fpsIdx { get; set; }
	public int fps { get => FPS[fpsIdx]; }
	public double deltatime { get => 1d / fps; }
	public Random rng { get; set; }

	public float timer { protected internal set; get; }

	public Vector2 mouse { get => Raylib.GetMousePosition() - new Vector2(project.width * 0.5f, project.height * 0.5f); }
	public Vector2 tasmouse { get; set; }
	public Vector2 mousepos { get => TAS ? tasmouse : mouse; }

	public List<Thread> PressFlag();
	public string Execute(Sprite spr, Block block, Thread thread);
	public string Execute(Sprite spr, Block block);
	public string Execute(Thread thread);
}
