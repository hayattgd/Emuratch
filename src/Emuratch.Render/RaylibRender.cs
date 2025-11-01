using Emuratch.Core.Render;
using Emuratch.Core.Scratch;
using Emuratch.Core.Utils;
using Raylib_cs;
using Svg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Emuratch.Render;

public class RaylibRender : IRender
{
	public const int SVGResolution = 4;

	public RaylibRender(Project project)
	{
		this.project = project;
		// Raylib.SetConfigFlags(ConfigFlags.VSyncHint | ConfigFlags.ResizableWindow);
		// Raylib.InitWindow((int)Project.defaultWidth, (int)Project.defaultHeight, "Emuratch");
	}

	public readonly Project project;

	Dictionary<string, Image> images = [];
	Dictionary<string, IntPtr> imgcolors = [];
	Dictionary<string, Texture2D> textures = [];
	Dictionary<string, Raylib_cs.Sound> sounds = [];

	public Image GetImage(Sprite spr, Costume costume)
	{
		string key = $"{spr.name}\n{costume.name}";
		if (images.TryGetValue(key, out var value))
		{
			return value;
		}
		else
		{
			//Load from path
			string imagepath = $"{costume.assetId}.{costume.dataFormat}";
			if (costume.dataFormat != "svg")
			{
				Image loadedimage = Raylib.LoadImage(imagepath);
				images.Add(key, loadedimage);
				return loadedimage;
			}
			else
			{
				if (File.Exists(project.GetAbsolutePath(imagepath)))
				{
					//Convert .svg to .png
					string pngpath = Path.ChangeExtension(imagepath, ".png");

					if (costume.dataFormat == "svg")
					{
						pngpath = Path.ChangeExtension(pngpath, $"x{SVGResolution}.png");
						if (!File.Exists(project.GetAbsolutePath(pngpath)))
						{
							var svg = SvgDocument.Open(project.GetAbsolutePath(imagepath));
							using var bitmap = svg.Draw((int)svg.Width.Value * SVGResolution, (int)svg.Height.Value * SVGResolution);
							bitmap?.Save(project.GetAbsolutePath(pngpath));
						}
					}
					Image loadedimage = Raylib.LoadImage(pngpath);
					images.Add(key, loadedimage);
					return loadedimage;
				}
				else
				{
					Image image = Raylib.GenImageColor(32, 32, new Color(255, 0, 255, 255));
					images.Add(key, image);
					return image;
				}
			}
		}
	}
	public Texture2D GetTexture(Sprite spr, Costume costume)
	{
		string key = $"{spr.name}\n{costume.name}";
		if (textures.TryGetValue(key, out var value))
		{
			return value;
		}
		else
		{
			Texture2D texture = Raylib.LoadTextureFromImage(GetImage(spr, costume));
			textures.Add(key, texture);
			return texture;
		}
	}
	public IntPtr GetColors(Sprite spr, Costume costume)
	{
		string key = $"{spr.name}\n{costume.name}";
		if (imgcolors.TryGetValue(key, out var value))
		{
			return value;
		}
		else
		{
			IntPtr colors;
			unsafe
			{
				colors = (IntPtr)Raylib.LoadImageColors(GetImage(spr, costume));
			}
			imgcolors.Add(key, colors);
			return colors;
		}
	}
	public Raylib_cs.Sound GetSound(Core.Scratch.Sound sound)
	{
		string key = sound.assetId;
		if (sounds.TryGetValue(key, out var value))
		{
			return value;
		}
		else
		{
			Raylib_cs.Sound loaded = Raylib.LoadSound($"{sound.assetId}.{sound.dataFormat}");
			sounds.Add(key, loaded);
			return loaded;
		}
	}

	public bool PointInsideSprite(Sprite spr, Vector2 pos)
	{
		return spr.x <= pos.X && spr.x + GetImage(spr, spr.costume).Width >= pos.X && spr.y <= pos.Y && spr.y + GetImage(spr, spr.costume).Height >= pos.Y;
	}

