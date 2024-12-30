using Emuratch.Core.Scratch;
using Raylib_cs;
using System.Linq;
using System.Numerics;

namespace Emuratch.Core.Render;

public class Emurender : RenderType
{
	public Emurender(Project project)
	{
		this.project = project;
	}

	public readonly Project project;

	public void RenderAll()
	{
		Raylib.ClearBackground(Color.White);

		RenderSprite(project.stage);
		var list = project.sprites.ToList();
		list.Sort((a, b) => a.layoutOrder.CompareTo(b.layoutOrder));

		foreach (var sprite in list)
		{
			RenderSprite(sprite);
		}

//#if DEBUG
//		Raylib.DrawText(Raylib.GetFPS().ToString(), 2, 2, 20, Color.Lime);
//#endif
	}

	public void RenderSprite(Sprite spr)
	{
		if (spr.costumes.Length == 0) return;

		Costume costume = spr.costumes[spr.currentCostume];
		Vector2 offset = Vector2.Zero;

		if (costume.dataFormat == "png")
		{
			offset = new(-costume.rotationCenterX + costume.image.Width, costume.rotationCenterY - costume.image.Height);
			offset /= costume.bitmapResolution;
		}
		else if (costume.dataFormat == "svg")
		{
			offset = new(-costume.rotationCenterX + costume.image.Width / 2f, costume.rotationCenterY - costume.image.Height / 2f);
			offset += new Vector2(costume.image.Width / 4f, costume.image.Height / 4f);
		}

			Vector2 pos = ScratchToRaylib(
			spr.x - offset.X,
			(spr.y - offset.Y) * -1f
		);
		if (spr.isStage) pos = ScratchToRaylib(-costume.rotationCenterX / 2, -costume.rotationCenterY / 2);

		float size = spr.size / 100 / costume.bitmapResolution;

		Raylib.DrawTexturePro(
			costume.texture,
			new(
				0, 0,
				costume.image.Width,
				costume.image.Height
			),
			new(
				(int)pos.X, (int)pos.Y,
				costume.image.Width * size,
				costume.image.Height * size
			),
			offset * 2,
			spr.direction - 90,
			Color.White
		);

		if (!spr.isStage)
		{
			Vector2 pivot = pos + offset * 2;
			Raylib.DrawCircle((int)pivot.X, (int)pivot.Y, 5, Color.Blue);
			Raylib.DrawCircle((int)pos.X, (int)pos.Y, 5, Color.Yellow);
		}
	}

	public Vector2 ScratchToRaylib(float x, float y)
	{
		return new(project.width / 2f + x, project.height / 2f + y);
		//return new((int)x, (int)y);
	}
}
