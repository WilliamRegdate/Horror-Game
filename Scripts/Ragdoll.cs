using Godot;

public partial class Ragdoll : Node3D
{
	[Export] PhysicalBoneSimulator3D _ragdoll;
	[Export] public Label3D Label;
	[Export] public Node3D View;
	double _timer = 10;
	public void StartRagdoll()
	{
		_ragdoll.PhysicalBonesStartSimulation();
	}

	public override void _Process(double delta)
	{
		if (_timer > 0)
		{
			_timer -= delta;
			return;
		}
		_ragdoll.PhysicalBonesStopSimulation();
		SetProcess(false); //stop node proccessing
	}
}
