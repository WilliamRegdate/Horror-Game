using Godot;
using System;

public partial class EnableFullscreen : OptionButton, IAppliable
{
    private const string Section = "Video";
    private const string Key = "fullscreen_index";
	
    public void Apply()
    {
        var window = GetViewport().GetWindow();
        switch (Selected)
        {
            case 1: window.Mode = Window.ModeEnum.Fullscreen; break;
            case 2: window.Mode = Window.ModeEnum.ExclusiveFullscreen; break;
            default: window.Mode = Window.ModeEnum.Windowed; break;
        }
    }

    public void SaveSettings(ConfigFile config)
    {
        config.SetValue(Section, Key, Selected);
    }

    public void LoadSettings(ConfigFile config)
    {
        if (config.HasSectionKey(Section, Key))
            Selected = (int)config.GetValue(Section, Key);
    }
}
