using Godot;
using System;
using System.Collections.Generic;
using System.Linq;


public partial class ProceduralGenerator : Node
{
    private struct PlacementRecord
    {
        public int SceneIndex; //note -1 = wall, -2 = _corridorStraight, -3 = _corridorCorner, -4 = _corridorStairs;
        public Vector3 Position;
        public Vector3 RotationDegrees;
    }
    [Export] string[] Rooms;
    [Export] int TotalRoomsPerCluster;
    [Export] Vector3I LevelBounds;
    [Export] int[] RoomWeights;
    
    public Vector3 StartRoomPos;
    private bool _firstMove = true;
    public RoomManager RoomManager;
    public PropLoader PropLoader;

    public volatile float Progress = 0f;

    private List<PlacementRecord> _placements = new();
    private List<Aabb> _placedRoomBounds = new(); // world-space AABBs of every room placed so far
    private int _totalWeight;
    private const int MaxPlacementAttempts = 40; // how many spawn locations to check before giving up
    private const int ClusterAmount = 6; //how many room clusters to have  

    //corridors
    [Export] PackedScene _corridorStraight;
    [Export] PackedScene _corridorCorner;
    [Export] PackedScene _corridorStairs;
    //wall
    [Export] PackedScene _wall;

    private List<RoomCluster> _roomClusters = new();

    private RandomNumberGenerator rng;

    private Dictionary<PackedScene, Aabb> _localAabbCache = new();

 

    public void PrewarmAabbCache()
    {
        PropLoader = new();
        RoomManager = new();
        foreach (string item in Rooms)
            RoomManager.AddRoom(item);
        foreach (int weight in RoomWeights)
            _totalWeight += weight;

        foreach (var room in RoomManager.Rooms)
            GetOrCacheLocalAabb(room.Room);
        GetOrCacheLocalAabb(_corridorStraight);
        GetOrCacheLocalAabb(_corridorCorner);
        GetOrCacheLocalAabb(_corridorStairs);
        
    }
    private Aabb GetOrCacheLocalAabb(PackedScene scene)
    {
        if (_localAabbCache.TryGetValue(scene, out Aabb cached))
            return cached;

        Node3D temp = scene.Instantiate<Node3D>(); // main thread, fine to instantiate+measure+discard
        Aabb? combined = null;
        CollectLocalMeshAabbs(temp, temp, ref combined); // measure relative to temp's own root, not global
        Aabb result = combined ?? new Aabb(Vector3.Zero, Vector3.Zero);

        temp.QueueFree(); // safe here — main thread, message queue gets flushed normally
        _localAabbCache[scene] = result;
        return result;
    }

    public void Generate()
    {
        GD.Print($"Generate() called — instance {GetInstanceId()}, placements before: {_placements.Count}");
        rng = new RandomNumberGenerator();
        rng.Randomize();
        

        float stageCompletion; 
        for (int c = 0; c < ClusterAmount; c++)
        {
            stageCompletion = (c + 1) /(float)ClusterAmount;
            RoomCluster cluster = GenerateCluster();
            if (cluster != null)
            {
                _roomClusters.Add(cluster);
                GD.Print(cluster.Doors.Count);
            }
            Progress = stageCompletion * 30; // 40% done
        }
        LinkClusters();

        //close leftover doors
        foreach (var cluster in _roomClusters)
        {
            int i = 0;
            i++;
            stageCompletion = i/(float)_roomClusters.Count();

            foreach(var door in cluster.Doors)
            {
                CloseDoor(door);
            }
            Progress =  90 + (stageCompletion * 10);
        }
        GD.Print($"Generate() finished — total placements: {_placements.Count}");
    }

