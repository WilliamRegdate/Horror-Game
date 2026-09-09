using Godot;
using System;

public partial class ChangePLayerName : Button
{
	private NetworkHandler _networkHandler;
	[Export]LineEdit _text;
	public override void _Ready()
	{
		_networkHandler = GetNode<NetworkHandler>("/root/NetworkHandler");
	}
	public void OnPressed()
	{
		_networkHandler.SubmitPlayerName(_text.Text);
	}
}
