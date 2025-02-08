using Emuratch.Core.Scratch;
using GLib;
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

		if (Raylib.IsKeyDown(KeyboardKey.F3))
		{
			Raylib.DrawLine(0, (int)project.height / 2, (int)project.width, (int)project.height / 2, Color.Red);
			Raylib.DrawLine((int)project.width / 2, 0, (int)project.width / 2, (int)project.height, Color.Red);
		}

		var list = project.sprites.ToList();
		list.Sort((a, b) => a.layoutOrder.CompareTo(b.layoutOrder));
		list.Reverse();

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

		offset = new((costume.rotationCenterX - costume.image.Width / 4) / costume.bitmapResolution, (costume.rotationCenterY - costume.image.Height / 4) / costume.bitmapResolution);

		Vector2 pos = ScratchToRaylib(
			spr.x,
			-spr.y
		);

		if (spr.isStage)
		{
			pos = new(0, 0);
			offset = new(0, 0);
		}

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

		// if (!spr.isStage)
		// {
		// 	Vector2 pivot = pos + offset * 2;
		// 	Raylib.DrawCircle((int)pivot.X, (int)pivot.Y, 5, Color.Blue);
		// 	Raylib.DrawCircle((int)pos.X, (int)pos.Y, 5, Color.Yellow);
		// }
	}

	public Vector2 ScratchToRaylib(float x, float y)
	{
		return new(project.width / 2f + x, project.height / 2f + y);
		//return new((int)x, (int)y);
	}
}