    // Attempts to generate one full cluster (a chain of TotalRooms rooms) at a random,
    // non-overlapping starting position within LevelBounds. Returns null if no valid
    // starting position could be found after MaxPlacementAttempts tries.
    private RoomCluster GenerateCluster()
    {
        for (int attempt = 0; attempt < MaxPlacementAttempts; attempt++)
        {
            Vector3 startPosition = new Vector3(
                rng.RandiRange(-LevelBounds.X, LevelBounds.X) * 5,
                rng.RandiRange(0, LevelBounds.Y) * 5,
                rng.RandiRange(-LevelBounds.Z, LevelBounds.Z) * 5
            );

            if (_firstMove)
            {
                startPosition = new(0,0,0);
                _firstMove = false;
            }

            // Tentatively spawn the first room of the cluster and check it doesn't
            // overlap anything already placed (by an earlier cluster).
            Aabb firstRoomBounds = SpawnRoomAt(startPosition, Vector3.Zero, out List<DoorData> firstRoomDoors);
            

            if (OverlapsAnyPlacedRoom(firstRoomBounds))
            {
                // This starting position doesn't work — discard and try a new random spot.
                _placements.RemoveAt(_placements.Count-1);
                continue;
            }

            _placedRoomBounds.Add(firstRoomBounds);
            List<DoorData> openDoors = firstRoomDoors;

            int roomsPlaced = 1;
            while (roomsPlaced < TotalRoomsPerCluster && openDoors.Count > 0)
            {
                DoorData parentDoor = openDoors[0];
                openDoors.RemoveAt(0);

                Aabb? newRoomBounds = SpawnRoomAttachedTo(parentDoor, out List<DoorData> newDoors);

                if (newRoomBounds == null)
                {
                    CloseDoor(parentDoor);
                    continue;
                }



                _placedRoomBounds.Add((Aabb)newRoomBounds);
                roomsPlaced++;

                foreach (var item in newDoors)
                    openDoors.Add(item);
            }

            // The cluster's remaining open doors become its external attachment points,
            // used later to connect this cluster to others.
            return new RoomCluster(openDoors);
        }

        // Couldn't find a non-overlapping starting position after several tries.
        GD.PrintErr("Could not find a non-overlapping starting position for a new cluster; skipping it.");
        return null;
    }
    private void CloseDoor(DoorData door)
    {
        _placements.Add(new PlacementRecord { SceneIndex = -1, Position = door.Position, RotationDegrees = door.Rotation });
    }

    // Spawns a room directly at the given world position/rotation (used for the very first room).
    private Aabb? SpawnRoomAttachedTo(DoorData parentDoor, out List<DoorData> resultingDoors)
    {
        parentDoor.Position += new Quaternion(Vector3.Up, Mathf.DegToRad(parentDoor.Rotation.Y)) * new Vector3(-5, 0, 0);

        for (int attempt = 0; attempt < MaxPlacementAttempts; attempt++)
        {
            int random = GetWeightedIndex();
            RoomData roomTemplate = RoomManager.Rooms[random];
            List<DoorData> localDoors = new List<DoorData>(roomTemplate.Doors); // truly local copy

            PlacementRecord _candidateRecord = new PlacementRecord { SceneIndex = random};

            int doorIndex = rng.RandiRange(0, localDoors.Count - 1);
            DoorData childDoor = localDoors[doorIndex];
            localDoors.RemoveAt(doorIndex); // only affects local copy, shared template untouched

            float roomYaw = parentDoor.Rotation.Y - childDoor.Rotation.Y + 180f;
            _candidateRecord.RotationDegrees = new Vector3(0, roomYaw, 0);
            _candidateRecord.Position = parentDoor.Position - new Quaternion(Vector3.Up, Mathf.DegToRad(_candidateRecord.RotationDegrees.Y)) * childDoor.Position;

            Aabb localAabb = GetOrCacheLocalAabb(roomTemplate.Room);
            Transform3D placementTransform = new Transform3D(new Basis(Vector3.Up, Mathf.DegToRad(_candidateRecord.RotationDegrees.Y)), _candidateRecord.Position);
            Aabb worldAabb = placementTransform * localAabb;

            worldAabb.Size -= new Vector3(0.3f, 0.3f, 0.3f);

            if (OverlapsAnyPlacedRoom(worldAabb))
            {
                continue;
            }
            _placements.Add(_candidateRecord);

            resultingDoors = GetWorldDoors(localDoors, _candidateRecord.Position, _candidateRecord.RotationDegrees);
            return worldAabb;
        }

        resultingDoors = new List<DoorData>();
        return null;
    }

