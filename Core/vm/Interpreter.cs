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
					spr.x += MoveMultiplier * float.Parse(block.inputs[0].value) * MathF.Sin(spr.direction * DegToRad);
					spr.y -= MoveMultiplier * float.Parse(block.inputs[0].value) * MathF.Cos(spr.direction * DegToRad);
					ClampToStage(spr);
					break;
				}

			case Block.opcodes.motion_turnright:
				{
					spr.direction += float.Parse(block.inputs[0].value) * 2;
					break;
				}

			case Block.opcodes.motion_turnleft:
				{
					spr.direction -= float.Parse(block.inputs[0].value) * 2;
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
					spr.x = float.Parse(block.inputs[0].value);
					spr.y = float.Parse(block.inputs[1].value);
					ClampToStage(spr);
					break;
				}

			case Block.opcodes.motion_glideto:
				{
					thread.delay = float.Parse(block.inputs[0].value);

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
					spr.x += float.Parse(block.inputs[1].value);
					spr.y += float.Parse(block.inputs[2].value);
					ClampToStage(spr);
					return "";
				}

			case Block.opcodes.motion_pointindirection:
				{
					spr.direction = float.Parse(block.inputs[0].value);
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
					spr.x += float.Parse(block.inputs[0].value);
					ClampToStage(spr);
					break;
				}

			case Block.opcodes.motion_setx:
				{
					spr.x = float.Parse(block.inputs[0].value);
					ClampToStage(spr);
					break;
				}

			case Block.opcodes.motion_changeyby:
				{
					spr.y += float.Parse(block.inputs[0].value);
					ClampToStage(spr);
					break;
				}

			case Block.opcodes.motion_sety:
				{
					spr.y = float.Parse(block.inputs[0].value);
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
					float sec = float.Parse(block.inputs[0].value);
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
					OverlayRender.RenderDialogue((int)spr.x, (int)spr.y + spr.costumes[spr.currentCostume].image.Height, block.inputs[0].value);
					break;
				}

			case Block.opcodes.looks_thinkforsecs:
				{
					float sec = float.Parse(block.inputs[0].value);
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
					OverlayRender.RenderDialogue((int)spr.x, (int)spr.y + spr.costumes[spr.currentCostume].image.Height, block.inputs[0].value);
					break;
				}

			case Block.opcodes.looks_switchcostumeto:
				{
					spr.currentCostume = int.Parse(block.inputs[0].value);
					break;
				}

			case Block.opcodes.looks_costume:
				{
					int index;
					index =
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
					break;
				}

			case Block.opcodes.looks_nextbackdrop:
				{
					break;
				}

			case Block.opcodes.looks_changesizeby:
				{
					spr.size += float.Parse(block.inputs[0].value);
					break;
				}

			case Block.opcodes.looks_setsizeto:
				{
					spr.size = float.Parse(block.inputs[0].value);
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
					break;
				}

			case Block.opcodes.looks_goforwardbackwardlayers:
				{
					break;
				}

			case Block.opcodes.looks_costumenumbername:
				{
					return block.fields[0] == "number" ? spr.currentCostume.ToString() : spr.costumes[spr.currentCostume].name;
				}

			case Block.opcodes.looks_backdropnumbername:
				{
					break;
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
					break;
				}

			case Block.opcodes.control_wait:
				{
					float sec = float.Parse(block.inputs[0].value);
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
					break;
				}

			case Block.opcodes.sensing_keyoptions:
				{
					break;
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
					break;
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
					double number = (rng.NextDouble() + float.Parse(block.inputs[0].value) * (float.Parse(block.inputs[1].value) - float.Parse(block.inputs[0].value)));
					return number.ToString(); //limit this as int or float depending on inputs
				}

			case Block.opcodes.operator_gt:
				{
					return Boolstr(float.Parse(block.inputs[0].value) > float.Parse(block.inputs[1].value));
				}

			case Block.opcodes.operator_lt:
				{
					return Boolstr(float.Parse(block.inputs[0].value) < float.Parse(block.inputs[1].value));
				}

			case Block.opcodes.operator_equals:
				{
					return Boolstr(block.inputs[0].value == block.inputs[1].value);
				}

			case Block.opcodes.operator_and:
				{
					break;
				}

			case Block.opcodes.operator_or:
				{
					break;
				}

			case Block.opcodes.operator_not:
				{
					break;
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
					break;
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
