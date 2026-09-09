using Godot;
using System;

public partial class InteractRay : RayCast3D
{
	[Export] private Player _player;
	public override void _Ready()
	{
		CollideWithAreas = true;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		if (!IsMultiplayerAuthority())
		{
			return;
		}

		if (Input.IsActionJustPressed("interact")&& IsColliding())
		{

			var collider = GetCollider() as Node;
			if (collider is IInteractable interactable)
			{
				interactable.Interact(_player);
			}

		}	
	}
}
