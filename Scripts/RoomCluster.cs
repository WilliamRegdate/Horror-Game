using Godot;
using System;
using System.Collections.Generic;

class RoomCluster
{
    //links to other room clusters
    public int links = 0;
    public Vector3 Center;
    public List<DoorData> Doors;

    public RoomCluster(List<DoorData> doors)
    {
        Doors = new(doors);
        GetClusterCenter();
    }

    private void GetClusterCenter()
    {
        Vector3 total = new();
        foreach (var door in Doors)
            total += door.Position;
        Center = total / Doors.Count;
    }
}