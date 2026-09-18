using Godot;

public partial class Battery : Area3D, IInteractable
{
	[Export] CollisionShape3D _shape;
	public void Interact(Player player)
	{
		Node3D parent = GetParent() as Node3D;
		parent.Hide();
		_shape.Disabled = true;
		player.Batteries += 1;
		Rpc(nameof(Remove));
	}
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void Remove()
	{
		Node3D parent = GetParent() as Node3D;
		parent.Hide();
		Monitoring = false;
		_shape.Disabled = true;
	}
}
