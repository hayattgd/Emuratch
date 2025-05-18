using Emuratch.Core.Scratch;
using Raylib_cs;

namespace Emuratch.Core.Render;

public interface RenderType
{
	public void RenderAll();
	public void RenderSprite(Sprite spr);
	public Color GetColorOnPixel(int x, int y);
}
