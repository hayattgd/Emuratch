using Emuratch.Core.Render;
using Emuratch.Core.vm;
using Newtonsoft.Json;
using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;

namespace Emuratch.Core.Scratch;

public class Sprite
{
	const float DegToRad = MathF.PI / 180;
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

	public bool isClone = false;
	public Sprite? original;

	public BoundingBox boundingBox
	{
		get
		{
			Vector2 offset = Emurender.GetOffset(costume);
			Vector2 min = new(x - offset.X * 2, y + offset.Y * 2);
			Vector2 max = new(min.X + costume.image.Width, min.Y - costume.image.Height);
			return new(new(min, 0), new(max, 1));
		}
	}

	public BoundingBox RaylibBoundingBox
	{
		get
		{
			Vector2 bmin = Emurender.ScratchToRaylib(boundingBox.Min, Application.project);
			Vector2 bmax = Emurender.ScratchToRaylib(boundingBox.Max, Application.project);
			return new(new(bmin, 0), new(bmax, 1));
		}
	}

	public Vector2 RaylibPosition => Emurender.ScratchToRaylib(x, y, Application.project);
	public Vector2 RaylibOrigin
	{
		get
		{
			Vector2 raylibpos = RaylibPosition;
			Vector2 offset = Emurender.GetOffset(costume);
			return new(raylibpos.X - offset.X * 2, raylibpos.Y - offset.Y * 2);
		}
	}

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
		Sprite clone = new();

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

		foreach (var block in clone.blocks)
		{
			if (block.Value.opcode == Block.Opcodes.control_start_as_clone)
			{
				Thread thread = new(clone, block.Value);
				Program.app.threads.Add(thread);
				Application.runner.Execute(ref thread);
			}
		}

		return clone;
	}

	// Detects using Bounding boxes
	public bool SpritesIntersectApproximately(Sprite a, Sprite b)
	{
		return Raylib.CheckCollisionRecs(new(a.x, a.y, a.costume.image.Width, a.costume.image.Height), new(b.x, b.y, b.costume.image.Width, b.costume.image.Height));
	}

	public bool PointInsideSprite(Vector2 pos)
	{
		return x <= pos.X && x + costume.image.Width >= pos.X && y <= pos.Y && y + costume.image.Height >= pos.Y;
	}

	public Raylib_cs.Color? GetColorOnPixel(int x, int y)
	{
		// 1. ワールド行列（位置・回転・スケール）

		Matrix3x2 worldMatrix = Matrix3x2.CreateScale(size) * Matrix3x2.CreateRotation(direction * DegToRad) * Matrix3x2.CreateTranslation(new(this.x, this.y));

		// 2. 逆行列を求める（ワールド座標 → ローカル座標）

		if (!Matrix3x2.Invert(worldMatrix, out Matrix3x2 invMatrix)) return null; // 逆行列が求まらない場合は判定不可

		// 3. ポイントをローカル座標に変換

		Vector2 localPoint = Vector2.Transform(new(x, y), invMatrix);

		// 4. 範囲チェック

		if (localPoint.X < 0 || localPoint.Y < 0 || localPoint.X >= costume.image.Width || localPoint.Y >= costume.image.Height) return null;

		return costume.GetColor((int)localPoint.X, (int)localPoint.Y);
	}
}
