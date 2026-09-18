using Godot;
using System;

public partial class ChangePlayerName : Button
{
    private NetworkHandler _networkHandler;
    [Export] LineEdit _text;

    private const string SettingsPath = "user://settings.cfg";
    private const string PlayerSection = "Player";
    private const string NameKey = "player_name";

    public override void _Ready()
    {
        _networkHandler = GetNode<NetworkHandler>("/root/NetworkHandler");
        LoadPlayerName();
    }

    public void OnPressed()
    {
        SavePlayerName();
        _networkHandler.SubmitPlayerName(_text.Text);
    }

    private void LoadPlayerName()
    {
        var config = new ConfigFile();
        if (config.Load(SettingsPath) != Error.Ok)
            return;

        if (config.HasSectionKey(PlayerSection, NameKey))
            _text.Text = (string)config.GetValue(PlayerSection, NameKey);
    }

    private void SavePlayerName()
    {
        var config = new ConfigFile();
        config.Load(SettingsPath); // ok to ignore Error — missing file just means we start fresh
        config.SetValue(PlayerSection, NameKey, _text.Text);
        config.Save(SettingsPath);
    }
}