    private Aabb SpawnRoomAt(Vector3 position, Vector3 rotationDegrees, out List<DoorData> resultingDoors)
    {
        int random = GetWeightedIndex();
        RoomData roomTemplate = RoomManager.Rooms[random];

        Aabb localAabb = GetOrCacheLocalAabb(roomTemplate.Room);
        Transform3D placementTransform = new Transform3D(
            new Basis(Vector3.Up, Mathf.DegToRad(rotationDegrees.Y)), position);
        Aabb worldAabb = placementTransform * localAabb;

        _placements.Add(new PlacementRecord { SceneIndex = random, Position = position, RotationDegrees = rotationDegrees });

        resultingDoors = GetWorldDoors(new List<DoorData>(roomTemplate.Doors), position, rotationDegrees);
        return worldAabb;
    }

    // Computes the world-space bounding box of a room by scanning its MeshInstance3D children
    // and merging their (already-transformed) AABBs.
    private Aabb GetWorldAabb(Node3D roomNode)
        {
            Aabb? combined = null;
            CollectMeshAabbs(roomNode, ref combined);
            return combined ?? new Aabb(ComputeGlobalTransform(roomNode).Origin, Vector3.Zero);
        }

    private void CollectMeshAabbs(Node node, ref Aabb? combined)
    {
        if (node is MeshInstance3D meshInstance && meshInstance.Mesh != null)
        {
            Aabb localAabb = meshInstance.Mesh.GetAabb();
            Aabb worldAabb = ComputeGlobalTransform(meshInstance) * localAabb; // was meshInstance.GlobalTransform

            combined = combined.HasValue ? combined.Value.Merge(worldAabb) : worldAabb;
        }

        foreach (Node child in node.GetChildren())
            CollectMeshAabbs(child, ref combined);
    }
    private bool OverlapsAnyPlacedRoom(Aabb candidateBounds)
    {
        foreach (Aabb placed in _placedRoomBounds)
        {
            if (placed.Intersects(candidateBounds))
            {
                return true;
            }
        }
        return false;
    }

    // Converts a room's local door list into world-space DoorData, excluding the door
    // that's no longer "open" doesn't matter here since caller already removed/replaced as needed.
    private List<DoorData> GetWorldDoors(List<DoorData> doors, Vector3 position, Vector3 rotationDegrees)
    {
        List<DoorData> result = new();
        Quaternion rotation = new Quaternion(Vector3.Up, Mathf.DegToRad(rotationDegrees.Y));

        foreach (var item in doors)
        {
            Vector3 worldPos = rotation * item.Position;
            worldPos += position;
            Vector3 worldRot = new Vector3(0, item.Rotation.Y + rotationDegrees.Y, 0);
            result.Add(new DoorData(worldPos, worldRot));
        }
        return result;
    }

    private int GetWeightedIndex()
    {
        float roll = rng.RandfRange(0, _totalWeight);
        float cumulative = 0.0f;
        for (int i = 0; i < RoomWeights.Length; i++)
        {
            cumulative += RoomWeights[i];
            if (roll <= cumulative)
            {
                return i;
            }
        }
        return 0;
    }

