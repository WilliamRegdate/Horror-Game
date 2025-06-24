using Godot;
using System;

public partial class Wall : MeshInstance3D
{
	[Export] Node3D Corridor;
	[Export] int WallNum; //0 = left, 1 = front, 2 = right, 3 = back
	[Export] PackedScene CorridorPrefab = GD.Load<PackedScene>("res://Prefabs/corridor.tscn");
	public int Chance = 5;
	

	public override void _Ready()
	{
		GD.Seed(Time.GetTicksMsec());
		
		if (GD.RandRange(0, Chance) < 10)
		{
			// Use CallDeferred to add the child after the current frame
			CallDeferred(MethodName.SpawnCorridor);
		}
	}

	private void SpawnCorridor()
	{
		Node3D newWall = (Node3D)CorridorPrefab.Instantiate();
		Corridor.AddChild(newWall);
		newWall.Rotation = Corridor.Rotation;// + GetParent<Node3D>().Rotation;
		// Position logic remains the same
		if (WallNum == 0)
		{
			newWall.Position = new Vector3(Corridor.Position.X, Corridor.Position.Y, Corridor.Position.Z + 6);
		}
		else if (WallNum == 1)
		{
			newWall.Position = new Vector3(Corridor.Position.X + 6, Corridor.Position.Y, Corridor.Position.Z);
		}
		else if (WallNum == 2)
		{
			newWall.Position = new Vector3(Corridor.Position.X, Corridor.Position.Y, Corridor.Position.Z - 6);
		}
		else
		{
			newWall.Position = new Vector3(Corridor.Position.X - 6, Corridor.Position.Y, Corridor.Position.Z);
		}
		
		GD.Print("made object");
		QueueFree();
		GetParent().QueueFree();
	}
}
