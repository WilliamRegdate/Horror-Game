using Godot;

public partial class Draw : RayCast3D
{
	[Export] PackedScene _cross;
	[Export] Player _player;
	[Export] TextureRect _showColor;
	[Export] Label _label;

	public Color[] Colors =
	[
		Color.Color8(255,255,255),
		Color.Color8(255,110,110),
		Color.Color8(133,173,253),
		Color.Color8(234,215,122),
		Color.Color8(69,221,126),
		Color.Color8(32,32,32),
		Color.Color8(215,128,255)
	];
	public int CurrentIndex = 0;

	GameManager _gameManager;
	public override void _Ready()
	{
		_showColor.Hide();
		_gameManager = GetNode<GameManager>("/root/GameManager");
	}

	public override void _Process(double delta)
	{
		if (!IsMultiplayerAuthority()) return;
		_label.Text = $"Chalk Color:{Colors[CurrentIndex]} \nCurrentIndex: {CurrentIndex}";


		if (Input.IsActionPressed("draw"))
		{
			_showColor.Show();
		
			if (Input.IsActionJustPressed("scroll_up"))
			{
				CurrentIndex = (CurrentIndex + 1) % Colors.Length;
				_showColor.Modulate = Colors[CurrentIndex];
			}
			if (Input.IsActionJustPressed("scroll_down"))
			{
				CurrentIndex = (CurrentIndex - 1 + Colors.Length) % Colors.Length;
				_showColor.Modulate = Colors[CurrentIndex];
			}
		}
		else
		{
			_showColor.Hide();
		}
		if (Input.IsActionJustReleased("draw"))
		{
			if (_player.Chalk > 0)
			{
				if (IsColliding())
				{
					Vector3 hitPoint = GetCollisionPoint();
					Vector3 hitNormal = GetCollisionNormal();
					Rpc(nameof(PlaceCross), CurrentIndex, hitPoint, hitNormal);
					_player.Chalk--;
				}
			}
		}
	}
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	public void PlaceCross(int colorIndex, Vector3 hitPoint, Vector3 hitNormal)
	{
		Decal node = _cross.Instantiate() as Decal;
		_gameManager.AddChild(node);

		Vector3 up = hitNormal.Normalized();
		Vector3 fallback = Mathf.Abs(up.Dot(Vector3.Up)) > 0.99f ? Vector3.Forward : Vector3.Up;
		Vector3 right = fallback.Cross(up).Normalized();
		Vector3 forward = up.Cross(right).Normalized();

		node.GlobalTransform = new Transform3D(new Basis(right, up, forward), hitPoint);
		node.Modulate = Colors[colorIndex];
	}
}
