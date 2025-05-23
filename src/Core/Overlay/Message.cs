using Raylib_cs;

namespace Emuratch.Core.Overlay;

public record struct Message(string message)
{
	public readonly float added = (float)Raylib.GetTime();
	public readonly float duration = 3;
	public readonly string message = message;
}
