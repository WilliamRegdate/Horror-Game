using Godot;
using System;

public partial class Locker : Area3D, IInteractable
{
	[Export] private AnimationPlayer animation;
	private bool _isOpen = false;

	public void Interact(Player player)
	{
		Rpc(nameof(MoveDoor), _isOpen);
	}
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void MoveDoor(bool open)
	{
		_isOpen = open;
		if (_isOpen)
		{
			animation.Play("Close");
		}
		else
		{
			animation.Play("Open");
		}
		_isOpen = !_isOpen;
	}
}
