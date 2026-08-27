using Godot;
using System;

public partial class Fan : Node3D
{
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		RotationDegrees += Vector3.Up * 180 * (float)delta;
	}
}
