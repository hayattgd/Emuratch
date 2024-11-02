using Emuratch.Core.vm;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Emuratch.Core.Scratch;

public class Sprite
{
    public bool isStage = false;

    public string name = "";

    [JsonConverter(typeof(VariableConverter))]
    public Variable[] variables = Array.Empty<Variable>();

    [JsonConverter(typeof(BlockConverter))]
    public Dictionary<string, Block> blocks = new() { };

	[JsonConverter(typeof(CommentConverter))]
	public Comment[] comments = Array.Empty<Comment>();

    public int currentCostume = 0;

	[JsonConverter(typeof(CostumeConverter))]
    public Costume[] costumes = Array.Empty<Costume>();

	[JsonConverter(typeof(SoundConverter))]
	public Sound[] sounds = Array.Empty<Sound>();

    public float volume = 100;
    public int layoutOrder = 0;

    //Sprite
    public bool visible = true;
    public float x = 0;
    public float y = 0;
    public float size = 100;
    public float direction = 90;
    public bool draggable = false;
    public string rotationStyle = "all around";

    //Stage
    public float tempo = 60;
    public float videoTransparency = 50;
    public string videoState = "on";
    public string textToSpeechLanguage = "";

	public IEnumerator<Unloadable> GetEnumerator()
	{
		foreach (var item in costumes)
		{
			yield return item;
		}

		foreach (var item in sounds)
		{
			yield return item;
		}
	}
}
