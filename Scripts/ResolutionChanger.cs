using Godot;

public partial class ResolutionChanger : OptionButton, IAppliable
{
    Vector2I[] _options = 
    [
        new(1920,1080),
        new(1600,900),
        new(1280,720),
		new(854,480),
		new(640,360),
		new(256,144)
    ];

    [Export] private Vector2I _baseResolution = new(1920, 1080);

    public override void _Ready()
    {
    }

    public void Apply()
    {
        if (Selected < _options.Length && Selected >= 0)
        {
            var window = GetViewport().GetWindow();
            window.Size = _options[Selected];

            window.ContentScaleMode = Window.ContentScaleModeEnum.Viewport;
            window.ContentScaleAspect = Window.ContentScaleAspectEnum.Expand;
            window.ContentScaleSize = _options[Selected];
        }
    }
}