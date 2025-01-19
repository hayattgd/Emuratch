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

		if (block == null && returnto[^1] != null) block = returnto[^1];
		if (block == null) return;

		var self = this;
		runner.Execute(sprite, block, ref self);
		block = self.block;

		if (block.nextId == string.Empty)
		{
			if (forever || repeats > 0)
			{
				block = returnto[^1];
			}
			else if(returnto.Count <= 0)
			{
				return;
			}
			else
			{
				block = returnto[^1];
				returnto.RemoveAt(returnto.Count - 1);
			}
		}
		else
		{
			block = block.Next(sprite);
		}
	}

	public Block? condition;

	public float delay;
	public int repeats = 0;
	public bool forever = false;
	public List<Block> returnto = new();
	public bool nextframe = true;

	public readonly Sprite sprite;
	public Block block;

	Runner runner { get => Application.runner; }
}
