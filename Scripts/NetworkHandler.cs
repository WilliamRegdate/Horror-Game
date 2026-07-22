using Godot;
using System;
using System.Net;
public partial class NetworkHandler : Node
{
	const string IP_ADDRESS = "localhost"; 
	const int PORT =  23000;
	ENetMultiplayerPeer peer;

	public void StartServer(int maxPlayers = 32)
	{
		peer = new();
		peer.CreateServer(PORT, maxPlayers);
		Multiplayer.MultiplayerPeer = peer;
	}
	public void StartClient()
	{
		peer = new();
		peer.CreateClient(IP_ADDRESS, PORT);
		Multiplayer.MultiplayerPeer = peer;
	}
}
