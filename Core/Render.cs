using Emuratch.Core.Scratch;
using Raylib_cs;
using System.Linq;
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
		Raylib.ClearBackground(Color.White);

		RenderSprite(project.background);
		var list = project.sprites.ToList();
		list.Sort((a, b) => a.layoutOrder.CompareTo(b.layoutOrder));

		foreach (var sprite in list)
		{
			RenderSprite(sprite);
		}
	}

	public void RenderSprite(Sprite spr)
	{
		Costume costume = spr.costumes[spr.currentCostume];
		Vector2 offset = new(costume.rotationCenterX, costume.rotationCenterY / 2);
		offset /= costume.bitmapResolution;

		Vector2 pos = ScratchToRaylib(
			spr.x - offset.X,
			spr.y - offset.Y
		);
		if (spr.isStage) pos = ScratchToRaylib(-costume.rotationCenterX/2, -costume.rotationCenterY/2);

		Raylib.DrawTextureEx(
			costume.texture,
			new(
				(int)pos.X,
				(int)pos.Y
			),
			spr.direction - 90,
			spr.size / 100 / costume.bitmapResolution,
			Color.White
		);
	}

	public Vector2 ScratchToRaylib(float x, float y)
	{
		return new(project.width / 2 + x, project.height / 2 + y);
		//return new((int)x, (int)y);
	}
}
