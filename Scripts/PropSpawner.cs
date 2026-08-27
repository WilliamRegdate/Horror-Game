using Godot;
using System;
using System.Collections.Generic;


//Class responsible for loading Rooms from the JSON and keeping track of them
public partial class PropSpawner : Node3D
{
	[Export] private string _shape;


    //Json File as a string

	private PackedScene selected;

    public override void _Ready()
    {
        if (!IsMultiplayerAuthority()) 
        {
            QueueFree();
            return;
        }
        if (GetParent() is not GetRoomSet parent)
        {
            GD.PrintErr("Parent does not have GetRoomSet attached");
            return;
        }
        string propSet = parent.GetSet();

        var generator = GetParent().GetParent<ProceduralGenerator>();
        if (generator == null)
        {
            GD.PrintErr("prop spawner is not parented correctly could not access procedural generator");
            return;
        }

        IReadOnlyList<PropData> propList = generator.PropLoader.Props;

        List<int> propsInSet = new();
        for (int i = 0; i < propList.Count; i++)
        {
            if (propList[i].Set == propSet && propList[i].Shape == _shape)
                propsInSet.Add(i);
        }
        if (propsInSet.Count == 0)
        {
            GD.PrintErr($"No props found for set = {propSet}, shape = {_shape}");
            return;
        }
        generator.Placements.Add(new ProceduralGenerator.PlacementRecord { SceneIndex = propsInSet[GD.RandRange(0, propsInSet.Count - 1)], Position = GlobalPosition, RotationDegrees = GlobalRotationDegrees, IsProp = true });

        QueueFree();
    }
}

