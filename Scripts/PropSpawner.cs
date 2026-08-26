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

        List<PropData> propsInSet = new();
        for (int i = 0; i < propList.Count; i++)
        {
            if (propList[i].Set == propSet && propList[i].Shape == _shape)
                propsInSet.Add(propList[i]);
        }

        if (propsInSet.Count == 0)
        {
            GD.PrintErr($"No props found for set={propSet}, shape={_shape}");
            return;
        }

        PackedScene chosenProp = propsInSet[GD.RandRange(0, propsInSet.Count - 1)].Scene;
        Node3D node = chosenProp.Instantiate<Node3D>();

        // Capture transform now while this node is still in the tree
        Vector3 spawnPosition = Position;
        Vector3 spawnRotation = Rotation;
        Node parentNode = GetParent();

        // Defer everything that touches the scene tree
        Callable.From(() =>
        {
            parentNode.AddChild(node);
            node.Position = spawnPosition;
            node.Rotation = spawnRotation;
        }).CallDeferred();

        QueueFree();
    }
}

