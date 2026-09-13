using Godot;

public partial class PlayerSpawner : MultiplayerSpawner
{
    [Export] public PackedScene PlayerScene;
    private readonly Vector3[] _spawnLocations = 
    [
        new( 1,0,20),
        new(-1,0,20),
        new( 1,0,18),
        new(-1,0,18),
        new( 1,0,16),
        new(-1,0,16),
        new( 1,0,14),
        new(-1,0,14)
    ];
    private int _spawnIndex;
    private bool _hasStarted = false;

    public override void _Ready()
    {
        SpawnFunction = new Callable(this, nameof(SpawnPlayer));
    }
    
    public void BeginSpawning()
    {
        if (!Multiplayer.IsServer()) return;
        if (_hasStarted) return;
        _hasStarted = true;

        SpawnForPeer(Multiplayer.GetUniqueId());
        foreach (long id in Multiplayer.GetPeers())
            SpawnForPeer(id);

        Multiplayer.PeerConnected += SpawnForPeer;
        Multiplayer.PeerDisconnected += DespawnForPeer;
    }

    private void SpawnForPeer(long id)
    {
        int index = _spawnIndex % _spawnLocations.Length; // wraps if >8 players join
        _spawnIndex++;

        var data = new Godot.Collections.Array { id, index };
        Spawn(data);
    }
    public void DespawnForPeer(long id)
    {
        Player player = GetNode(SpawnPath).GetNodeOrNull($"Player_{id}") as Player;
        var synchronizer = player.GetNodeOrNull<MultiplayerSynchronizer>("MultiplayerSynchronizer");
        if (synchronizer != null)
            synchronizer.ProcessMode = ProcessModeEnum.Disabled;

        player.CallDeferred(Node.MethodName.QueueFree);
    }
    private Node SpawnPlayer(Variant data)
    {
        var args = data.AsGodotArray();
        long id = args[0].AsInt64();
        int spawnIndex = args[1].AsInt32();

        var player = PlayerScene.Instantiate<Node3D>();
        player.Name = $"Player_{id}";
        player.SetMultiplayerAuthority((int)id);
        player.Position = _spawnLocations[spawnIndex];
        return player;
    }
}