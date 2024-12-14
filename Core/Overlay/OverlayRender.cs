using Emuratch.Core.Scratch;
using Raylib_cs;
using System;
using System.Xml.Serialization;

namespace Emuratch.Core.Overlay;

public static class OverlayRender
{
	const int fontsize = 20;

	public static void RenderMessage(Core.Overlay.Message msg, int idx)
	{
		int height = 28;
		int margin = height - fontsize;
		int padding = 3;
		int width = Raylib.MeasureText(msg.message, fontsize);

		RenderDialogue(padding, padding + (padding + height) * idx, padding + margin, height, msg.message);
	}

	public static void RenderTransparentRect(int x, int y, int w, int h, Color rect, Color outline)
	{
		Raylib.DrawRectangle(x, y, w, h, rect);
		Raylib.DrawRectangleLines(x, y, w, h, outline);
	}

	public static void RenderTransparentRect(int x, int y, int w, int h)
	{
		RenderTransparentRect(x, y, w, h, new(255, 255, 255, 200), new(40, 40, 40, 200));
	}

	public static void RenderDialogue(int x, int y, string text)
	{
		RenderDialogue(x, y, 3, 28, text);
	}

	public static void RenderDialogue(int x, int y, int padding, int h, string text)
	{
		int width = Raylib.MeasureText(text, fontsize);
		RenderTransparentRect(x, y, width + padding * 2, h);
		Raylib.DrawText(text, x + padding, (h - fontsize) / 2 + y, fontsize, new Color(40, 40, 40, 200));
	}

	public static void RenderMonitor(Monitor monitor)
	{
		switch (monitor.mode)
		{
			case Monitor.Mode.@default:
				{
					break;
				}

			case Monitor.Mode.large:
				{
					break;
				}

			case Monitor.Mode.slider:
				{
					break;
				}

			case Monitor.Mode.list:
				{
					break;
				}

			default: throw new NotImplementedException();

		}
	}
}
