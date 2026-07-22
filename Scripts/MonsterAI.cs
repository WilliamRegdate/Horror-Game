using Godot;

public enum MonsterState
{
	Search,
	Taunt,
	Hunt,
	Chase,
	Escape
}
public enum MonsterSpeed
{
	Walk = 6,
	Run = 11
}

public partial class MonsterAI : CharacterBody3D
{
	private ImmediateMesh _debugMesh;
	[Export] private RayCast3D _checkForPlayer;
	[Export] private Monster _monster;
	[Export] private NavigationAgent3D _agent;
	private Node3D _player;


	bool _locked = true;
	private Vector3 _levelMiddle = new(0,0,0);

	int _movespeed = (int)MonsterSpeed.Walk;

    private bool _hasDestination = false;
	private const float RotationSpeed = 2.0f;
    public override void _Ready()
    {
		_agent.TargetPosition = Position;
		_checkForPlayer.AddException(this);
    }

	public void OnStartGame()
	{
		_player = GetTree().GetFirstNodeInGroup("Player") as Node3D;
        if (_player == null)
        {
            GD.PrintErr("Player not found in group");
            return;
        }
		_locked = false;
	}
	public override void _PhysicsProcess(double delta)
	{
		if (Input.IsActionPressed("ui_cancel"))
		{
			_state = MonsterState.Escape;
		}

		Vector3 velocity = new();

		if (!_agent.IsNavigationFinished())
		{

			Vector3 destination = _agent.GetNextPathPosition();
			Vector3 localDestination = destination - GlobalPosition;
			Vector3 direction = localDestination.Normalized();
			
			velocity = direction * _movespeed;

			Vector3 flatDirection = new Vector3(direction.X, 0, direction.Z);
			if (flatDirection.LengthSquared() > 0.01f)
			{
				float targetAngle = Mathf.Atan2(-flatDirection.X, -flatDirection.Z);
				float currentAngle = _monster.Rotation.Y;
				float smoothedAngle = Mathf.LerpAngle(currentAngle, targetAngle, RotationSpeed * (float)delta);
				_monster.Rotation = new Vector3(_monster.Rotation.X, smoothedAngle, _monster.Rotation.Z);
			}
		}
		else
		{
			Velocity = new(0,0,0);
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	//AI TIME!!
    public override void _Process(double delta)
    {
		if (_locked)
			return;
		if (_startState)
			GD.Print("set state :", _state);
        switch(_state)
		{
			case MonsterState.Escape:
				Escape(delta);
				break;
			case MonsterState.Hunt:
				Hunt(delta);
				break;
			case MonsterState.Search:
				Search(delta);
				break;
			case MonsterState.Chase:
				Chase(delta);
				break;
			case MonsterState.Taunt:
				Taunt(delta); 
				break;
		}
    }

	//AI VARS
	MonsterState _state;
	bool _startState = true;
	double _timer = 0f;


	private Vector3 _escapePos;
	public void Escape(double delta)
	{
		if (_startState)
		{
			SetSpeed(MonsterSpeed.Run);
			_timer = 20;
			_startState = false;
			_escapePos = _levelMiddle - _player.Position;
		}
		if (_timer < 0)
		{
			_state = MonsterState.Search;
			_startState = true;
			return;
		}
		_timer -= delta;
		_agent.TargetPosition = _escapePos;
		_hasDestination = true;
	}
	Vector3 _checkPos;
	public void Hunt(double delta)
	{
		if (_startState)
		{
			_checkPos = _agent.TargetPosition;
			_agent.TargetPosition = _checkPos;
			_startState = false;
		}

		_agent.TargetPosition = _checkPos;

		_checkForPlayer.TargetPosition = _checkForPlayer.ToLocal(_player.GlobalPosition);
		if (_checkForPlayer.IsColliding())
		{
			if (_checkForPlayer.GetCollider() == _player)
			{
				_startState = true;
				_state = MonsterState.Chase;	
			}
		}
	}
	public void Search(double delta)
	{
		

		if (_startState)
		{
			SetSpeed(MonsterSpeed.Walk);
			_startState = false;
		}
		_agent.TargetPosition = _player.GlobalPosition;
		_checkForPlayer.TargetPosition = _checkForPlayer.ToLocal(_player.GlobalPosition);

		if (_checkForPlayer.IsColliding())
		{
			if (_checkForPlayer.GetCollider() == _player)
			{
				_state = MonsterState.Chase;
				_startState = true;
				_timer = 2.5;
			}
		}
	}
	double _chaseTime;
	public void Chase(double delta)
	{
		if (_startState)
		{
			SetSpeed(MonsterSpeed.Run);
			_agent.TargetPosition = GlobalPosition;
			_startState = false;
			_chaseTime = 2.5;
		}
		if (_timer > 0)
		{
			_timer -= delta;
			return;
		}
		_checkForPlayer.TargetPosition = _checkForPlayer.ToLocal(_player.GlobalPosition);
		if (_checkForPlayer.IsColliding())
		{
			if (_checkForPlayer.GetCollider() == _player)
			{
				_chaseTime = 2;
			}
		}
		_agent.TargetPosition = _player.GlobalPosition;
		_chaseTime -= delta;
		if (_chaseTime < 0)
		{
			_startState = true;
			_state = MonsterState.Hunt;
		}
	}
	public void Taunt(double delta)
	{
		
	}

	private void SetSpeed(MonsterSpeed num)
	{
		int speed = (int)num;
		_movespeed = speed;
		_monster.StepDuration = 1.2f / speed;

	}

}
