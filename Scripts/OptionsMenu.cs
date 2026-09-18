using Godot;

public partial class OptionsMenu : VBoxContainer
{
    [Export] Node[] _options;
    private const string SettingsPath = "user://settings.cfg";

    public override void _Ready()
    {
        LoadAndApplySettings();
    }

    public void ApplySettings()
    {
        foreach (var option in _options)
        {
            if (option is IAppliable appliable)
                appliable.Apply();
        }
        SaveSettings();
    }

    private void SaveSettings()
    {
        var config = new ConfigFile();
        foreach (var option in _options)
        {
            if (option is IAppliable appliable)
                appliable.SaveSettings(config);
        }
        config.Save(SettingsPath);
    }

    private void LoadAndApplySettings()
    {
        var config = new ConfigFile();
        if (config.Load(SettingsPath) != Error.Ok)
            return; // no file yet (first launch) — defaults are fine

        foreach (var option in _options)
        {
            if (option is IAppliable appliable)
            {
                appliable.LoadSettings(config);
                appliable.Apply(); // apply immediately, not just on next manual Apply click
            }
        }
    }
}
interface IAppliable
{
    void Apply();
    void SaveSettings(ConfigFile config);
    void LoadSettings(ConfigFile config);
}
