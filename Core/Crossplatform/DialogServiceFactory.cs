using System;

public static class DialogServiceFactory
{
    public static IDialogService CreateDialogService()
    {
#if _WINDOWS_
        return new WindowsDialogService();
#else
        return new ConsoleDialogService();
#endif
    }
}