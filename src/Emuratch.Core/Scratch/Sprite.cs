using Emuratch.Core.vm;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Emuratch.Core.Utils;
using Emuratch.Core.Render;

namespace Emuratch.Core.Scratch;

public class Sprite(Project project)
{
	public struct Dialog
	{
		public DialogType type;
		public string text;
		public Number duration;
		public bool infinite;
	}

	public enum DialogType
	{
		Say,
		Think
	};

	public enum RotationStyle
	{
		all_around,
		left_right,
		dont_rotate
	};

	public Project project = project;
	public Dialog dialog;

	public bool isStage = false;

	public string name = "";

	[JsonConverter(typeof(VariableConverter))]
	public Variable[] variables = Array.Empty<Variable>();

	[JsonConverter(typeof(BlockConverter))]
	public Dictionary<string, Block> blocks = new();

	[JsonConverter(typeof(CommentConverter))]
	public Comment[] comments = Array.Empty<Comment>();

	public int currentCostume;

	public void SetCostume(int idx)
	{
		currentCostume = (idx + 1) % (costumes.Length + 1) - 1;
	}

	[JsonConverter(typeof(CostumeConverter))]
	public Costume[] costumes = Array.Empty<Costume>();

	public Costume costume
	{
		get => costumes[currentCostume];
		set => currentCostume = costumes.ToList().IndexOf(value);
	}

	public Sound[] sounds = Array.Empty<Sound>();

	public Number volume = 100;

	public int layoutOrder;

	public void SetLayoutOrder(int order)
	{
		layoutOrder = int.Max(order, 0);
	}

	//Sprite
	public bool visible = true;
	public Number x = 0;
	public Number y = 0;
	public Vector2 Position => new(x, y);
	public Number size = 100;
	public Number direction = 90;
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
	public Number tempo = 60;
	public Number videoTransparency = 50;
	public string videoState = "on";
	public string textToSpeechLanguage = "";

	public bool isClone = false;
	public Sprite? original;

	public BoundingBox boundingBox
	{
		get
		{
			Vector2 offset = RenderUtility.GetOffset(costume);
			Vector2 min = new(x - offset.X * 2, y + offset.Y * 2);
			Vector2 max = new(min.X + costume.Width, min.Y - costume.Height);
			return new(min, max);
		}
	}

	public void SetPosition(Number x, Number y)
	{
		this.x = x;
		this.y = y;
		KeepInsideStage(project.width, project.height);
	}

	public void SetRotation(Number dir)
	{
		float result = dir;
		if (dir > 180)
		{
			result = -180 - (180 - dir);
		}
		else if (dir < -179)
		{
			result = 180 + (180 + dir);
		}

		direction = result;
	}

	public void ClampInsideStage(uint width, uint height)
	{
		float halfwidth = width / 2;
		float halfheight = height / 2;

		Number up = halfheight - boundingBox.Min.Y;
		Number down = -halfheight - boundingBox.Max.Y;
		Number right = halfwidth - boundingBox.Max.X;
		Number left = -halfwidth - boundingBox.Min.X;

		if (up < 0) { y += up; }
		if (down > 0) { y += down; }
		if (right < 0) { x += right; }
		if (left > 0) { x += left; }
	}

	public void KeepInsideStage(uint width, uint height)
	{
		float halfwidth = width / 2;
		float halfheight = height / 2;

		Number up = halfheight - boundingBox.Max.Y;
		Number down = -halfheight - boundingBox.Min.Y;
		Number right = halfwidth - boundingBox.Min.X;
		Number left = -halfwidth - boundingBox.Max.X;

		if (up < 0) { y += up; }
		if (down > 0) { y += down; }
		if (right < 0) { x += right; }
		if (left > 0) { x += left; }
	}

	internal void UpdateBlocks()
	{
		foreach (var block in blocks)
		{
			block.Value.sprite = this;
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

	public static Sprite Clone(Sprite original)
	{
		Sprite clone = new(original.project);

		clone.isClone = true;
		clone.original = original;

		clone.name = original.name;

		clone.x = original.x;
		clone.y = original.y;
		clone.direction = original.direction;
		clone.rotationStyle = original.rotationStyle;

		clone.variables = original.variables;
		clone.blocks = original.blocks;
		clone.costumes = original.costumes;
		clone.currentCostume = original.currentCostume;
		clone.sounds = original.sounds;
		// clone.comments = original.comments; // probably not needed

		clone.draggable = original.draggable;

		clone.size = original.size;
		clone.visible = original.visible;
		clone.layoutOrder = original.layoutOrder - 1;

		clone.tempo = original.tempo;
		clone.volume = original.volume;

		clone.textToSpeechLanguage = original.textToSpeechLanguage;

		clone.videoState = original.videoState;
		clone.videoTransparency = original.videoTransparency;

		return clone;
	}
}