using Godot;

public partial class KeyMapping : Button, IAppliable
{
    [Export] private Label _currentKeyName;
    [Export] private string _actionName; // must match an action name in Project Settings > Input Map

    private const string Section = "Keybinds";
    private bool _listeningForInput = false;

    public override void _Ready()
    {
        Pressed += OnPressed;
        UpdateLabel();
    }

    private void OnPressed()
    {
        _currentKeyName.Text = "Press any key...";
        // Deferred so the same click/keypress that triggered this button doesn't
        // immediately get captured as the new binding.
        CallDeferred(nameof(BeginListening));
    }

    private void BeginListening()
    {
        _listeningForInput = true;
    }

    public override void _Input(InputEvent @event)
	{
		if (!_listeningForInput)
			return;

		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			if (keyEvent.Keycode == Key.Escape)
			{
				_listeningForInput = false;
				UpdateLabel();
				GetViewport().SetInputAsHandled();
				return;
			}

			RebindTo(keyEvent);
			_listeningForInput = false;
			GetViewport().SetInputAsHandled();
		}
		else if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
		{
			RebindTo(mouseEvent);
			_listeningForInput = false;
			GetViewport().SetInputAsHandled();
		}
	}

    private void RebindTo(InputEvent newEvent)
    {
        if (string.IsNullOrEmpty(_actionName) || !InputMap.HasAction(_actionName))
        {
            GD.PrintErr($"KeyMapping: action \"{_actionName}\" not found in InputMap.");
            return;
        }

        InputMap.ActionEraseEvents(_actionName);
        InputMap.ActionAddEvent(_actionName, newEvent);
        UpdateLabel();
    }

    private void UpdateLabel()
    {
        if (string.IsNullOrEmpty(_actionName) || !InputMap.HasAction(_actionName))
        {
            _currentKeyName.Text = "Unbound";
            return;
        }

        var events = InputMap.ActionGetEvents(_actionName);
        _currentKeyName.Text = events.Count > 0 ? events[0].AsText() : "Unbound";
    }

    public void Apply()
    {
        // Rebinding already takes effect immediately in RebindTo — InputMap changes
        // are live the moment they're made. Nothing left to apply, just keep the
        // label honest in case something else changed the binding underneath us.
        UpdateLabel();
    }

    public void SaveSettings(ConfigFile config)
    {
        if (string.IsNullOrEmpty(_actionName) || !InputMap.HasAction(_actionName))
            return;

        var events = InputMap.ActionGetEvents(_actionName);
        if (events.Count == 0) return;

        var evt = events[0];
        if (evt is InputEventKey key)
        {
            config.SetValue(Section, _actionName + "_type", "key");
            config.SetValue(Section, _actionName + "_code", (int)key.Keycode);
        }
        else if (evt is InputEventMouseButton mouse)
        {
            config.SetValue(Section, _actionName + "_type", "mouse");
            config.SetValue(Section, _actionName + "_code", (int)mouse.ButtonIndex);
        }
    }

    public void LoadSettings(ConfigFile config)
    {
        string typeKey = _actionName + "_type";
        string codeKey = _actionName + "_code";

        if (string.IsNullOrEmpty(_actionName) ||
            !config.HasSectionKey(Section, typeKey) ||
            !config.HasSectionKey(Section, codeKey) ||
            !InputMap.HasAction(_actionName))
            return;

        string type = (string)config.GetValue(Section, typeKey);
        int code = (int)config.GetValue(Section, codeKey);

        InputEvent restoredEvent = type switch
        {
            "key" => new InputEventKey { Keycode = (Key)code },
            "mouse" => new InputEventMouseButton { ButtonIndex = (MouseButton)code },
            _ => null
        };

        if (restoredEvent == null)
            return;

        InputMap.ActionEraseEvents(_actionName);
        InputMap.ActionAddEvent(_actionName, restoredEvent);
        UpdateLabel();
    }
}