using System;
using System.Net;
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

	[Export] private Label _errorMessage;
	[Export] private CanvasLayer _errorBox;
	[Export] private TextEdit _ipInput;
	[Export] private LineEdit _nameInput;
	[Export] private Label _playerList;

	private NetworkHandler _networkHandler;
    public override void _Ready()
    {
		_networkHandler = GetNode<NetworkHandler>("/root/NetworkHandler");
		_networkHandler.NetworkStopped += _partyMenu.Hide;
		_networkHandler.UpdatePlayerNames += UpdatePlayerList;
		Multiplayer.ConnectedToServer += AddPlayerName;
		Multiplayer.PeerDisconnected += RemovePlayerName;

        _optionsMenu.Visible = false;
		_playMenu.Visible = false;
		_partyMenu.Visible = false;
		_startGameButton.Visible = false;
		_loadProgress.Visible = true;
		_errorBox.Visible = false;

		
		Input.MouseMode = Input.MouseModeEnum.Visible;
    }

	public override void _Process(double delta)
    {
		if (_networkHandler.JustDisconnectedFromServer)
		{
			PrintError(_networkHandler.lastDisconnectReason);
			_networkHandler.JustDisconnectedFromServer = false;
		}
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
        _worldInstance = packed.Instantiate() as GameManager;
        _worldInstance.Visible = false;
		_worldInstance.Connect(
		GameManager.SignalName.WorldReady,
		Callable.From(() => OnWorldReady(_worldInstance))
		);

		StartGame += _worldInstance.OnStartGame;
		
		GetTree().Root.AddChild(_worldInstance);

		if (_worldInstance?.LevelMaker != null)
		{
        	_loadProgress.Value = _worldInstance.Generator.Progress;
		}
    }
	private void UpdatePlayerList()
	{
		_playerList.Text = "";
		foreach((int id, string name) in _networkHandler.PlayerNames)
		{
			_playerList.Text += $"{name}\n";
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
		if (!_networkHandler.StartServer())
		{ 
			PrintError("Cannot host server: already connected to a server.\n Try leaving the current server first");
			return;
		}
		_networkHandler.SubmitPlayerName(_nameInput.Text);
		_partyMenu.Visible = true;
		_isLoading = true;
		_loadProgress.Visible = true;
		ResourceLoader.LoadThreadedRequest(GameScenePath);
	}
	public void PressedJoinSignal()
	{
		_networkHandler.IpAddress = _ipInput.Text;
		if (!_networkHandler.StartClient())
		{
			PrintError("Cannot Join server: already connected to a server.\n Try leaving the current server first");
			return;
		}

		_partyMenu.Visible = true;
		_loadProgress.Visible = false;
		CanvasItem ipTextBox = _playMenu.GetChild(2) as CanvasItem;
		ipTextBox.Visible = false;
		_playMenu.Visible = false;


		//load the Game manager but dont do anything as the host server does that
		PackedScene packed = ResourceLoader.Load(GameScenePath) as PackedScene;
        _worldInstance = packed.Instantiate() as GameManager;
		GetTree().Root.AddChild(_worldInstance);
	}
	public void ToggledJoinSignal(bool toggledOn)
	{
		CanvasItem ipTextBox = _playMenu.GetChild(2) as CanvasItem;
		ipTextBox.Visible = toggledOn;
	}

	public void PressedLeaveGroupSignal()
	{
		_networkHandler.Disconnect();
		_worldInstance.QueueFree();
	}
    public override void _ExitTree()
    {
        _networkHandler.NetworkStopped -= _partyMenu.Hide;
		StartGame -= _worldInstance.OnStartGame;
		_networkHandler.UpdatePlayerNames -= UpdatePlayerList;
    }
	[Signal] public delegate void StartGameEventHandler();
	public void PressedStartGameSignal()
	{
		EmitSignal(SignalName.StartGame);
		CallDeferred(nameof(SendBeginGameRpc));
	}

	private void SendBeginGameRpc()
	{
		Rpc(nameof(BeginGame));
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	private void BeginGame()
	{
		CallDeferred(nameof(SwitchToMainScene));
	}
	private void SwitchToMainScene()
	{
		_worldInstance.Visible = true;

		GetTree().CurrentScene?.QueueFree();
		GetTree().CurrentScene = _worldInstance;

		_worldInstance.GetNode<PlayerSpawner>("Players").BeginSpawning();
	}

	private void PrintError(string message)
	{
		_errorBox.Show();
		_errorMessage.Text = message;
	}
	private void AddPlayerName()
	{
		_networkHandler.SubmitPlayerName(_nameInput.Text);
	}
	public void RemovePlayerName(long id)
	{
		_networkHandler.RemovePlayerName((int)id);
	}
}

