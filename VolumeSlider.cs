using Godot;
public partial class VolumeSlider : HSlider, IAppliable
{
    [Export] private string _busName = "Master";

    private const string Section = "Audio";
    private const string Key = "volume";

    public void Apply()
    {
        int busIndex = AudioServer.GetBusIndex(_busName);
        if (busIndex == -1) return;

        // Value is treated as linear 0-1 — set MinValue/MaxValue/Step on the Slider node itself in the editor
        AudioServer.SetBusVolumeDb(busIndex, Mathf.LinearToDb((float)Value));
        AudioServer.SetBusMute(busIndex, Value <= 0.0);
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
