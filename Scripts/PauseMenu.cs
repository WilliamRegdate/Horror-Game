using Godot;
using System;

public partial class PauseMenu : CanvasLayer
{
	[Export] private PlayerCamera _camera;
	NetworkHandler _networkHandler;
	public override void _Ready()
	{
		Visible = false;
		_networkHandler = GetNode<NetworkHandler>("/root/NetworkHandler");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (!IsMultiplayerAuthority()) return;
		if (Input.IsActionJustPressed("ui_cancel"))
		{
			Visible = !Visible;
			if (Visible)
			{
				Input.MouseMode = Input.MouseModeEnum.Visible;
				_camera.DisableMouse = true;
			}
			else
			{
				Input.MouseMode = Input.MouseModeEnum.Captured;
				_camera.DisableMouse = false;
			}
		}
	}
	
	public void OnResumePressed()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
		Visible = false;
		_camera.DisableMouse = false;
	}
	public void OnQuitPressed()
	{
		_networkHandler.Disconnect();
	}	
}
