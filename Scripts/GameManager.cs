using Godot;
using System;
using System.Threading.Tasks;

public partial class GameManager : Node3D
{
    [Export] public PackedScene LevelMaker;
    public ProceduralGenerator Generator;
    [Signal] public delegate void WorldReadyEventHandler();

    public override void _Ready()
	{

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
		
		//send generation data to clients
		var (sceneIndices, positions, rotations) = Generator.ExportPlacements();
		Rpc(nameof(ReceivePlacements), sceneIndices, positions, rotations);
		
	}

	[Rpc(MultiplayerApi.RpcMode.Authority)]
	private void ReceivePlacements(int[] sceneIndices, Vector3[] positions, Vector3[] rotations)
	{
		if (Multiplayer.IsServer()) return; // server already built its own copy directly above

		CanvasLayer filter = GetNode("CanvasLayer") as CanvasLayer;
		filter.Show();

		AddChild(Generator);
		Generator.LoadPlacements(sceneIndices, positions, rotations);
		Generator.BuildFromPlacements();
	}
}