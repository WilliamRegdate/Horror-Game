using Godot;
using System;
using System.Collections.Generic;

public struct DoorData
{
    public Vector3 Position;
    public Vector3 Rotation;

    public DoorData(Vector3 position, Vector3 rotation)
    {
        Position = position;
        Rotation = rotation;
    }
}

public struct RoomData
{
    public List<DoorData> Doors;
    public PackedScene Room;

    public RoomData(PackedScene room)
    {
        Room = room;
        Doors = new List<DoorData>();
    }
}

public class RoomManager
{
    public List<RoomData> Rooms = new List<RoomData>();

    //Json File as a string
    private string JsonFile;
    public RoomManager()
    {
        //load json file into a string
        string jsonPath = "res://Json/Rooms.Json";
        FileAccess file = FileAccess.Open(jsonPath, FileAccess.ModeFlags.Read);
        JsonFile = file.GetAsText();
        file.Close();
    }

    public void AddRoom(string roomName)
    {
        // Parse JSON string
        Variant parsed = Json.ParseString(JsonFile);
        if (parsed.VariantType != Variant.Type.Dictionary)
        {
            GD.PrintErr("rooms.json root is not a dictionary.");
            return;
        }

        var roomsDict = (Godot.Collections.Dictionary)parsed;

        // Check if room exists
        if (!roomsDict.ContainsKey(roomName))
        {
            GD.PrintErr($"Room '{roomName}' not found in rooms.json");
            return;
        }
        // Get specific room's data
        var roomData = (Godot.Collections.Dictionary)roomsDict[roomName];

        // Load the scene
        var scenePath = roomData["scene"].AsString();
        var scene = ResourceLoader.Load<PackedScene>(scenePath);
        if (scene == null)
        {
            GD.PrintErr($"Failed to load scene: {scenePath}");
            return;
        }

        // Create the new RoomData object
        var newRoom = new RoomData(scene);

        // Parse doors
        if (roomData.TryGetValue("doors", out var doorsVariant) && doorsVariant.VariantType == Variant.Type.Array)
        {
            var doors = doorsVariant.As<Godot.Collections.Array>();

            foreach (Godot.Collections.Dictionary door in doors)
            {
                var pos = door["position"].As<Godot.Collections.Array>();
                var rot = door["rotation"].As<Godot.Collections.Array>();

                var position = new Vector3(pos[0].AsSingle(), pos[1].AsSingle(), pos[2].AsSingle());
                var rotation = new Vector3(rot[0].AsSingle(), rot[1].AsSingle(), rot[2].AsSingle());
                
                newRoom.Doors.Add(new DoorData(position, rotation));
            }
        }
        else
        {
            GD.PrintErr($"Room '{roomName}' has no valid 'doors' array.");
        }

        Rooms.Add(newRoom);
    }
}

