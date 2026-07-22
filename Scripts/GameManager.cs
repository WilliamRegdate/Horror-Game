using Godot;
using System;
using System.Threading.Tasks;


public partial class GameManager : Node3D
{
	[Export] public PackedScene LevelMaker;
	[Export] private PackedScene _player;

	public ProceduralGenerator Generator;
	[Signal] public delegate void WorldReadyEventHandler();
    public override void _Ready()
    {
		GD.Print($"GameManager._Ready called — instance {GetInstanceId()}");
		var events = GetNode<MainMenu>("/root/Menu");
    	events.StartGame += OnStartGame;
		Generator = LevelMaker.Instantiate() as ProceduralGenerator;
		Generator.PrewarmAabbCache();
        Task.Run(() =>
        {  
            try
			{
				Generator.Generate();
				CallDeferred(nameof(OnGenerationComplete), Generator);
			}
			catch (Exception e)
			{
				GD.PrintErr("Generation failed: ", e);
			}
        });
    }
	[Signal] public delegate void StartGameEventHandler();

    private void OnGenerationComplete(Node generatorObj)
	{
		var generator = (ProceduralGenerator)generatorObj;
		AddChild(generator);
		
		generator.BuildFromPlacements();
		EmitSignal(SignalName.WorldReady);
	}
	private void OnStartGame()
	{
		Node playerInstance =_player.Instantiate();
		AddChild(playerInstance);

		playerInstance.AddToGroup("Player");

		EmitSignal(SignalName.StartGame);
	}
}
