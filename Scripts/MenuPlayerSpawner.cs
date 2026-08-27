using Godot;

public partial class MenuPlayerSpawner : MultiplayerSpawner
{
    private NetworkHandler _networkHandler;

    public override void _Ready()
    {
        SpawnFunction = new Callable(this, nameof(SpawnPlayerLabel));

        _networkHandler = GetNode<NetworkHandler>("/root/NetworkHandler");
        _networkHandler.ServerStarted += OnServerStarted;
        _networkHandler.NetworkStopped += OnNetworkStopped;

        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;
    }
    
    public override void _ExitTree()
    {
        Multiplayer.PeerConnected -= OnPeerConnected;
        Multiplayer.PeerDisconnected -= OnPeerDisconnected;

        if (IsInstanceValid(_networkHandler))
        {
            _networkHandler.ServerStarted -= OnServerStarted;
            _networkHandler.NetworkStopped -= OnNetworkStopped;
        }
    }

    private void OnNetworkStopped()
    {
        foreach (Node child in GetNode(SpawnPath).GetChildren())
            child.QueueFree();
    }

    private void OnServerStarted()
    {
        OnPeerConnected(Multiplayer.GetUniqueId());
    }

    private void OnPeerConnected(long id)
    {
        if (!Multiplayer.IsServer()) return;
        Spawn(id);
    }
    

    private void OnPeerDisconnected(long id)
    {
        if (!Multiplayer.IsServer()) return;

        var label = GetNode(SpawnPath).GetNodeOrNull($"Player_{id}");
        label?.QueueFree();
    }
    public void OnStartGame()
    {
        if (!Multiplayer.IsServer()) return;

        foreach (Node child in GetChildren())
            child.QueueFree();
    }

    private Node SpawnPlayerLabel(Variant data)
    {
        long id = data.AsInt64();
        return new Label { Name = $"{id}", Text = $"<Player {id}>" };
    }

}