#if WINDOWS
using System.Windows.Forms;

public class WindowsDialogService : IDialogService
{
    public string ShowFileDialog()
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        return openFileDialog.ShowDialog() == DialogResult.OK ? openFileDialog.FileName : null;
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