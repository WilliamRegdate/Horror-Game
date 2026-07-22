using Godot;

public partial class MainMenu : Node3D
{
	private const string GameScenePath = "res://game.tscn";
	private bool _isLoading = false;
	private GameManager _worldInstance;

	[Export] private VBoxContainer _playMenu;
	[Export] private TabContainer _optionsMenu;
	[Export] private VBoxContainer _partyMenu;
	[Export] private ProgressBar _loadProgress;
	[Export] private Button _startGameButton;


	private NetworkHandler _networkHandler;
    public override void _Ready()
    {
		_networkHandler = GetNode<NetworkHandler>("/root/NetworkHandler");
        _optionsMenu.Visible = false;
		_playMenu.Visible = false;
		_partyMenu.Visible = false;
		_startGameButton.Visible = false;
		_loadProgress.Visible = true;
    }

	public override void _Process(double delta)
    {
        if (!_isLoading) return;

        var status = ResourceLoader.LoadThreadedGetStatus(GameScenePath);
        if (status != ResourceLoader.ThreadLoadStatus.Loaded)
		{ 
			if (_worldInstance?.LevelMaker != null)
			{
				_loadProgress.Value = _worldInstance.Generator.Progress;
			}
			return;
		}

        var packed = (PackedScene)ResourceLoader.LoadThreadedGet(GameScenePath);
        _worldInstance = packed.Instantiate() as GameManager; // creates nodes, doesn't call _Ready yet

        _worldInstance.Visible = false;
		_worldInstance.Connect(
		GameManager.SignalName.WorldReady,
		Callable.From(() => OnWorldReady(_worldInstance))
		);
		GetTree().Root.AddChild(_worldInstance);

		if (_worldInstance?.LevelMaker != null)
		{
        	_loadProgress.Value = _worldInstance.Generator.Progress;
		}
    }

    private void OnWorldReady(Node3D worldInstance)
    {
		_isLoading = false;
		_loadProgress.Visible = false;
		_startGameButton.Visible = true;
    }

	public void ToggledPlaySignal(bool toggledOn)
	{
		_playMenu.Visible = toggledOn;
	}
	public void ToggledOptionsSignal(bool toggledOn)
	{
		_optionsMenu.Visible = toggledOn;
	}
	public void PressedQuitSignal()
	{
		GetTree().Quit();
	}
	public void PressedHostSignal()
	{
		if (_isLoading) return;
		_networkHandler.StartServer();
		_partyMenu.Visible = true;
		_isLoading = true;
		ResourceLoader.LoadThreadedRequest(GameScenePath);
	}
	public void ToggledJoinSignal(bool toggledOn)
	{
		_networkHandler.StartClient();
	}
	[Signal] public delegate void StartGameEventHandler();
	public void PressedStartGameSignal()
	{
		EmitSignal(SignalName.StartGame);
		CallDeferred(nameof(SwitchToMainScene));
	}
	private void SwitchToMainScene()
	{
		_worldInstance.Visible = true;
        GetTree().CurrentScene?.QueueFree();
        GetTree().CurrentScene = _worldInstance;
	}
}
