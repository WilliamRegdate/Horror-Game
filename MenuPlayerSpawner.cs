using Godot;

public partial class PlayerSpawner : MultiplayerSpawner
{
    [Export] public PackedScene PlayerScene;

    public override void _Ready()
    {
        SpawnFunction = new Callable(this, nameof(SpawnPlayer));

        if (!Multiplayer.IsServer()) return;

        // Spawn for everyone already connected (they joined back on the menu)
        SpawnForPeer(Multiplayer.GetUniqueId());
        foreach (long id in Multiplayer.GetPeers())
            SpawnForPeer(id);

        // Handle anyone joining mid-game, if that's supported
        Multiplayer.PeerConnected += SpawnForPeer;
    }

    public override void _ExitTree()
    {
        if (Multiplayer.IsServer())
            Multiplayer.PeerConnected -= SpawnForPeer;
    }

    private void SpawnForPeer(long id) => Spawn(id);

    private Node SpawnPlayer(Variant data)
    {
        long id = data.AsInt64();
        var player = PlayerScene.Instantiate<Node3D>();
        player.Name = $"Player_{id}";
        player.SetMultiplayerAuthority((int)id); // that peer controls their own character
        return player;
    }
}