using Godot;

public partial class ResolutionChanger : OptionButton, IAppliable
{
    Vector2I[] _options = 
    [
        new(1920,1080), new(1600,900), new(1280,720),
        new(854,480), new(640,360), new(256,144)
    ];

    [Export] private Vector2I _baseResolution = new(1920, 1080);

    private const string Section = "Video";
    private const string Key = "resolution_index";

    public void Apply()
    {
        if (Selected < _options.Length && Selected >= 0)
        {
            var window = GetViewport().GetWindow();

            // Window stays fixed — this control is now a performance setting, not a display-size one
            window.Size = _baseResolution;
            window.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
            window.ContentScaleAspect = Window.ContentScaleAspectEnum.Expand;
            window.ContentScaleSize = _baseResolution; // fixed UI design resolution — never changes

            // 3D-only render resolution — this is the part that actually changes with selection
            var viewport = GetViewport();
            viewport.Scaling3DMode = Viewport.Scaling3DModeEnum.Bilinear;
            viewport.Scaling3DScale = (float)_options[Selected].X / _baseResolution.X;
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