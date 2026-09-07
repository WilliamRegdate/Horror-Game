using Godot;

public partial class DrillSwitch : Area3D, IInteractable
{
	// Called when the node enters the scene tree for the first time.
	[Export] AnimationPlayer _animation;
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}
	public void Interact(Player player)
	{
		Rpc(nameof(TurnOnDrill));
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void TurnOnDrill()
	{
		_animation.Play("drillPowerOn");
	}
}
