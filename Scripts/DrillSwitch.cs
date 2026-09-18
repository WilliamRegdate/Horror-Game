using Godot;

public partial class DrillSwitch : Area3D, IInteractable
{
	// Called when the node enters the scene tree for the first time.
	[Export] AnimationPlayer _animation;
	GameManager _gameManager;
	[Export] AudioStreamPlayer3D _audio;
	public override void _Ready()
	{
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}
	public void Interact(Player player)
	{
		_gameManager = GetNode<GameManager>("/root/GameManager");
		_audio.Play();
		if (_gameManager.GeneratorsOn >= 3)
			Rpc(nameof(TurnOnDrill));
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void TurnOnDrill()
	{
		_animation.Play("drillPowerOn");
	}
}
