#nullable disable

using Emuratch.Core.Scratch;
using Emuratch.Core.Turbowarp;
using Emuratch.Core.vm;
using Emuratch.Render;
using Raylib_cs;
using System.IO.Compression;
using System.IO;
using System;
using System.Collections.Generic;
using Emuratch.Core.Render;
using Emuratch.UI.Crossplatform;

namespace Emuratch;

public class Application
{
	public static Project project { get; private set; }
	public static string projectpath { get; private set; } = "";
	public static bool projectloaded { get; private set; }
	public static bool projectloading { get; private set; }

	public List<Thread> threads { get; set; } = [];

	public enum Runners
	{
		Interpreter,
		JITCompilier
	}

	public enum Renders
	{
		Null,
		Raylib
	}

	public Runners runnertype = Runners.Interpreter;
	public Renders rendertype = Renders.Raylib;

	internal static IRender render;
	internal static IRunner runner;

	public static bool debug = false;
	public static bool disablerender = false;

	public void UnloadProject()
	{
		render.Unload();
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
		runner.InvokeEvent(Block.Opcodes.event_whenkeypressed);

		runner.timer += (float)runner.deltatime;

		if (threads != null)
		{
			threads.Sort((a, b) => a.sprite.layoutOrder.CompareTo(b.sprite.layoutOrder));
			// threads.ForEach( t => t.Step() );
			// This throws InvaildOperationException
			// So use "for" instead
			for (int i = 0; i < threads.Count; i++)
			{
				threads[i].Step();
			}
		}
	}

	public void OnUpdate()
	{
		if (projectloading)
		{
			projectloading = false;
			var loaded = LoadProject();
			if (loaded != null)
			{
				project = loaded;
				render?.Unload();
				render = rendertype switch
				{
					Renders.Raylib => new RaylibRender(project),
					_ => new NullRender(),
				};

				runner = runnertype switch
				{
					_ => new Interpreter(project, render)
				};
				runner.fps = Configuration.Config.framerate;
			}
		}

		if (Raylib.IsKeyPressed(KeyboardKey.F1))
		{
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
			if (Raylib.IsKeyPressed(KeyboardKey.Pause))
			{
				runner.paused = !runner.paused;
			}

			if (Raylib.IsKeyPressed(KeyboardKey.F5))
			{
				runner.timer = 0;
				threads.Clear();
				threads = runner.InvokeEvent(Block.Opcodes.event_whenflagclicked);
				project.clones.Clear();
				Raylib.SetTargetFPS(runner.paused ? int.MaxValue : runner.fps);
			}

			if (Raylib.IsKeyPressed(KeyboardKey.F6))
			{
				threads.Clear();
				Raylib.SetTargetFPS(0);
			}

			if (Raylib.IsKeyPressed(KeyboardKey.F3))
			{
				debug = !debug;
			}

			if (Raylib.IsKeyPressed(KeyboardKey.F4))
			{
				disablerender = !disablerender;
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

		try
		{
			Project LoadedProject = Project.LoadProject(jsonpath);
			LoadedProject.runner = runner;
			Configuration.ApplyConfig(ref LoadedProject);
			Raylib.SetWindowSize((int)LoadedProject.width, (int)LoadedProject.height);

			projectloaded = projectpath != "";
			return LoadedProject;
		}
		catch (Exception)
		{
			projectloaded = false;
			return null;
		}
	}

	public string GetAbsolutePath(string path)
	{
		return projectpath + Path.DirectorySeparatorChar + path;
	}
}
