using Godot;

public partial class Locker : Area3D, IInteractable
{
	[Export] private AnimationPlayer animation;
	private bool _isOpen = false;

	public void Interact(Player player)
	{
		if (animation.IsPlaying())
		{
			return;
		}
		_isOpen = !_isOpen;
		Rpc(nameof(MoveDoor), _isOpen);
	}
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void MoveDoor(bool isOpen)
	{
		_isOpen = isOpen;
		animation.Play(isOpen ? "Open" : "Close");
	}
}
