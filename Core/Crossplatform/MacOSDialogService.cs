#if _MACOS_
namespace Emuratch.Core.Crossplatform
{
	public class MacOSDialogService : IDialogService
	{
		public string ShowFileDialog(FileFilter[] filter)
		{
			
		}

		public void ShowMessageDialog(string message)
		{
			
		}

		public bool ShowYesNoDialog(string message)
		{
			return false
		}
	}
}
#endif