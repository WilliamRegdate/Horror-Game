using Godot;

public partial class GeneratorSwitch : Area3D, IInteractable
{
	// Called when the node enters the scene tree for the first time.
	[Export] SpotLight3D _light;
	[Export] AnimationPlayer _player;
	bool _isOn;
	public override void _Ready()
	{
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}
	public void Interact(Player player)
	{
		Rpc(nameof(RunGenerator));
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void RunGenerator()
	{
		if (_isOn)
			return;
			_player.Play("turnOn");
		_isOn = true;
		GameManager gameManager = GetNode<GameManager>("/root/GameManager");
		gameManager.GeneratorsOn++;
	}
}
