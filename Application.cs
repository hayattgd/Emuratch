#nullable disable

using Emuratch.Core.Scratch;
using Emuratch.Core.Turbowarp;
using Emuratch.Core.vm;
using Emuratch.Core.Render;
using Emuratch.Core.Overlay;
using Emuratch.Core.Crossplatform;
using Raylib_cs;
using System.IO.Compression;
using System.IO;
using System;
using System.Collections.Generic;

namespace Emuratch;

public class Application
{
	public static Project project { get; private set; }
	public static string projectpath { get; private set; } = "";
	public static bool projectloaded { get; private set; }
	public static bool projectloading { get; private set; }

	public List<Thread> threads { get; private set; }

	public enum Runners
	{
		Interpreter,
		Compilier,
#if DEBUG
		Scratch,
#endif
	}

	public enum Renders
	{
		Emurender,
#if DEBUG
		Scratch,
#endif
	}

	public Runners runnertype;
	public Renders rendertype;

	internal static RenderType render;
	internal static Runner runner;

	public readonly List<Message> messages = new();

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

	public void UpdateScratch()
	{
		if (threads != null)
		{
			threads.Sort((a, b) => a.sprite.layoutOrder.CompareTo(b.sprite.layoutOrder));
			threads.ForEach((t) =>
			{
				t.Step();
			});
		}

		runner.timer += (float)runner.deltatime;
	}

	public void OnUpdate()
	{
		if (projectloading)
		{
			projectloading = false;
			project = LoadProject();
			if (project != null)
			{
				switch (rendertype)
				{
					case Renders.Emurender:
						render = new Emurender(project);
						break;

#if DEBUG
					case Renders.Scratch:
						break;
#endif
				}

				switch (runnertype)
				{
					case Runners.Interpreter:
						runner = new Interpreter(project);
						break;

					case Runners.Compilier:
						break;

#if DEBUG
					case Runners.Scratch:
						break;
#endif
				}
				runner.fps = Configuration.Config.framerate;

				messages.Add(new("Project loaded"));
			}
			else
			{
				messages.Add(new("Error occurred"));
			}
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
			string text = "Press F1 to load project file.\n\nPress F5 to press flag.";
			if (projectloading)
			{
				text = "Processing file...";
			}
			Raylib.DrawText(text, 16, 16, 20, Color.DarkGray);
		}
		else if (runner != null)
		{
			Raylib.SetTargetFPS(runner.paused ? int.MaxValue : runner.fps);

			if (Raylib.IsKeyPressed(KeyboardKey.Pause))
			{
				runner.paused = !runner.paused;
			}

			if (Raylib.IsKeyPressed(KeyboardKey.F5))
			{
				runner.timer = 0;
				threads = runner.PressFlag();
				messages.Add(new("Flag pressed"));
			}

			if (Raylib.IsKeyPressed(KeyboardKey.F2))
			{
				Raylib.SetWindowSize((int)project.width, (int)project.height);
			}

			if (Raylib.IsKeyPressed(KeyboardKey.LeftBracket))
			{
				runner.fps -= 2;
				if (runner.fps < 2) runner.fps = 2;
			}

			if (Raylib.IsKeyPressed(KeyboardKey.RightBracket))
			{
				runner.fps += 2;
			}

			render.RenderAll();
			if (!runner.paused || Raylib.IsKeyPressed(KeyboardKey.Minus))
			{
				UpdateScratch();
			}
		}

		foreach (var item in messages)
		{
			OverlayRender.RenderMessage(item, messages.IndexOf(item));
		}

		messages.RemoveAll((msg) => msg.added + msg.duration < Raylib.GetTime());

		Raylib.EndDrawing();
	}

	public int Unload()
	{
		if (project != null) UnloadProject();
		Raylib.CloseWindow();

		return 0;
	}

	public Project LoadProject()
	{
		IDialogService dialog = DialogServiceFactory.CreateDialogService();
		projectpath = dialog.ShowFileDialog(
		[
			new("Archived project", "sb3", "zip", "7z"),
			new("project.json"),
		]);
        if (!string.IsNullOrEmpty(projectpath))
        {
            messages.Add(new("Project loaded successfully."));
            return LoadProject(projectpath);
        }
        else
        {
            dialog.ShowMessageDialog("File selection canceled.");
        }

		return null;
	}

	public Project LoadProject(string path)
	{
		string suffix = ".Emuratch_Extract";

		string ext = path.Split('.')[^1];
		string jsonpath = "";
		if (ext == "sb3" || ext == "zip" || ext == "7z")
		{
			string directory = path + suffix;
			try
			{
				jsonpath = directory + $"{Path.DirectorySeparatorChar}project.json";

				bool alreadyExisted = false;
				if (Directory.Exists(directory))
				{
					if (File.Exists(jsonpath))
					{
						alreadyExisted = true;
					}
					else if (DialogServiceFactory.CreateDialogService().ShowYesNoDialog("Folder already exists. Overwrite on it?"))
					{
						Directory.Delete(path + suffix, true);
					}
					else
					{
						return null;
					}
				}

				if (!alreadyExisted)
				{
					Directory.CreateDirectory(path + suffix);

					using var archive = ZipFile.Open(path, ZipArchiveMode.Read);
					archive.ExtractToDirectory(path + suffix);
				}
			}
			catch (Exception ex)
			{
				DialogServiceFactory.CreateDialogService().ShowMessageDialog(ex.Message);
				return null;
			}
		}
		else if (ext == "json")
		{
			jsonpath = path;
		}

		projectpath = Path.GetDirectoryName(jsonpath) ?? "";

		Directory.SetCurrentDirectory(projectpath);

		string json = File.ReadAllText(jsonpath);

		if (Project.LoadProject(json, out Project LoadedProject))
		{
			Configuration.ApplyConfig(ref LoadedProject);
			Raylib.SetWindowSize((int)LoadedProject.width, (int)LoadedProject.height);

			projectloaded = projectpath != "";
			return LoadedProject;
		}

		projectloaded = false;
		return null;
	}

	public string GetAbsolutePath(string path)
	{
		return projectpath + Path.DirectorySeparatorChar + path;
	}
}
