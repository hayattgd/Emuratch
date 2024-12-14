using Raylib_cs;
using Svg;

namespace Emuratch.Core.Overlay;

public struct Message
{
	public Message(string text)
	{
		added = (float)Raylib.GetTime();
		duration = 3;
		message = text;
	}

	public float added;
	public float duration;
	public string message;
}