    private void LinkClusters()
    {
        if (_roomClusters.Count == 0) return;

        float stageCompletion;
        Progress = 30;

        var failedPairs = new HashSet<(int, int)>();

        // ── Phase 1: Prim's MST with priority queue ───────────────────────────────
        var connected = new HashSet<int> { 0 };
        var unconnected = new HashSet<int>();
        for (int i = 1; i < _roomClusters.Count; i++)
            unconnected.Add(i);

        // Seed the queue with all edges from cluster 0
        var edgeQueue = new PriorityQueue<(int from, int to), float>();
        foreach (int to in unconnected)
        {
            float d = (_roomClusters[0].Center - _roomClusters[to].Center).LengthSquared();
            edgeQueue.Enqueue((0, to), d);
        }

        while (unconnected.Count > 0 && edgeQueue.Count > 0)
        {
            var (from, to) = edgeQueue.Dequeue();

            // Skip stale entries — `to` may have been connected via a different edge already
            if (!unconnected.Contains(to)) continue;

            var key = (Math.Min(from, to), Math.Max(from, to));
            if (failedPairs.Contains(key)) continue;

            bool linked = TryLinkClusters(from, to, failedPairs);
            if (linked)
            {
                connected.Add(to);
                unconnected.Remove(to);

                // Newly connected cluster opens edges to everything still unconnected
                foreach (int next in unconnected)
                {
                    float d = (_roomClusters[to].Center - _roomClusters[next].Center).LengthSquared();
                    edgeQueue.Enqueue((to, next), d);
                }
            }
            else
            {
                failedPairs.Add(key);
            }
        }

        if (unconnected.Count > 0)
            GD.PrintErr("LinkClusters Phase 1: could not connect all clusters; some will be unreachable.");

        // ── Phase 2: optional extra connections ───────────────────────────────────
        // For each cluster, randomly try to link it to one additional neighbour.
        // This adds loops to the graph so the player has multiple routes between areas.
        const float extraConnectionChance = 1.0f;

        for (int i = 0; i < _roomClusters.Count; i++)
        {
            stageCompletion = i / (float)_roomClusters.Count;
            Progress = 50 + stageCompletion * 20;
            if (GD.Randf() > extraConnectionChance) continue;
            if (_roomClusters[i].Doors.Count == 0) continue;

            // Pick a random other cluster that still has open doors.
            var candidates = new List<int>();
            for (int j = 0; j < _roomClusters.Count; j++)
            {
                if (j == i) continue;
                if (_roomClusters[j].Doors.Count == 0) continue;
                var key = (Math.Min(i, j), Math.Max(i, j));
                if (failedPairs.Contains(key)) continue;
                candidates.Add(j);
            }

            if (candidates.Count == 0) continue;

            int pick = candidates[rng.RandiRange(0, candidates.Count - 1)];
            TryLinkClusters(i, pick, failedPairs);
        }

        // ── Phase 3: fill leftover open doors (randomly, not exhaustively) ────────
        // Not every door is filled — dead ends are intentional.
        const float doorFillChance = 0.9f;

        for (int i = 0; i < _roomClusters.Count; i++)
        {
            stageCompletion = i /(float) _roomClusters.Count;
            Progress = 70 + stageCompletion * 20;
            // Iterate a copy so we can safely remove from the original.
            foreach (DoorData door in new List<DoorData>(_roomClusters[i].Doors))
            {
                // Guard against doors already consumed by a prior iteration in this phase.
                bool stillOpen = false;
                foreach (DoorData liveDoor in _roomClusters[i].Doors)
                {
                    if (liveDoor.Position == door.Position)
                    {
                        stillOpen = true;
                        break;
                    }
                }
                if (!stillOpen) continue;

                if (GD.Randf() > doorFillChance) continue;

                // Find the closest other door on the same cluster.
                float bestD = float.MaxValue;
                DoorData bestDoor = default;

                foreach (DoorData other in _roomClusters[i].Doors)
                {
                    if (other.Position == door.Position) continue;
                    float d = (other.Position - door.Position).LengthSquared();
                    if (d < bestD)
                    {
                        bestD = d;
                        bestDoor = other;
                    }
                }

                List<DoorData> path = LinkDoors(door, bestDoor);
                if (path.Count == 0) continue;

                PlaceCorridors(path, door, bestDoor);
                _roomClusters[i].Doors.Remove(door);
                _roomClusters[i].Doors.Remove(bestDoor);
            }
        }
    }

