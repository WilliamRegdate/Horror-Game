using Godot;
using System;

public partial class PlayerAnimation : Node3D
{
	private Player _player;
	private Vector3 _originalPosition = new();

	[Export] private Node3D[] _footRestPositions;
	[Export] private Node3D[] _legTargets;
	private bool[] _isStepping = new bool[2];
	private Vector3[] _stepStartPos = new Vector3[2];
	private Vector3[] _stepEndPos = new Vector3[2];
	private float[] _stepProgress = new float[2];
	[Export] private SkeletonModifier3D[] _ikInfluenceTargets;

	[Export] private float _ikFadeStartSpeed = 1.5f; 
	[Export] private float _ikFadeEndSpeed = 2.5f;

	[Export] private AnimationTree _animationTree;

	public override void _Ready()
	{
		_originalPosition = Position;
		_player = GetParent() as Player;
		TopLevel = true;
		_animationTree.Active = true;
	}

	public override void _Process(double delta)
	{
		RotateBody(delta);
		UpdateMovementTracking(delta);
		UpdateIkInfluence();
		UpdateLocomotionBlend();
		Step(delta);
	}

	private void UpdateLocomotionBlend()
	{
		_animationTree.Set("parameters/BlendSpace1D/blend_position", _moveSpeed);

		float walkSpeed = _movingBackward? -1 : 1;
		if (_moveSpeed < 6)
		{
			_animationTree.Set("parameters/TimeScale/scale", _moveSpeed * 0.86f * walkSpeed);
		}
		else
		{
			_animationTree.Set("parameters/TimeScale/scale", _moveSpeed * 0.69f * walkSpeed);
		}
		
	}

	private void UpdateIkInfluence()
	{
		float t = _ikFadeEndSpeed > _ikFadeStartSpeed
			? Mathf.Clamp((_moveSpeed - _ikFadeStartSpeed) / (_ikFadeEndSpeed - _ikFadeStartSpeed), 0f, 1f)
			: (_moveSpeed < _ikFadeStartSpeed ? 0f : 1f);

		float influence = 1f - t;

		foreach (var ikTarget in _ikInfluenceTargets)
			ikTarget.Influence = influence;
	}

	private void UpdateMovementTracking(double delta)
	{
		Vector3 currentPos = _player.GlobalPosition;
		Quaternion currentRot = _player.GlobalTransform.Basis.GetRotationQuaternion();

		if (_firstFrame || delta <= 0)
		{
			_firstFrame = false;
			_lastPlayerPos = currentPos;
			_lastPlayerRot = currentRot;
			_moveSpeed = 0f;
			_rotSpeedDeg = 0f;
			return;
		}
		_moveVelocity = currentPos - _lastPlayerPos;
		_moveVelocity.Y = 0;
		_moveSpeed = _moveVelocity.Length() / (float)delta;
		_rotSpeedDeg = Mathf.RadToDeg(_lastPlayerRot.AngleTo(currentRot)) / (float)delta;

		_lastPlayerPos = currentPos;
		_lastPlayerRot = currentRot;
	}

	private float GetCurrentStepThreshold()
	{
		bool isStill = _moveSpeed < 0.025f && _rotSpeedDeg < 5f;
		return isStill ? _stillThreshold : _moveThreshold;
	}

	private void SetLegTargetPosition(int i, Vector3 desiredPos)
	{
		_legTargets[i].GlobalPosition = desiredPos;
	}

	[Export] private Node3D _camera;
	bool _movingBackward;
	private void RotateBody(double delta)
	{
		GlobalPosition = _player.GlobalPosition + _originalPosition;

		Vector3 scale = GlobalTransform.Basis.Scale;
		Quaternion current = GlobalTransform.Basis.GetRotationQuaternion();

		Quaternion target;
		if (_moveSpeed < 0.1f)
		{
			target = _player.GlobalTransform.Basis.GetRotationQuaternion();
		}
		else
		{
			Vector3 dir = _moveVelocity.Normalized();

			Vector3 camForward = -_camera.GlobalTransform.Basis.Z;
			camForward.Y = 0;
			camForward = camForward.Normalized();

			_movingBackward = camForward.Dot(dir) < -0.2f;
			Vector3 facingDir = _movingBackward ? -dir : dir;

			Vector3 up = Mathf.Abs(facingDir.Dot(Vector3.Up)) > 0.99f ? Vector3.Forward : Vector3.Up;
			target = Basis.LookingAt(facingDir, up).GetRotationQuaternion();
		}

		float angleDiff = current.AngleTo(target);
		if (angleDiff < 0.0001f)
			return;

		float maxStep = Mathf.DegToRad(360) * (float)delta;
		float weight = Mathf.Min(1f, maxStep / angleDiff);

		Quaternion newRotation = current.Slerp(target, weight);
		Basis newBasis = new Basis(newRotation).Scaled(scale);
		GlobalTransform = new Transform3D(newBasis, GlobalPosition);
	}

	private float _stepDuration = 0.15f;
	private float _stepArcHeight = 0.18f;
	private float _moveThreshold = 0.18f;
	private float _stillThreshold = 0.02f;
	private void Step(double delta)
	{
		if (_moveSpeed  > _ikFadeEndSpeed)
		{
			SetLegTargetPosition(0,_footRestPositions[0].GlobalPosition);
			SetLegTargetPosition(1,_footRestPositions[1].GlobalPosition);
		}
		for (int i = 0; i < 2; i++)
		{
			if (!_isStepping[i])
			{
				float distSq = (_footRestPositions[i].GlobalPosition - _legTargets[i].GlobalPosition).LengthSquared();
				if (distSq > GetCurrentStepThreshold() && !_isStepping[1 - i])
				{
					_isStepping[i] = true;
					_stepStartPos[i] = _legTargets[i].GlobalPosition;
					_stepEndPos[i] = _footRestPositions[i].GlobalPosition;
					_stepProgress[i] = 0f;
				}
				else
				{
					continue;
				}
			}

			_stepProgress[i] += (float)delta / _stepDuration;

			if (_stepProgress[i] >= 1f)
			{
				_stepProgress[i] = 1f;
				_isStepping[i] = false;
			}

			Vector3 flatPos = _stepStartPos[i].Lerp(_stepEndPos[i], _stepProgress[i]);
			float arc = Mathf.Sin(_stepProgress[i] * Mathf.Pi) * _stepArcHeight;

			SetLegTargetPosition(i, flatPos + Vector3.Up * arc);
		}
	}

	private Vector3 _lastPlayerPos;
	private Quaternion _lastPlayerRot;
	private bool _firstFrame = true;
	private float _moveSpeed;
	private float _rotSpeedDeg;
	private Vector3 _moveVelocity;
}