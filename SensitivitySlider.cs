using Godot;
public partial class SensitivitySlider : HSlider, IAppliable
{
    public static float Sensitivity { get; private set; } = 1f;

    private const string Section = "Controls";
    private const string Key = "sensitivity";

    public void Apply()
    {
        Sensitivity = (float)Value;
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
