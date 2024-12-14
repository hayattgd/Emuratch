using Emuratch.Core.Scratch;
using System.Collections.Generic;

namespace Emuratch.Core.vm;

public class Thread
{
	public Thread(Sprite sprite, Block block)
	{
		this.sprite = sprite;
		this.block = block;
	}

	public void Step()
	{
		if (!nextframe)
		{
			nextframe = true;
			return;
		}

		if (delay > 0)
		{
			delay -= (float)runner.deltatime;

			//clamp
			if (delay < 0)
			{
				delay = 0;
			}

			return;
		}

		runner.Execute(sprite, block, this);

		if (block.nextId == string.Empty)
		{
			if (forever || repeats > 0)
			{
				block = returnto[returnto.Count - 1];
			}

			return;
		}

		block = block.Next(sprite);
	}

	public Block? condition;

	public float delay = 0;
	public int repeats = 0;
	public bool forever = false;
	public List<Block> returnto = new();
	public bool nextframe = true;

	public Sprite sprite;
	public Block block;

	Runner runner { get => Application.runner; }
}
