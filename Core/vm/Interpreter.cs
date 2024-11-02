using Emuratch.Core.Scratch;
using System.Collections.Generic;
using System;

namespace Emuratch.Core.vm;

public class Interpreter : Executer
{
  //  public readonly static List<Action<Sprite, List<object>>> Operations = new()
  //  {
  //      //motion_movesteps
  //      (spr, arg) => {
  //          spr.x += (float)arg[0] * MathF.Sin(spr.direction);
  //          spr.x += (float)arg[0] * MathF.Cos(spr.direction);
  //      },

  //      //motion_turnright
  //      (spr, arg) => {
  //          spr.direction += (float)arg[0];
  //      },

  //      //motion_turnleft
  //      (spr, arg) => {
  //          spr.direction -= (float)arg[0];
  //      },

  //      //motion_goto
  //      (spr, arg) => {
  //          spr.x = ((Sprite)arg[0]).x;
  //          spr.y = ((Sprite)arg[0]).y;
  //      },

  //      //motion_gotoxy
  //      (spr, arg) => {
  //          spr.x = (float)arg[0];
  //          spr.y = (float)arg[1];
  //      },

  //      //motion_glideto
  //      (spr, arg) => {
  //          spr.x = (float)arg[0];
  //          spr.y = (float)arg[1];
  //      },
  //  };

    public Sprite Sprite { get; set; }

    public Interpreter(Sprite spr)
    {
        Sprite = spr;
    }

