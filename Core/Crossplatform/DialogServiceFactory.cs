using System;

namespace Emuratch.Core.Crossplatform
{
	public static class DialogServiceFactory
	{
		public static IDialogService CreateDialogService()
		{
	#if _WINDOWS_
			return new WindowsDialogService();
	#elif _LINUX_
			return new LinuxDialogService();
	#else
			return new ConsoleDialogService();
	#endif
		}
	}
}