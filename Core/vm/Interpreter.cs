using Emuratch.Core.Overlay;
using Emuratch.Core.Render;
using Emuratch.Core.Scratch;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Emuratch.Core.vm;

public class Interpreter : IRunner
{
	const float DegToRad = MathF.PI / 180;
	const float MoveMultiplier = 1f;

	public Interpreter(Project project)
	{
		this.project = project;
		rng = new();

		eventBlocks = [];

		foreach (var spr in Application.project.sprites)
		{
			foreach (var block in spr.blocks)
			{
				if (block.Value.opcode.ToString()[..5] == "event") //[..5] equal to Substring(0, 5)
				{
					if (!eventBlocks.ContainsKey(block.Value.opcode))
						eventBlocks.Add(block.Value.opcode, []);

					eventBlocks[block.Value.opcode].Add(block.Value);
				}
			}
		}

		foreach (var sprite in project.sprites)
		{
			sprite.UpdateBlocks();
		}
	}

	public Project project { get; set; }

	public bool TAS { get; set; }
	public bool paused { get; set; }

	public int fps { get; set; }
	public float Deltatime { get => (float)(1d / fps); }
	public Random rng { get; set; }

	public float timer { get; set; }

	public Vector2 mouse {
		get {
			Vector2 inverted = Raylib.GetMousePosition() - new Vector2(Raylib.GetScreenWidth() * 0.5f, Raylib.GetScreenHeight() * 0.5f);
			return new(inverted.X, -inverted.Y);
		}
	}
	public Vector2 tasmouse { get; set; }
	public Vector2 mousepos { get => TAS ? tasmouse : mouse; }

