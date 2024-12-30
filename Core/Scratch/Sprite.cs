using Emuratch.Core.vm;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Emuratch.Core.Scratch;

public class Sprite
{
	public enum RotationStyle
	{
		all_around,
		left_right,
		dont_rotate
	};

	public bool isStage = false;

	public string name = "";

	[JsonConverter(typeof(VariableConverter))]
	public Variable[] variables = Array.Empty<Variable>();

	[JsonConverter(typeof(BlockConverter))]
	public Dictionary<string, Block> blocks = new();

	[JsonConverter(typeof(CommentConverter))]
	public Comment[] comments = Array.Empty<Comment>();

	public int currentCostume;

	[JsonConverter(typeof(CostumeConverter))]
	public Costume[] costumes = Array.Empty<Costume>();

	public Costume costume
	{
		get => costumes[currentCostume];
		set => currentCostume = costumes.ToList().IndexOf(value);
	}

	[JsonConverter(typeof(SoundConverter))]
	public Sound[] sounds = Array.Empty<Sound>();

	public float volume = 100;

	public int layoutOrder;

	public void SetLayoutOrder(int order)
	{
		layoutOrder = Math.Clamp(order, 0, Application.project.sprites.Length);
	}

	//Sprite
	public bool visible = true;
	public float x = 0;
	public float y = 0;
	public float size = 100;
	public float direction = 90;
	public bool draggable = false;
	public string rotationStyle = "all around";

	public RotationStyle Rotationstyle
	{
		get
		{
			return rotationStyle switch
			{
				"all around" => RotationStyle.all_around,
				"left-right" => RotationStyle.left_right,
				_ => RotationStyle.dont_rotate
			};
		}

		set
		{
			rotationStyle = value switch
			{
				RotationStyle.all_around => "all around",
				RotationStyle.left_right => "left-right",
				RotationStyle.dont_rotate => "don't rotate",
				_ => rotationStyle
			};
		}
	}

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
	
	public void UpdateBlocks()
	{
		foreach (var block in blocks)
		{
			for (int i = 0; i < block.Value.inputs.Count; i++)
			{
				//Normal foreach loop doesn't work here
				//This is because the foreach loop creates a copy of the value
				//So the value is changed in the copy, not the original
				Block.Input input = block.Value.inputs[i];
				input.sprite = this;
				block.Value.inputs[i] = input;
			}
		}
	}
}
