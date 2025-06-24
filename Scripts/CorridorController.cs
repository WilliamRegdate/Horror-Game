using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;



public partial class CorridorController : StaticBody3D
{
	[Export] string[] Rooms;
	[Export] int LevelSize;
	[Export] int[] RoomWeights;
	public RoomManager roomManager;
	private List<DoorData> OpenDoors = new();
	private List<Vector3> FullBoxes = new();
	private int totalWeight;
	public override void _Ready()
	{
		roomManager = new();
		foreach (string item in Rooms)
		{
			roomManager.AddRoom(item);
		}
		foreach (int weight in RoomWeights)
		{
			totalWeight += weight;
		}

		SpawnRandomRoomAt(new(0, 0, 0), new(0, 90, 0));
		int roomCounter = 0;
		bool isOpen;
		while (roomCounter < LevelSize && OpenDoors.Count != 0)
		{
			
			//check if door is not colliding with any objects
			isOpen = false;
			while (!isOpen)
			{

				isOpen = true;
				foreach (var item in FullBoxes)
				{

					if (OpenDoors[0].Position == item && OpenDoors.Count > 1)
					{
						isOpen = false;
						//delete door if it overlaps
						OpenDoors.RemoveAt(0);
						//repeat with next door
						break;
					}
				}
			}
			if (OpenDoors.Count == 0)
			{
				break;
			}
			roomCounter++;
			SpawnRandomRoomAt(OpenDoors[0].Position, OpenDoors[0].Rotation);

			//clean up used door
			OpenDoors.RemoveAt(0);
		}
	}

	private void SpawnRandomRoomAt(Vector3 position, Vector3 rotationDegrees)
	{
		int random = 0;
		float roll = GD.RandRange(0, totalWeight);
		float cumulative = 0.0f;
		for (int i = 0; i < RoomWeights.Length; i++)
		{
			cumulative += RoomWeights[i];
			if (roll <= cumulative)
			{
				random = i;
				break;
			}
		}
		RoomData newRoom = roomManager.Rooms[random];

		Node3D newRoomNode = newRoom.Room.Instantiate<Node3D>();
		AddChild(newRoomNode);
		GD.Print(position);
		newRoomNode.Position = position;
		newRoomNode.RotationDegrees = rotationDegrees;

		FullBoxes.Add(newRoomNode.Position);
		foreach (var item in newRoom.Doors)
		{
			//rotate the local door exit depending on parent rotation
			Vector3 worldPos = item.Position * newRoomNode.Quaternion.Inverse();
			//transform into world space
			worldPos = worldPos.Round();
			worldPos += position;

			//transform rotation into world space
			Vector3 worldRot = item.Rotation + rotationDegrees;
			
			OpenDoors.Add(new DoorData(worldPos, worldRot));
		}
	
	}
	
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}
}
