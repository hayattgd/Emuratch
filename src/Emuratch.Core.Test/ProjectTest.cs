﻿using Emuratch.Core.Render;
using Emuratch.Core.Scratch;
using Emuratch.Core.vm;

namespace Emuratch.Core.Test;

public class ProjectTest
{
	[Fact]
	public void Load()
	{
		Project? project = Project.LoadProject($"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}Projects{Path.DirectorySeparatorChar}LoadTest{Path.DirectorySeparatorChar}project.json", typeof(Interpreter), typeof(NullRender));
		if (project == null) Assert.Fail("Project didn't load");

		Assert.Equal("Here's sprite name", project.sprites[0].name);
		Assert.Equal("d1163a80056fa6ba2893e707e61ddc7e", project.sprites[0].costumes[0].assetId);
		Assert.Equal(174, (int)project.sprites[0].costumes[0].Width);
		Assert.Equal(74, (int)project.sprites[0].costumes[0].Height);
		Assert.Equal(0, (int)project.sprites[0].x);
		Assert.Equal(0, (int)project.sprites[0].y);
		Assert.Equal(90, (int)project.sprites[0].direction);
		Assert.Equal(3, project.sprites[0].blocks.Count);
		Assert.False(project.sprites[0].isClone);
		Assert.True(project.stage.isStage);
		Assert.False(project.stage.isClone);
	}
}
