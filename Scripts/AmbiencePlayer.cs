using Godot;
using System;

public partial class AmbiencePlayer : AudioStreamPlayer3D
{
	public void OnStartGame()
	{
		Autoplay = true;
		Playing = true;
	}
}
