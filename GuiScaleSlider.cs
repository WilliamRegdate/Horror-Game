using Godot;
public partial class GuiScaleSlider : HSlider, IAppliable
{
    private const string Section = "Video";
    private const string Key = "gui_scale";
	
    public void Apply()
    {
        var window = GetViewport().GetWindow();
        window.ContentScaleFactor = (float)Value;
    }

    public void SaveSettings(ConfigFile config)
    {
        config.SetValue(Section, Key, Value);
    }

    public void LoadSettings(ConfigFile config)
    {
        if (config.HasSectionKey(Section, Key))
            Value = (double)config.GetValue(Section, Key);
    }
	public void Reset()
	{
		Value = 1;
	}
}
