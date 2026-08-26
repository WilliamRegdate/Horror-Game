using Godot;
using System.Threading.Tasks;

public partial class NetworkHandler : Node
{
    public string IpAddress = "";
    const int PORT = 23000;
    ENetMultiplayerPeer peer;
    private Upnp _upnp;

    [Signal] public delegate void ServerStartedEventHandler();
    [Signal] public delegate void NetworkStoppedEventHandler();

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

        _ = SetupUpnpAsync(PORT); // best-effort, fire-and-forget — LAN hosting works regardless

        return true;
    }

    public bool StartClient()
    {
        if (IsNetworkActive())
        {
            return false;
        }

        peer = new();
        Error err = peer.CreateClient(IpAddress, PORT);
        if (err != Error.Ok)
        {
            GD.PrintErr($"Failed to create client: {err}");
            peer = null;
            return false;
        }

        Multiplayer.MultiplayerPeer = peer;
        return true;
    }

    public void Disconnect()
    {
        if (peer != null)
        {
            peer.Close();
            peer = null;
        }

        if (_upnp != null)
        {
            _upnp.DeletePortMapping(PORT, "UDP");
            _upnp = null;
        }

        Multiplayer.MultiplayerPeer = null;
        EmitSignal(SignalName.NetworkStopped);
    }

    private async Task SetupUpnpAsync(int port)
    {
        _upnp = new Upnp();

        // Discover() and AddPortMapping() are synchronous and block — keep off the main thread.
        Upnp.UpnpResult discoverResult = await Task.Run(() => (Upnp.UpnpResult)_upnp.Discover());

        if (discoverResult != Upnp.UpnpResult.Success)
        {
            GD.Print($"UPNP discovery failed ({discoverResult}). Hosting still works over LAN or with manual port forwarding.");
            return;
        }

        if (_upnp.GetGateway() is not UpnpDevice gateway || !gateway.IsValidGateway())
        {
            GD.Print("UPNP: no valid gateway found.");
            return;
        }

        Upnp.UpnpResult mapResult = await Task.Run(() =>
            (Upnp.UpnpResult)_upnp.AddPortMapping(port, port, "MyGame", "UDP"));

        if (mapResult != Upnp.UpnpResult.Success)
        {
            GD.PrintErr($"UPNP port mapping failed: {mapResult}");
            return;
        }

        GD.Print($"UPNP port mapping succeeded. External IP: {_upnp.QueryExternalAddress()}");
    }
}