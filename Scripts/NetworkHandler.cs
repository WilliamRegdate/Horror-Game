using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class NetworkHandler : Node
{
    public string lastDisconnectReason;
    public bool JustDisconnectedFromServer = false;

    public string IpAddress = "";
    const int PORT = 23000;
    ENetMultiplayerPeer peer;
    private Upnp _upnp;

    [Signal] public delegate void ServerStartedEventHandler();
    [Signal] public delegate void NetworkStoppedEventHandler();

    [Signal] public delegate void UpdatePlayerNamesEventHandler();

    public Dictionary<int, string> PlayerNames = new();
    public override void _Ready()
    {
        Multiplayer.ServerDisconnected += OnServerDisconnected;
        Multiplayer.ConnectionFailed += OnServerDisconnected;
    }
    private void OnServerDisconnected()
    {
        
        
            lastDisconnectReason = "Lost connection to the server.";
            JustDisconnectedFromServer = true;
        
        Disconnect();
    }
    public bool IsNetworkActive()
    {
        return peer != null && peer.GetConnectionStatus() != MultiplayerPeer.ConnectionStatus.Disconnected;
    }
    public void SubmitPlayerName(string name)
    {
        RpcId(1, nameof(RpcSubmitPlayerName), name);
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void RpcSubmitPlayerName(string name)
    {
        int senderId = Multiplayer.GetRemoteSenderId();
        PlayerNames[senderId] = name;

        List<int> idList = new();
        List<string> nameList = new();
        foreach ((int key, string value) in PlayerNames)
        {
            idList.Add(key);
            nameList.Add(value);
        }
        int[] ids = idList.ToArray();
        string[] names = nameList.ToArray();
        Rpc(nameof(RpcUpdatePlayerNames), ids, names);
    }
    public void RemovePlayerName(int id)
    {
        PlayerNames.Remove(id);
        List<int> idList = new();
        List<string> nameList = new();
        foreach ((int key, string value) in PlayerNames)
        {
            idList.Add(key);
            nameList.Add(value);
        }
        int[] ids = idList.ToArray();
        string[] names = nameList.ToArray();
        Rpc(nameof(RpcUpdatePlayerNames), ids, names);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    private void RpcUpdatePlayerNames(int[] ids, string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            PlayerNames[ids[i]] = names[i];
        }
        EmitSignal(SignalName.UpdatePlayerNames);
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
        PlayerNames = new();
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