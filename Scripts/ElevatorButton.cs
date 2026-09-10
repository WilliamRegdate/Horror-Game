using Godot;
using System;

public partial class ElevatorButton : Area3D, IInteractable
{
	[Export] private AnimationPlayer _startSpeech;
	[Export] private AnimationPlayer _GoDown;
	bool _pressed;

	public void Interact(Player player)
	{
		if (_pressed)
		{
			return;
		}
		Rpc(nameof(GoDownForPeers));
	}
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void GoDownForPeers()
	{
		_startSpeech.Play("RESET");
		_GoDown.Play("Descent");
		_pressed = true;
	}
}
