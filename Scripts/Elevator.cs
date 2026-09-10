using Godot;
using System;

public partial class Elevator : Node3D
{
	GameManager _gameManager;
	[Export] AnimationPlayer _player;
	[Export] AudioStreamPlayer3D _audio;
	public override void _Ready()
	{
		_gameManager = GetNode<GameManager>("/root/GameManager");
		_gameManager.StartGame += OnStartGame;
	}
	public void OnStartGame()
	{
		_player.Play("StartGame");
		_audio.Play();
	}
    public override void _ExitTree()
    {
        _gameManager.StartGame -= OnStartGame;
    }

	
}
