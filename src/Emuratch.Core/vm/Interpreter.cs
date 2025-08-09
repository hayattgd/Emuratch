using Emuratch.Core.Render;
using Emuratch.Core.Scratch;
using Emuratch.Core.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;

namespace Emuratch.Core.vm;

public class Interpreter : IRunner
{
	const double DegToRad = Math.PI / 180;
	const double RadToDeg = 180 / Math.PI;

	public Interpreter(Project project, IRender render)
	{
		this.render = render;
		this.project = project;
		threads = [];
		rng = new();

		eventBlocks = [];
		procedures = [];

		foreach (var spr in project.sprites)
		{
			foreach (var block in spr.blocks)
			{
				if (block.Value.opcode.ToString()[..5] == "event") //[..5] equal to Substring(0, 5)
				{
					if (!eventBlocks.ContainsKey(block.Value.opcode))
						eventBlocks.Add(block.Value.opcode, []);

					eventBlocks[block.Value.opcode].Add(block.Value);
				}

				if (block.Value.opcode == Block.Opcodes.procedures_definition)
				{
					string proccode = spr.blocks[block.Value.inputs[0].RawValue].mutation.proccode;
					if (procedures.ContainsKey(proccode)) { continue; }
					procedures.Add(proccode, block.Value);
				}
			}
		}

		foreach (var sprite in project.sprites)
		{
			sprite.UpdateBlocks();
		}
	}

	Dictionary<Block.Opcodes, List<Block>> eventBlocks;
	Dictionary<string, Block> procedures;

	public IRender render { get; }
	public Project project { get; set; }
	public List<Thread> threads { get; set; }

	public bool TAS { get; set; }
	public bool paused { get; set; }

	public int fps { get; set; }
	public Number Deltatime { get => (Number)(1d / fps); }
	public Random rng { get; set; }

	public Number timer { get; set; }

	public Vector2 mouse => render.MousePosition;
	public Vector2 tasmouse { get; set; }
	public Vector2 mousepos { get => TAS ? tasmouse : mouse; }

	public readonly struct ReturnValue
	{
		public ReturnValue()
		{
			value = "";
			interrupt = false;
		}

		public ReturnValue(string ret)
		{
			value = ret;
			interrupt = true;
		}

		public readonly string value;
		public readonly bool interrupt;
	}

