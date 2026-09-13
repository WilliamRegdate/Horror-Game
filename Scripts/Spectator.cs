using Godot;
using System.Linq;

public partial class Spectator : Camera3D
{
	public Node3D CurrentView;
	NetworkHandler _networkHandler;
	GameManager _gameManager;
	int _currentlySpectating;
	public override void _Ready()
	{
		//Visible = false;
		Input.MouseMode = Input.MouseModeEnum.Visible;
		_networkHandler = GetNode<NetworkHandler>("/root/NetworkHandler");
		_gameManager = GetNode<GameManager>("/root/GameManager");
		Current = true;
	}

	public override void _Process(double delta)
	{
		if (CurrentView == null)
			return;
		GlobalPosition = CurrentView.GlobalPosition;
		GlobalRotation = CurrentView.GlobalRotation;
	}
	
	public void NextPlayer()
	{
		Node3D[] views = GetViews();
		GD.Print(views.Length);

		if (views.Length == 0)
			return;

		_currentlySpectating++;
		_currentlySpectating %= views.Length;
		CurrentView = views[_currentlySpectating];
	}
	public Node3D[] GetViews()
	{
		var tree = GetTree();
		if (tree == null)
			return System.Array.Empty<Node3D>();

		return tree.GetNodesInGroup("SpectatorView")
			.OfType<Node3D>()
			.ToArray();
	}
	public void PreviousPlayer()
	{
		Node3D[] views = GetViews();

		if (views.Length == 0)
			return;

		_currentlySpectating--;
		if (_currentlySpectating < 0)
			_currentlySpectating = views.Length - 1;

		CurrentView = views[_currentlySpectating];
	}
	public void OnQuitPressed()
	{
		_networkHandler.Disconnect();
	}	
}
