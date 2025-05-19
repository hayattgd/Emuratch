using Emuratch.Core.Overlay;
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

	public Vector2 mouse { get => Raylib.GetMousePosition() - new Vector2(project.width * 0.5f, project.height * 0.5f); }
	public Vector2 tasmouse { get; set; }
	public Vector2 mousepos { get => TAS ? tasmouse : mouse; }

	static readonly Dictionary<Block.Opcodes, Operation> operations = new()
	{
		{
			Block.Opcodes.motion_movesteps,
			(ref Thread thread, Project project) => {
				thread.sprite.x += MoveMultiplier * StrNumber(thread.block.inputs[0].value) * MathF.Sin(thread.sprite.direction * DegToRad);
				thread.sprite.y -= MoveMultiplier * StrNumber(thread.block.inputs[0].value) * MathF.Cos(thread.sprite.direction * DegToRad);
				ClampToStage(thread.sprite, project);
				return "";
			}
		}
	};

	Dictionary<Block.Opcodes, List<Block>> eventBlocks;

	delegate string? Operation(ref Thread thread, Project project);

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

	static dynamic StrNumber(string str)
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

		return 0;
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
					// const float alphamultiplier = 0.25f;
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

	public string Execute(ref Thread thread)
	{
		return Execute(thread.sprite, thread.block, ref thread);
	}

	public string Execute(Sprite spr, Block block)
	{
		Thread thread = new(spr, block);
		return Execute(spr, block, ref thread);
	}

	string Execute(Sprite spr, Block block, ref Thread thread)
	{
		if (!Application.projectloaded) return string.Empty;

		switch (block.opcode)
		{
			case Block.Opcodes.motion_turnright:
				{
					spr.direction += StrNumber(block.inputs[0].value) * 2;
					break;
				}

			case Block.Opcodes.motion_turnleft:
				{
					spr.direction -= StrNumber(block.inputs[0].value) * 2;
					break;
				}

			case Block.Opcodes.motion_goto:
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

			case Block.Opcodes.motion_goto_menu:
				{
					return block.fields[0];
				}

			case Block.Opcodes.motion_gotoxy:
				{
					spr.x = StrNumber(block.inputs[0].value);
					spr.y = StrNumber(block.inputs[1].value);
					ClampToStage(spr);
					break;
				}

			case Block.Opcodes.motion_glideto:
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

			case Block.Opcodes.motion_glideto_menu:
				{
					return block.fields[0];
				}

			case Block.Opcodes.motion_glidesecstoxy:
				{
					spr.x += StrNumber(block.inputs[1].value);
					spr.y += StrNumber(block.inputs[2].value);
					ClampToStage(spr);
					return "";
				}

			case Block.Opcodes.motion_pointindirection:
				{
					spr.direction = StrNumber(block.inputs[0].value);
					break;
				}

			case Block.Opcodes.motion_pointtowards:
				{
					Vector2 pos = Vector2.Zero;

					if (block.inputs[0].value == "_mouse_")
					{
						pos = mousepos;
					}
					else if (block.inputs[0].value == "_random_")
					{
						spr.direction = rng.Next(-180, 180);
						break;
					}
					else
					{
						try
						{
							Sprite destination = Application.project.sprites.First(sprite => sprite.name == block.inputs[0].value);
							pos = new(destination.x, destination.y);
						}
						catch (Exception ex)
						{
							if (ex.Message != "Sequence contains no matching element") throw;
						}
					}

					spr.direction = MathF.Atan((pos.X - spr.x) / (pos.Y - spr.y) + 180 * pos.Y < spr.y ? 1 : 0);
					break;
				}

			case Block.Opcodes.motion_pointtowards_menu:
				{
					return block.fields[0];
				}

			case Block.Opcodes.motion_changexby:
				{
					spr.x += StrNumber(block.inputs[0].value);
					ClampToStage(spr);
					break;
				}

			case Block.Opcodes.motion_setx:
				{
					spr.x = StrNumber(block.inputs[0].value);
					ClampToStage(spr);
					break;
				}

			case Block.Opcodes.motion_changeyby:
				{
					spr.y += StrNumber(block.inputs[0].value);
					ClampToStage(spr);
					break;
				}

			case Block.Opcodes.motion_sety:
				{
					spr.y = StrNumber(block.inputs[0].value);
					ClampToStage(spr);
					break;
				}

			case Block.Opcodes.motion_ifonedgebounce:
				{
					break;
				}

			case Block.Opcodes.motion_setrotationstyle:
				{
					spr.rotationStyle = block.fields[0];
					break;
				}

			case Block.Opcodes.motion_xposition:
				{
					return spr.x.ToString();
				}

			case Block.Opcodes.motion_yposition:
				{
					return spr.y.ToString();
				}

			case Block.Opcodes.motion_direction:
				{
					return spr.direction.ToString();
				}

			case Block.Opcodes.looks_sayforsecs:
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

			case Block.Opcodes.looks_say:
				{
					OverlayRender.RenderDialogue(-(int)spr.x, -(int)spr.y - spr.costume.image.Height, block.inputs[0].value);
					break;
				}

			case Block.Opcodes.looks_thinkforsecs:
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

			case Block.Opcodes.looks_think:
				{
					OverlayRender.RenderDialogue((int)spr.x, (int)spr.y + spr.costume.image.Height, block.inputs[0].value);
					break;
				}

			case Block.Opcodes.looks_switchcostumeto:
				{
					if (int.TryParse(block.inputs[0].value, out int id))
					{
						spr.currentCostume = id;
					}
					else
					{
						try
						{
							spr.costume = spr.costumes.First(x => x.name == block.inputs[0].value);
						}
						catch (Exception ex)
						{
							if (ex.GetType() != typeof(InvalidOperationException))
							{
								throw;
							}
						}
					}
					break;
				}

			case Block.Opcodes.looks_costume:
				{
					int index =
						int.TryParse(block.fields[0], out int number) ?
						number :
						spr.costumes.ToList().IndexOf(spr.costumes.First(x => x.name == block.fields[0]));

					return index.ToString();
				}

			case Block.Opcodes.looks_nextcostume:
				{
					spr.currentCostume++;
					//add codes to repeat costume
					break;
				}

			case Block.Opcodes.looks_switchbackdropto:
				{
					break;
				}

			case Block.Opcodes.looks_backdrops:
				{
					return project.stage.currentCostume.ToString(); //Need check if it returns id or name
				}

			case Block.Opcodes.looks_nextbackdrop:
				{
					break;
				}

			case Block.Opcodes.looks_changesizeby:
				{
					spr.size += StrNumber(block.inputs[0].value);
					break;
				}

			case Block.Opcodes.looks_setsizeto:
				{
					spr.size = StrNumber(block.inputs[0].value);
					break;
				}

			case Block.Opcodes.looks_changeeffectby:
				{
					break;
				}

			case Block.Opcodes.looks_seteffectto:
				{
					break;
				}

			case Block.Opcodes.looks_cleargraphiceffects:
				{
					break;
				}

			case Block.Opcodes.looks_show:
				{
					spr.visible = true;
					break;
				}

			case Block.Opcodes.looks_hide:
				{
					spr.visible = false;
					break;
				}

			case Block.Opcodes.looks_gotofrontback:
				{
					spr.SetLayoutOrder(block.fields[0] == "front" ? project.sprites.Length : 0);
					break;
				}

			case Block.Opcodes.looks_goforwardbackwardlayers:
				{
					spr.SetLayoutOrder(
						spr.layoutOrder + int.Parse(block.inputs[0].value) * (block.fields[0] == "forward" ? 1 : -1)
					);
					break;
				}

			case Block.Opcodes.looks_costumenumbername:
				{
					return block.fields[0] == "number" ? spr.currentCostume.ToString() : spr.costume.name;
				}

			case Block.Opcodes.looks_backdropnumbername:
				{
					return block.fields[0] == "number" ? project.stage.currentCostume.ToString() : project.stage.costume.name;
				}

			case Block.Opcodes.looks_size:
				{
					return spr.size.ToString();
				}

			case Block.Opcodes.sound_playuntildone:
				{
					Raylib.PlaySound(spr.sounds.First(x => x.name == block.inputs[0].value).sound);
					return "";
				}

			case Block.Opcodes.sound_sounds_menu:
				{
					return block.fields[0];
				}

			case Block.Opcodes.sound_play:
				{
					Raylib.PlaySound(spr.sounds.First(x => x.name == block.inputs[0].value).sound);
					break;
				}

			case Block.Opcodes.sound_stopallsounds:
				{
					break;
				}

			case Block.Opcodes.sound_changeeffectby:
				{
					break;
				}

			case Block.Opcodes.sound_seteffectto:
				{
					break;
				}

			case Block.Opcodes.sound_cleareffects:
				{
					break;
				}

			case Block.Opcodes.sound_changevolumeby:
				{
					break;
				}

			case Block.Opcodes.sound_setvolumeto:
				{
					spr.volume = StrNumber(block.inputs[0].value);
					break;
				}

			case Block.Opcodes.sound_volume:
				{
					return spr.volume.ToString();
				}

			case Block.Opcodes.event_whenflagclicked:
				{
					break;
				}

			case Block.Opcodes.event_whenkeypressed:
				{
					if (block.fields[0] == "any") return Boolstr(Raylib.GetKeyPressed() > 0);

					KeyboardKey key = StrKey(block.fields[0]);
					if (Raylib.IsKeyPressedRepeat(key) || Raylib.IsKeyPressed(key))
					{
						break;
					}
					else
					{
						return "";
					}
				}

			case Block.Opcodes.event_whenthisspriteclicked:
				{
					break;
				}

			case Block.Opcodes.event_whenstageclicked:
				{
					break;
				}

			case Block.Opcodes.event_whenbackdropswitchesto:
				{
					break;
				}

			case Block.Opcodes.event_whengreaterthan:
				{
					break;
				}

			case Block.Opcodes.event_whenbroadcastreceived:
				{
					break;
				}

			case Block.Opcodes.event_broadcast:
				{
					foreach (var sprite in project.sprites)
					{
						foreach (var top in sprite.blocks.Where(x => x.Value.opcode == Block.Opcodes.event_whenbroadcastreceived))
						{
							Execute(sprite, top.Value);
						}
					}
					break;
				}

			case Block.Opcodes.event_broadcastandwait:
				{
					foreach (var sprite in project.sprites)
					{
						foreach (var top in sprite.blocks.Where(x => x.Value.opcode == Block.Opcodes.event_whenbroadcastreceived))
						{
							Execute(sprite, top.Value);
						}
					}
					break;
				}

			case Block.Opcodes.control_wait:
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

			case Block.Opcodes.control_repeat:
				{
					thread.block = spr.blocks[block.inputs[1].RawValue];
					thread.returnto.Add(new(thread.block, int.Parse(block.inputs[0].value)));
					return "";
				}

			case Block.Opcodes.control_forever:
				{
					thread.block = spr.blocks[block.inputs[0].RawValue];
					thread.returnto.Add(new(thread.block));
					break;
				}

			case Block.Opcodes.control_if:
				{
					if (Strbool(block.inputs[0].value))
					{
						Execute(spr, spr.blocks[block.inputs[1].RawValue], ref thread);
					}

					break;
				}

			case Block.Opcodes.control_if_else:
				{
					if (Strbool(block.inputs[2].value))
					{
						Execute(spr, spr.blocks[block.inputs[0].RawValue], ref thread);
					}
					else
					{
						Execute(spr, spr.blocks[block.inputs[1].RawValue], ref thread);
					}

					break;
				}

			case Block.Opcodes.control_wait_until:
				{
					break;
				}

			case Block.Opcodes.control_repeat_until:
				{
					break;
				}

			case Block.Opcodes.control_while:
				{
					break;
				}

			case Block.Opcodes.control_stop:
				{
					break;
				}

			// case Block.opcodes.control_start_as_clone

			case Block.Opcodes.control_create_clone_of:
				{
					project.clones.Add(Sprite.Clone(project.sprites.First(x => x.name == block.inputs[0].value)));
					break;
				}

			case Block.Opcodes.control_create_clone_of_menu:
				{
					return block.fields[0] == "_myself_" ? spr.name : block.fields[0];
				}

			case Block.Opcodes.control_delete_this_clone:
				{
					if (!spr.isClone) break;

					project.clones.Remove(spr);
					break;
				}

			case Block.Opcodes.sensing_touchingobject:
				{
					if (!project.sprites.Any(x => x.name == block.inputs[0].value)) return "false";
					Sprite target = project.sprites.First(x => x.name == block.inputs[0].value);
					if (!CheckBoundingBoxOverlap(spr, target)) return "false";
					if (!CheckPixelOverlap(spr, target)) return "false";
					return "true";
				}

			case Block.Opcodes.sensing_touchingobjectmenu:
				{
					return block.fields[0];
				}

			case Block.Opcodes.sensing_touchingcolor:
				{
					for (int x = 0; x < spr.costume.image.Width; x++)
					{
						for (int y = 0; y < spr.costume.image.Height; y++)
						{
							if (spr.costume.GetColor(x, y).A != 0)
							{
								Color color = Application.render.GetColorOnPixel(x, y);
								if (block.inputs[0].value == $"#{color.R:X2}{color.G:X2}{color.B:X2}")
								{
									return "true";
								}
							}
						}
					}
					return "false";
				}

			case Block.Opcodes.sensing_coloristouchingcolor:
				{
					break;
				}

			case Block.Opcodes.sensing_distanceto:
				{
					break;
				}

			case Block.Opcodes.sensing_distancetomenu:
				{
					break;
				}

			case Block.Opcodes.sensing_askandwait:
				{
					break;
				}

			case Block.Opcodes.sensing_answer:
				{
					break;
				}

			case Block.Opcodes.sensing_keypressed:
				{
					if (block.inputs[0].value == "any") return Boolstr(Raylib.GetKeyPressed() > 0);

					if (StrKey(block.inputs[0].value) == KeyboardKey.Null && block.inputs[0].value.Length > 1)
					{
						return Boolstr(Raylib.IsKeyDown(StrKey(block.inputs[0].value[0].ToString())));
					}

					return Boolstr(Raylib.IsKeyDown(StrKey(block.inputs[0].value)));
				}

			case Block.Opcodes.sensing_keyoptions:
				{
					return block.fields[0];
				}

			case Block.Opcodes.sensing_mousedown:
				{
					return Boolstr(Raylib.IsMouseButtonDown(MouseButton.Left));
				}

			case Block.Opcodes.sensing_mousex:
				{
					return Raylib.GetMouseX().ToString();
				}

			case Block.Opcodes.sensing_mousey:
				{
					return Raylib.GetMouseY().ToString();
				}

			case Block.Opcodes.sensing_setdragmode:
				{
					break;
				}

			case Block.Opcodes.sensing_loudness:
				{
					return "0";
				}

			case Block.Opcodes.sensing_timer:
				{
					return timer.ToString();
				}

			case Block.Opcodes.sensing_resettimer:
				{
					timer = 0;
					break;
				}

			case Block.Opcodes.sensing_of:
				{
					break;
				}

			case Block.Opcodes.sensing_of_object_menu:
				{
					break;
				}

			case Block.Opcodes.sensing_current:
				{
					break;
				}

			case Block.Opcodes.sensing_dayssince2000:
				{
					break;
				}

			case Block.Opcodes.sensing_username:
				{
					return "USERNAME";
				}

			case Block.Opcodes.operator_add:
				{
					return (StrNumber(block.inputs[0].value) + StrNumber(block.inputs[1].value)).ToString();
				}

			case Block.Opcodes.operator_subtract:
				{
					return (StrNumber(block.inputs[0].value) - StrNumber(block.inputs[1].value)).ToString();
				}

			case Block.Opcodes.operator_multiply:
				{
					return (StrNumber(block.inputs[0].value) * StrNumber(block.inputs[1].value)).ToString();
				}

			case Block.Opcodes.operator_divide:
				{
					return (StrNumber(block.inputs[0].value) / StrNumber(block.inputs[1].value)).ToString();
				}

			case Block.Opcodes.operator_random:
				{
					double number = rng.NextDouble() + StrNumber(block.inputs[0].value) * (StrNumber(block.inputs[1].value) - StrNumber(block.inputs[0].value));
					return number.ToString();
				}

			case Block.Opcodes.operator_gt:
				{
					return Boolstr(StrNumber(block.inputs[0].value) > StrNumber(block.inputs[1].value));
				}

			case Block.Opcodes.operator_lt:
				{
					return Boolstr(StrNumber(block.inputs[0].value) < StrNumber(block.inputs[1].value));
				}

			case Block.Opcodes.operator_equals:
				{
					return Boolstr(block.inputs[0].value.ToLower() == block.inputs[1].value.ToLower());
				}

			case Block.Opcodes.operator_and:
				{
					return Boolstr(Strbool(block.inputs[0].value) && Strbool(block.inputs[1].value));
				}

			case Block.Opcodes.operator_or:
				{
					return Boolstr(Strbool(block.inputs[0].value) || Strbool(block.inputs[1].value));
				}

			case Block.Opcodes.operator_not:
				{
					return Boolstr(!Strbool(block.inputs[0].value));
				}

			case Block.Opcodes.operator_join:
				{
					return block.inputs[0].value + block.inputs[1].value;
				}

			case Block.Opcodes.operator_letter_of:
				{
					return block.inputs[0].value[int.Parse(block.inputs[1].value)].ToString();
				}

			case Block.Opcodes.operator_length:
				{
					return block.inputs[0].value.Length.ToString();
				}

			case Block.Opcodes.operator_contains:
				{
					break;
				}

			case Block.Opcodes.operator_mod:
				{
					break;
				}

			case Block.Opcodes.operator_round:
				{
					break;
				}

			case Block.Opcodes.operator_mathop:
				{
					break;
				}

			case Block.Opcodes.data_variable:
				{
					return project.stage.variables.First(x => x.name == block.fields[0]).value.ToString() ?? "";
				}

			case Block.Opcodes.data_setvariableto:
				{
					project.stage.variables.First(x => x.name == block.fields[0]).value = block.inputs[0];
					break;
				}

			case Block.Opcodes.data_changevariableby:
				{
					break;
				}

			case Block.Opcodes.data_showvariable:
				{
					break;
				}

			case Block.Opcodes.data_hidevariable:
				{
					break;
				}

			case Block.Opcodes.data_listcontents:
				{
					break;
				}

			case Block.Opcodes.data_addtolist:
				{
					break;
				}

			case Block.Opcodes.data_deleteoflist:
				{
					break;
				}

			case Block.Opcodes.data_deletealloflist:
				{
					break;
				}

			case Block.Opcodes.data_insertatlist:
				{
					break;
				}

			case Block.Opcodes.data_replaceitemoflist:
				{
					break;
				}

			case Block.Opcodes.data_itemoflist:
				{
					break;
				}

			case Block.Opcodes.data_itemnumoflist:
				{
					break;
				}

			case Block.Opcodes.data_lengthoflist:
				{
					break;
				}

			case Block.Opcodes.data_listcontainsitem:
				{
					break;
				}

			case Block.Opcodes.data_showlist:
				{
					break;
				}

			case Block.Opcodes.data_hidelist:
				{
					break;
				}

			case Block.Opcodes.procedures_definition:
				{
					break;
				}

			case Block.Opcodes.procedures_prototype:
				{
					break;
				}

			case Block.Opcodes.procedures_call:
				{
					break;
				}

			case Block.Opcodes.argument_reporter_string_number:
				{
					break;
				}

			case Block.Opcodes.argument_reporter_boolean:
				{
					break;
				}

			default:
				{
					string? returnValue = operations[block.opcode](ref thread, project);
					if (returnValue != null)
					{
						return returnValue;
					}
					else
					{
						break;
					}
				}
		}

		if (block.nextId != "")
		{
			return Execute(spr, block.Next(spr), ref thread);
		}
		else
		{
			thread.block.nextId = "";
		}

		return string.Empty;
	}
}
