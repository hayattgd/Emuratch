using System.IO;

namespace Emuratch.Core.Crossplatform;
public interface IDialogService
{
	string ShowFileDialog(FileFilter[] filters);
	void ShowMessageDialog(string message);
	bool ShowYesNoDialog(string message);
}

public struct FileFilter
{
	public FileFilter(string title, params string[] extensions)
	{
		Title = title;
		Extensions = extensions;
	}

	public FileFilter(string extension)
	{
		Title = extension;
		Extensions = [extension];
	}

	public string Title;
	public string[] Extensions;

	public override string ToString()
	{
		return Title + "|" + string.Join(";", Extensions) + "|";
	}
}