using Emuratch.Core.Scratch;
using System.Collections.Generic;
using System;
using System.Numerics;
using Emuratch.Core.Utils;
using Emuratch.Core.Render;

namespace Emuratch.Core.vm;

public interface IRunner
{
	public Project project { get; protected internal set; }
	public List<Thread> threads { get; set; }
	public IRender render { get; }

	public bool TAS { get; set; }
	public bool paused { get; set; }

	public int fps { get; set; }
	public double deltatime { get => 1d / fps; }
	public Random rng { get; set; }

	public Number timer { get; set; }

	public Vector2 mouse { get; }
	public Vector2 tasmouse { get; set; }
	public Vector2 mousepos { get => TAS ? tasmouse : mouse; }

	public List<Thread> InvokeEvent(Block.Opcodes opcodes);
	public string Execute(Sprite spr, Block block);
	public string Execute(ref Thread thread);
}