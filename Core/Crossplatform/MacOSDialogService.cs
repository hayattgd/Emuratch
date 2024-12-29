#if MACOS
using System;
using System.Runtime.InteropServices;

public class MacOSDialogService : IDialogService
{
    [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
    private static extern IntPtr NSOpenPanel_openPanel();

    [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
    private static extern void objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
    private static extern IntPtr objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector);

    [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
    private static extern IntPtr objc_msgSend_bool(IntPtr receiver, IntPtr selector, bool arg1);

    [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
    private static extern IntPtr objc_msgSend_IntPtr_bool(IntPtr receiver, IntPtr selector, IntPtr arg1, bool arg2);

    private static readonly IntPtr selRunModal = sel_registerName("runModal");
    private static readonly IntPtr selFilename = sel_registerName("filename");

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "sel_registerName")]
    private static extern IntPtr sel_registerName(string name);

    public string ShowFileDialog()
    {
        IntPtr openPanel = NSOpenPanel_openPanel();
        objc_msgSend(openPanel, selRunModal);
        IntPtr url = objc_msgSend_IntPtr(openPanel, selFilename);
        return Marshal.PtrToStringAuto(url);
    }

    [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
    private static extern IntPtr NSAlert_alloc();

    [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
    private static extern IntPtr NSAlert_init(IntPtr alert);

    [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
    private static extern void NSAlert_setMessageText(IntPtr alert, IntPtr message);

    [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
    private static extern void NSAlert_addButtonWithTitle(IntPtr alert, IntPtr title);

    [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
    private static extern int NSAlert_runModal(IntPtr alert);

    public void ShowMessageDialog(string message)
    {
        IntPtr alert = NSAlert_alloc();
        alert = NSAlert_init(alert);
        IntPtr nsMessage = NSString_Create(message);
        NSAlert_setMessageText(alert, nsMessage);
        NSAlert_addButtonWithTitle(alert, NSString_Create("OK"));
        NSAlert_runModal(alert);
    }

    public bool ShowYesNoDialog(string message)
    {
        IntPtr alert = NSAlert_alloc();
        alert = NSAlert_init(alert);
        IntPtr nsMessage = NSString_Create(message);
        NSAlert_setMessageText(alert, nsMessage);
        NSAlert_addButtonWithTitle(alert, NSString_Create("Yes"));
        NSAlert_addButtonWithTitle(alert, NSString_Create("No"));
        int result = NSAlert_runModal(alert);
        return result == 1000; // 1000 is the return value for the first button (Yes)
    }

    [DllImport("/System/Library/Frameworks/Foundation.framework/Foundation")]
    private static extern IntPtr NSString_Create(string str);
}
#endif