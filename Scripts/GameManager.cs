using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class GameManager : Node3D
{
    [Export] public PackedScene LevelMaker;
	[Export] Node3D _monster;
    public ProceduralGenerator Generator;
	public int GeneratorsOn;
    [Signal] public delegate void WorldReadyEventHandler();

	public NetworkHandler _networkHandler;

    public override void _Ready()
	{
		_networkHandler = GetNode<NetworkHandler>("/root/NetworkHandler");
		_networkHandler.NetworkStopped += OnServerClosed;
		
		Generator = LevelMaker.Instantiate() as ProceduralGenerator;
		Generator.PrewarmAabbCache();

		if (!Multiplayer.IsServer()) return; // clients wait for the server's data instead

		Task.Run(() =>
		{
			try
			{
				Generator.Generate();
				CallDeferred(nameof(OnGenerationComplete));
			}
			catch (Exception e)
			{
				GD.PrintErr("Generation failed: ", e);
			}
		});
	}

	private void OnServerClosed()
	{
		// _networkHandler.lastDisconnectReason  = "Disconnected from the server.";
		// _networkHandler.JustDisconnectedFromServer = true;
		ProcessMode = ProcessModeEnum.Disabled; //freeze processing to stop crash
		GetTree().ChangeSceneToFile("res://Menu.tscn");
		_networkHandler.NetworkStopped -= OnServerClosed;
	}

	private void OnGenerationComplete()
	{
		AddChild(Generator);
		Generator.BuildFromPlacements();
		EmitSignal(SignalName.WorldReady);
	}
	public void OnStartGame()
	{
		if (!Multiplayer.IsServer()) return;

		CanvasLayer filter = GetNode("CanvasLayer") as CanvasLayer;
		filter.Show();
		_monster.GlobalPosition = Generator.MonsterSpawn;
		
		//send generation data to clients
		var (sceneIndices, positions, rotations, areProps) = Generator.ExportPlacements();
		int[] intArray = new int[areProps.Length];
		for (int i = 0; i < areProps.Length; i++)
		{
			intArray[i] = areProps[i] ? 1 : 0;
		}

		Rpc(nameof(ReceivePlacements), sceneIndices, positions, rotations, intArray);

		
	}

	[Rpc(MultiplayerApi.RpcMode.Authority)]
	private void ReceivePlacements(int[] sceneIndices, Vector3[] positions, Vector3[] rotations, int[] areProps)
	{
		if (Multiplayer.IsServer()) return; // server already built its own copy directly above
		bool[] boolArray = new bool[areProps.Length];
		for (int i = 0; i < areProps.Length; i++)
		{
			boolArray[i] = areProps[i] == 1 ? true : false;
		}

		CanvasLayer filter = GetNode("CanvasLayer") as CanvasLayer;
		filter.Show();
		GD.Print(sceneIndices.Length," ", positions.Length," ", rotations.Length," ", boolArray.Length);

		AddChild(Generator);
		Generator.LoadPlacements(sceneIndices, positions, rotations, boolArray);
		Generator.BuildFromPlacements();
	}
}