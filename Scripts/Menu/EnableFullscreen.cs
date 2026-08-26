using Godot;
using System;

public partial class EnableFullscreen : OptionButton, IAppliable
{

    [Export] private Vector2I _baseResolution = new(1920, 1080);

    public void Apply()
    {
		var window = GetViewport().GetWindow();
        switch (Selected)
		{
			case 1:
				window.Mode = Window.ModeEnum.Fullscreen;
				break;
			case 2:
				window.Mode = Window.ModeEnum.ExclusiveFullscreen;
				break;
			default:
				window.Mode = Window.ModeEnum.Windowed;
				break;
		}
    }
}
