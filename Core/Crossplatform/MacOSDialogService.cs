#if _MACOS_
using System;
using System.Runtime.InteropServices;

public class MacOSDialogService : IDialogService
{
    

    public string ShowFileDialog()
    {
        Console.WriteLine("Please enter the path to the project file:");
        return Console.ReadLine();
    }

    public void ShowMessageDialog(string message)
    {
        Console.WriteLine(message);
    }

    public bool ShowYesNoDialog(string message)
    {
        Console.WriteLine(message + " (y/n)");
        string response = Console.ReadLine().ToLower();
        return response == "y" || response == "yes";
    }
}
#endif