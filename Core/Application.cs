using Emuratch.Core.Scratch;
using Raylib_cs;
using System.IO.Compression;
using System.IO;
using System;
using System.Windows.Forms;
using System.Text;

namespace Emuratch.Core;

public class Application
{
	public Project? project { get; private set; }
	public string projectpath { get; private set; } = "";
	public bool projectloaded { get; private set; } = false;

	Render? render;

	public void Initialize()
	{
		Raylib.InitWindow(800, 600, "Emuratch");
	}

	public void OnUpdate()
	{
		if (Raylib.IsKeyPressed(KeyboardKey.F1))
		{
			project = LoadProject();
			if (project != null) render = new(project);
		}

		Raylib.BeginDrawing();

		Raylib.ClearBackground(Color.White);

		if (!projectloaded)
		{
			Raylib.DrawText("Press F1 to load scratch project file", 16, 16, 20, Color.DarkGray);
		}
		else
		{
			render?.RenderAll();
		}

		Raylib.EndDrawing();
	}

	public void Unload()
	{
		Raylib.CloseWindow();
	}

	public Project? LoadProject()
	{
		OpenFileDialog openFileDialog = new()
		{
			Title = "Select .sb3 file.",
			Filter = "Scratch project file (*.sb3)|*.sb3|Project.json|project.json",
			RestoreDirectory = true
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
		if (ext == "sb3")
		{
			try
			{
				if (!Directory.Exists(path + " Emuratch"))
				{
					Directory.CreateDirectory(path + " Emuratch");
				}

				using (var archive = ZipFile.Open(path, ZipArchiveMode.Read))
				{
					archive.ExtractToDirectory(path + " Emuratch");
				}

				jsonpath = path + @" Emuratch\project.json";

			}
			catch (Exception)
			{
				throw;
			}
		}
		else if (ext == "json")
		{
			jsonpath = path;
		}

		projectpath = Path.GetDirectoryName(jsonpath) ?? "";
		byte[] bytes = Encoding.ASCII.GetBytes(projectpath);

		unsafe
		{
			fixed (byte* p = bytes)
			{
				sbyte* sp = (sbyte*)p;
				Raylib.ChangeDirectory(sp);
			}
		}

		json = File.ReadAllText(jsonpath);

		if(Project.LoadProject(json, out Project project))
		{
			projectloaded = projectpath != "";
			return project;
		}

		projectloaded = false;
		return null;
	}
}
