using Emuratch.Core.Render;
using Emuratch.Core.Scratch;
using Emuratch.Core.vm;

namespace Emuratch.Core.Test;

public class InterpreterTest
{
	[Fact]
	public void Blocks()
	{
		IRunner runner;

		Project? project = Project.LoadProject($"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}Projects{Path.DirectorySeparatorChar}LoadTest{Path.DirectorySeparatorChar}project.json");
		if (project == null) Assert.Fail("Project didn't load");

		runner = new Interpreter(project, new NullRender());
		project.runner = runner;

		Assert.Equal(10, Interpreter.StrNumber(runner.Execute(project.sprites[0], new()
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

		Assert.Equal(10, Interpreter.StrNumber(runner.Execute(project.sprites[0], new()
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

		Assert.Equal(13.8f, project.sprites[0].x);
	}
}
