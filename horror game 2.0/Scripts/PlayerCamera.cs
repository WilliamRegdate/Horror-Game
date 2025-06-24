using System;
using Godot;

public partial class PlayerCamera : Camera3D
{
    [Export] public float MouseSensitivity;
    [Export] public float BobFrequency; // How fast the bobbing happens
    [Export] public float BobHeight;    // How high the bobbing goes
    [Export] public float BobSway;      // Side-to-side movement amount

    [Export] public AudioStream[] FootstepSounds;
    [Export] public AudioStreamPlayer3D _audioPlayer1;
    [Export] public AudioStreamPlayer3D _audioPlayer2;
    
    private Vector2 _mouseDelta;
    private float _rotationX = 0f;
    private float _bobTime = 0f;
    private Vector3 _originalPosition;
    private CharacterBody3D _player;
    private bool usePlayer1;
    private bool soundCooldown;

    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
        _player = GetParent<CharacterBody3D>();
        _originalPosition = Position; // Store initial camera position
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
        float deltaTime = (float)delta;
        
        // Handle camera rotation (your original code)
        _rotationX = Mathf.Clamp(_rotationX - _mouseDelta.Y * MouseSensitivity, -90f, 90f);
        RotationDegrees = new Vector3(_rotationX, RotationDegrees.Y, 0);
        _player.RotationDegrees -= new Vector3(0, _mouseDelta.X * MouseSensitivity, 0);
        _mouseDelta = Vector2.Zero;
        
        // Handle view bobbing
        HandleViewBobbing(deltaTime);
    }

    private void HandleViewBobbing(float deltaTime)
    {
        float speed = _player.Velocity.Length();
        bool isMoving = speed > 0.1f && _player.IsOnFloor();
        
        if (isMoving)
        {
            // Increment bobbing timer based on movement speed
            _bobTime += deltaTime * BobFrequency * speed;
            
            // Calculate bobbing effect using sine waves
            float verticalBob = Mathf.Sin(_bobTime) * BobHeight;
            float horizontalSway = Mathf.Sin(_bobTime * 0.5f) * BobSway;
            
            // Apply bobbing to camera position
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
            if(MathF.Sin(_bobTime) > 0)
            {
                soundCooldown = false;
            }
        }
        else
        {
            // Smoothly return to original position when not moving
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
            
        // Play random footstep sound
        var randomIndex = GD.RandRange(0, FootstepSounds.Length - 1);
        if (usePlayer1)
        {
        _audioPlayer1.Stream = FootstepSounds[randomIndex];
        _audioPlayer1.Play();
        usePlayer1 = !usePlayer1;
        return;
        }
        _audioPlayer2.Stream = FootstepSounds[randomIndex];
        _audioPlayer2.Play();
        usePlayer1 = !usePlayer1;
    }
}