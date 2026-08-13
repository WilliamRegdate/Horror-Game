using System;
using Godot;

public partial class PlayerCamera : Camera3D
{
    [Export] public float MouseSensitivity;
    [Export] public float BobFrequency;
    [Export] public float BobHeight;
    [Export] public float BobSway;

    [Export] public AudioStream[] FootstepSounds;
    [Export] private AudioStreamPlayer3D _audioPlayer;

    private AudioStreamPlaybackPolyphonic _polyphonic;
    
    private Vector2 _mouseDelta;
    private float _rotationX = 0f;
    private float _bobTime = 0f;
    public Vector3 _originalPosition;
    private Player _player;
    private bool soundCooldown;
    public bool IsCrouching { get; set; }
    private float _standHeight;
    private float _crouchHeight = -0.5f;

    public override void _Ready()
    {
        if (IsMultiplayerAuthority())
            Current = true;
        // Audio needs to run on every instance — other players need to hear your footsteps
        _audioPlayer.Stream = new AudioStreamPolyphonic();
        _audioPlayer.Play();
        _polyphonic = _audioPlayer.GetStreamPlayback() as AudioStreamPlaybackPolyphonic;

        if (!Current) return; // input/mouse-capture only matters for your own view
        Input.MouseMode = Input.MouseModeEnum.Captured;
        _player = GetParent<Player>();
        _originalPosition = Position;
        _standHeight = Position.Y;
    }

    public override void _Input(InputEvent @event)
    {

        if (@event is InputEventMouseMotion mouseMotion)
        {
            _mouseDelta = mouseMotion.Relative;
        }
    }

    public override void _Process(double delta)
    {
        if (!IsMultiplayerAuthority()) return;
        float deltaTime = (float)delta;
        
        _rotationX = Mathf.Clamp(_rotationX - _mouseDelta.Y * MouseSensitivity, -90f, 90f);
        RotationDegrees = new Vector3(_rotationX, RotationDegrees.Y, 0);
        _player.RotationDegrees -= new Vector3(0, _mouseDelta.X * MouseSensitivity, 0);
        _mouseDelta = Vector2.Zero;
        
        HandleViewBobbing(deltaTime);
    }

    private void HandleViewBobbing(float deltaTime)
    {
        float targetY = IsCrouching ? _crouchHeight : _standHeight;
        _originalPosition.Y = Mathf.Lerp(_originalPosition.Y, targetY, deltaTime * 8f);

        float speed = _player.Velocity.Length();
        bool isMoving = speed > 0.1f && _player.IsOnFloor();

        if (isMoving)
        {
            _bobTime += deltaTime * BobFrequency * speed;

            float verticalBob    = Mathf.Sin(_bobTime) * BobHeight;
            float horizontalSway = Mathf.Sin(_bobTime * 0.5f) * BobSway;

            Position = new Vector3(
                _originalPosition.X + horizontalSway,
                _originalPosition.Y + verticalBob,
                _originalPosition.Z
            );
            if (Mathf.Sin(_bobTime) < -0.9f && !soundCooldown)
            {
                PlayFootstep();
                soundCooldown = true;
            }
            if (MathF.Sin(_bobTime) > 0)
                soundCooldown = false;
        }
        else
        {
            _bobTime = 0f;
            Position = new Vector3(
                Mathf.Lerp(Position.X, _originalPosition.X, deltaTime * 10f),
                Mathf.Lerp(Position.Y, _originalPosition.Y, deltaTime * 10f),
                _originalPosition.Z
            );
        }
    }

    private void PlayFootstep()
    {
        var randomIndex = GD.RandRange(0, FootstepSounds.Length - 1);
        _polyphonic.PlayStream(FootstepSounds[randomIndex], 0, 5);
    }
}