	public void RenderAll()
	{
		Raylib.ClearBackground(Color.White);
		RenderSprite(project.stage);
		var list = project.sprites.ToList();
		var all = list.Concat(project.clones);

		list = [.. all];
		list.Sort((a, b) => a.layoutOrder.CompareTo(b.layoutOrder));
		list.Reverse();

		foreach (var sprite in list)
		{
			RenderSprite(sprite);
		}

		foreach (var monitor in project.monitors)
		{
			RenderMonitor(monitor);
		}

		foreach (var sprite in list)
		{
			if (sprite.dialog != null)
			{
				var pos = ScratchToRaylib(sprite.Position);
				Raylib.DrawText(sprite.dialog.Value.text, (int)pos.X - sprite.costume.Width / 2, (int)pos.Y - sprite.costume.Height / 2 - 20, 20, Color.Black);
			}
		}

		if (!project.debug) return;

		Raylib.DrawLine(0, Raylib.GetRenderHeight() / 2, Raylib.GetRenderWidth(), Raylib.GetRenderHeight() / 2, Color.Red);
		Raylib.DrawLine(Raylib.GetRenderWidth() / 2, 0, Raylib.GetRenderWidth() / 2, Raylib.GetRenderHeight(), Color.Red);

		foreach (var sprite in list)
		{
			DrawDebugInfo(sprite);
		}
	}

	public void RenderSprite(Sprite spr)
	{
		if (!spr.visible) return;
		if (spr.costumes.Length == 0) return;

		Vector2 offset = RenderUtility.GetOffset(spr.costume);

		// if (spr.isStage)
		// {
		// 	pos = new(0, 0);
		// 	offset = new(0, 0);
		// }
		int bitmapRes = spr.costume.bitmapResolution;
		if (bitmapRes == 0) { bitmapRes = 1; }
		if (spr.costume.dataFormat == "svg") { bitmapRes *= SVGResolution; }

		float size = spr.size / 100 / bitmapRes;

		Vector2 raylibpos = RaylibPosition(spr);
		Image img = GetImage(spr, spr.costume);

		float direction = spr.direction - 90;
		if (spr.Rotationstyle == Sprite.RotationStyle.dont_rotate ||
			spr.Rotationstyle == Sprite.RotationStyle.left_right)
		{
			direction = 0;
		}

		Rectangle source;

		if (spr.Rotationstyle == Sprite.RotationStyle.left_right &&
			spr.direction < 0)
		{
			source = new(
				img.Width, 0,
				-img.Width,
				img.Height
			);
		}
		else
		{
			source = new(
				0, 0,
				img.Width,
				img.Height
			);
		}

		Raylib.DrawTexturePro(
			GetTexture(spr, spr.costume),
			source,
			new Rectangle(
				raylibpos.X, raylibpos.Y,
				img.Width * size,
				img.Height * size
			),
			offset * 2,
			direction,
			Color.White
		);
	}

	private void RenderNormal(Monitor monitor)
	{
		var displaynameWidth = Raylib.MeasureText(monitor.DisplayName, 14);
		var value = "";
		if (monitor.sprname == "")
		{
			value = project.stage.variables[monitor.id].value.ToString();
		}

		var rect = new Rectangle((int)monitor.pos.X, (int)monitor.pos.Y, displaynameWidth + 40 + Raylib.MeasureText(value, 20), 30);
		Raylib.DrawRectangleRounded(rect, 0.2f, 2, Color.RayWhite);
		Raylib.DrawText(monitor.DisplayName, (int)monitor.pos.X + 5, (int)monitor.pos.Y + 4, 20, Color.Black);
		var valueBgRect = new Rectangle((int)monitor.pos.X + displaynameWidth + 40, (int)monitor.pos.Y, 10 + Raylib.MeasureText(value, 20), 30);
		Raylib.DrawRectangleRounded(valueBgRect, 0.2f, 2, Color.Orange);
		Raylib.DrawText(value, (int)monitor.pos.X + displaynameWidth + 45, (int)monitor.pos.Y + 4, 20, Color.White);
	}

	public void RenderMonitor(Monitor monitor)
	{
		switch (monitor.mode)
		{
			case Monitor.Mode.normal:
				RenderNormal(monitor);
				break;

			case Monitor.Mode.large:
				break;

			case Monitor.Mode.slider:
				break;

			case Monitor.Mode.list:
				break;
		}
	}

	public static Vector2 ScratchToRaylib(float x, float y) => new(Raylib.GetScreenWidth() / 2f + x, Raylib.GetScreenHeight() / 2f - y);
	public static Vector2 ScratchToRaylib(Vector2 vec) => ScratchToRaylib(vec.X, vec.Y);
	public static Vector2 ScratchToRaylib(Vector3 vec) => ScratchToRaylib(vec.X, vec.Y);

