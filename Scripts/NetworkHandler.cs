using Godot;

public partial class NetworkHandler : Node
{
    const string IP_ADDRESS = "192.168.1.153";
    const int PORT = 23000;
    ENetMultiplayerPeer peer;

    [Signal] public delegate void ServerStartedEventHandler();

    public bool IsNetworkActive()
    {
        return peer != null && peer.GetConnectionStatus() != MultiplayerPeer.ConnectionStatus.Disconnected;
    }

    public bool StartServer(int maxPlayers = 32)
    {
        if (IsNetworkActive())
        {
            return false;
        }

        peer = new();
        Error err = peer.CreateServer(PORT, maxPlayers);
        if (err != Error.Ok)
        {
            GD.PrintErr($"Failed to create server: {err}");
            peer = null;
            return false;
        }

        Multiplayer.MultiplayerPeer = peer;
        EmitSignal(SignalName.ServerStarted);
        return true;
    }

    public bool StartClient()
    {
        if (IsNetworkActive())
        {
            return false;
        }

        peer = new();
        Error err = peer.CreateClient(IP_ADDRESS, PORT);
        if (err != Error.Ok)
        {
            GD.PrintErr($"Failed to create client: {err}");
            peer = null;
            return false;
        }

        Multiplayer.MultiplayerPeer = peer;
        return true;
    }

    [Signal] public delegate void NetworkStoppedEventHandler();
    public void Disconnect()
    {
        if (peer != null)
        {
            peer.Close();
            peer = null;
        }
        Multiplayer.MultiplayerPeer = null;
        EmitSignal(SignalName.NetworkStopped);
    }
}