using Godot;
using System;

public partial class Player : CharacterBody3D
{
	[Export] public  float BaseSpeed;
	[Export] public  float JumpHeight;
	public float SoundLevel; //level of sound currently being made by player

	[Export] private RayCast3D _checkForHead;
	public float Speed;
	[Export] private CollisionShape3D _collider;
	private CapsuleShape3D _capsule;
	[Export] PlayerCamera _camera;
	[Export] Label _soundLabel;

	//make sound
	float _soundMultiplier = 1.0f;

    public override void _Ready()
    {
        _capsule = (CapsuleShape3D)_collider.Shape;
    }

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		//Add the gravity.
		// if (!IsOnFloor())
		// {
		// 	velocity += GetGravity() * (float)delta;
		// }

		//creative mode flight
		if (Input.IsActionPressed("game_jump"))
		{
			Position += new Vector3(0, Speed*0.05f, 0);
		}
		if (Input.IsActionPressed("game_crouch"))
		{
			Position += new Vector3(0, -Speed*0.05f, 0);
		}

		_soundLabel.Text = $"sound level: {SoundLevel}";

		// Handle Jump.
		if (Input.IsActionJustPressed("game_jump") && IsOnFloor())
		{
			velocity.Y = JumpHeight;
		}

		if (Input.IsActionJustPressed("ui_cancel"))
			SaveSceneToDisk(GetTree().Root.GetChild(0), "res://debug_dungeon_snapshot.tscn");
		if (Input.IsActionPressed("game_crouch"))
		{
			_camera.IsCrouching = true;
			Speed = BaseSpeed * 0.3f;
			_collider.Position = new(0, -0.56f, 0);
			_capsule.Height = 0.88f;
			_soundMultiplier = 0.05f;
		}
		else if (Input.IsActionPressed("game_sprint"))
		{
			Speed = BaseSpeed * 5.3f;
			_soundMultiplier = 5;
		}
		
		else
		{
			if (!_checkForHead.IsColliding())
			{
				Speed = BaseSpeed;
				_camera.IsCrouching = false;
				_collider.Position = Vector3.Zero;
				_capsule.Height = 2.0f;
				_soundMultiplier = 1.0f;
			}

		}

		Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X * 0.6f, 0, inputDir.Y)).Normalized();
		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;
		}
		else
		{
			//friction
			velocity.X = 0.0f;
			velocity.Z = 0.0f;
		}

		SoundLevel = direction.LengthSquared() * _soundMultiplier;

		Velocity = velocity;
		MoveAndSlide();
	}
	private void SaveSceneToDisk(Node root, string path)
	{
		SetOwnerRecursive(root, root); // walk existing tree, assign ownership before packing

		var packedScene = new PackedScene();
		Error result = packedScene.Pack(root);

		if (result != Error.Ok)
		{
			GD.PrintErr("Failed to pack scene: ", result);
			return;
		}

		Error saveResult = ResourceSaver.Save(packedScene, path);
		if (saveResult != Error.Ok)
			GD.PrintErr("Failed to save scene: ", saveResult);
		else
			GD.Print("Saved scene to: ", path);
	}

	private void SetOwnerRecursive(Node node, Node ownerRoot)
	{
		foreach (Node child in node.GetChildren())
		{
			child.Owner = ownerRoot;
			SetOwnerRecursive(child, ownerRoot);
		}
	}
}
