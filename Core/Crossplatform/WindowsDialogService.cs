#if _WINDOWS_
using System.Windows.Forms;

namespace Emuratch.Core.Crossplatform
{
	public class WindowsDialogService : IDialogService
	{
		public string ShowFileDialog(FileFilter[] filters)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			
			string filterstr = "";
			foreach (var filter in filters)
			{
				filterstr += filter.ToString();
			}
			filterstr = filterstr.Substring(0, filterstr.Length - 1);

			openFileDialog.Filter = filterstr;
			return openFileDialog.ShowDialog() == DialogResult.OK ? openFileDialog.FileName : "";
		}

		public void ShowMessageDialog(string message)
		{
			MessageBox.Show(message);
		}

		public bool ShowYesNoDialog(string message)
		{
			DialogResult result = MessageBox.Show(message, "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
			return result == DialogResult.Yes;
		}
	}
}
#endif