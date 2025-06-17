using Emuratch.Core.Render;
using Emuratch.Core.Scratch;
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

		Costume costume = spr.costumes[spr.currentCostume];

		Vector2 offset = RenderUtility.GetOffset(spr.costume);

		// if (spr.isStage)
		// {
		// 	pos = new(0, 0);
		// 	offset = new(0, 0);
		// }

		float size = spr.size / 100 / costume.bitmapResolution / SVGResolution;

		Vector2 raylibpos = RaylibPosition(spr);

		Image img = GetImage(spr, costume);
		Raylib.DrawTexturePro(
			GetTexture(spr, costume),
			new Rectangle(
				0, 0,
				img.Width,
				img.Height
			),
			new Rectangle(
				raylibpos.X, raylibpos.Y,
				img.Width * size,
				img.Height * size
			),
			offset * 2,
			spr.direction - 90,
			Color.White
		);
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
		// Apply Position, Rotation, Scale

		Matrix3x2 worldMatrix = Matrix3x2.CreateScale(spr.size) * Matrix3x2.CreateRotation(spr.direction * IRender.DegToRad) * Matrix3x2.CreateTranslation(new(spr.x, spr.y));

		// Convert World Position to Local Position

		if (!Matrix3x2.Invert(worldMatrix, out Matrix3x2 invMatrix)) return null; // 逆行列が求まらない場合は判定不可

		// Convert Point to Local Position

		Vector2 localPoint = Vector2.Transform(new(x, y), invMatrix);

		// Check for limit

		Image img = GetImage(spr, spr.costume);
		if (localPoint.X < 0 || localPoint.Y < 0 || localPoint.X >= img.Width || localPoint.Y >= img.Height) return null;

		Color color = GetColor((int)localPoint.X, (int)localPoint.Y, spr);
		return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
	}

	public void DrawDebugInfo(Sprite spr)
	{
		Costume costume = spr.costumes[spr.currentCostume];
		// Vector2 offset = new((costume.rotationCenterX - costume.image.Width / 4) / costume.bitmapResolution, (costume.rotationCenterY - costume.image.Height / 4) / costume.bitmapResolution);
		Vector2 offset = RenderUtility.GetOffset(costume);

		Vector2 pos = ScratchToRaylib(spr.x, spr.y);

		Vector2 origin = ScratchToRaylib((int)MathF.Round(spr.x - GetImage(spr, costume).Width / 2), (int)MathF.Round(spr.y + GetImage(spr, costume).Height / 2));
		Vector2 max = ScratchToRaylib((int)MathF.Round(spr.x + GetImage(spr, costume).Width / 2), (int)MathF.Round(spr.y - GetImage(spr, costume).Height / 2));

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

	public Color GetColor(int x, int y, Sprite spr)
	{
		Image img = GetImage(spr, spr.costume);
		if (x > img.Width || x < 0) return Color.Blank;
		if (y > img.Height || y < 0) return Color.Blank;

		Color color;
		unsafe
		{
			color = ((Color*)GetColors(spr, spr.costume))[y * img.Width + x];
		}
		return color;
	}

	public BoundingBox RaylibBoundingBox(Sprite spr)
	{
		Vector2 bmin = ScratchToRaylib(spr.boundingBox.Min);
		Vector2 bmax = ScratchToRaylib(spr.boundingBox.Max);
		return new(new(bmin, 0), new(bmax, 1));
	}

	public Vector2 RaylibPosition(Sprite spr) => ScratchToRaylib(spr.x, spr.y);
	public Vector2 RaylibOrigin(Sprite spr)
	{
		Vector2 raylibpos = RaylibPosition(spr);
		Vector2 offset = RenderUtility.GetOffset(spr.costume);
		return new(raylibpos.X - offset.X * 2, raylibpos.Y - offset.Y * 2);
	}

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
		get {
			Vector2 inverted = Raylib.GetMousePosition() - new Vector2(Raylib.GetScreenWidth() * 0.5f, Raylib.GetScreenHeight() * 0.5f);
			return new(inverted.X, -inverted.Y);
		}
	}

	public void Unload()
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