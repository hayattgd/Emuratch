using Emuratch.Core.Project;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Emuratch.Core.vm;

public class Block
{
    //https://en.scratch-wiki.info/wiki/List_of_Block_Opcodes
    public readonly static List<string> opcodes = new()
    {
        "motion_movesteps",
        "motion_turnright",
        "motion_turnleft",
        "motion_goto",
        "motion_gotoxy",
        "motion_glideto",
        "motion_glidesecstoxy",
        "motion_pointindirection",
        "motion_pointtowards",
        "motion_changexby",
        "motion_setx",
        "motion_changeyby",
        "motion_sety",
        "motion_ifonedgebounce",
        "motion_setrotationstyle",
        "motion_xposition",
        "motion_yposition",
        "motion_direction",

        "looks_sayforsecs",
        "looks_thinkforsecs",
        "looks_think",
        "looks_switchcostumeto",
        "looks_nextcostume",
        "looks_switchbackdropto",
        "looks_switchbackdroptoandwait",
        "looks_nextbackdrop",
        "looks_changesizeby",
        "looks_setsizeto",
        "looks_changeeffectby",
        "looks_seteffectto",
        "looks_cleargraphiceffects",
        "looks_gotofrontback",
        "looks_goforwardbackwardlayers",
        "looks_costumenumbername",
        "looks_backdropnumbername",
        "looks_size",

        "sound_playuntildone",
        "sound_play",
        "sound_stopallsounds",
        "sound_changeeffectby",
        "sound_seteffectto",
        "sound_cleareffects",
        "sound_changevolumeby",
        "sound_setvolumeto",
        "sound_volume",

        "event_whenflagclicked",
        "event_whenkeypressed",
        "event_whenthisspriteclicked",
        "event_whenstageclicked",
        "event_whenbackdropswitchesto",
        "event_whengreaterthan",
        "event_whenbroadcastreceived",
        "event_broadcast",
        "event_broadcastandwait",

        "control_wait",
        "control_forever",
        "control_if",
        "control_if_else",
        "control_wait_until",
        "control_repeat_until",
        "control_stop",
        "control_start_as_clone",
        "control_create_clone_of",
        "control_delete_this_clone",

        "sensing_touchingobject",
        "sensing_touchingcolor",
        "sensing_coloristouchingcolor",
        "sensing_distanceto",
        "sensing_askandwait",
        "sensing_answer",
        "sensing_keypressed",
        "sensing_mousedown",
        "sensing_mousex",
        "sensing_mousey",
        "sensing_setdragmode",
        "sensing_loudness",
        "sensing_timer",
        "sensing_resettimer",
        "sensing_of",
        "sensing_current",
        "sensing_dayssince2000",
        "sensing_username",

        "operator_add",
        "operator_subtract",
        "operator_multiply",
        "operator_divide",
        "operator_random",
        "operator_gt",
        "operator_lt",
        "operator_equals",
        "operator_and",
        "operator_or",
        "operator_not",
        "operator_join",
        "operator_letter_of",
        "operator_length",
        "operator_contains",
        "operator_mod",
        "operator_round",
        "operator_mathop",

        "data_variable",
        "data_setvariableto",
        "data_changevariableby",
        "data_showvariable",
        "data_hidevariable",

        "data_listcontents",
        "data_addtolist",
        "data_deleteoflist",
        "data_deletealloflist",
        "data_insertatlist",
        "data_replaceitemoflist",
        "data_itemoflist",
        "data_itemnumoflist",
        "data_lengthoflist",
        "data_listcontainsitem",
        "data_showlist",
        "data_hidelist",

        "procedures_definition",
        "procedures_call",
        "argument_reporter_string_number",
        "argument_reporter_boolean",
    };

    public string opcode = "";
    public Block? next;
    public string nextId = "";
    public Block? parent;
    public string parentId = "";
    public List<Block> inputs = new();
    public List<object> fields = new();
}

public class BlockConverter : JsonConverter<Dictionary<string, Block>>
{
	public override void WriteJson(JsonWriter writer, Dictionary<string, Block>? value, JsonSerializer serializer)
	{
		throw new NotImplementedException();
	}

	public override Dictionary<string, Block>? ReadJson(JsonReader reader, Type objectType, Dictionary<string, Block>? existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
        var obj = JObject.Load(reader).Values();

        Dictionary<string, Block> blocks = new() { };

        foreach (var item in obj)
        {
            string[] keys = item.Path.Split(',');
            //^1 = keys.Length - 1
            //I didn't knew it!
            string key = keys[^1];

            blocks.Add(key, new()
            {
                opcode = item["opcode"]?.ToString() ?? "",
                next = null,
                nextId = item["next"]?.ToString() ?? "",
                parent = null,
                parentId = item["parent"]?.ToString() ?? "",
            });
        }

        return blocks;
    }
}