	static readonly Dictionary<Block.Opcodes, Operation> operations = new()
	{
		{
			Block.Opcodes.motion_movesteps,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				thread.sprite.x += MoveMultiplier * StrNumber(thread.block.inputs[0].value) * MathF.Sin(thread.sprite.direction * DegToRad);
				thread.sprite.y += MoveMultiplier * StrNumber(thread.block.inputs[0].value) * MathF.Cos(thread.sprite.direction * DegToRad);
				interpreter.ClampToStage(thread.sprite);
				return null;
			}
		},
		{
			Block.Opcodes.motion_turnright,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				thread.sprite.direction += StrNumber(thread.block.inputs[0].value) * 2;
				return null;
			}
		},
		{
			Block.Opcodes.motion_turnleft,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				thread.sprite.direction -= StrNumber(thread.block.inputs[0].value) * 2;
				return null;
			}
		},
		{
			Block.Opcodes.motion_goto,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				Vector2 pos;

				if (thread.block.inputs[0].value == "_random_")
				{
					pos = new(
						interpreter.rng.Next((int)(Application.project.width * -0.5f), (int)(Application.project.width * 0.5f)),
						interpreter.rng.Next((int)(Application.project.height * -0.5f), (int)(Application.project.height * 0.5f))
					);
				}
				else if (thread.block.inputs[0].value == "_mouse_")
				{
					pos = interpreter.mousepos;
				}
				else
				{
					string destinationstr = thread.block.inputs[0].value;
					Sprite destination = Application.project.sprites.First(spr => spr.name == destinationstr);
					pos = new(destination.x, destination.y);
				}

				thread.sprite.x = pos.X;
				thread.sprite.y = pos.Y;
				interpreter.ClampToStage(thread.sprite);
				return null;
			}
		},
		{
			Block.Opcodes.motion_goto_menu,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return thread.block.fields[0];
			}
		},
		{
			Block.Opcodes.motion_gotoxy,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				thread.sprite.x = StrNumber(thread.block.inputs[0].value);
				thread.sprite.y = StrNumber(thread.block.inputs[1].value);
				interpreter.ClampToStage(thread.sprite);
				return null;
			}
		},
		{
			Block.Opcodes.motion_glideto,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				thread.delay = StrNumber(thread.block.inputs[0].value);

				Vector2 pos;

				if (thread.block.inputs[0].value == "_random_")
				{
					pos = new(
						interpreter.rng.Next((int)(Application.project.width * -0.5f), (int)(Application.project.width * 0.5f)),
						interpreter.rng.Next((int)(Application.project.height * -0.5f), (int)(Application.project.height * 0.5f))
					);
				}
				else if (thread.block.inputs[0].value == "_mouse_")
				{
					pos = interpreter.mousepos;
				}
				else
				{
					string destinationstr = thread.block.inputs[0].value;
					Sprite destination = Application.project.sprites.First(spr => spr.name == destinationstr);
					pos = new(destination.x, destination.y);
				}

				thread.sprite.x = pos.X;
				thread.sprite.y = pos.Y;
				interpreter.ClampToStage(thread.sprite);
				return null;
			}
		},
		{
			Block.Opcodes.motion_glideto_menu,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return thread.block.fields[0];
			}
		},
		{
			Block.Opcodes.motion_glidesecstoxy,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				thread.sprite.x += StrNumber(thread.block.inputs[1].value);
				thread.sprite.y += StrNumber(thread.block.inputs[2].value);
				interpreter.ClampToStage(thread.sprite);
				return null;
			}
		},
		{
			Block.Opcodes.motion_pointindirection,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				thread.sprite.direction = StrNumber(thread.block.inputs[0].value);
				return "0";
			}
		},
		{
			Block.Opcodes.motion_pointtowards,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				Vector2 pos = Vector2.Zero;

				if (thread.block.inputs[0].value == "_mouse_")
				{
					pos = interpreter.mousepos;
					Emurender.dbginfo = $"{interpreter.mousepos}, {Raylib.GetMousePosition()}, {Raylib.GetScreenWidth()}x{Raylib.GetScreenHeight()}";
				}
				else if (thread.block.inputs[0].value == "_random_")
				{
					thread.sprite.direction = interpreter.rng.Next(-180, 180);
					return null;
				}
				else
				{
					try
					{
						string destinationstr = thread.block.inputs[0].value;
						Sprite destination = Application.project.sprites.First(spr => spr.name == destinationstr);
						pos = new(destination.x, destination.y);
					}
					catch (Exception ex)
					{
						if (ex.Message != "Sequence contains no matching element") throw;
					}
				}

				thread.sprite.direction = MathF.Atan2(pos.X - thread.sprite.x, pos.Y - thread.sprite.y) * (180 / MathF.PI);
				if (thread.sprite.direction > 180) thread.sprite.direction -= 360;
				if (thread.sprite.direction < -180) thread.sprite.direction += 360;
				return null;
			}
		},
		{
			Block.Opcodes.motion_pointtowards_menu,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return thread.block.fields[0];
			}
		},
		{
			Block.Opcodes.motion_changexby,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				thread.sprite.x += StrNumber(thread.block.inputs[0].value);
				interpreter.ClampToStage(thread.sprite);
				return null;
			}
		},
		{
			Block.Opcodes.motion_setx,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				thread.sprite.x = StrNumber(thread.block.inputs[0].value);
				interpreter.ClampToStage(thread.sprite);
				return null;
			}
		},
		{
			Block.Opcodes.motion_changeyby,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				thread.sprite.y += StrNumber(thread.block.inputs[0].value);
				interpreter.ClampToStage(thread.sprite);
				return null;
			}
		},
		{
			Block.Opcodes.motion_sety,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				thread.sprite.y = StrNumber(thread.block.inputs[0].value);
				interpreter.ClampToStage(thread.sprite);
				return null;
			}
		},
		{
			Block.Opcodes.motion_ifonedgebounce,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.motion_setrotationstyle,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				thread.sprite.rotationStyle = thread.block.fields[0];
				return null;
			}
		},
		{
			Block.Opcodes.motion_xposition,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return thread.sprite.x.ToString();
			}
		},
		{
			Block.Opcodes.motion_yposition,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return thread.sprite.y.ToString();
			}
		},
		{
			Block.Opcodes.motion_direction,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return thread.sprite.direction.ToString();
			}
		},
		{
			Block.Opcodes.looks_sayforsecs,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				float sec = StrNumber(thread.block.inputs[0].value);
				if (sec > 0)
				{
					thread.delay = sec;
				}
				else
				{
					thread.nextframe = false;
				}
				return null;
			}
		},
		{
			Block.Opcodes.looks_say,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				OverlayRender.RenderDialogue(-(int)thread.sprite.x, -(int)thread.sprite.y - thread.sprite.costume.image.Height, thread.block.inputs[0].value);
				return null;
			}
		},
		{
			Block.Opcodes.looks_thinkforsecs,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				float sec = StrNumber(thread.block.inputs[0].value);
				if (sec > 0)
				{
					thread.delay = sec;
				}
				else
				{
					thread.nextframe = false;
				}
				return null;
			}
		},
		{
			Block.Opcodes.looks_think,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				OverlayRender.RenderDialogue((int)thread.sprite.x, (int)thread.sprite.y + thread.sprite.costume.image.Height, thread.block.inputs[0].value);
				return null;
			}
		},
		{
			Block.Opcodes.looks_switchcostumeto,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				try
				{
					string costumestr = thread.block.inputs[0].value;
					thread.sprite.costume = thread.sprite.costumes.First(x => x.name == costumestr);
				}
				catch (Exception ex)
				{
					if (ex.GetType() != typeof(InvalidOperationException))
					{
						throw;
					}
					else if (StrNumber(thread.block.inputs[0].value, out var id))
					{
						thread.sprite.currentCostume = id;
					}
				}

				return null;
			}
		},
		{
			Block.Opcodes.looks_costume,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				string costume = thread.block.fields[0];
				int index =
					int.TryParse(thread.block.fields[0], out int number) ?
					number :
					thread.sprite.costumes.ToList().IndexOf(thread.sprite.costumes.First(x => x.name == costume));

				return index.ToString();
			}
		},
		{
			Block.Opcodes.looks_nextcostume,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				thread.sprite.currentCostume++;
				//add codes to repeat costume
				return null;
			}
		},
		{
			Block.Opcodes.looks_switchbackdropto,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.looks_backdrops,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return project.stage.currentCostume.ToString(); //Need check if it returns id or name
			}
		},
		{
			Block.Opcodes.looks_nextbackdrop,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.looks_changesizeby,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				thread.sprite.size += StrNumber(thread.block.inputs[0].value);
				return null;
			}
		},
		{
			Block.Opcodes.looks_setsizeto,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				thread.sprite.size = StrNumber(thread.block.inputs[0].value);
				return null;
			}
		},
		{
			Block.Opcodes.looks_changeeffectby,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.looks_seteffectto,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.looks_cleargraphiceffects,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.looks_show,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				thread.sprite.visible = true;
				return null;
			}
		},
		{
			Block.Opcodes.looks_hide,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				thread.sprite.visible = false;
				return null;
			}
		},
		{
			Block.Opcodes.looks_gotofrontback,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				thread.sprite.SetLayoutOrder(thread.block.fields[0] == "front" ? project.sprites.Length : 0);
				return null;
			}
		},
		{
			Block.Opcodes.looks_goforwardbackwardlayers,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				thread.sprite.SetLayoutOrder(
					thread.sprite.layoutOrder + int.Parse(thread.block.inputs[0].value) * (thread.block.fields[0] == "forward" ? 1 : -1)
				);
				return null;
			}
		},
		{
			Block.Opcodes.looks_costumenumbername,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return thread.block.fields[0] == "number" ? thread.sprite.currentCostume.ToString() : thread.sprite.costume.name;
			}
		},
		{
			Block.Opcodes.looks_backdropnumbername,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return thread.block.fields[0] == "number" ? project.stage.currentCostume.ToString() : project.stage.costume.name;
			}
		},
		{
			Block.Opcodes.looks_size,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return thread.sprite.size.ToString();
			}
		},
		{
			Block.Opcodes.sound_playuntildone,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				string sound = thread.block.inputs[0].value;
				Raylib.PlaySound(thread.sprite.sounds.First(x => x.name == sound).sound);
				return null;
			}
		},
		{
			Block.Opcodes.sound_sounds_menu,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return thread.block.fields[0];
			}
		},
		{
			Block.Opcodes.sound_play,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				string sound = thread.block.inputs[0].value;
				Raylib.PlaySound(thread.sprite.sounds.First(x => x.name == sound).sound);
				return null;
			}
		},
		{
			Block.Opcodes.sound_stopallsounds,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.sound_changeeffectby,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.sound_seteffectto,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.sound_cleareffects,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.sound_changevolumeby,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.sound_setvolumeto,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				thread.sprite.volume = StrNumber(thread.block.inputs[0].value);
				return null;
			}
		},
		{
			Block.Opcodes.sound_volume,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return thread.sprite.volume.ToString();
			}
		},
		{
			Block.Opcodes.event_whenflagclicked,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.event_whenkeypressed,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				if (thread.block.fields[0] == "any") return Boolstr(Raylib.GetKeyPressed() > 0);

				KeyboardKey key = interpreter.StrKey(thread.block.fields[0]);
				if (Raylib.IsKeyPressedRepeat(key) || Raylib.IsKeyPressed(key))
				{
					return null;
				}
				else
				{
					return null;
				}
			}
		},
		{
			Block.Opcodes.event_whenthisspriteclicked,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.event_whenstageclicked,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.event_whenbackdropswitchesto,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.event_whengreaterthan,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.event_whenbroadcastreceived,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.event_broadcast,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				foreach (Sprite spr in project.sprites)
				{
					foreach (var top in spr.blocks.Where(x => x.Value.opcode == Block.Opcodes.event_whenbroadcastreceived))
					{
						interpreter.Execute(spr, top.Value);
					}
				}
				return null;
			}
		},
		{
			Block.Opcodes.event_broadcastandwait,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				foreach (var spr in project.sprites)
				{
					foreach (var top in thread.sprite.blocks.Where(x => x.Value.opcode == Block.Opcodes.event_whenbroadcastreceived))
					{
						interpreter.Execute(thread.sprite, top.Value);
					}
				}
				return null;
			}
		},
		{
			Block.Opcodes.control_wait,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				float sec = StrNumber(thread.block.inputs[0].value);
				if (sec > 0)
				{
					thread.delay = sec;
				}
				else
				{
					thread.nextframe = false;
				}
				
				return null;
			}
		},
		{
			Block.Opcodes.control_repeat,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				thread.block = thread.sprite.blocks[thread.block.inputs[1].RawValue];
				thread.returnto.Add(new(thread.block, int.Parse(thread.block.inputs[0].value)));
				return null;
			}
		},
		{
			Block.Opcodes.control_forever,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				thread.block = thread.sprite.blocks[thread.block.inputs[0].RawValue];
				thread.returnto.Add(new(thread.block));
				return null;
			}
		},
		{
			Block.Opcodes.control_if,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				if (Strbool(thread.block.inputs[0].value))
				{
					thread.block = thread.sprite.blocks[thread.block.inputs[1].RawValue];
					interpreter.Execute(ref thread);
				}

				return null;
			}
		},
		{
			Block.Opcodes.control_if_else,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				if (Strbool(thread.block.inputs[2].value))
				{
					thread.block = thread.sprite.blocks[thread.block.inputs[0].RawValue];
					interpreter.Execute(ref thread);
				}
				else
				{
					thread.block = thread.sprite.blocks[thread.block.inputs[1].RawValue];
					interpreter.Execute(ref thread);
				}

				return null;
			}
		},
		{
			Block.Opcodes.control_wait_until,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.control_repeat_until,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.control_while,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.control_stop,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.control_create_clone_of,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				string clonemenu = thread.block.inputs[0].value;
				project.clones.Add(Sprite.Clone(project.sprites.First(x => x.name == clonemenu)));
				return null;
			}
		},
		{
			Block.Opcodes.control_create_clone_of_menu,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return thread.block.fields[0] == "_myself_" ? thread.sprite.name : thread.block.fields[0];
			}
		},
		{
			Block.Opcodes.control_delete_this_clone,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				if (!thread.sprite.isClone) return null;

				project.clones.Remove(thread.sprite);
				return null;
			}
		},
		{
			Block.Opcodes.sensing_touchingobject,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				string sprite = thread.block.inputs[0].value;
				if (!project.sprites.Any(x => x.name == sprite)) return "false";
				Sprite target = project.sprites.First(x => x.name == sprite);
				if (!CheckBoundingBoxOverlap(thread.sprite, target)) return "false";
				if (!CheckPixelOverlap(thread.sprite, target)) return "false";
				return "true";
			}
		},
		{
			Block.Opcodes.sensing_touchingobjectmenu,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return thread.block.fields[0];
			}
		},
		{
			Block.Opcodes.sensing_touchingcolor,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				for (int x = 0; x < thread.sprite.costume.image.Width; x++)
				{
					for (int y = 0; y < thread.sprite.costume.image.Height; y++)
					{
						if (thread.sprite.costume.GetColor(x, y).A != 0)
						{
							Color color = Application.render.GetColorOnPixel(x, y);
							if (thread.block.inputs[0].value == $"#{color.R:X2}{color.G:X2}{color.B:X2}")
							{
								return "true";
							}
						}
					}
				}
				return "false";
			}
		},
		{
			Block.Opcodes.sensing_coloristouchingcolor,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.sensing_distanceto,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.sensing_distancetomenu,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.sensing_askandwait,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.sensing_answer,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.sensing_keypressed,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				if (thread.block.inputs[0].value == "any") return Boolstr(Raylib.GetKeyPressed() > 0);

				if (interpreter.StrKey(thread.block.inputs[0].value) == KeyboardKey.Null && thread.block.inputs[0].value.Length > 1)
				{
					return Boolstr(Raylib.IsKeyDown(interpreter.StrKey(thread.block.inputs[0].value[0].ToString())));
				}

				return Boolstr(Raylib.IsKeyDown(interpreter.StrKey(thread.block.inputs[0].value)));
			}
		},
		{
			Block.Opcodes.sensing_keyoptions,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return thread.block.fields[0];
			}
		},
		{
			Block.Opcodes.sensing_mousedown,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return Boolstr(Raylib.IsMouseButtonDown(MouseButton.Left));
			}
		},
		{
			Block.Opcodes.sensing_mousex,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return Raylib.GetMouseX().ToString();
			}
		},
		{
			Block.Opcodes.sensing_mousey,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return Raylib.GetMouseY().ToString();
			}
		},
		{
			Block.Opcodes.sensing_setdragmode,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.sensing_loudness,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return "0";
			}
		},
		{
			Block.Opcodes.sensing_timer,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return interpreter.timer.ToString();
			}
		},
		{
			Block.Opcodes.sensing_resettimer,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				interpreter.timer = 0;
				return null;
			}
		},
		{
			Block.Opcodes.sensing_of,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.sensing_of_object_menu,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.sensing_current,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.sensing_dayssince2000,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.sensing_username,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return "USERNAME";
			}
		},
		{
			Block.Opcodes.operator_add,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return (StrNumber(thread.block.inputs[0].value) + StrNumber(thread.block.inputs[1].value)).ToString();
			}
		},
		{
			Block.Opcodes.operator_subtract,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return (StrNumber(thread.block.inputs[0].value) - StrNumber(thread.block.inputs[1].value)).ToString();
			}
		},
		{
			Block.Opcodes.operator_multiply,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return (StrNumber(thread.block.inputs[0].value) * StrNumber(thread.block.inputs[1].value)).ToString();
			}
		},
		{
			Block.Opcodes.operator_divide,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return (StrNumber(thread.block.inputs[0].value) / StrNumber(thread.block.inputs[1].value)).ToString();
			}
		},
		{
			Block.Opcodes.operator_random,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				double number = interpreter.rng.NextDouble() + StrNumber(thread.block.inputs[0].value) * (StrNumber(thread.block.inputs[1].value) - StrNumber(thread.block.inputs[0].value));
				return number.ToString();
			}
		},
		{
			Block.Opcodes.operator_gt,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return Boolstr(StrNumber(thread.block.inputs[0].value) > StrNumber(thread.block.inputs[1].value));
			}
		},
		{
			Block.Opcodes.operator_lt,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return Boolstr(StrNumber(thread.block.inputs[0].value) < StrNumber(thread.block.inputs[1].value));
			}
		},
		{
			Block.Opcodes.operator_equals,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return Boolstr(thread.block.inputs[0].value.ToLower() == thread.block.inputs[1].value.ToLower());
			}
		},
		{
			Block.Opcodes.operator_and,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return Boolstr(Strbool(thread.block.inputs[0].value) && Strbool(thread.block.inputs[1].value));
			}
		},
		{
			Block.Opcodes.operator_or,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return Boolstr(Strbool(thread.block.inputs[0].value) || Strbool(thread.block.inputs[1].value));
			}
		},
		{
			Block.Opcodes.operator_not,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return Boolstr(!Strbool(thread.block.inputs[0].value));
			}
		},
		{
			Block.Opcodes.operator_join,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return thread.block.inputs[0].value + thread.block.inputs[1].value;
			}
		},
		{
			Block.Opcodes.operator_letter_of,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return thread.block.inputs[0].value[int.Parse(thread.block.inputs[1].value)].ToString();
			}
		},
		{
			Block.Opcodes.operator_length,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return thread.block.inputs[0].value.Length.ToString();
			}
		},
		{
			Block.Opcodes.operator_contains,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.operator_mod,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.operator_round,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.operator_mathop,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.data_variable,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				string variable = thread.block.fields[0];
				return project.stage.variables.First(x => x.name == variable).value.ToString() ?? "";
			}
		},
		{
			Block.Opcodes.data_setvariableto,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				string variable = thread.block.fields[0];
				project.stage.variables.First(x => x.name == variable).value = thread.block.inputs[0];
				return null;
			}
		},
		{
			Block.Opcodes.data_changevariableby,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.data_showvariable,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.data_hidevariable,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.data_listcontents,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.data_addtolist,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.data_deleteoflist,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.data_deletealloflist,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.data_insertatlist,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.data_replaceitemoflist,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.data_itemoflist,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.data_itemnumoflist,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.data_lengthoflist,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.data_listcontainsitem,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.data_showlist,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.data_hidelist,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.procedures_definition,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.procedures_prototype,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.procedures_call,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.argument_reporter_string_number,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
		{
			Block.Opcodes.argument_reporter_boolean,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return null;
			}
		},
	};

	Dictionary<Block.Opcodes, List<Block>> eventBlocks;

	delegate string? Operation(ref Thread thread, Project project, Interpreter interpreter);

	public List<Thread> InvokeEvent(Block.Opcodes opcode)
	{
		List<Thread> threads = [];

		if (!eventBlocks.TryGetValue(opcode, out _)) return new();

		foreach (var ev in eventBlocks[opcode])
		{
			Thread t = new(ev);
			threads.Add(t);
			Execute(ref t);
		}

		return threads;
	}

	public KeyboardKey StrKey(string str)
	{
		return str switch
		{
			"space" => KeyboardKey.Space,
			"left arrow" => KeyboardKey.Left,
			"right arrow" => KeyboardKey.Right,
			"up arrow" => KeyboardKey.Up,
			"down arrow" => KeyboardKey.Down,
			"enter" => KeyboardKey.Enter,
			"a" => KeyboardKey.A,
			"b" => KeyboardKey.B,
			"c" => KeyboardKey.C,
			"d" => KeyboardKey.D,
			"e" => KeyboardKey.E,
			"f" => KeyboardKey.F,
			"g" => KeyboardKey.G,
			"h" => KeyboardKey.H,
			"i" => KeyboardKey.I,
			"j" => KeyboardKey.J,
			"k" => KeyboardKey.K,
			"l" => KeyboardKey.L,
			"m" => KeyboardKey.M,
			"n" => KeyboardKey.N,
			"o" => KeyboardKey.O,
			"p" => KeyboardKey.P,
			"q" => KeyboardKey.Q,
			"r" => KeyboardKey.R,
			"s" => KeyboardKey.S,
			"t" => KeyboardKey.T,
			"u" => KeyboardKey.U,
			"v" => KeyboardKey.V,
			"w" => KeyboardKey.W,
			"x" => KeyboardKey.X,
			"y" => KeyboardKey.Y,
			"z" => KeyboardKey.Z,
			"0" => KeyboardKey.Zero,
			"1" => KeyboardKey.One,
			"2" => KeyboardKey.Two,
			"3" => KeyboardKey.Three,
			"4" => KeyboardKey.Four,
			"5" => KeyboardKey.Five,
			"6" => KeyboardKey.Six,
			"7" => KeyboardKey.Seven,
			"8" => KeyboardKey.Eight,
			"9" => KeyboardKey.Nine,
			"-" => KeyboardKey.Minus,
			"," => KeyboardKey.Comma,
			"." => KeyboardKey.Period,
			"`" => KeyboardKey.Grave,
			"=" => KeyboardKey.Equal,
			"[" => KeyboardKey.LeftBracket,
			"]" => KeyboardKey.RightBracket,
			"\\" => KeyboardKey.Backslash,
			";" => KeyboardKey.Semicolon,
			"'" => KeyboardKey.Apostrophe,
			"/" => KeyboardKey.Slash,
			//Using "join" block, we can do these tricks.
			"control" => KeyboardKey.LeftControl,
			"shift" => KeyboardKey.LeftShift,
			"backspace" => KeyboardKey.Backspace,
			"insert" => KeyboardKey.Insert,
			"page up" => KeyboardKey.PageUp,
			"page down" => KeyboardKey.PageDown,
			"end" => KeyboardKey.End,
			"home" => KeyboardKey.Home,
			"scroll lock" => KeyboardKey.ScrollLock,
			_ => KeyboardKey.Null
		};
	}

	void ClampToStage(Sprite spr)
	{
		ClampToStage(spr, project);
	}

	static void ClampToStage(Sprite spr, Project project)
	{
		float width = spr.costume.image.Width / 4f * spr.costume.bitmapResolution;
		float height = spr.costume.image.Height / 4f * spr.costume.bitmapResolution;

		spr.x = Math.Clamp(spr.x, project.width * -0.5f - width, project.width * 0.5f + width);
		spr.y = Math.Clamp(spr.y, project.height * -0.5f - height, project.height * 0.5f + height);
	}

	static string Boolstr(bool boolean)
	{
		return boolean ? "true" : "false";
	}

	static bool Strbool(string str)
	{
		return str == "true";
	}

	static dynamic? StrNumber(string str)
	{
		if (str == "Infinity") return double.MaxValue;
		if (str == "-Infinity") return double.MinValue;

		if (str == "true") return 1;
		if (str == "false") return 0;

		if (str.Contains('.'))
		{
			if (float.TryParse(str, out var num))
			{
				return num;
			}
		}
		else
		{
			if (int.TryParse(str, out var num))
			{
				return num;
			}
		}

		return null;
	}

	static bool StrNumber(string str, out dynamic value)
	{
		if (str == "Infinity") { value = double.MaxValue; return true; }
		if (str == "-Infinity") { value = double.MinValue; return true; }

		if (str == "true") { value = 1; return true; }
		if (str == "false") { value = 0; return false; }

		if (str.Contains('.'))
		{
			if (float.TryParse(str, out var num))
			{
				value = num;
				return true;
			}
		}
		else
		{
			if (int.TryParse(str, out var num))
			{
				value = num;
				return true;
			}
		}

		value = 0;
		return false;
	}

	public static bool CheckBoundingBoxOverlap(Sprite a, Sprite b)
	{
		return Raylib.CheckCollisionBoxes(
			a.RaylibBoundingBox,
			b.RaylibBoundingBox
		);
	}

	public static bool CheckPixelOverlap(Sprite a, Sprite b)
	{
		// Get overlap of 2 bounding boxes
		int startX = (int)Math.Max(a.RaylibBoundingBox.Min.X, b.RaylibBoundingBox.Min.X);
		int startY = (int)Math.Max(a.RaylibBoundingBox.Min.Y, b.RaylibBoundingBox.Min.Y);
		int endX = (int)Math.Min(a.RaylibBoundingBox.Max.X, b.RaylibBoundingBox.Max.X);
		int endY = (int)Math.Min(a.RaylibBoundingBox.Max.Y, b.RaylibBoundingBox.Max.Y);

		if (Application.debug && Program.app.rendertype == Application.Renders.Emurender)
		{
			Raylib.DrawRectangle(startX, startY, endX - startX, endY - startY, new(0, 255, 0, 100));
		}

		for (int x = startX; x < endX; x++)
		{
			for (int y = startY; y < endY; y++)
			{
				Vector2 apos = a.RaylibOrigin;
				Vector2 bpos = b.RaylibOrigin;

				int localXA = x - (int)apos.X;
				int localYA = y - (int)apos.Y;
				int localXB = x - (int)bpos.X;
				int localYB = y - (int)bpos.Y;

				Color pixelA = a.costume.GetColor(localXA, localYA);
				Color pixelB = b.costume.GetColor(localXB, localYB);

				if (Application.debug && Program.app.rendertype == Application.Renders.Emurender)
				{
					Vector2 localoffset = new(Raylib.GetRenderWidth() / 2, Raylib.GetRenderHeight() / 2);
					if (pixelA.A == 0)
					{
						Raylib.DrawPixel(localXA + (int)localoffset.X, localYA + (int)localoffset.Y, new(0, 0, 255, 127));
					}
					else
					{
						Raylib.DrawPixel(localXA + (int)localoffset.X, localYA + (int)localoffset.Y, new(pixelA.R, pixelA.G, pixelA.B, pixelA.A));
					}

					if (pixelB.A == 0)
					{
						Raylib.DrawPixel(localXB + (int)localoffset.X, localYB + (int)localoffset.Y, new(255, 0, 0, 127));
					}
					else
					{
						Raylib.DrawPixel(localXB + (int)localoffset.X, localYB + (int)localoffset.Y, new(pixelB.R, pixelB.G, pixelB.B, pixelB.A));
					}
				}

				// If Alpha is over 0 then return true as Scratch does.
				if (pixelA.A > 0 && pixelB.A > 0)
				{
					return true;
				}
			}
		}

		return false;
	}

	public string Execute(Sprite spr, Block block)
	{
		Thread thread = new(spr, block);
		return Execute(ref thread);
	}

	public string Execute(ref Thread thread)
	{
		if (!Application.projectloaded) return string.Empty;

		string? returnValue = operations[thread.block.opcode](ref thread, project, this);
		if (returnValue != null) return returnValue;

		if (!string.IsNullOrEmpty(thread.block.nextId))
		{
			thread.block = thread.block.Next(thread.sprite);
			return Execute(ref thread);
		}

		return string.Empty;
	}
}
