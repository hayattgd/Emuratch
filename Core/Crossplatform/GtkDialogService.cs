using Gtk;

namespace Emuratch.Core.Crossplatform;

public class GtkDialogService : IDialogService
{
	public void GtkQuit()
	{
		while (Gtk.Application.EventsPending())
		{
			Gtk.Application.RunIteration(); // Run it for while so gtk dialog will close
		}
		Gtk.Application.Quit();
	}

	public string ShowFileDialog(FileFilter[] filters)
	{
		Gtk.Application.Init();
		var dialog = new FileChooserDialog("File dialog", null, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);
		dialog.SelectMultiple = false;
		
		foreach (var filter in filters)
		{
			var filefilter = new Gtk.FileFilter { Name = filter.Title };
			foreach (var ext in filter.Extensions)
			{
				filefilter.AddPattern($"*.{ext}");
			}

			dialog.AddFilter(filefilter);
		}

		if (dialog.Run() == (int)ResponseType.Accept)
		{
			string name = dialog.Filename;
			dialog.Destroy();
			GtkQuit();
			return name;
		}

		dialog.Destroy();
		GtkQuit();
		return "";
	}

	public void ShowMessageDialog(string message)
	{
		Gtk.Application.Init();
		var dialog = new MessageDialog(null, DialogFlags.Modal, MessageType.Info, ButtonsType.Ok, message);
		dialog.Run();
		dialog.Destroy();
		GtkQuit();
	}

	public bool ShowYesNoDialog(string message)
	{
		Gtk.Application.Init();
		var dialog = new MessageDialog(null, DialogFlags.Modal, MessageType.Info, ButtonsType.YesNo, message);

		if (dialog.Run() == (int)ResponseType.Yes)
		{
			dialog.Destroy();
			GtkQuit();
			return true;
		}
		else
		{
			dialog.Destroy();
			GtkQuit();
			return false;
		}
	}
}