    // Attempts to find and place a corridor path between the best available door
    // pair from cluster A to cluster B. Returns true if a path was placed.
    private bool TryLinkClusters(int indexA, int indexB, HashSet<(int, int)> failedPairs)
    {
        RoomCluster a = _roomClusters[indexA];
        RoomCluster b = _roomClusters[indexB];

        if (a.Doors.Count == 0 || b.Doors.Count == 0)
        {
            GD.PrintErr($"TryLinkClusters: cluster {indexA} or {indexB} has no open doors.");
            return false;
        }

        // Build a priority queue of every (doorA, doorB) pair sorted by distance.
        // Tries the closest pair first, falls through to farther ones if the path is blocked.
        var pairs = new PriorityQueue<(DoorData, DoorData), float>();

        foreach (DoorData dA in a.Doors)
        {
            foreach (DoorData dB in b.Doors)
            {
                float dist = (dA.Position - dB.Position).LengthSquared();
                pairs.Enqueue((dA, dB), dist);
            }
        }

        while (pairs.Count > 0)
        {
            var (doorA, doorB) = pairs.Dequeue();

            List<DoorData> path = LinkDoors(doorA, doorB);
            if (path.Count == 0)
                continue;

            PlaceCorridors(path, doorA, doorB);
            a.Doors.Remove(doorA);
            b.Doors.Remove(doorB);
            a.links++;
            b.links++;
            return true;
        }

        // Every door pair between these two clusters failed to path.
        failedPairs.Add((Math.Min(indexA, indexB), Math.Max(indexA, indexB)));
        return false;
    }

    private const float GridCellSize = 5f;

    // Flat horizontal moves (no change in height).
    private static readonly Vector3I[] FlatDirections = new Vector3I[]
    {
        new Vector3I(1, 0, 0), new Vector3I(-1, 0, 0),
        new Vector3I(0, 0, 1), new Vector3I(0, 0, -1),
    };

    // Stair moves: advance one cell horizontally while also rising/falling one cell in Y.
    private static readonly Vector3I[] StairDirections = new Vector3I[]
    {
        // up
        new Vector3I(1, 1, 0), new Vector3I(-1, 1, 0),
        new Vector3I(0, 1, 1), new Vector3I(0, 1, -1),
        // down
        new Vector3I(1, -1, 0), new Vector3I(-1, -1, 0),
        new Vector3I(0, -1, 1), new Vector3I(0, -1, -1),
    };

    private static readonly Vector3I[] GridDirections12 =
        FlatDirections.Concat(StairDirections).ToArray();

    /// <summary>
    /// Finds a path of grid cells linking two doors using A* on a 5x5x5 grid, treating
    /// already-placed rooms as obstacles. Movement is 12-directional: 4 flat horizontal
    /// moves, plus 8 diagonal stair moves that rise/fall one cell while advancing one cell
    /// horizontally.
    /// </summary>
    private List<DoorData> LinkDoors(DoorData door1, DoorData door2)
    {
        if (!TryWorldToGrid(door1.Position, out Vector3I startCell) ||
            !TryWorldToGrid(door2.Position, out Vector3I endCell))
        {
            GD.PrintErr($"LinkDoors: door positions are not aligned to the {GridCellSize}-unit grid. " +
                        $"door1={door1.Position}, door2={door2.Position}");
            return new List<DoorData>();
        }

        if (startCell == endCell)
            return new List<DoorData>();

        // FIX: removed IsCellBlocked checks on startCell and endCell — door cells
        // always overlap their own room's AABB and would block every single path.
        // The pathfinder already handles start/end correctly by only blocking neighbours.

        if (IsCellBlocked(startCell))
        {
            // Vector3 worldPos = GridToWorld(startCell);
            // Vector3 halfSize = new Vector3(GridCellSize, GridCellSize, GridCellSize) * 0.4f;
            // Aabb cellBounds  = new Aabb(worldPos - halfSize, halfSize * 2f);
            // MeshInstance3D cube = new();
            // cube.Mesh = new BoxMesh();
            // AddChild(cube);
            // cube.Scale = halfSize;
            // cube.Position = worldPos; 

            return new List<DoorData>();  // ← returns empty for every door
        }
        if (IsCellBlocked(endCell))
        {
            return new List<DoorData>();  // ← returns empty for every door
        }

        var openSet   = new List<Vector3I> { startCell };
        var cameFrom  = new Dictionary<Vector3I, Vector3I>();
        var gScore    = new Dictionary<Vector3I, float> { [startCell] = 0f };
        var fScore    = new Dictionary<Vector3I, float> { [startCell] = Heuristic(startCell, endCell) };
        var closedSet = new HashSet<Vector3I>();

        while (openSet.Count > 0)
        {
            if (openSet.Count > 1500)
                break;

            Vector3I current = openSet[0];
            float bestF = fScore.TryGetValue(current, out var f0) ? f0 : float.MaxValue;
            for (int i = 1; i < openSet.Count; i++)
            {
                float f = fScore.TryGetValue(openSet[i], out var fi) ? fi : float.MaxValue;
                if (f < bestF) { bestF = f; current = openSet[i]; }
            }

            if (current == endCell)
                return ReconstructPath(cameFrom, current);

            openSet.Remove(current);
            closedSet.Add(current);

            Vector3I incomingDir = cameFrom.TryGetValue(current, out Vector3I cameFromCell)
                ? current - cameFromCell
                : Vector3I.Zero;
            Vector3I forbiddenDir = -incomingDir;
            bool arrivedViaStair = incomingDir.Y != 0;

            foreach (Vector3I dir in GridDirections12)
            {
                // Forbid reversing direction
                if (incomingDir != Vector3I.Zero &&
                    new Vector3I(dir.X, 0, dir.Z) == new Vector3I(forbiddenDir.X, 0, forbiddenDir.Z))
                    continue;

                bool isStairMove = dir.Y != 0;

                // Stairs must continue in the same horizontal direction
                if (arrivedViaStair && new Vector3(dir.X, 0, dir.Z) != new Vector3(incomingDir.X, 0, incomingDir.Z))
                    continue;

                Vector3I neighbor = current + dir;

                // Forbid a staircase landing directly on the end cell
                if (isStairMove && neighbor == endCell)
                    continue;

                if (closedSet.Contains(neighbor))
                    continue;

                bool blocked = isStairMove
                    ? IsStairMoveBlocked(current, neighbor)
                    : IsCellBlocked(neighbor);

                if (neighbor != endCell && blocked)
                    continue;

                float moveCost = isStairMove ? GridCellSize * 2f : GridCellSize;
                float tentativeG = gScore[current] + moveCost;

                if (!gScore.TryGetValue(neighbor, out float neighborG) || tentativeG < neighborG)
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor]   = tentativeG;
                    fScore[neighbor]   = tentativeG + Heuristic(neighbor, endCell);

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        GD.PrintErr("LinkDoors: no valid path found between doors.");
        return new List<DoorData>();
    }

    private float Heuristic(Vector3I a, Vector3I b)
    {
        return (Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y) + Mathf.Abs(a.Z - b.Z)) * GridCellSize;
    }