	public System.Drawing.Color GetColorOnPixel(int x, int y)
	{
		foreach (Sprite spr in project.sprites)
		{
			if (!PointInsideSprite(spr, new(x, y))) continue;

			System.Drawing.Color? ret = GetColorOnPixel(spr, x, y);
			if (ret != null) return (System.Drawing.Color)ret;
		}

		return System.Drawing.Color.Black;
	}

	public System.Drawing.Color? GetColorOnPixel(Sprite spr, int x, int y)
	{
		float scale = spr.size / 100f;
		float rotationRad = (float)((90 - spr.direction) * IRender.DegToRad);
		
		Matrix3x2 worldMatrix = 
			Matrix3x2.CreateScale(scale) *
			Matrix3x2.CreateRotation(rotationRad) *
			Matrix3x2.CreateTranslation(new(spr.x, spr.y));

		if (!Matrix3x2.Invert(worldMatrix, out Matrix3x2 invMatrix))
		{
			return null;
		}
		var worldPoint = new Vector2(x, y);
		var localPoint = Vector2.Transform(worldPoint, invMatrix);
		
		var costume = spr.costume;
		Image img = GetImage(spr, costume);

		float resolutionFactor = (costume.Width > 0) ? (img.Width / costume.Width) : 1.0f;

		var scaledLocalPoint = localPoint * resolutionFactor;
		var scaledRotCenter = new Vector2(costume.rotationCenterX, costume.rotationCenterY) * resolutionFactor;

		var imagePoint = new Vector2(
			scaledLocalPoint.X + scaledRotCenter.X,
			-scaledLocalPoint.Y + scaledRotCenter.Y
		);
		if (imagePoint.X >= 0 && imagePoint.X < img.Width &&
			imagePoint.Y >= 0 && imagePoint.Y < img.Height)
		{
			Color color = GetColor((int)imagePoint.X, (int)imagePoint.Y, spr);
			return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
		}

		return null;
	}

