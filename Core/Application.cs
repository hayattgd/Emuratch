using Emuratch.Core.Scratch;
using Raylib_cs;
using System.IO.Compression;
using System.IO;
using System;
using System.Windows.Forms;
using System.Text;
using Emuratch.Core.Turbowarp;

namespace Emuratch.Core;

public class Application
{
	public Project? project { get; private set; }
	public string projectpath { get; private set; } = "";
	public bool projectloaded { get; private set; } = false;
	public bool projectloading { get; private set; } = false;

	Render? render;

	public void UnloadProject()
	{
		project?.Unload();
		projectloaded = false;
		projectpath = "";
	}

	public void Initialize()
	{
		Raylib.SetConfigFlags(ConfigFlags.VSyncHint | ConfigFlags.ResizableWindow);
		Raylib.InitWindow((int)Project.defaultWidth, (int)Project.defaultHeight, "Emuratch");
	}

	public void OnUpdate()
	{
		if (projectloading)
		{
			projectloading = false;
			project = LoadProject();
			if (project != null) render = new(project);
		}

		if (Raylib.IsKeyPressed(KeyboardKey.F1))
		{
			if (project != null) UnloadProject();
			projectloading = true;
		}

		Raylib.BeginDrawing();

		Raylib.ClearBackground(Color.RayWhite);

		if (!projectloaded)
		{
			string text = "Press F1 to load project file.";
			if (projectloading)
			{
				text = "Processing file...";
			}
			Raylib.DrawText(text, 16, 16, 20, Color.DarkGray);
		}
		else
		{
			render?.RenderAll();
		}

		Raylib.EndDrawing();
	}

	public int Unload()
	{
		if (project != null) UnloadProject();
		Raylib.CloseWindow();

		return 0;
	}

	public Project? LoadProject()
	{
		OpenFileDialog openFileDialog = new()
		{
			Title = "Select .sb3 file.",
			Filter = "Scratch project file|*.sb3;project.json|" +
					 "Archived project.json|*.sb3;*.zip;*.7z",
			RestoreDirectory = true,
		};

		if (openFileDialog.ShowDialog() == DialogResult.OK)
		{
			string filepath = openFileDialog.FileName;
			return LoadProject(filepath);
		}

		return null;
	}

	public Project? LoadProject(string path)
	{
		string ext = path.Split('.')[^1];
		string jsonpath = "";
		string json = "";
		if (ext == "sb3" || ext == "zip" || ext == "7z")
		{
			string directory = path + " Emuratch";
			try
			{
				jsonpath = path + @" Emuratch\project.json";

				if (Directory.Exists(directory))
				{
					if (MessageBox.Show("Folder already exists. Delete it?", "Error", MessageBoxButtons.YesNo) == DialogResult.Yes)
					{
						Directory.Delete(path + " Emuratch", true);
					}
					else
					{
						return null;
					}
				}

				Directory.CreateDirectory(path + " Emuratch");

				using var archive = ZipFile.Open(path, ZipArchiveMode.Read);
				archive.ExtractToDirectory(path + " Emuratch");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error");
				return null;
			}
		}
		else if (ext == "json")
		{
			jsonpath = path;
		}

		projectpath = Path.GetDirectoryName(jsonpath) ?? "";

		Directory.SetCurrentDirectory(projectpath);

		json = File.ReadAllText(jsonpath);

		if (Project.LoadProject(json, out Project project))
		{
			Configuration.ApplyConfig(ref project);
			Raylib.SetWindowSize((int?)project?.width ?? (int)Project.defaultWidth, (int?)project?.height ?? (int)Project.defaultHeight);

			projectloaded = projectpath != "";
			return project;
		}

		projectloaded = false;
		return null;
	}

	public string GetAbsolutePath(string path)
	{
		return Program.app.projectpath + Path.DirectorySeparatorChar + path;
	}
}
