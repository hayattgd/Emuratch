public interface IDialogService
{
	string ShowFileDialog();
	void ShowMessageDialog(string message);
	bool ShowYesNoDialog(string message);
}