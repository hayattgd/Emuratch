using Emuratch.Core.Scratch;
using Emuratch.Core.Utils;
using System;
using System.Drawing;
using System.Numerics;

namespace Emuratch.Core.Render;

public interface IRender : IUnloadable
{
	const double DegToRad = Math.PI / 180;

	public void RenderAll();
	public void RenderSprite(Sprite spr);
	public Color GetColorOnPixel(int x, int y);
	public Color? GetColorOnPixel(Sprite spr, int x, int y);

	public void PlaySound(Sound sound);

	public bool IsAnyKeyDown();
	public bool IsStringKey(string key);
	public bool IsKeyDown(string key);
	public bool IsKeyPressedOnce(string key);
	public bool IsKeyRepeated(string key);

	public bool IsMouseDown();
	public Vector2 MousePosition { get; }

	// only used for debug purpose, define empty function if this isnt required
	public void DrawRectangle(int x, int y, int w, int h, Color color);
	public void DrawPoint(int x, int y, Color color);
	public void DrawPixel(int x, int y, Color color);
}