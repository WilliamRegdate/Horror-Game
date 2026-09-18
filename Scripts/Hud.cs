using Godot;

public partial class Hud : CanvasLayer
{
	[Export] TextureProgressBar _batteryFillAmount;
	[Export] TextureRect _chalkImage;
	[Export] Draw _chalkColor;

	[Export] Label _batteryLabel;
	[Export] Label _chalkLabel;
	[Export] Torch _torch;
	[Export] Player _player;



	public override void _Ready()
	{
		Hide();
		if(!IsMultiplayerAuthority())
			return;
		Show();
	}

	
	public override void _Process(double delta)
	{
		if(!IsMultiplayerAuthority())
			return;
		_batteryLabel.Text = $"{_player.Batteries}";
		_chalkLabel.Text = $"{_player.Chalk}";
		_batteryFillAmount.Value = _torch.CurrentPowerLevel;
		_chalkImage.Modulate = _chalkColor.Colors[_chalkColor.CurrentIndex];
	}
}
