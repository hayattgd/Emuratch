using System.Numerics;

namespace Emuratch.Core.Utils;

public struct BoundingBox(Vector2 min, Vector2 max)
{
	public Vector2 Min = min;
	public Vector2 Max = max;
}