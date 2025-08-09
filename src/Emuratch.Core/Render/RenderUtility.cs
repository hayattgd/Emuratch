using Emuratch.Core.Scratch;
using System.Numerics;

namespace Emuratch.Core.Render;

public static class RenderUtility
{
	public static Vector2 GetOffset(Costume costume)
	{
		return new(costume.rotationCenterX / (costume.bitmapResolution * 2), costume.rotationCenterY / (costume.bitmapResolution * 2));
	}

	public static Vector2 GetOrigin(Costume costume, Vector2 fixedPosition)
	{
		Vector2 offset = GetOffset(costume);
		return new(fixedPosition.X - offset.X * 2, fixedPosition.Y - offset.Y * 2);
	}
}