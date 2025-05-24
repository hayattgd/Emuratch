using System;

namespace Emuratch.UI.Crossplatform
{
	public static class DialogServiceFactory
	{
		public static IDialogService CreateDialogService()
		{
	#if _WINDOWS_
			return new WindowsDialogService();
	#elif _LINUX_ || _MACOS_
			return new GtkDialogService();
	#else
			return new ConsoleDialogService();
	#endif
		}
	}
}