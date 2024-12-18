using Raylib_cs;

namespace Emuratch.Core.Overlay;

public record struct Message(string message)
{
	public float added = (float)Raylib.GetTime();
	public float duration = 3;
	public string message = message;
}