	static readonly Dictionary<Block.Opcodes, Operation> operations = new()
	{
		{
			Block.Opcodes.motion_movesteps,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				Number x = thread.sprite.x + StrNumber(thread.block.inputs[0].value) * Math.Sin(thread.sprite.direction * DegToRad);
				Number y = thread.sprite.y + StrNumber(thread.block.inputs[0].value) * Math.Cos(thread.sprite.direction * DegToRad);
				thread.sprite.SetPosition(x, y);
				return null;
			}
		},
		{
			Block.Opcodes.motion_turnright,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				thread.sprite.SetRotation(thread.sprite.direction + StrNumber(thread.block.inputs[0].value));
				return null;
			}
		},
		{
			Block.Opcodes.motion_turnleft,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				thread.sprite.SetRotation(thread.sprite.direction -= StrNumber(thread.block.inputs[0].value));
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
						interpreter.rng.Next((int)(project.width * -0.5f), (int)(project.width * 0.5f)),
						interpreter.rng.Next((int)(project.height * -0.5f), (int)(project.height * 0.5f))
					);
				}
				else if (thread.block.inputs[0].value == "_mouse_")
				{
					pos = interpreter.mousepos;
				}
				else
				{
					string destinationstr = thread.block.inputs[0].value;
					Sprite destination = project.sprites.First(spr => spr.name == destinationstr);
					pos = new(destination.x, destination.y);
				}

				thread.sprite.SetPosition(pos.X, pos.Y);
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
				thread.sprite.SetPosition(StrNumber(thread.block.inputs[0].value),  StrNumber(thread.block.inputs[1].value));
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
						interpreter.rng.Next((int)(project.width * -0.5f), (int)(project.width * 0.5f)),
						interpreter.rng.Next((int)(project.height * -0.5f), (int)(project.height * 0.5f))
					);
				}
				else if (thread.block.inputs[0].value == "_mouse_")
				{
					pos = interpreter.mousepos;
				}
				else
				{
					string destinationstr = thread.block.inputs[0].value;
					Sprite destination = project.sprites.First(spr => spr.name == destinationstr);
					pos = new(destination.x, destination.y);
				}

				thread.sprite.SetPosition(pos.X, pos.Y);
				return "";
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
				thread.sprite.SetPosition(StrNumber(thread.block.inputs[1].value), StrNumber(thread.block.inputs[2].value));
				return "";
			}
		},
		{
			Block.Opcodes.motion_pointindirection,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				thread.sprite.SetRotation(StrNumber(thread.block.inputs[0].value));
				return null;
			}
		},
		{
			Block.Opcodes.motion_pointtowards,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				Vector2 pos = Vector2.Zero;

				if (thread.block.inputs[0].value == "_mouse_")
				{
					pos = interpreter.mousepos;
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
						Sprite destination = project.sprites.First(spr => spr.name == destinationstr);
						pos = new(destination.x, destination.y);
					}
					catch (Exception ex)
					{
						if (ex.Message != "Sequence contains no matching element") throw;
					}
				}

				thread.sprite.SetRotation(MathF.Atan2(pos.X - thread.sprite.x, pos.Y - thread.sprite.y) * (180 / MathF.PI));
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
				thread.sprite.SetPosition(thread.sprite.x + StrNumber(thread.block.inputs[0].value), thread.sprite.y);
				return null;
			}
		},
		{
			Block.Opcodes.motion_setx,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				thread.sprite.SetPosition(StrNumber(thread.block.inputs[0].value), thread.sprite.y);
				return null;
			}
		},
		{
			Block.Opcodes.motion_changeyby,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				thread.sprite.SetPosition(thread.sprite.x, thread.sprite.y + StrNumber(thread.block.inputs[0].value));
				return null;
			}
		},
		{
			Block.Opcodes.motion_sety,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				thread.sprite.SetPosition(thread.sprite.x, StrNumber(thread.block.inputs[0].value));
				return null;
			}
		},
		{
			Block.Opcodes.motion_ifonedgebounce,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				double angle = thread.sprite.direction;

				double radians = (90 - angle) * DegToRad;

				double dx = Math.Cos(radians);
				double dy = Math.Sin(radians);
				float halfwidth = project.width / 2;
				float halfheight = project.height / 2;

				if (thread.sprite.boundingBox.Min.Y > halfheight || thread.sprite.boundingBox.Max.Y < -halfheight)
				{
					dy = -dy;
					thread.sprite.ClampInsideStage(project.width, project.height);
				}
				else if (thread.sprite.boundingBox.Min.X < -halfwidth || thread.sprite.boundingBox.Max.X > halfwidth)
				{
					dx = -dx;
					thread.sprite.ClampInsideStage(project.width, project.height);
				}

				thread.sprite.direction = 90 - Math.Atan2(dy, dx) * RadToDeg;
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
				Number sec = StrNumber(thread.block.inputs[0].value);
				if (sec > 0)
				{
					thread.delay = sec;
					thread.sprite.dialog = new()
					{
						type = Sprite.DialogType.Say,
						text = thread.block.inputs[0].value,
						duration = sec,
						infinite = false
					};
				}
				else
				{
					thread.nextframe = false;
				}
				return "";
			}
		},
		{
			Block.Opcodes.looks_say,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				thread.sprite.dialog = new()
				{
					type = Sprite.DialogType.Say,
					text = thread.block.inputs[0].value,
					duration = 0,
					infinite = true
				};
				return null;
			}
		},
		{
			Block.Opcodes.looks_thinkforsecs,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				Number sec = StrNumber(thread.block.inputs[0].value);
				if (sec > 0)
				{
					thread.delay = sec;
					thread.sprite.dialog = new()
					{
						type = Sprite.DialogType.Think,
						text = thread.block.inputs[0].value,
						duration = sec,
						infinite = false
					};
				}
				else
				{
					thread.nextframe = false;
				}
				return "";
			}
		},
		{
			Block.Opcodes.looks_think,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				thread.sprite.dialog = new()
				{
					type = Sprite.DialogType.Think,
					text = thread.block.inputs[0].value,
					duration = 0,
					infinite = true
				};
				return null;
			}
		},
		{
			Block.Opcodes.looks_switchcostumeto,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				if (StrNumber(thread.block.inputs[0].value, out var id))
				{
					thread.sprite.SetCostume(id);
				}
				else {
					string costumestr = thread.block.inputs[0].value;
					thread.sprite.costume = thread.sprite.costumes.First(x => x.name == costumestr);
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
				thread.sprite.SetCostume(thread.sprite.currentCostume + 1);
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
				interpreter.render.PlaySound(thread.sprite.sounds.First(x => x.name == sound));
				return "";
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
				interpreter.render.PlaySound(thread.sprite.sounds.First(x => x.name == sound));
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
				if (thread.block.fields[0] == "any") return Boolstr(interpreter.render.IsAnyKeyDown());

				if (interpreter.render.IsKeyRepeated(thread.block.fields[0]) || interpreter.render.IsKeyPressedOnce(thread.block.fields[0]))
				{
					return null;
				}
				else
				{
					return "";
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
				Number sec = StrNumber(thread.block.inputs[0].value);
				if (sec > 0)
				{
					thread.delay = sec;
				}
				else
				{
					thread.nextframe = false;
				}

				return "";
			}
		},
		{
			Block.Opcodes.control_repeat,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				thread.block = thread.sprite.blocks[thread.block.inputs[1].RawValue];
				thread.returnto.Add(new(thread.block, int.Parse(thread.block.inputs[0].value)));
				return "";
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
					// thread.block = thread.sprite.blocks[thread.block.inputs[1].RawValue];
					// interpreter.Execute(ref thread);
					interpreter.Execute(thread.sprite, thread.sprite.blocks[thread.block.inputs[1].RawValue]);
				}

				return null;
			}
		},
		{
			Block.Opcodes.control_if_else,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				if (Strbool(thread.block.inputs[0].value))
				{
					thread.block = thread.sprite.blocks[thread.block.inputs[1].RawValue];
					interpreter.Execute(ref thread);
				}
				else
				{
					thread.block = thread.sprite.blocks[thread.block.inputs[2].RawValue];
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
				Sprite clone = Sprite.Clone(project.sprites.First(x => x.name == clonemenu));
				project.clones.Add(clone);
				foreach (var block in clone.blocks)
				{
					if (block.Value.opcode == Block.Opcodes.control_start_as_clone)
					{
						Thread t = new(clone, block.Value, interpreter);
						interpreter.threads.Add(thread);
						interpreter.Execute(ref thread);
					}
				}
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
				if (!interpreter.CheckPixelOverlap(thread.sprite, target)) return "false";
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
				for (int x = 0; x < thread.sprite.costume.Width; x++)
				{
					for (int y = 0; y < thread.sprite.costume.Height; y++)
					{
						if (interpreter.render.GetColorOnPixel(x, y).A != 0)
						{
							Color color = interpreter.render.GetColorOnPixel(x, y);
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
				if (thread.block.inputs[0].value == "any") return Boolstr(interpreter.render.IsAnyKeyDown());

				if (!interpreter.render.IsStringKey(thread.block.inputs[0].value) && thread.block.inputs[0].value.Length > 1)
				{
					return Boolstr(interpreter.render.IsKeyDown(thread.block.inputs[0].value));
				}

				return Boolstr(interpreter.render.IsKeyDown(thread.block.inputs[0].value));
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
				return Boolstr(interpreter.render.IsMouseDown());
			}
		},
		{
			Block.Opcodes.sensing_mousex,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return interpreter.mousepos.X.ToString();
			}
		},
		{
			Block.Opcodes.sensing_mousey,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				return interpreter.mousepos.Y.ToString();
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
				DateTime time = DateTime.Now;
				if (thread.block.inputs[0].value == "YEAR")
				{
					return time.Year.ToString();
				}
				else if (thread.block.inputs[0].value == "MONTH")
				{
					return time.Month.ToString();
				}
				else if (thread.block.inputs[0].value == "DATE")
				{
					return time.Day.ToString();
				}
				else if (thread.block.inputs[0].value == "DAYOFWEEK")
				{
					return ConvertDayOfWeek(time.DayOfWeek);
				}
				else if (thread.block.inputs[0].value == "HOUR")
				{
					return time.Hour.ToString();
				}
				else if (thread.block.inputs[0].value == "MINUTE")
				{
					return time.Minute.ToString();
				}
				else if (thread.block.inputs[0].value == "SECOND")
				{
					return time.Second.ToString();
				}
				return "0";
			}
		},
		{
			Block.Opcodes.sensing_dayssince2000,
			(ref Thread thread, Project project, Interpreter interpreter) => {
				const double msPerDay = 24 * 60 * 60 * 1000;
				DateTime start = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
				DateTime today = DateTime.UtcNow;
				double mSecsSinceStart = (today - start).TotalMilliseconds;
				return (mSecsSinceStart / msPerDay).ToString();
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
				if (interpreter.procedures.ContainsKey(thread.block.mutation.proccode))
				{
					interpreter.Execute(thread.sprite, interpreter.procedures[thread.block.mutation.proccode]);
				}
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

	delegate string? Operation(ref Thread thread, Project project, Interpreter interpreter);

	public List<Thread> InvokeEvent(Block.Opcodes opcode)
	{
		List<Thread> threads = [];

		if (!eventBlocks.TryGetValue(opcode, out _)) return new();

		foreach (var ev in eventBlocks[opcode])
		{
			Thread t = new(ev, this);
			threads.Add(t);
			Execute(ref t);
		}

		return threads;
	}

	public static string ConvertDayOfWeek(DayOfWeek day)
	{
		return day switch
		{
			DayOfWeek.Sunday => "1",
			DayOfWeek.Monday => "2",
			DayOfWeek.Tuesday => "3",
			DayOfWeek.Wednesday => "4",
			DayOfWeek.Thursday => "5",
			DayOfWeek.Friday => "6",
			DayOfWeek.Saturday => "7",
			_ => "0"
		};
	}

	public static string Boolstr(bool boolean)
	{
		return boolean ? "true" : "false";
	}

	public static bool Strbool(string str)
	{
		return str == "true";
	}

	public static Number StrNumber(string str) => (Number)str;

	public static bool StrNumber(string str, out Number value)
	{
		if (str == "Infinity") { value = double.MaxValue; return true; }
		if (str == "-Infinity") { value = double.MinValue; return true; }

		if (str == "true") { value = 1; return true; }
		if (str == "false") { value = 0; return false; }

		if (str.Contains('.'))
		{
			if (Number.TryParse(str, out var num))
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

		value = new(0);
		return false;
	}

	public static bool CheckBoundingBoxOverlap(Sprite a, Sprite b)
	{
		return !(
			   a.boundingBox.Max.X < b.boundingBox.Min.X
			|| a.boundingBox.Min.X > b.boundingBox.Max.X
			|| a.boundingBox.Min.Y < b.boundingBox.Max.Y
			|| a.boundingBox.Max.Y > b.boundingBox.Min.Y
			);
	}

	public bool CheckPixelOverlap(Sprite a, Sprite b)
	{
		// Matrix3x2 transformA = Matrix3x2.CreateScale(a.size / 100f) * Matrix3x2.CreateRotation((float)((90 - a.direction) * DegToRad)) * Matrix3x2.CreateTranslation(a.x, a.y);
		// Matrix3x2 transformB = Matrix3x2.CreateScale(b.size / 100f) * Matrix3x2.CreateRotation((float)((90 - b.direction) * DegToRad)) * Matrix3x2.CreateTranslation(b.x, b.y);
		// Vector2 rotCenterA = new Vector2(a.costume.rotationCenterX, a.costume.rotationCenterY);
		// Vector2[] localCornersA = { new Vector2(-rotCenterA.X, rotCenterA.Y), new Vector2(a.costume.Width - rotCenterA.X, rotCenterA.Y), new Vector2(a.costume.Width - rotCenterA.X, -(a.costume.Height - rotCenterA.Y)), new Vector2(-rotCenterA.X, -(a.costume.Height - rotCenterA.Y)) };
		// Vector2[] worldCornersA = new Vector2[localCornersA.Length];
		// for (int i = 0; i < localCornersA.Length; i++)
		// {
		// 	worldCornersA[i] = Vector2.Transform(localCornersA[i], transformA);
		// }
		// Vector2 rotCenterB = new Vector2(b.costume.rotationCenterX, b.costume.rotationCenterY);
		// Vector2[] localCornersB = { new Vector2(-rotCenterB.X, rotCenterB.Y), new Vector2(b.costume.Width - rotCenterB.X, rotCenterB.Y), new Vector2(b.costume.Width - rotCenterB.X, -(b.costume.Height - rotCenterB.Y)), new Vector2(-rotCenterB.X, -(b.costume.Height - rotCenterB.Y)) };
		// Vector2[] worldCornersB = new Vector2[localCornersB.Length];
		// for (int i = 0; i < localCornersB.Length; i++)
		// {
		// 	worldCornersB[i] = Vector2.Transform(localCornersB[i], transformB);
		// }
		// Vector2 aMin = new(worldCornersA.Min(v => v.X), worldCornersA.Max(v => v.Y));
		// Vector2 aMax = new(worldCornersA.Max(v => v.X), worldCornersA.Min(v => v.Y));
		// render.DrawRectangle((int)aMin.X, (int)aMin.Y, (int)(aMax.X - aMin.X), (int)(aMax.Y - aMin.Y), Color.Blue);

		// Vector2 bMin = new(worldCornersB.Min(v => v.X), worldCornersB.Max(v => v.Y));
		// Vector2 bMax = new(worldCornersB.Max(v => v.X), worldCornersB.Min(v => v.Y));
		// render.DrawRectangle((int)bMin.X, (int)bMin.Y, (int)(bMax.X - bMin.X), (int)(bMax.Y - bMin.Y), Color.Red);
		// float overlapStartX = Math.Max(worldCornersA.Min(v => v.X), worldCornersB.Min(v => v.X));
		// float overlapEndX = Math.Min(worldCornersA.Max(v => v.X), worldCornersB.Max(v => v.X));
		// float overlapStartY = Math.Max(worldCornersA.Min(v => v.Y), worldCornersB.Min(v => v.Y));
		// float overlapEndY = Math.Min(worldCornersA.Max(v => v.Y), worldCornersB.Max(v => v.Y));
		// if (overlapStartX >= overlapEndX || overlapStartY >= overlapEndY)
		// {
		// 	return false;
		// }
		// for (int y = (int)overlapStartY; y < (int)overlapEndY; y++)
		// {
		// 	for (int x = (int)overlapStartX; x < (int)overlapEndX; x++)
		// 	{
		// 		var pixelA = render.GetColorOnPixel(a, x, y);
		// 		if (pixelA.HasValue && pixelA.Value.A > 0)
		// 		{
		// 			var pixelB = render.GetColorOnPixel(b, x, y);
		// 			if (pixelB.HasValue && pixelB.Value.A > 0)
		// 			{
		// 				return true;
		// 			}
		// 		}
		// 	}
		// }
		// return false;
		
		// Adjust origin to Left-Top
		var abounding = new BoundingBox(
			new(
				project.width / 2f + a.boundingBox.Min.X,
				project.height / 2f - a.boundingBox.Min.Y
			),
			new(
				project.width / 2f + a.boundingBox.Max.X,
				project.height / 2f - a.boundingBox.Max.Y
			)
		);
		var bbounding = new BoundingBox(
			new(
				project.width / 2f + b.boundingBox.Min.X,
				project.height / 2f - b.boundingBox.Min.Y
			),
			new(
				project.width / 2f + b.boundingBox.Max.X,
				project.height / 2f - b.boundingBox.Max.Y
			)
		);

		// Get overlap of 2 bounding boxes
		int startX = (int)Math.Max(a.boundingBox.Min.X, b.boundingBox.Min.X);
		int startY = (int)Math.Min(a.boundingBox.Min.Y, b.boundingBox.Min.Y);
		int endX = (int)Math.Min(a.boundingBox.Max.X, b.boundingBox.Max.X);
		int endY = (int)Math.Max(a.boundingBox.Max.Y, b.boundingBox.Max.Y);

		// int startX = (int)Math.Min(a.boundingBox.Min.X, b.boundingBox.Min.X);
		// int startY = (int)Math.Max(a.boundingBox.Min.Y, b.boundingBox.Min.Y);
		// int endX = (int)Math.Max(a.boundingBox.Max.X, b.boundingBox.Max.X);
		// int endY = (int)Math.Min(a.boundingBox.Max.Y, b.boundingBox.Max.Y);

		if (project.debug)
		{
			render.DrawRectangle(startX, startY, endX - startX, startY - endY, Color.Red);
			render.DrawPoint(startX, startY, Color.Green);
			render.DrawPoint(endX, endY, Color.Blue);
		}
		
		for (int x = startX; x < endX; x++)
		{
			for (int y = startY; y > endY; y--)
			{
				Color? pixelA = render.GetColorOnPixel(a, x, y);
				Color? pixelB = render.GetColorOnPixel(b, x, y);

				if (pixelA == null || pixelB == null)
				{
					continue;
				}

				if (project.debug)
				{
					if (pixelA.Value.A > 0)
					{
						render.DrawPixel(x, y, pixelA.Value);
					}

					if (pixelB.Value.A > 0)
					{
						render.DrawPixel(x, y, pixelB.Value);
					}
				}

				if (pixelA.Value.A > 0 && pixelB.Value.A > 0)
					{
						return true;
					}
			}
		}

		return false;
	}

	public string Execute(Sprite spr, Block block)
	{
		Thread thread = new(spr, block, this);
		return Execute(ref thread);
	}

	public string Execute(ref Thread thread)
	{
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