    private bool TryWorldToGrid(Vector3 worldPos, out Vector3I cell)
    {
        const float epsilon = 0.01f;
        Vector3 cellF   = worldPos / GridCellSize;
        Vector3 rounded = new Vector3(Mathf.Round(cellF.X), Mathf.Round(cellF.Y), Mathf.Round(cellF.Z));

        if ((cellF - rounded).Length() > epsilon)
        {
            cell = Vector3I.Zero;
            return false;
        }

        cell = new Vector3I((int)rounded.X, (int)rounded.Y, (int)rounded.Z);
        return true;
    }

    private Vector3 GridToWorld(Vector3I cell)
    {
        return new Vector3(cell.X, cell.Y, cell.Z) * GridCellSize;
    }

    private bool IsCellBlocked(Vector3I cell)
    {
        Vector3 worldPos = GridToWorld(cell);
        Vector3 halfSize = new Vector3(GridCellSize, GridCellSize, GridCellSize) * 0.4f;
        Aabb cellBounds  = new Aabb(worldPos - halfSize, halfSize * 2f);
        return OverlapsAnyPlacedRoom(cellBounds);
    }

    private bool IsStairMoveBlocked(Vector3I from, Vector3I to)
    {
        Vector3 fromWorld = GridToWorld(from);
        Vector3 toWorld   = GridToWorld(to);
        Vector3 halfCell  = new Vector3(GridCellSize, GridCellSize, GridCellSize) * 0.4f;

        Aabb fromBounds = new Aabb(fromWorld - halfCell, halfCell * 2f);
        Aabb toBounds   = new Aabb(toWorld   - halfCell, halfCell * 2f);
        Aabb combined   = fromBounds.Merge(toBounds);

        return OverlapsAnyPlacedRoom(combined);
    }

