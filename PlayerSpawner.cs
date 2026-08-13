using Godot;

public partial class PlayerSpawner : MultiplayerSpawner
{
    [Export] public PackedScene PlayerScene;
    private bool _hasStarted = false;

    public override void _Ready()
    {
        SpawnFunction = new Callable(this, nameof(SpawnPlayer));
    }

    public override void _ExitTree()
    {
        if (!Multiplayer.IsServer()) return;
        Multiplayer.PeerConnected -= SpawnForPeer;
        Multiplayer.PeerDisconnected -= DespawnForPeer;
    }

    public void BeginSpawning()
    {
        if (!Multiplayer.IsServer()) return;
        if (_hasStarted) return; // guard against double-calls
        _hasStarted = true;

        SpawnForPeer(Multiplayer.GetUniqueId());
        foreach (long id in Multiplayer.GetPeers())
            SpawnForPeer(id);

        // Anyone joining mid-game after Start was pressed still gets spawned.
        Multiplayer.PeerConnected += SpawnForPeer;
        Multiplayer.PeerDisconnected += DespawnForPeer;
    }

    private void SpawnForPeer(long id) => Spawn(id);

    private void DespawnForPeer(long id)
    {
        GetNode(SpawnPath).GetNodeOrNull($"Player_{id}")?.QueueFree();
    }

    private Node SpawnPlayer(Variant data)
    {
        long id = data.AsInt64();
        var player = PlayerScene.Instantiate<Node3D>();
        player.Name = $"Player_{id}";
        player.SetMultiplayerAuthority((int)id);
        return player;
    }
}