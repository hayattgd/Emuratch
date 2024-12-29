using System;

public static class DialogServiceFactory
{
    public static IDialogService CreateDialogService()
    {
#if WINDOWS
        return new WindowsDialogService();
#elif MACOS
        return new MacOSDialogService();
#elif LINUX
        return new LinuxDialogService();
#else
        throw new PlatformNotSupportedException("This platform is not supported.");
#endif
    }
}