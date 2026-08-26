using Godot;

public partial class GeneratorSwitch : Area3D, IInteractable
{
	// Called when the node enters the scene tree for the first time.
	[Export] SpotLight3D _light;
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		
	}
	public void Interact(Player player)
	{
		_light.LightEnergy = 16;
	}
}
