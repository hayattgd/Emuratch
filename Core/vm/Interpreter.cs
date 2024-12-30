#nullable disable

using Emuratch.Core.Overlay;
using Emuratch.Core.Scratch;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Emuratch.Core.vm;

public class Interpreter : Runner
{
	const float DegToRad = MathF.PI / 180;
	const float MoveMultiplier = 1f;

	public Interpreter(Project project)
	{
		this.project = project;
		rng = new();
	}

	public Project project { get; set; }

	public bool TAS { get; set; }
	public bool paused { get; set; }

	public int fps { get; set; }
	public float Deltatime { get => (float)(1d / fps); }
	public Random rng { get; set; }

	public float timer { get; set; }

	public Vector2 mouse { get => Raylib.GetMousePosition() - new Vector2(project.width * 0.5f, project.height * 0.5f); }
	public Vector2 tasmouse { get; set; }
	public Vector2 mousepos { get => TAS ? tasmouse : mouse; }

	public List<Thread> PressFlag()
	{
		foreach (var sprite in project.sprites)
		{
			sprite.UpdateBlocks();
		}

		List<Thread> threads = new();

		foreach (var spr in Application.project.sprites)
		{
			foreach (var block in spr.blocks)
			{
				if (block.Value.opcode == Block.opcodes.event_whenflagclicked)
				{
					Thread t = new(spr, block.Value);
					threads.Add(t);
					Execute(t);
				}
			}
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
		float width = spr.costume.image.Width / 4f * spr.costume.bitmapResolution;
		float height = spr.costume.image.Height / 4f * spr.costume.bitmapResolution;

		spr.x = Math.Clamp(spr.x, project.width * -0.5f - width, project.width * 0.5f + width);
		spr.y = Math.Clamp(spr.y, project.height * -0.5f - height, project.height * 0.5f + height);
	}

	string Boolstr(bool boolean)
	{
		return boolean ? "true" : "false";
	}

	bool Strbool(string str)
	{
		return str == "true";
	}

	dynamic StrNumber(string str)
	{
		if (str == "Infinity") return double.MaxValue;
		if (str == "-Infinity") return double.MinValue;

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

		return str.Length;
	}

	public string Execute(Thread thread)
	{
		return Execute(thread.sprite, thread.block, thread);
	}

	public string Execute(Sprite spr, Block block)
	{
		return Execute(spr, block, new(spr, block));
	}

	public string Execute(Sprite spr, Block block, Thread thread)
	{
		if (!Application.projectloaded) return string.Empty;

		block.inputs.ForEach(
			input => {
				input.sprite = spr;
			});

		switch (block.opcode)
		{
			case Block.opcodes.motion_movesteps:
				{
					spr.x += MoveMultiplier * StrNumber(block.inputs[0].value) * MathF.Sin(spr.direction * DegToRad);
					spr.y -= MoveMultiplier * StrNumber(block.inputs[0].value) * MathF.Cos(spr.direction * DegToRad);
					ClampToStage(spr);
					break;
				}

			case Block.opcodes.motion_turnright:
				{
					spr.direction += StrNumber(block.inputs[0].value) * 2;
					break;
				}

			case Block.opcodes.motion_turnleft:
				{
					spr.direction -= StrNumber(block.inputs[0].value) * 2;
					break;
				}

			case Block.opcodes.motion_goto:
				{
					Vector2 pos;

					if (block.inputs[0].value == "_random_")
					{
						pos = new(
							rng.Next((int)(Application.project.width * -0.5f), (int)(Application.project.width * 0.5f)),
							rng.Next((int)(Application.project.height * -0.5f), (int)(Application.project.height * 0.5f))
						);
					}
					else if (block.inputs[0].value == "_mouse_")
					{
						pos = mousepos;
					}
					else
					{
						Sprite destination = Application.project.sprites.First(sprite => sprite.name == block.inputs[0].value);
						pos = new(destination.x, destination.y);
					}

					spr.x = pos.X;
					spr.y = pos.Y;
					ClampToStage(spr);
					break;
				}

			case Block.opcodes.motion_goto_menu:
				{
					return block.fields[0];
				}

			case Block.opcodes.motion_gotoxy:
				{
					spr.x = StrNumber(block.inputs[0].value);
					spr.y = StrNumber(block.inputs[1].value);
					ClampToStage(spr);
					break;
				}

			case Block.opcodes.motion_glideto:
				{
					thread.delay = StrNumber(block.inputs[0].value);

					Vector2 pos;

					if (block.inputs[0].value == "_random_")
					{
						pos = new(
							rng.Next((int)(Application.project.width * -0.5f), (int)(Application.project.width * 0.5f)),
							rng.Next((int)(Application.project.height * -0.5f), (int)(Application.project.height * 0.5f))
						);
					}
					else if (block.inputs[0].value == "_mouse_")
					{
						pos = mousepos;
					}
					else
					{
						Sprite destination = Application.project.sprites.First(sprite => sprite.name == block.inputs[0].value);
						pos = new(destination.x, destination.y);
					}

					spr.x = pos.X;
					spr.y = pos.Y;
					ClampToStage(spr);
					return "";
				}

			case Block.opcodes.motion_glideto_menu:
				{
					return block.fields[0];
				}

			case Block.opcodes.motion_glidesecstoxy:
				{
					spr.x += StrNumber(block.inputs[1].value);
					spr.y += StrNumber(block.inputs[2].value);
					ClampToStage(spr);
					return "";
				}

			case Block.opcodes.motion_pointindirection:
				{
					spr.direction = StrNumber(block.inputs[0].value);
					break;
				}

			case Block.opcodes.motion_pointtowards:
				{
					Vector2 pos;

					if (block.inputs[0].value == "_mouse_")
					{
						pos = mousepos;
					}
					else
					{
						Sprite destination = Application.project.sprites.First(sprite => sprite.name == block.inputs[0].value);
						pos = new(destination.x, destination.y);
					}

					spr.direction = MathF.Atan((pos.X - spr.x) / (pos.Y - spr.y) + 180 * pos.Y < spr.y ? 1 : 0);
					break;
				}

			case Block.opcodes.motion_pointtowards_menu:
				{
					return block.fields[0];
				}

			case Block.opcodes.motion_changexby:
				{
					spr.x += StrNumber(block.inputs[0].value);
					ClampToStage(spr);
					break;
				}

			case Block.opcodes.motion_setx:
				{
					spr.x = StrNumber(block.inputs[0].value);
					ClampToStage(spr);
					break;
				}

			case Block.opcodes.motion_changeyby:
				{
					spr.y += StrNumber(block.inputs[0].value);
					ClampToStage(spr);
					break;
				}

			case Block.opcodes.motion_sety:
				{
					spr.y = StrNumber(block.inputs[0].value);
					ClampToStage(spr);
					break;
				}

			case Block.opcodes.motion_ifonedgebounce:
				{
					break;
				}

			case Block.opcodes.motion_setrotationstyle:
				{
					spr.rotationStyle = block.fields[0];
					break;
				}

			case Block.opcodes.motion_xposition:
				{
					return spr.x.ToString();
				}

			case Block.opcodes.motion_yposition:
				{
					return spr.y.ToString();
				}

			case Block.opcodes.motion_direction:
				{
					return spr.direction.ToString();
				}

			case Block.opcodes.looks_sayforsecs:
				{
					float sec = StrNumber(block.inputs[0].value);
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

			case Block.opcodes.looks_say:
				{
					OverlayRender.RenderDialogue((int)spr.x, (int)spr.y + spr.costume.image.Height, block.inputs[0].value);
					break;
				}

			case Block.opcodes.looks_thinkforsecs:
				{
					float sec = StrNumber(block.inputs[0].value);
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

			case Block.opcodes.looks_think:
				{
					OverlayRender.RenderDialogue((int)spr.x, (int)spr.y + spr.costume.image.Height, block.inputs[0].value);
					break;
				}

			case Block.opcodes.looks_switchcostumeto:
				{
					if (int.TryParse(block.inputs[0].value, out int id))
					{
						spr.currentCostume = id;
					}
					else
					{
						spr.costume = spr.costumes.First(x => x.name == block.inputs[0].value);
					}
					break;
				}

			case Block.opcodes.looks_costume:
				{
					int index =
						int.TryParse(block.fields[0], out int number) ?
						number :
						spr.costumes.ToList().IndexOf(spr.costumes.First(x => x.name == block.fields[0]));

					return index.ToString();
				}

			case Block.opcodes.looks_nextcostume:
				{
					spr.currentCostume++;
					//add codes to repeat costume
					break;
				}

			case Block.opcodes.looks_switchbackdropto:
				{
					break;
				}

			case Block.opcodes.looks_backdrops:
				{
					return project.stage.currentCostume.ToString(); //Need check if it returns id or name
				}

			case Block.opcodes.looks_nextbackdrop:
				{
					break;
				}

			case Block.opcodes.looks_changesizeby:
				{
					spr.size += StrNumber(block.inputs[0].value);
					break;
				}

			case Block.opcodes.looks_setsizeto:
				{
					spr.size = StrNumber(block.inputs[0].value);
					break;
				}

			case Block.opcodes.looks_changeeffectby:
				{
					break;
				}

			case Block.opcodes.looks_seteffectto:
				{
					break;
				}

			case Block.opcodes.looks_cleargraphiceffects:
				{
					break;
				}

			case Block.opcodes.looks_show:
				{
					spr.visible = true;
					break;
				}

			case Block.opcodes.looks_hide:
				{
					spr.visible = false;
					break;
				}

			case Block.opcodes.looks_gotofrontback:
				{
					spr.SetLayoutOrder(block.fields[0] == "front" ? project.sprites.Length : 0);
					break;
				}

			case Block.opcodes.looks_goforwardbackwardlayers:
				{
					spr.SetLayoutOrder(
						spr.layoutOrder + int.Parse(block.inputs[0].value) * (block.fields[0] == "forward" ? 1 : -1)
					);
					break;
				}

			case Block.opcodes.looks_costumenumbername:
				{
					return block.fields[0] == "number" ? spr.currentCostume.ToString() : spr.costume.name;
				}

			case Block.opcodes.looks_backdropnumbername:
				{
					return block.fields[0] == "number" ? project.stage.currentCostume.ToString() : project.stage.costume.name;
				}

			case Block.opcodes.looks_size:
				{
					return spr.size.ToString();
				}

			case Block.opcodes.sound_playuntildone:
				{
					Raylib.PlaySound(spr.sounds.First(x => x.name == block.inputs[0].value).sound);
					return "";
				}

			case Block.opcodes.sound_sounds_menu:
				{
					return block.fields[0];
				}

			case Block.opcodes.sound_play:
				{
					Raylib.PlaySound(spr.sounds.First(x => x.name == block.inputs[0].value).sound);
					break;
				}

			case Block.opcodes.sound_stopallsounds:
				{
					break;
				}

			case Block.opcodes.sound_changeeffectby:
				{
					break;
				}

			case Block.opcodes.sound_seteffectto:
				{
					break;
				}

			case Block.opcodes.sound_cleareffects:
				{
					break;
				}

			case Block.opcodes.sound_changevolumeby:
				{
					break;
				}

			case Block.opcodes.sound_setvolumeto:
				{
					spr.volume =	StrNumber	(block.inputs[0].value);
					break;
				}

			case Block.opcodes.sound_volume:
				{
					return spr.volume.ToString();
				}

			case Block.opcodes.event_whenflagclicked:
				{
					break;
				}

			case Block.opcodes.event_whenkeypressed:
				{
					break;
				}

			case Block.opcodes.event_whenthisspriteclicked:
				{
					break;
				}

			case Block.opcodes.event_whenstageclicked:
				{
					break;
				}

			case Block.opcodes.event_whenbackdropswitchesto:
				{
					break;
				}

			case Block.opcodes.event_whengreaterthan:
				{
					break;
				}

			case Block.opcodes.event_whenbroadcastreceived:
				{
					break;
				}

			case Block.opcodes.event_broadcast:
				{
					foreach (var sprite in project.sprites)
					{
						foreach (var top in sprite.blocks.Where(x => x.Value.opcode == Block.opcodes.event_whenbroadcastreceived))
						{
							Execute(sprite, top.Value);
						}
					}
					break;
				}

			case Block.opcodes.event_broadcastandwait:
				{
					foreach (var sprite in project.sprites)
					{
						foreach (var top in sprite.blocks.Where(x => x.Value.opcode == Block.opcodes.event_whenbroadcastreceived))
						{
							Execute(sprite, top.Value);
						}
					}
					break;
				}

			case Block.opcodes.control_wait:
				{
					float sec = StrNumber(block.inputs[0].value);
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

			case Block.opcodes.control_repeat:
				{
					break;
				}

			case Block.opcodes.control_forever:
				{
					thread.block = spr.blocks[block.inputs[0].OriginalValue];
					thread.returnto.Add(thread.block);
					thread.forever = true;
					break;
				}

			case Block.opcodes.control_if:
				{
					if (Strbool(block.inputs[1].value)) return Execute(spr, spr.blocks[block.inputs[0].OriginalValue], thread);

					break;
				}

			case Block.opcodes.control_if_else:
				{
					break;
				}

			case Block.opcodes.control_wait_until:
				{
					break;
				}

			case Block.opcodes.control_repeat_until:
				{
					break;
				}

			case Block.opcodes.control_while:
				{
					break;
				}

			case Block.opcodes.control_stop:
				{
					break;
				}

			case Block.opcodes.control_start_as_clone:
				{
					break;
				}

			case Block.opcodes.control_create_clone_of:
				{
					break;
				}

			case Block.opcodes.control_create_clone_of_menu:
				{
					break;
				}

			case Block.opcodes.control_delete_this_clone:
				{
					break;
				}

			case Block.opcodes.sensing_touchingobject:
				{
					break;
				}

			case Block.opcodes.sensing_touchingobjectmenu:
				{
					break;
				}

			case Block.opcodes.sensing_touchingcolor:
				{
					break;
				}

			case Block.opcodes.sensing_coloristouchingcolor:
				{
					break;
				}

			case Block.opcodes.sensing_distanceto:
				{
					break;
				}

			case Block.opcodes.sensing_distancetomenu:
				{
					break;
				}

			case Block.opcodes.sensing_askandwait:
				{
					break;
				}

			case Block.opcodes.sensing_answer:
				{
					break;
				}

			case Block.opcodes.sensing_keypressed:
				{
					if (block.inputs[0].value == "any") return Boolstr(Raylib.GetKeyPressed() > 0);

					return Boolstr(Raylib.IsKeyDown(StrKey(block.inputs[0].value)));
				}

			case Block.opcodes.sensing_keyoptions:
				{
					return block.fields[0];
				}

			case Block.opcodes.sensing_mousedown:
				{
					return Boolstr(Raylib.IsMouseButtonDown(MouseButton.Left));
				}

			case Block.opcodes.sensing_mousex:
				{
					break;
				}

			case Block.opcodes.sensing_mousey:
				{
					break;
				}

			case Block.opcodes.sensing_setdragmode:
				{
					break;
				}

			case Block.opcodes.sensing_loudness:
				{
					break;
				}

			case Block.opcodes.sensing_timer:
				{
					break;
				}

			case Block.opcodes.sensing_resettimer:
				{
					break;
				}

			case Block.opcodes.sensing_of:
				{
					break;
				}

			case Block.opcodes.sensing_of_object_menu:
				{
					break;
				}

			case Block.opcodes.sensing_current:
				{
					break;
				}

			case Block.opcodes.sensing_dayssince2000:
				{
					break;
				}

			case Block.opcodes.sensing_username:
				{
					break;
				}

			case Block.opcodes.operator_add:
				{
					return 0.ToString();
				}

			case Block.opcodes.operator_subtract:
				{
					break;
				}

			case Block.opcodes.operator_multiply:
				{
					break;
				}

			case Block.opcodes.operator_divide:
				{
					break;
				}

			case Block.opcodes.operator_random:
				{
					double number = (rng.NextDouble() + StrNumber(block.inputs[0].value) * (StrNumber(block.inputs[1].value) - StrNumber(block.inputs[0].value)));
					return number.ToString(); //limit this as int or float depending on inputs
				}

			case Block.opcodes.operator_gt:
				{
					return Boolstr(StrNumber(block.inputs[0].value) > StrNumber(block.inputs[1].value));
				}

			case Block.opcodes.operator_lt:
				{
					return Boolstr(StrNumber(block.inputs[0].value) < StrNumber(block.inputs[1].value));
				}

			case Block.opcodes.operator_equals:
				{
					return Boolstr(block.inputs[0].value.ToLower() == block.inputs[1].value.ToLower());
				}

			case Block.opcodes.operator_and:
				{
					return Boolstr(Strbool(block.inputs[0].value) && Strbool(block.inputs[1].value));
				}

			case Block.opcodes.operator_or:
				{
					return Boolstr(Strbool(block.inputs[0].value) || Strbool(block.inputs[1].value));
				}

			case Block.opcodes.operator_not:
				{
					return Boolstr(!Strbool(block.inputs[0].value));
				}

			case Block.opcodes.operator_join:
				{
					return block.inputs[0].value + block.inputs[1].value;
				}

			case Block.opcodes.operator_letter_of:
				{
					return block.inputs[0].value[int.Parse(block.inputs[1].value)].ToString();
				}

			case Block.opcodes.operator_length:
				{
					return block.inputs[0].value.Length.ToString();
				}

			case Block.opcodes.operator_contains:
				{
					break;
				}

			case Block.opcodes.operator_mod:
				{
					break;
				}

			case Block.opcodes.operator_round:
				{
					break;
				}

			case Block.opcodes.operator_mathop:
				{
					break;
				}

			case Block.opcodes.data_variable:
				{
					break;
				}

			case Block.opcodes.data_setvariableto:
				{
					break;
				}

			case Block.opcodes.data_changevariableby:
				{
					break;
				}

			case Block.opcodes.data_showvariable:
				{
					break;
				}

			case Block.opcodes.data_hidevariable:
				{
					break;
				}

			case Block.opcodes.data_listcontents:
				{
					break;
				}

			case Block.opcodes.data_addtolist:
				{
					break;
				}

			case Block.opcodes.data_deleteoflist:
				{
					break;
				}

			case Block.opcodes.data_deletealloflist:
				{
					break;
				}

			case Block.opcodes.data_insertatlist:
				{
					break;
				}

			case Block.opcodes.data_replaceitemoflist:
				{
					break;
				}

			case Block.opcodes.data_itemoflist:
				{
					break;
				}

			case Block.opcodes.data_itemnumoflist:
				{
					break;
				}

			case Block.opcodes.data_lengthoflist:
				{
					break;
				}

			case Block.opcodes.data_listcontainsitem:
				{
					break;
				}

			case Block.opcodes.data_showlist:
				{
					break;
				}

			case Block.opcodes.data_hidelist:
				{
					break;
				}

			case Block.opcodes.procedures_definition:
				{
					break;
				}

			case Block.opcodes.procedures_prototype:
				{
					break;
				}

			case Block.opcodes.procedures_call:
				{
					break;
				}

			case Block.opcodes.argument_reporter_string_number:
				{
					break;
				}

			case Block.opcodes.argument_reporter_boolean:
				{
					break;
				}
		}

		if (block.nextId != "")
		{
			return Execute(spr, block.Next(spr), thread);
		}

		return string.Empty;
	}
}