	public void DrawDebugInfo(Sprite spr)
	{
		Costume costume = spr.costumes[spr.currentCostume];
		// Vector2 offset = new((costume.rotationCenterX - costume.image.Width / 4) / costume.bitmapResolution, (costume.rotationCenterY - costume.image.Height / 4) / costume.bitmapResolution);
		Vector2 offset = RenderUtility.GetOffset(costume);

		Vector2 pos = ScratchToRaylib(spr.x, spr.y);

		Vector2 origin = ScratchToRaylib((int)MathF.Round(spr.x - GetImage(spr, costume).Width / 2), (int)MathF.Round(spr.y + GetImage(spr, costume).Height / 2));
		Vector2 max = ScratchToRaylib((int)MathF.Round(spr.x + GetImage(spr, costume).Width / 2), (int)MathF.Round(spr.y - GetImage(spr, costume).Height / 2));

		Vector2 pivot = pos - offset * 2;
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

	public Color GetColor(int x, int y, Sprite spr)
	{
		Image img = GetImage(spr, spr.costume);
		// if (rayliborigin.X > img.Width || rayliborigin.X < 0) return Color.Blank;
		// if (rayliborigin.Y > img.Height || rayliborigin.Y < 0) return Color.Blank;
		// Console.WriteLine($"x:{rayliborigin.X}, y:{rayliborigin.Y}");
		Color color;
		unsafe
		{
			color = ((Color*)GetColors(spr, spr.costume))[y * img.Width + x];
		}
		return color;
	}

	public Vector2 RaylibPosition(Sprite spr) => ScratchToRaylib(spr.x, spr.y);

	public void PlaySound(Core.Scratch.Sound sound)
	{
		Raylib.PlaySound(GetSound(sound));
	}

	public KeyboardKey StrKey(string str)
	{
		return str switch
		{
			"space" => KeyboardKey.Space,
			"left arrow" => KeyboardKey.Left,
			"right arrow" => KeyboardKey.Right,
			"up arrow" => KeyboardKey.Up,
			"down arrow" => KeyboardKey.Down,
			"enter" => KeyboardKey.Enter,
			"a" => KeyboardKey.A,
			"b" => KeyboardKey.B,
			"c" => KeyboardKey.C,
			"d" => KeyboardKey.D,
			"e" => KeyboardKey.E,
			"f" => KeyboardKey.F,
			"g" => KeyboardKey.G,
			"h" => KeyboardKey.H,
			"i" => KeyboardKey.I,
			"j" => KeyboardKey.J,
			"k" => KeyboardKey.K,
			"l" => KeyboardKey.L,
			"m" => KeyboardKey.M,
			"n" => KeyboardKey.N,
			"o" => KeyboardKey.O,
			"p" => KeyboardKey.P,
			"q" => KeyboardKey.Q,
			"r" => KeyboardKey.R,
			"s" => KeyboardKey.S,
			"t" => KeyboardKey.T,
			"u" => KeyboardKey.U,
			"v" => KeyboardKey.V,
			"w" => KeyboardKey.W,
			"x" => KeyboardKey.X,
			"y" => KeyboardKey.Y,
			"z" => KeyboardKey.Z,
			"0" => KeyboardKey.Zero,
			"1" => KeyboardKey.One,
			"2" => KeyboardKey.Two,
			"3" => KeyboardKey.Three,
			"4" => KeyboardKey.Four,
			"5" => KeyboardKey.Five,
			"6" => KeyboardKey.Six,
			"7" => KeyboardKey.Seven,
			"8" => KeyboardKey.Eight,
			"9" => KeyboardKey.Nine,
			"-" => KeyboardKey.Minus,
			"," => KeyboardKey.Comma,
			"." => KeyboardKey.Period,
			"`" => KeyboardKey.Grave,
			"=" => KeyboardKey.Equal,
			"[" => KeyboardKey.LeftBracket,
			"]" => KeyboardKey.RightBracket,
			"\\" => KeyboardKey.Backslash,
			";" => KeyboardKey.Semicolon,
			"'" => KeyboardKey.Apostrophe,
			"/" => KeyboardKey.Slash,
			//Using "join" block, we can do these tricks.
			"control" => KeyboardKey.LeftControl,
			"shift" => KeyboardKey.LeftShift,
			"backspace" => KeyboardKey.Backspace,
			"insert" => KeyboardKey.Insert,
			"page up" => KeyboardKey.PageUp,
			"page down" => KeyboardKey.PageDown,
			"end" => KeyboardKey.End,
			"home" => KeyboardKey.Home,
			"scroll lock" => KeyboardKey.ScrollLock,
			_ => KeyboardKey.Null
		};
	}

	public bool IsStringKey(string key)
	{
		return StrKey(key) != KeyboardKey.Null;
	}

	public bool IsAnyKeyDown() => Raylib.GetKeyPressed() > 0;
	public bool IsKeyDown(string key) => Raylib.IsKeyDown(StrKey(key));
	public bool IsKeyPressedOnce(string key) => Raylib.IsKeyPressed(StrKey(key));
	public bool IsKeyRepeated(string key) => Raylib.IsKeyPressedRepeat(StrKey(key));

	public bool IsMouseDown() => Raylib.IsMouseButtonDown(MouseButton.Left);
	public Vector2 MousePosition
	{
		get
		{
			Vector2 inverted = Raylib.GetMousePosition() - new Vector2(Raylib.GetScreenWidth() * 0.5f, Raylib.GetScreenHeight() * 0.5f);
			return new(inverted.X, -inverted.Y);
		}
	}

	public void DrawRectangle(int x, int y, int w, int h, System.Drawing.Color color)
	{
		Raylib_cs.Color raylibcolor = new(color.R, color.G, color.B, color.A);
		var pos = ScratchToRaylib(x, y);
		Raylib.DrawRectangleLines((int)pos.X, (int)pos.Y, w, h, raylibcolor);
	}

	public void DrawPoint(int x, int y, System.Drawing.Color color)
	{
		Raylib_cs.Color raylibcolor = new(color.R, color.G, color.B, color.A);
		var pos = ScratchToRaylib(x, y);
		Raylib.DrawCircle((int)pos.X, (int)pos.Y, 3, raylibcolor);
	}

	public void DrawPixel(int x, int y, System.Drawing.Color color)
	{
		Raylib_cs.Color raylibcolor = new(color.R, color.G, color.B, color.A);
		var pos = ScratchToRaylib(x, y);
		Raylib.DrawPixel((int)pos.X, (int)pos.Y, raylibcolor);
	}

	public void Dispose()
	{
		foreach (var img in images)
		{
			Raylib.UnloadImage(img.Value);
		}
		images.Clear();

		foreach (var cols in imgcolors)
		{
			Marshal.FreeCoTaskMem(cols.Value);
		}
		imgcolors.Clear();

		foreach (var tex in textures)
		{
			Raylib.UnloadTexture(tex.Value);
		}
		textures.Clear();

		foreach (var snd in sounds)
		{
			Raylib.UnloadSound(snd.Value);
		}
		sounds.Clear();

		// Raylib.CloseWindow();
	}
}