    public void Execute(Block block)
    {
		switch (block.opcode)
		{
			case Block.opcodes.motion_movesteps:
				{ 
					break; 
				};

			case Block.opcodes.motion_turnright:
				{ 
					break; 
				};

			case Block.opcodes.motion_turnleft:
				{ 
					break; 
				};

			case Block.opcodes.motion_goto:
				{
					break; 
				};

			case Block.opcodes.motion_gotoxy:
				{
					break; 
				};

			case Block.opcodes.motion_glideto:
				{
					break; 
				};

			case Block.opcodes.motion_glidesecstoxy:
				{ 
					break;
				};

			case Block.opcodes.motion_pointindirection:
				{ 
					break; 
				};

			case Block.opcodes.motion_pointtowards:
				{
					break; 
				};

			case Block.opcodes.motion_changexby:
				{
					break; 
				};

			case Block.opcodes.motion_setx:
				{
					break;
				};

			case Block.opcodes.motion_changeyby:
				{
					break;
				};

			case Block.opcodes.motion_sety:
				{
					break;
				};

			case Block.opcodes.motion_ifonedgebounce:
				{
					break;
				};

			case Block.opcodes.motion_setrotationstyle:
				{
					break;
				};

			case Block.opcodes.motion_xposition:
				{
					break;
				};

			case Block.opcodes.motion_yposition:
				{
					break;
				};

			case Block.opcodes.motion_direction:
				{
					break;
				};

			case Block.opcodes.looks_sayforsecs:
				{
					break;
				};

			case Block.opcodes.looks_thinkforsecs:
				{
					break;
				};

			case Block.opcodes.looks_think:
				{
					break;
				};

			case Block.opcodes.looks_switchcostumeto:
				{
					break;
				};

			case Block.opcodes.looks_nextcostume:
				{
					break;
				};

			case Block.opcodes.looks_switchbackdropto:
				{
					break;
				};

			case Block.opcodes.looks_switchbackdroptoandwait:
				{
					break;
				};

			case Block.opcodes.looks_nextbackdrop:
				{
					break;
				};

			case Block.opcodes.looks_changesizeby:
				{
					break;
				};

			case Block.opcodes.looks_setsizeto:
				{
					break;
				};

			case Block.opcodes.looks_changeeffectby:
				{
					break;
				};

			case Block.opcodes.looks_seteffectto:
				{
					break;
				};

			case Block.opcodes.looks_cleargraphiceffects:
				{
					break;
				};

			case Block.opcodes.looks_gotofrontback:
				{
					break;
				};

			case Block.opcodes.looks_goforwardbackwardlayers:
				{
					break;
				};

			case Block.opcodes.looks_costumenumbername:
				{
					break;
				};

			case Block.opcodes.looks_backdropnumbername:
				{
					break;
				};

			case Block.opcodes.looks_size:
				{
					break;
				};

			case Block.opcodes.sound_playuntildone:
				{
					break;
				};

			case Block.opcodes.sound_play:
				{
					break;
				};

			case Block.opcodes.sound_stopallsounds:
				{
					break;
				};

			case Block.opcodes.sound_changeeffectby:
				{
					break;
				};

			case Block.opcodes.sound_seteffectto:
				{
					break;
				};

			case Block.opcodes.sound_cleareffects:
				{
					break;
				};

			case Block.opcodes.sound_changevolumeby:
				{
					break;
				};

			case Block.opcodes.sound_setvolumeto:
				{
					break;
				};

			case Block.opcodes.sound_volume:
				{
					break;
				};

			case Block.opcodes.event_whenflagclicked:
				{
					break;
				};

			case Block.opcodes.event_whenkeypressed:
				{
					break;
				};

			case Block.opcodes.event_whenthisspriteclicked:
				{
					break;
				};

			case Block.opcodes.event_whenstageclicked:
				{
					break;
				};

			case Block.opcodes.event_whenbackdropswitchesto:
				{
					break;
				};

			case Block.opcodes.event_whengreaterthan:
				{
					break;
				};

			case Block.opcodes.event_whenbroadcastreceived:
				{
					break;
				};

			case Block.opcodes.event_broadcast:
				{
					break;
				};

			case Block.opcodes.event_broadcastandwait:
				{
					break;
				};

			case Block.opcodes.control_wait:
				{
					break;
				};

			case Block.opcodes.control_forever:
				{
					break;
				};

			case Block.opcodes.control_if:
				{
					break;
				};

			case Block.opcodes.control_if_else:
				{
					break;
				};

			case Block.opcodes.control_wait_until:
				{
					break;
				};

			case Block.opcodes.control_repeat_until:
				{
					break;
				};

			case Block.opcodes.control_stop:
				{
					break;
				};

			case Block.opcodes.control_start_as_clone:
				{
					break;
				};

			case Block.opcodes.control_create_clone_of:
				{
					break;
				};

			case Block.opcodes.control_delete_this_clone:
				{
					break;
				};

			case Block.opcodes.sensing_touchingobject:
				{
					break;
				};

			case Block.opcodes.sensing_touchingcolor:
				{
					break;
				};

			case Block.opcodes.sensing_coloristouchingcolor:
				{
					break;
				};

			case Block.opcodes.sensing_distanceto:
				{
					break;
				};

			case Block.opcodes.sensing_askandwait:
				{
					break;
				};

			case Block.opcodes.sensing_answer:
				{
					break;
				};

			case Block.opcodes.sensing_keypressed:
				{
					break;
				};

			case Block.opcodes.sensing_mousedown:
				{
					break;
				};

			case Block.opcodes.sensing_mousex:
				{
					break;
				};

			case Block.opcodes.sensing_mousey:
				{
					break;
				};

			case Block.opcodes.sensing_setdragmode:
				{
					break;
				};

			case Block.opcodes.sensing_loudness:
				{
					break;
				};

			case Block.opcodes.sensing_timer:
				{
					break;
				};

			case Block.opcodes.sensing_resettimer:
				{
					break;
				};

			case Block.opcodes.sensing_of:
				{
					break;
				};

			case Block.opcodes.sensing_current:
				{
					break;
				};

			case Block.opcodes.sensing_dayssince2000:
				{
					break;
				};

			case Block.opcodes.sensing_username:
				{
					break;
				};

			case Block.opcodes.operator_add:
				{
					break;
				};

			case Block.opcodes.operator_subtract:
				{
					break;
				};

			case Block.opcodes.operator_multiply:
				{
					break;
				};

			case Block.opcodes.operator_divide:
				{
					break;
				};

			case Block.opcodes.operator_random:
				{
					break;
				};

			case Block.opcodes.operator_gt:
				{
					break;
				};

			case Block.opcodes.operator_lt:
				{
					break;
				};

			case Block.opcodes.operator_equals:
				{
					break;
				};

			case Block.opcodes.operator_and:
				{
					break;
				};

			case Block.opcodes.operator_or:
				{
					break;
				};

			case Block.opcodes.operator_not:
				{
					break;
				};

			case Block.opcodes.operator_join:
				{
					break;
				};

			case Block.opcodes.operator_letter_of:
				{
					break;
				};

			case Block.opcodes.operator_length:
				{
					break;
				};

			case Block.opcodes.operator_contains:
				{
					break;
				};

			case Block.opcodes.operator_mod:
				{
					break;
				};

			case Block.opcodes.operator_round:
				{
					break;
				};

			case Block.opcodes.operator_mathop:
				{
					break;
				};

			case Block.opcodes.data_variable:
				{
					break;
				};

			case Block.opcodes.data_setvariableto:
				{
					break;
				};

			case Block.opcodes.data_changevariableby:
				{
					break;
				};

			case Block.opcodes.data_showvariable:
				{
					break;
				};

			case Block.opcodes.data_hidevariable:
				{
					break;
				};

			case Block.opcodes.data_listcontents:
				{
					break;
				};

			case Block.opcodes.data_addtolist:
				{
					break;
				};

			case Block.opcodes.data_deleteoflist:
				{
					break;
				};

			case Block.opcodes.data_deletealloflist:
				{
					break;
				};

			case Block.opcodes.data_insertatlist:
				{
					break;
				};

			case Block.opcodes.data_replaceitemoflist:
				{
					break;
				};

			case Block.opcodes.data_itemoflist:
				{
					break;
				};

			case Block.opcodes.data_itemnumoflist:
				{
					break;
				};

			case Block.opcodes.data_lengthoflist:
				{
					break;
				};

			case Block.opcodes.data_listcontainsitem:
				{
					break;
				};

			case Block.opcodes.data_showlist:
				{
					break;
				};

			case Block.opcodes.data_hidelist:
				{
					break;
				};

			case Block.opcodes.procedures_definition:
				{
					break;
				};

			case Block.opcodes.procedures_call:
				{
					break;
				};

			case Block.opcodes.argument_reporter_string_number:
				{
					break;
				};

			case Block.opcodes.argument_reporter_boolean:
				{
					break;
				};

			default:
				{
					break;
				};
		}

		if (block.nextId != null)
        {
            Execute(block.Next(Sprite));
        }
    }
}
