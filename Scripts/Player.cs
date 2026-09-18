using Godot;
using System.Linq;

public partial class Player : CharacterBody3D
{
	[Export] public  float BaseSpeed;
	[Export] public  float JumpHeight;
	public float SoundLevel; //level of sound currently being made by player

	[Export] private RayCast3D _checkForHead;
	public float Speed;
	[Export] public CollisionShape3D Collider;
	public double BatteryLife;
	public int Batteries = 5;
	public int Chalk = 10;
	private CapsuleShape3D _capsule;
	[Export] PlayerCamera _camera;
	[Export] Node3D _playerMesh; 
	[Export] Node3D _torchNode;
	private bool debugMode = false;
	[Export] public bool IsCrouched;
	NetworkHandler _networkHandler;
	[Export] private AudioListener3D _listener;

    public override void _Ready()
    {
		
		_networkHandler = GetNode<NetworkHandler>("/root/NetworkHandler");
        _capsule = (CapsuleShape3D)Collider.Shape;
		if (!IsMultiplayerAuthority()) return;
		_playerMesh.Hide();
		_playerMesh.QueueFree();
		_listener.MakeCurrent();
    }
	public override void _PhysicsProcess(double delta)
	{
		if (!IsMultiplayerAuthority())
		{
			if (IsCrouched)
			{
				_camera.IsCrouching = true;
				Speed = BaseSpeed * 0.3f;
				Collider.Position = new(0, -0.56f, 0);
				_capsule.Height = 0.88f;
				_torchNode.Position = new (0.75f, 0, -0.65f);
			}
			return;
		}
		Vector3 velocity = Velocity;

		if (Input.IsActionJustPressed("debug"))
		{
			debugMode = !debugMode;
			Collider.Disabled = debugMode;

			GetViewport().DebugDraw = debugMode
				? Viewport.DebugDrawEnum.Unshaded
				: Viewport.DebugDrawEnum.Disabled;
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
					Collider.Position = Vector3.Zero;
					_capsule.Height = 2.0f;
					_torchNode.Position = new (0.75f, -1, -0.65f);
				}
			}
			if (IsCrouched)
			{
				_camera.IsCrouching = true;
				Speed = BaseSpeed * 0.3f;
				Collider.Position = new(0, -0.56f, 0);
				_capsule.Height = 0.88f;
				_torchNode.Position = new (0.75f, 0, -0.65f);
			}
			else if (Input.IsActionPressed("game_sprint"))
			{
				Speed = BaseSpeed * 2f;
			}
		}

		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_up", "move_down");
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
	
	public void Die()
	{
		if (!Multiplayer.IsServer()) return;

		int ownerId = GetMultiplayerAuthority();

		// Stop replicating this node before anything else happens —
		// no RPCs, no ragdoll spawn, nothing, until this is off.
		var synchronizer = GetNodeOrNull<MultiplayerSynchronizer>("MultiplayerSynchronizer");
		if (synchronizer != null)
			synchronizer.ProcessMode = ProcessModeEnum.Disabled;

		GameManager gameManager = GetNode<GameManager>("/root/GameManager");
		NodePath path = gameManager.SpawnRagdoll(ownerId, _networkHandler.PlayerNames[ownerId], GlobalPosition, GlobalRotation);

		if (ownerId == Multiplayer.GetUniqueId())
			DieRpc(path);
		else
			RpcId(ownerId, nameof(DieRpc), path);

		PlayerSpawner spawner = GetParent() as PlayerSpawner;
		spawner.DespawnForPeer(ownerId);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
	public void DieRpc(NodePath path)
	{
		GameManager gameManager = GetNode<GameManager>("/root/GameManager");
		PackedScene spectator = (PackedScene)ResourceLoader.Load("res://Prefabs/spectator.tscn");
		Spectator specNode = spectator.Instantiate() as Spectator;
		specNode.CurrentView = GetNode(path) as Node3D;

		gameManager.AddChild(specNode);
	}
}
