using Emuratch.Core.Scratch;
using Raylib_cs;
using System;
using System.Linq;
using System.Numerics;

namespace Emuratch.Core.Render;

public class Emurender : IRender
{
	public Emurender(Project project)
	{
		this.project = project;
	}

	public readonly Project project;

	public void RenderAll()
	{
		Raylib.ClearBackground(Color.White);
		var list = project.sprites.ToList();

		if (!Application.disablerender)
		{
			RenderSprite(project.stage);
			var all = list.Concat(project.clones);
			
			list = [..all];
			list.Sort((a, b) => a.layoutOrder.CompareTo(b.layoutOrder));
			list.Reverse();

			foreach (var sprite in list)
			{
				RenderSprite(sprite);
			}
		}

		if (!Application.debug) return;
		Raylib.DrawLine(0, Raylib.GetRenderHeight() / 2, Raylib.GetRenderWidth(), Raylib.GetRenderHeight() / 2, Color.Red);
		Raylib.DrawLine(Raylib.GetRenderWidth() / 2, 0, Raylib.GetRenderWidth() / 2, Raylib.GetRenderHeight(), Color.Red);

		foreach (var sprite in list)
		{
			DrawDebugInfo(sprite);
		}

#if DEBUG
		Raylib.DrawText(dbginfo, 2, 2, 20, Color.Lime);
#endif
	}
	
	public static string dbginfo = "";

	public void RenderSprite(Sprite spr)
	{
		if (!spr.visible) return;
		if (spr.costumes.Length == 0) return;

		Costume costume = spr.costumes[spr.currentCostume];

		Vector2 offset = GetOffset(spr.costume);

		// if (spr.isStage)
		// {
		// 	pos = new(0, 0);
		// 	offset = new(0, 0);
		// }

		float size = spr.size / 100 / costume.bitmapResolution;

		Raylib.DrawTexturePro(
			costume.texture,
			new(
				0, 0,
				costume.image.Width,
				costume.image.Height
			),
			new(
				(int)spr.RaylibPosition.X, (int)spr.RaylibPosition.Y,
				costume.image.Width * size,
				costume.image.Height * size
			),
			offset * 2,
			spr.direction - 90,
			Color.White
		);
	}

	public static Vector2 GetOffset(Costume costume)
	{
		// return new((costume.rotationCenterX - costume.image.Width / 4) / costume.bitmapResolution, (costume.rotationCenterY - costume.image.Height / 4) / costume.bitmapResolution);
		return new(costume.rotationCenterX / (costume.bitmapResolution * 2), costume.rotationCenterY / (costume.bitmapResolution * 2));
	}

	public static Vector2 ScratchToRaylib(float x, float y) => new(Raylib.GetScreenWidth() / 2f + x, Raylib.GetScreenHeight() / 2f - y);
	public static Vector2 ScratchToRaylib(Vector2 vec) => ScratchToRaylib(vec.X, vec.Y);
	public static Vector2 ScratchToRaylib(Vector3 vec) => ScratchToRaylib(vec.X, vec.Y);

	public Color GetColorOnPixel(int x, int y)
	{
		foreach (Sprite spr in project.sprites)
		{
			if (!spr.PointInsideSprite(new(x, y))) continue;

			Color? ret = spr.GetColorOnPixel(x, y);
			if (ret != null) return (Color)ret;
		}

		return Color.Black;
	}

	public void DrawDebugInfo(Sprite spr)
	{
		Costume costume = spr.costumes[spr.currentCostume];
		// Vector2 offset = new((costume.rotationCenterX - costume.image.Width / 4) / costume.bitmapResolution, (costume.rotationCenterY - costume.image.Height / 4) / costume.bitmapResolution);
		Vector2 offset = GetOffset(costume);

		Vector2 pos = ScratchToRaylib(spr.x, spr.y);

		Vector2 origin = ScratchToRaylib((int)Math.Round(spr.x - costume.image.Width / 2), (int)Math.Round(spr.y + costume.image.Height / 2));
		Vector2 max = ScratchToRaylib((int)Math.Round(spr.x + costume.image.Width / 2), (int)Math.Round(spr.y - costume.image.Height / 2));

		Vector2 pivot = pos + offset * 2;
		Raylib.DrawCircle((int)pivot.X, (int)pivot.Y, 3, Color.Yellow);
		Raylib.DrawCircle((int)pos.X, (int)pos.Y, 3, Color.Red);
		Raylib.DrawCircle((int)max.X, (int)max.Y, 3, Color.Blue);
		Raylib.DrawCircle((int)origin.X, (int)origin.Y, 3, Color.SkyBlue);

		Vector2 bmin = ScratchToRaylib(spr.boundingBox.Min);
		Vector2 bmax = ScratchToRaylib(spr.boundingBox.Max);

		Color wire = new(90, 90, 255, 100);
		// Color boundingbox = new(90, 255, 9, 100);

		Raylib.DrawLine(0, (int)bmin.Y, Raylib.GetRenderWidth(), (int)bmin.Y, wire);
		Raylib.DrawLine(0, (int)bmax.Y, Raylib.GetRenderWidth(), (int)bmax.Y, wire);
		Raylib.DrawLine((int)bmin.X, 0, (int)bmin.X, Raylib.GetRenderHeight(), wire);
		Raylib.DrawLine((int)bmax.X, 0, (int)bmax.X, Raylib.GetRenderHeight(), wire);
		Raylib.DrawRectangle((int)bmin.X, (int)bmin.Y, (int)(bmax.X - bmin.X), (int)(bmax.Y - bmin.Y), new(255, 0, 0, 100));

		Raylib.DrawText($"{pos.X}, {pos.Y}\n{offset.X}, {offset.Y}", (int)pos.X, (int)pos.Y, 10, Color.Green);

		// Raylib.DrawBoundingBox(spr.boundingBox, boundingbox);
	}
}
