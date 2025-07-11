﻿using Emuratch.Core.Render;
using Emuratch.Core.Scratch;
using Emuratch.Core.Utils;
using Emuratch.Core.vm;

namespace Emuratch.Core.Test;

public class InterpreterTest
{

	[
		Theory,
		InlineData(Number.PrecisionMode.Float),
		InlineData(Number.PrecisionMode.Double)
	]
	public void Blocks(Number.PrecisionMode precision)
	{
		Number.SetDefaultPrecision(precision);
		IRunner runner;

		Project? project = Project.LoadProject($"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}Projects{Path.DirectorySeparatorChar}LoadTest{Path.DirectorySeparatorChar}project.json", typeof(Interpreter), typeof(NullRender));
		if (project == null) Assert.Fail("Project didn't load");

		runner = project.runner ?? throw new NullReferenceException();

		Assert.Equal(10, (double)Interpreter.StrNumber(runner.Execute(project.sprites[0], new()
		{
			opcode = Block.Opcodes.operator_add,
			inputs = {
				new()
				{
					value = "3.8"
				},
				new()
				{
					value = "6.2"
				}
			}
		})));

		Assert.Equal(10, (int)Interpreter.StrNumber(runner.Execute(project.sprites[0], new()
		{
			opcode = Block.Opcodes.operator_subtract,
			inputs = {
				new()
				{
					value = "13.8"
				},
				new()
				{
					value = "3.8"
				}
			}
		})));

		runner.Execute(project.sprites[0], new()
		{
			opcode = Block.Opcodes.motion_movesteps,
			inputs = {
				new()
				{
					value = "13.8"
				}
			}
		});

		Assert.Equal(13.8f, (float)project.sprites[0].x);
	}
}
