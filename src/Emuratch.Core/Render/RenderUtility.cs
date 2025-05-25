using Emuratch.Core.Scratch;
using System.Numerics;

namespace Emuratch.Core.Render;

public static class RenderUtility
{
	public static Vector2 GetOffset(Costume costume)
	{
		return new(costume.rotationCenterX / (costume.bitmapResolution * 2), costume.rotationCenterY / (costume.bitmapResolution * 2));
	}
}