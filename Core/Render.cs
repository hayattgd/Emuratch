using Emuratch.Core.Scratch;
using Raylib_cs;
using System.Numerics;

namespace Emuratch.Core;

public class Render
{
	public Render(Project project)
	{
		this.project = project;
	}

	public Project project;

	public void RenderAll()
	{
		RenderSprite(project.background);
		foreach (var sprite in project.sprites)
		{
			RenderSprite(sprite);
		}
	}

	public void RenderSprite(Sprite spr)
	{
		Vector2 pos = ScratchToRaylib(spr.x, spr.y);
		Raylib.DrawTexture(spr.costumes[spr.currentCostume].texture, (int)pos.X, (int)pos.Y, Color.White);
	}

	public Vector2 ScratchToRaylib(float x, float y)
	{
		return new(project.width / 2 + x, project.height / 2 + y);
		//return new((int)x, (int)y);
	}
}
