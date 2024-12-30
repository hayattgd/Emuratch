using System;

public static class DialogServiceFactory
{
    public static IDialogService CreateDialogService()
    {
#if _WINDOWS_
        return new WindowsDialogService();
#elif _MACOS_
        return new MacOSDialogService();
#else
        return new LinuxDialogService();
#endif
    }
}