    private List<DoorData> ReconstructPath(Dictionary<Vector3I, Vector3I> cameFrom, Vector3I current)
    {
        List<Vector3I> cellPath = new() { current };
        while (cameFrom.TryGetValue(current, out Vector3I prev))
        {
            current = prev;
            cellPath.Add(current);
        }
        cellPath.Reverse();

        List<DoorData> result = new();
        for (int i = 0; i < cellPath.Count; i++)
        {
            Vector3 worldPos = GridToWorld(cellPath[i]);
            result.Add(new DoorData(worldPos, new Vector3(0, 0, 0)));
        }

        return result;
    }

    private void PlaceCorridors(List<DoorData> path, DoorData startDoor, DoorData endDoor)
    {
        startDoor.Position += new Quaternion(Vector3.Up, Mathf.DegToRad(startDoor.Rotation.Y)) * new Vector3(-5, 0, 0);
        endDoor.Position   += new Quaternion(Vector3.Up, Mathf.DegToRad(endDoor.Rotation.Y))   * new Vector3(-5, 0, 0);
        startDoor.Position  = startDoor.Position.Round();
        endDoor.Position    = endDoor.Position.Round();
        path.Insert(0, startDoor);
        path.Add(endDoor);

        for (int i = 1; i < path.Count - 1; i++)
        {
            Vector3 previous = path[i - 1].Position;
            Vector3 current  = path[i].Position;
            Vector3 next     = path[i + 1].Position;

            Vector3 dirIn  = current - previous;
            Vector3 dirOut = next - current;

            int sceneIndex; //note -1 = _wall, -2 = _corridorStraight, -3 = _corridorCorner, -4 = _corridorStairs;
            PackedScene scene; 
            Vector3 position;
            float rotY;

            if (dirIn.Dot(dirOut) == 0) // corner
            {
                scene = _corridorCorner;
                sceneIndex = -3;
                position = current;
                Vector3 flatIn  = new(dirIn.X,  0, dirIn.Z);
                Vector3 flatOut = new(dirOut.X, 0, dirOut.Z);
                rotY = GetCornerRotation(flatIn, flatOut);
            }
            else if (dirIn.Y < 0) // downhill stair
            {
                sceneIndex = -4;
                scene = _corridorStairs;
                position = current;
                Vector3 flatIn = new(dirIn.X, 0, dirIn.Z);
                rotY = GetStraightRotationY(-flatIn);
            }
            else if (dirIn.Y > 0) // uphill stair
            {
                sceneIndex = -4;
                scene = _corridorStairs;
                position = current - Vector3.Up * 5;
                Vector3 flatIn = new(dirIn.X, 0, dirIn.Z);
                rotY = GetStraightRotationY(flatIn);
            }
            else // flat straight
            {
                sceneIndex = -2;
                scene = _corridorStraight;
                position = current;
                rotY = GetStraightRotationY(dirIn);
            }

            Vector3 rotationDegrees = new(0, rotY, 0);
            _placements.Add(new PlacementRecord { SceneIndex = sceneIndex, Position = position, RotationDegrees = rotationDegrees });

            Aabb localAabb = GetOrCacheLocalAabb(scene);
            Transform3D placementTransform = new Transform3D(new Basis(Vector3.Up, Mathf.DegToRad(rotY)), position);
            _placedRoomBounds.Add(placementTransform * localAabb);
        }
    }

    private static float GetStraightRotationY(Vector3 dir)
    {
        if      (dir == Vector3.Forward * 5) return   0f;
        else if (dir == Vector3.Right   * 5) return -90f;
        else if (dir == Vector3.Back    * 5) return 180f;
        else if (dir == Vector3.Left    * 5) return  90f;

        GD.PrintErr($"Unhandled straight direction: {dir}");
        return 0f;
    }

    private static float GetCornerRotation(Vector3 arm1, Vector3 arm2)
    {
        if (CornerRotations.TryGetValue((arm1, arm2), out float rot))
            return rot;

        GD.PrintErr($"Unhandled corner arm pair: {arm1}, {arm2}");
        return 0f;
    }

