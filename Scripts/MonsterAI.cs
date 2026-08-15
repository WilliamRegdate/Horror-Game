using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
	private class PlayerTracker
	{

		public double Awareness;  // how aware the monster is of the player (how certain monster is of this players location)
		public Vector3 LastLocation; // where the monster thinks this player is 
		public Vector3 PositionLastFrame = new();
    	public float CurrentSpeed = 0;
		public PlayerTracker()
		{
			Awareness = 0;
			LastLocation = Vector3.Zero;
		}
	} 

	[Export] private RayCast3D _checkForPlayer;
	[Export] private Monster _monster;
	[Export] private NavigationAgent3D _agent;
	private Dictionary<Player,PlayerTracker> _players = new();

	[Export] public Label TestLabel;
	private Node3D _currentTarget;
	bool _locked = true;
	private Vector3 _levelMiddle = new(0,0,0);

	int _movespeed = (int)MonsterSpeed.Walk;

    private bool _hasDestination = false;
	private const float RotationSpeed = 2.0f;

    public override void _Ready()
    {
		_agent.TargetPosition = Position;
		_checkForPlayer.AddException(this);
		_currentTarget = this;
    }

	public void OnPlayerAdded(Node player)
	{
		_players.Add(player as Player, new());
		_locked = false;
	}
	public override void _PhysicsProcess(double delta)
	{
		if (!IsMultiplayerAuthority())
		{
			return;
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
	
		if (_locked || !IsMultiplayerAuthority())
			return;
		if (_startState)
			GD.Print("set state :", _state);

		//work out each player's sound level
		foreach (var (player, data) in _players)
		{
			Vector3 currentPos = player.GlobalPosition;
			float distanceMoved = currentPos.DistanceTo(data.PositionLastFrame);
			data.CurrentSpeed = distanceMoved / (float)delta;
			data.PositionLastFrame = currentPos;
			float instant = distanceMoved / (float)delta;
			data.CurrentSpeed = Mathf.Lerp(data.CurrentSpeed, instant, 0.2f);
			player.SoundLevel = Mathf.Abs(data.CurrentSpeed * 0.2) < 0.5 ? 0f : data.CurrentSpeed  * 0.2f;
		}
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
			_escapePos = _levelMiddle - _currentTarget.Position;
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
		//check for all players
		foreach (var item in _players)
		{
			_checkForPlayer.TargetPosition = _checkForPlayer.ToLocal(item.Key.GlobalPosition);
			if (_checkForPlayer.IsColliding())
			{
				
				if (_checkForPlayer.GetCollider() is Player)
				{
					_startState = true;
					_state = MonsterState.Chase;	
					_currentTarget = (Node3D)_checkForPlayer.GetCollider();
					return;
				}
			}
		}
	}
	const double _forcedHintInterval = 180;
	public void Search(double delta)
	{
		if (_startState)
		{
			SetSpeed(MonsterSpeed.Walk);
			_startState = false;
		}

		TestLabel.Text = "";

		float currentDistance;
		//check for all players
		foreach (var item in _players)
		{
			_checkForPlayer.TargetPosition = _checkForPlayer.ToLocal(item.Key.GlobalPosition);
			if (_checkForPlayer.IsColliding())
			{
				
				if (_checkForPlayer.GetCollider() is Player)
				{
					// _startState = true;
					// _state = MonsterState.Chase;	
					// _currentTarget = (Node3D)_checkForPlayer.GetCollider();
					// return;
				}
			}
			//get player sounds and add them to monsters awareness
			item.Value.Awareness += GetSoundLevel(item.Key) * delta;
			TestLabel.Text += $"Awareness: {item.Value.Awareness}\n";
			currentDistance = GlobalPosition.DistanceSquaredTo(item.Key.GlobalPosition);
			
			if (item.Value.Awareness  > 100)
			{
				_agent.TargetPosition = GetHintPosition(item.Key.Position, 1f); // move to target by 50%
				item.Value.Awareness = 0;
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
		//_checkForPlayer.TargetPosition = _checkForPlayer.ToLocal(_player.GlobalPosition);
		if (_checkForPlayer.IsColliding())
		{
		//	if (_checkForPlayer.GetCollider() == _player)
			{
				_chaseTime = 2;
			}
		}
		//_agent.TargetPosition = _player.GlobalPosition;
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
	/// <summary>
	/// returns the sound level the monster can hear
	/// </summary>
	/// <param name="player"></param>
	/// <returns></returns>
	private double GetSoundLevel(Player player)
	{
		return player.SoundLevel / (1 + 0.4 * GlobalPosition.DistanceSquaredTo(player.GlobalPosition)) * 70000;// * 10000;
	}
	/// <summary>
	/// returns the amount to move towards the target
	/// </summary>
	/// <param name="TargetPos"> pos of target in global space</param>
	/// <returns></returns>
	private Vector3 GetHintPosition(Vector3 targetPos, float hintAmount)
	{
		return GlobalPosition + ((targetPos - GlobalPosition) * hintAmount);
	}

}
