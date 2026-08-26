using Godot;

public partial class OptionsMenu : VBoxContainer
{
	[Export] Node[] _options;
	Button _apply;
	public void ApplySettings()
	{
		foreach (var option in _options)
		{
			if (option is IAppliable appliable)
			{
				GD.Print("Applied");
				appliable.Apply();
			}
		}
	}
}
interface IAppliable
{
	void Apply();
}
