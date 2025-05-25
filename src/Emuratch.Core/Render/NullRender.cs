using Emuratch.Core.Scratch;
using System.Drawing;
using System.Numerics;

namespace Emuratch.Core.Render;

public class NullRender : IRender
{
	public void RenderAll() { }
	public void RenderSprite(Sprite spr) { }
	public Color GetColorOnPixel(int x, int y) => new();
	public Color? GetColorOnPixel(Sprite spr, int x, int y) => new();

	public void PlaySound(Sound sound) { }

	public bool IsAnyKeyDown() => false;
	public bool IsStringKey(string key) => false;
	public bool IsKeyDown(string key) => false;
	public bool IsKeyPressedOnce(string key) => false;
	public bool IsKeyRepeated(string key) => false;

	public bool IsMouseDown() => false;
	public Vector2 MousePosition { get => new(); }

	public void Unload() { }
}
