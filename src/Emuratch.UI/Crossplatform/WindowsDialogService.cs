#if _WINDOWS_
using Cairo;
using System.Linq;
using System.Windows.Forms;

namespace Emuratch.UI.Crossplatform;

public class WindowsDialogService : IDialogService
{
	public string ShowFileDialog(FileFilter[] filters)
	{
		OpenFileDialog openFileDialog = new OpenFileDialog();
		
		string filterstr = "";
		for (int i = 0; i < filters.Length; i++)
		{
			for (int j = 0; j < filters[i].Extensions.Length; j++)
			{
				if (!filters[i].Extensions[j].Contains('.'))
				{
					filters[i].Extensions[j] = $"*.{filters[i].Extensions[j]}";
				}
			}
		}
		
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
#endif