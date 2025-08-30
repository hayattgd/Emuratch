using Emuratch.Core.Scratch;
using Emuratch.Core.Utils;
using System.Collections.Generic;

namespace Emuratch.Core.vm;

public class Thread
{
	public Thread(Block block, IRunner runner)
	{
		sprite = block.sprite;
		this.block = block;
		this.runner = runner;
	}

	public Thread(Sprite sprite, Block block, IRunner runner)
	{
		this.sprite = sprite;
		this.block = block;
		this.runner = runner;
	}

	public Thread(Sprite sprite, Block block, IRunner runner, bool instExec)
	{
		this.sprite = sprite;
		this.block = block;
		this.runner = runner;
		this.instExec = instExec;
	}

	private void Tick()
	{
		if (!nextframe)
		{
			nextframe = true;
			return;
		}

		if (delay > 0)
		{
			delay -= runner.deltatime;

			//clamp
			if (delay < 0)
			{
				delay = 0;
			}

			return;
		}

		if (block == null && returnto.Count == 0) return;
		if (block == null && returnto[^1].block != null) block = returnto[^1].block;
		if (block == null) return;

		var self = this;
		runner.Execute(ref self);
		block = self.block;

		if (block == null || block.nextId == string.Empty)
		{
			if (returnto.Count <= 0)
			{
				runner.threads.Remove(this);
				return;
			}
			else if (returnto[^1].condition != null)
			{
				if (Interpreter.Strbool(runner.Execute(sprite, returnto[^1].condition ?? new())))
				{
					block = returnto[^1].block.Parent(sprite).Next(sprite);
				}
				else
				{
					block = returnto[^1].block;
				}
			}
			else if (returnto[^1].forever || returnto[^1].repeats > 0)
			{
				block = returnto[^1].block;
				if (returnto[^1].repeats > 0) returnto[^1] = new(returnto[^1].block, returnto[^1].repeats - 1);
			}
			else
			{
				block = returnto[^1].block.Parent(sprite).Next(sprite);
				returnto.RemoveAt(returnto.Count - 1);
			}
		}
		else
		{
			block = block.Next(sprite);
		}
	}

	public void Step()
	{
		if (instExec)
		{
			int forceUpdateTicks = 50;
			while (runner.threads.Contains(this))
			{
				Tick();
				if (block == null)
				{
					runner.threads.Remove(this);
				}
				if (--forceUpdateTicks < 1) { return; }
			}
		}
		else
		{
			Tick();
		}
		
	}
	public Number delay;
	public List<Loop> returnto = [];
	public bool nextframe = true;
	public bool instExec = false;

	public readonly Sprite sprite;
	public Block block;

	internal readonly IRunner runner;
}

public struct Loop
{
	public Loop(Block block, int repeats)
	{
		this.block = block;
		this.repeats = repeats;
		forever = false;
	}
	public Loop(Block block, Block condition)
	{
		this.block = block;
		this.condition = condition;
		forever = false;
	}

	public Loop(Block block, bool forever)
	{
		this.block = block;
		repeats = 0;
		this.forever = forever;
	}

	public Loop(Block block)
	{
		this.block = block;
		repeats = 0;
		forever = false;
	}

	public Block block;
	public int repeats;
	public bool forever;
	public Block? condition;
}