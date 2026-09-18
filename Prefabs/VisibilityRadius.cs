using Godot;
using System;

public partial class VisibilityRadius : Area3D
{
	[Export] Torch _torch;
	[Export] public Player Player;
	[Export] CollisionShape3D _collider;

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (!Multiplayer.IsServer())
			return;
		if (_torch.Visible)
		{
			_collider.Disabled = false;
		}
		else
		{
			_collider.Disabled = true;
		}
	}
}
