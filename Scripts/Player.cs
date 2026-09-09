using Godot;

public partial class Player : CharacterBody3D
{
	[Export] public  float BaseSpeed;
	[Export] public  float JumpHeight;
	public float SoundLevel; //level of sound currently being made by player

	[Export] private RayCast3D _checkForHead;
	public float Speed;
	[Export] public CollisionShape3D _collider;
	private CapsuleShape3D _capsule;
	[Export] PlayerCamera _camera;
	[Export] Node3D _playerMesh; 
	[Export] Node3D _torchNode;
	private bool debugMode;
	[Export] public bool IsCrouched;
	NetworkHandler _networkHandler;

    public override void _Ready()
    {
		_networkHandler = GetNode<NetworkHandler>("/root/NetworkHandler");
        _capsule = (CapsuleShape3D)_collider.Shape;
		_torchNode.Hide();
		if (!IsMultiplayerAuthority()) return;
		_playerMesh.Hide();
		_playerMesh.QueueFree();
		_torchNode.Show();
    }
	    public override void _Process(double delta)
    {
		if (!IsMultiplayerAuthority()) return;
        if (Input.IsActionJustPressed("ui_cancel"))
		{
			_networkHandler.Disconnect();
		}
    }


	public override void _PhysicsProcess(double delta)
	{

		if (!IsMultiplayerAuthority()) return;
		Vector3 velocity = Velocity;

		if (Input.IsActionJustPressed("debug"))
		{
			debugMode = !debugMode;
			_collider.Disabled = debugMode;
		}

		if (debugMode)
		{
			//creative mode flight
			if (Input.IsActionPressed("game_jump"))
			{
				Position += new Vector3(0, Speed*0.05f, 0);
			}
			if (Input.IsActionPressed("game_crouch"))
			{
				Position += new Vector3(0, -Speed*0.05f, 0);
			}
			if (Input.IsActionPressed("game_sprint"))
			{
				Speed = BaseSpeed * 6f;
			}
		}
		else
		{
			//Add the gravity.
			if (!IsOnFloor())
			{
				velocity += GetGravity() * (float)delta;
			}

			// Handle Jump.
			if (Input.IsActionJustPressed("game_jump") && IsOnFloor())
			{
				velocity.Y = JumpHeight;
			}

			// if (Input.IsActionJustPressed("ui_cancel"))
			// 	SaveSceneToDisk(GetTree().Root.GetChild(0), "res://debug_dungeon_snapshot.tscn");
			if (Input.IsActionPressed("game_crouch"))
			{
				IsCrouched = true;
			}
			else
			{
				if (!_checkForHead.IsColliding())
				{
					Speed = BaseSpeed;
					IsCrouched = false;
					_camera.IsCrouching = false;
					_collider.Position = Vector3.Zero;
					_capsule.Height = 2.0f;
					_torchNode.Position = new (0.75f, -1, -0.65f);
				}
			}
			if (IsCrouched)
			{
				_camera.IsCrouching = true;
				Speed = BaseSpeed * 0.3f;
				_collider.Position = new(0, -0.56f, 0);
				_capsule.Height = 0.88f;
				_torchNode.Position = new (0.75f, 0, -0.65f);
			}
			else if (Input.IsActionPressed("game_sprint"))
			{
				Speed = BaseSpeed * 2f;
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
