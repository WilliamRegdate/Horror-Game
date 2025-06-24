using Godot;
using System;

public partial class Player : CharacterBody3D
{
	[Export] public  float BaseSpeed;
	[Export] public  float JumpHeight;

	private float Speed;

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		// Handle Jump.
		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
		{
			velocity.Y = JumpHeight;
		}
		if (Input.IsActionPressed("sprint"))
		{
			Speed = BaseSpeed * 2;
		}
		else
		{
			Speed = BaseSpeed;
		}

		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;
		}
		else
		{
			//friction
			velocity.X = 0.0f;
			velocity.Z = 0.0f;
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}