    private static readonly Dictionary<(Vector3, Vector3), float> CornerRotations = new()
    {
        { (Vector3.Back    * 5, Vector3.Right   * 5),  90f },
        { (Vector3.Right   * 5, Vector3.Back    * 5), -90f },

        { (Vector3.Right   * 5, Vector3.Forward * 5), 180f },
        { (Vector3.Forward * 5, Vector3.Right   * 5),   0f },

        { (Vector3.Forward * 5, Vector3.Left    * 5), -90f },
        { (Vector3.Left    * 5, Vector3.Forward * 5),  90f },

        { (Vector3.Left    * 5, Vector3.Back    * 5),   0f },
        { (Vector3.Back    * 5, Vector3.Left    * 5), 180f },
    };

    private static Vector3 NormaliseRotation(Vector3 rot)
    {
        float y = Mathf.Wrap(rot.Y, 0f, 360f);
        if (Mathf.IsEqualApprox(y, 270f)) y = -90f;
        if (Mathf.IsEqualApprox(y, 360f)) y =   0f;
        return new Vector3(0, y, 0);
    }
    private static Transform3D ComputeGlobalTransform(Node3D node)
    {
        Transform3D result = node.Transform;
        Node parent = node.GetParent();
        while (parent is Node3D parent3D)
        {
            result = parent3D.Transform * result;
            parent = parent3D.GetParent();
        }
        return result;
    }
    private void CollectLocalMeshAabbs(Node node, Node3D root, ref Aabb? combined)
    {
        if (node is MeshInstance3D meshInstance && meshInstance.Mesh != null)
        {
            Transform3D relative = ComputeRelativeTransform(meshInstance, root);
            Aabb worldAabb = relative * meshInstance.Mesh.GetAabb();
            combined = combined.HasValue ? combined.Value.Merge(worldAabb) : worldAabb;
        }
        foreach (Node child in node.GetChildren())
            CollectLocalMeshAabbs(child, root, ref combined);
    }

    private Transform3D ComputeRelativeTransform(Node3D node, Node3D stopAt)
    {
        Transform3D result = node.Transform;
        Node parent = node.GetParent();
        while (parent is Node3D parent3D && parent3D != stopAt)
        {
            result = parent3D.Transform * result;
            parent = parent3D.GetParent();
        }
        return result;
    }

    private int _buildIndex = 0;
    public void BuildFromPlacements()
    {
        if (_buildIndex >= _placements.Count)
            return;

        var record = _placements[_buildIndex];
        Node3D node = GetScenefromIndex(record.SceneIndex).Instantiate<Node3D>();
        node.Position = record.Position.Round();
        node.RotationDegrees = record.RotationDegrees.Round();
        AddChild(node);
        _buildIndex++;

        if (_buildIndex < _placements.Count)
            CallDeferred(nameof(BuildFromPlacements));
    }
    private PackedScene GetScenefromIndex(int i)
    {
        //note -1 = _wall, -2 = _corridorStraight, -3 = _corridorCorner, -4 = _corridorStairs;
        switch(i)
        {
            case -1:
                return _wall ;
            case -2:
                return _corridorStraight;
            case -3:
                return _corridorCorner;
            case -4:
                return _corridorStairs;
            default:
                return RoomManager.Rooms[i].Room;
        }
    }

    // Adjust "RoomPrefabs" to whatever your actual exported prefab array is called —
    // the one Generate() picks PlacementRecord.Scene values from.
    public (int[] sceneIndices, Vector3[] positions, Vector3[] rotations) ExportPlacements()
    {
        int count = _placements.Count;
        var sceneIndices = new int[count];
        var positions = new Vector3[count];
        var rotations = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            sceneIndices[i] = _placements[i].SceneIndex;
            positions[i] = _placements[i].Position;
            rotations[i] = _placements[i].RotationDegrees;
        }

        return (sceneIndices, positions, rotations);
    }

    public void LoadPlacements(int[] sceneIndices, Vector3[] positions, Vector3[] rotations)
    {
        _placements.Clear();
        for (int i = 0; i < sceneIndices.Length; i++)
        {
            _placements.Add(new PlacementRecord
            {
                SceneIndex = sceneIndices[i],
                Position = positions[i],
                RotationDegrees = rotations[i]
            });
        }
    }
}
