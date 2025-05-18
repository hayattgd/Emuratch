using Emuratch.Core.Scratch;
using Raylib_cs;

namespace Emuratch.Core.Render;

public interface IRender
{
	public void RenderAll();
	public void RenderSprite(Sprite spr);
	public Color GetColorOnPixel(int x, int y);
}
