using Godot;

public partial class Torch : SpotLight3D
{
	[Export] AudioStreamPlayer3D _audio;
	[Export] Player _player;
	bool _powerOut;
	bool _activated;
	const int BATTERY_LIFE = 90;
	public double CurrentPowerLevel = BATTERY_LIFE;

    public override void _Ready()
    {
        Hide();
    }

	public override void _Process(double delta)
	{
		if (_player.Batteries <= 0)
		{
			return;
		}
		if (!IsMultiplayerAuthority())
			return;
		if (Input.IsActionJustPressed("torch_toggle"))
		{
			if (!_powerOut)
			{
				_activated = !_activated;
				Visible = _activated;
			}
			_audio.PitchScale = (float)GD.RandRange(0.8,1.3);
			_audio.Play();
		}
		if (CurrentPowerLevel < 0)
		{
			_player.Batteries -= 1;
			CurrentPowerLevel = BATTERY_LIFE;
			if (_player.Batteries <= 0)
			{
				_activated = false;
				Visible = false;
				return;
			}
		}
		if (_activated)
		{
			CurrentPowerLevel -= delta;
		}
	}
}
