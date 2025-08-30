#nullable disable

using Emuratch.Core.Scratch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Emuratch.Core.vm;

public class Block
{
	//https://en.scratch-wiki.info/wiki/List_of_Block_Opcodes
	public enum Opcodes
	{
		motion_movesteps,
		motion_turnright,
		motion_turnleft,
		motion_goto,
		motion_goto_menu,
		motion_gotoxy,
		motion_glideto,
		motion_glideto_menu,
		motion_glidesecstoxy,
		motion_pointindirection,
		motion_pointtowards,
		motion_pointtowards_menu,
		motion_changexby,
		motion_setx,
		motion_changeyby,
		motion_sety,
		motion_ifonedgebounce,
		motion_setrotationstyle,
		motion_xposition,
		motion_yposition,
		motion_direction,

		looks_sayforsecs,
		looks_say,
		looks_thinkforsecs,
		looks_think,
		looks_switchcostumeto,
		looks_costume,
		looks_nextcostume,
		looks_switchbackdropto,
		looks_backdrops,
		looks_nextbackdrop,
		looks_changesizeby,
		looks_setsizeto,
		looks_changeeffectby,
		looks_seteffectto,
		looks_cleargraphiceffects,
		looks_show,
		looks_hide,
		looks_gotofrontback,
		looks_goforwardbackwardlayers,
		looks_costumenumbername,
		looks_backdropnumbername,
		looks_size,

		sound_playuntildone,
		sound_sounds_menu,
		sound_play,
		sound_stopallsounds,
		sound_changeeffectby,
		sound_seteffectto,
		sound_cleareffects,
		sound_changevolumeby,
		sound_setvolumeto,
		sound_volume,

		event_whenflagclicked,
		event_whenkeypressed,
		event_whenthisspriteclicked,
		event_whenstageclicked,
		event_whenbackdropswitchesto,
		event_whengreaterthan,
		event_whenbroadcastreceived,
		event_broadcast,
		event_broadcastandwait,

		control_wait,
		control_repeat,
		control_forever,
		control_if,
		control_if_else,
		control_wait_until,
		control_repeat_until,
		control_while,
		control_stop,
		control_start_as_clone,
		control_create_clone_of,
		control_create_clone_of_menu,
		control_delete_this_clone,

		sensing_touchingobject,
		sensing_touchingobjectmenu,
		sensing_touchingcolor,
		sensing_coloristouchingcolor,
		sensing_distanceto,
		sensing_distancetomenu,
		sensing_askandwait,
		sensing_answer,
		sensing_keypressed,
		sensing_keyoptions,
		sensing_mousedown,
		sensing_mousex,
		sensing_mousey,
		sensing_setdragmode,
		sensing_loudness,
		sensing_timer,
		sensing_resettimer,
		sensing_of,
		sensing_of_object_menu,
		sensing_current,
		sensing_dayssince2000,
		sensing_username,

		operator_add,
		operator_subtract,
		operator_multiply,
		operator_divide,
		operator_random,
		operator_gt,
		operator_lt,
		operator_equals,
		operator_and,
		operator_or,
		operator_not,
		operator_join,
		operator_letter_of,
		operator_length,
		operator_contains,
		operator_mod,
		operator_round,
		operator_mathop,

		data_variable,
		data_setvariableto,
		data_changevariableby,
		data_showvariable,
		data_hidevariable,

		data_listcontents,
		data_addtolist,
		data_deleteoflist,
		data_deletealloflist,
		data_insertatlist,
		data_replaceitemoflist,
		data_itemoflist,
		data_itemnumoflist,
		data_lengthoflist,
		data_listcontainsitem,
		data_showlist,
		data_hidelist,

		procedures_definition,
		procedures_prototype,
		procedures_call,
		argument_reporter_string_number,
		argument_reporter_boolean,
	};

	public struct Input
	{
		public bool isReference;
		public Sprite sprite;
		public string RawValue { private set; get; }

		public string value
		{
			set => RawValue = value;

			get
			{
				if (!isReference) return RawValue;
				return sprite.project.runner?.Execute(sprite, sprite.blocks[RawValue]) ?? throw new NullReferenceException();
			}
		}
	}

	public struct Mutation
	{
		public string tagName;
		//Array "children" isnt used in Scratch 3.0
		public string proccode;
		public string argumentids;
		public string argumentnames;
		public string warp;
		// public readonly bool Warp => Interpreter.Strbool(warp);
	}

	public Opcodes opcode = Opcodes.motion_movesteps;
	public string nextId = "";
	public string parentId = "";
	public List<Input> inputs = new();
	public readonly List<string> fields = new();
	public Mutation mutation;

	public Sprite sprite { get; internal set; }

	public Block Next(Sprite sprite)
	{
		return FindBlock(sprite, nextId);
	}

	public Block Parent(Sprite sprite)
	{
		return FindBlock(sprite, parentId);
	}

	public static Block FindBlock(Sprite sprite, string id)
	{
		if (id == string.Empty) return null;
		return sprite.blocks.First((a) => a.Key == id).Value;
	}
}

public class BlockConverter : JsonConverter<Dictionary<string, Block>>
{
	public override void WriteJson(JsonWriter writer, Dictionary<string, Block> value, JsonSerializer serializer)
	{
		throw new NotImplementedException();
	}

	public override Dictionary<string, Block> ReadJson(JsonReader reader, Type objectType, Dictionary<string, Block> existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		var obj = JObject.Load(reader);

		Dictionary<string, Block> blocks = new();

		foreach (var item in obj.Properties())
		{
			string key = item.Name;

			Block block = new()
			{
				opcode = Enum.Parse<Block.Opcodes>(item.Value["opcode"]?.ToString() ?? "motion_movesteps"),
				nextId = item.Value["next"]?.ToString() ?? "",
				parentId = item.Value["parent"]?.ToString() ?? "",
			};

			foreach (var input in (item.Value["inputs"] ?? throw new InvalidOperationException()).Values())
			{
				if (input[1]?.Type == JTokenType.Array)
				{
					block.inputs.Add(new()
					{
						value = input[1][1]?.ToString(),
						isReference = false
					});
				}
				else if (input[1]?.Type == JTokenType.String)
				{
					block.inputs.Add(new()
					{
						value = input[1].ToString(),
						isReference = true
					});
				}
			}
			block.fields.AddRange(from field in (item.Value["fields"] ?? throw new InvalidOperationException()).Values() select field[0]?.ToString());
			blocks.Add(key, block);

			if (item.Value["mutation"] == null) continue;

			Block.Mutation mutation = new()
			{
				tagName = item.Value["mutation"]?["tagName"]?.ToString() ?? "",
				proccode = item.Value["mutation"]?["proccode"]?.ToString() ?? "",
				argumentids = item.Value["mutation"]?["argumentids"]?.ToString() ?? "",
				argumentnames = item.Value["mutation"]?["argumentnames"]?.ToString() ?? "",
				// warp = (bool)item.Value["mutation"]["warp"].ToObject(typeof(bool))
			};

			block.mutation = mutation;
		}

		return blocks;
	}

	public override bool CanWrite => false;
}