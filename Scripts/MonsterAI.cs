using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Godot;

public enum MonsterState
{
	Search,
	Taunt,
	Hunt,
	Chase,
	Wander,
	Start,
	Kill
}
public enum MonsterSpeed
{
	Walk = 6,
	Run = 11
}

public partial class MonsterAI : CharacterBody3D
{
	public class PlayerTracker
	{
		public bool IsDead;
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
	public Dictionary<Player, PlayerTracker> Players { get; private set; } = new();
	private Player _currentTarget;
	bool _locked = true;
	private Vector3 _levelMiddle = new(0,0,0);

	int _movespeed = (int)MonsterSpeed.Walk;

    private bool _hasDestination = false;
	private const float RotationSpeed = 2.0f;

	// --- Chase audio ---
	// Two players so we can overlap: while one is finishing, the other starts the next clip on top of it.
	[Export] private AudioStreamPlayer3D _chaseAudioA;
	[Export] private AudioStreamPlayer3D _chaseAudioB;
	[Export] private Godot.Collections.Array<AudioStream> _chaseSounds = new();
	[Export(PropertyHint.Range, "0,1,0.01")] private float _chaseOverlapFraction = 0.6f; // start next clip once current is this far through

	private AudioStreamPlayer3D _activeChasePlayer;
	private AudioStreamPlayer3D _inactiveChasePlayer;
	private bool _chaseAudioActive = false;

	[Export] private Area3D _killZone;

    public override void _Ready()
    {
			

		_agent.TargetPosition = Position;
		_checkForPlayer.AddException(this);
		_currentTarget = new();
		_timer = 180;
		_timer = 60;
		_activeChasePlayer = _chaseAudioA;
		_inactiveChasePlayer = _chaseAudioB;
    }

	public void OnPlayerAdded(Node player)
	{
		Players.Add(player as Player, new());
		_locked = false;
	}
	public void OnPlayerRemoved(Node node)
	{
		if (node is Player player)
			Players.Remove(player);
		int largestAwareness = 0;
		foreach((Player currentPlayer, PlayerTracker playerData) in Players)
		{
			if (playerData.Awareness > largestAwareness) //set target to the next most ovbious target if a player is removed
			{
				largestAwareness = (int)playerData.Awareness;
				_currentTarget = currentPlayer;
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!Multiplayer.IsServer())
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
		UpdateChaseAudio(delta);
		if (_locked || !Multiplayer.IsServer())
			return;
		if (_startState)
			GD.Print("set state :", _state);

		//work out each player's sound level
		foreach (var (player, data) in Players)
		{
			Vector3 currentPos = player.GlobalPosition;
			float distanceMoved = currentPos.DistanceTo(data.PositionLastFrame);
			data.CurrentSpeed = distanceMoved / (float)delta;
			data.PositionLastFrame = currentPos;
			float instant = distanceMoved / (float)delta;
			data.CurrentSpeed = Mathf.Lerp(data.CurrentSpeed, instant, 0.2f);
			player.SoundLevel = Mathf.Abs(data.CurrentSpeed * 0.2) < 0.9 ? 0f : data.CurrentSpeed  * 0.2f;
		}


		//try kill player
		if (_killZone.HasOverlappingBodies())
		{
			var bodies = _killZone.GetOverlappingBodies();
			foreach (Node3D body in bodies)
			{
				if (body is Player playerToKill )
				{
					if (Players[playerToKill].IsDead)
						continue;
					if (CheckPlayerLineOfSight(playerToKill.Collider.GlobalPosition) != null)
					{
						_startState = true;
						_state = MonsterState.Kill;
						playerToKill.Die();
						Players[playerToKill].IsDead = true;
						return;
					}
				}
			}
		}
        switch(_state)
		{
			case MonsterState.Wander:
				Wander(delta);
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
			case MonsterState.Start:
				Start(delta); 
				break;
			case MonsterState.Kill:
				Kill(delta); 
				break;
		}
    }

	//AI VARS
	MonsterState _state = MonsterState.Start;
	bool _startState = true;
	double _timer = 0f;


	public void Start(double delta)
	{
		_startState = false;
		_agent.TargetPosition = GlobalPosition;
		if (_timer < 0)
		{
			_startState = true;
			_state = MonsterState.Search;
		}
		_timer -= delta;
	}

	//private Vector3 _escapePos;
	//_escapePos = _levelMiddle - _currentTarget.Position;

	public void Wander(double delta)
	{
		if (_startState)
		{
			SetSpeed(MonsterSpeed.Walk);
			_timer = 60;
			_secondaryTimer = 0;
			_startState = false;
		}
		if (_timer < 0)
		{
			_state = MonsterState.Search;
			_startState = true;
			return;
		}
		if (_secondaryTimer < 0)
		{
			_secondaryTimer = 15;
			_agent.TargetPosition = GlobalPosition + new Vector3(GD.RandRange(-30,30), GD.RandRange(-10,10), GD.RandRange(-30,30));
		}
		foreach (var (player, data) in Players)
		{
			Player seen = CheckPlayerLineOfSight(player.Collider.GlobalPosition);
			if (seen != null)
			{
				_startState = true;
				_timer = 0.5;
				_state = MonsterState.Chase;
				_currentTarget = seen;
				return;
			}
		}
		
		_timer -= delta;
		_secondaryTimer -= delta;
		_hasDestination = true;
	}
	Vector3 _checkPos;
	public void Hunt(double delta)
	{
		if (_startState)
		{
			_timer = 18;
			_checkPos = _agent.TargetPosition;
			_agent.TargetPosition = _checkPos;
			_startState = false;
		}

		_agent.TargetPosition = _checkPos;

		float currentDistance;
		//check for all players
		foreach (var item in Players)
		{
			Player seen = CheckPlayerLineOfSight(item.Key.Collider.GlobalPosition);
			if (seen != null)
			{
				_startState = true;
				_state = MonsterState.Chase;
				_currentTarget = seen;
				_timer = 0f;
				return;
			}

			item.Value.Awareness += GetSoundLevel(item.Key) * delta;
			currentDistance = GlobalPosition.DistanceSquaredTo(item.Key.GlobalPosition);

			if (item.Value.Awareness > 200)
			{
				_timer = 20;
				_agent.TargetPosition = item.Key.Position;
				item.Value.Awareness = 0;
			}
		}
		if (_timer > 0)
		{
			_timer -= delta;
			return;
		}
		_startState = true;
		_state = MonsterState.Wander;
	}
	const double _forcedHintInterval = 180;
	public void Search(double delta)
	{
		if (_startState)
		{
			_timer = 180;
			SetSpeed(MonsterSpeed.Walk);
			_startState = false;
		}

		float currentDistance;
		//check for all players
		foreach (var item in Players)
		{
			Player seen = CheckPlayerLineOfSight(item.Key.Collider.GlobalPosition);
			if (seen != null)
			{
				_startState = true;
				_state = MonsterState.Chase;
				_currentTarget = seen;
				_agent.TargetDesiredDistance = 1.5f;
				_timer = 2.5f;
				return;
			}

			item.Value.Awareness += GetSoundLevel(item.Key) * delta;
			currentDistance = GlobalPosition.DistanceSquaredTo(item.Key.Collider.GlobalPosition);

			if (item.Value.Awareness > 100)
			{
				_agent.TargetPosition = item.Key.Position;
				_agent.TargetDesiredDistance = GlobalPosition.DistanceTo(item.Key.GlobalPosition) * 0.5f;
				item.Value.Awareness = 0;
			}
		}
		//force hint every 180 seconds
		if (_timer < 0)
		{
			Vector3 averagePos = new();
			_timer = _forcedHintInterval;
			foreach (var (player, data) in Players)
			{
				averagePos += player.GlobalPosition;
			}
			averagePos /= Players.Count();
			_agent.TargetPosition = GetHintPosition(averagePos, 0.5f);
			return;
		}
		_timer -= delta;
	}
	double _secondaryTimer;
	public void Chase(double delta)
	{
		if (_startState)
		{
			SetSpeed(MonsterSpeed.Run);
			_agent.TargetPosition = GlobalPosition;
			_startState = false;
			_secondaryTimer = 2.5;
			StartChaseAudio();
		}

		if (_timer > 0)
		{
			_timer -= delta;
			return;
		}
		if (CheckPlayerLineOfSight(_currentTarget.GlobalPosition) == _currentTarget)
		{
			_secondaryTimer = 2;
		}
		_agent.TargetPosition = _currentTarget.GlobalPosition;
		_secondaryTimer -= delta;
		if (_secondaryTimer < 0)
		{
			_startState = true;
			_state = MonsterState.Hunt;
			StopChaseAudio();
		}
	}
	[Export] AudioStreamPlayer3D _death;
	public void Kill(double delta)
	{
		if (_startState)
		{
			StopChaseAudio();
			_activeChasePlayer.Stop();
			_death.Play();
			_startState = false;
			_timer = 20;
		}
		_timer -= delta;
		if (_timer < 0)
		{
			_startState = true;
			_state = MonsterState.Search;
		}
		_agent.TargetPosition = GlobalPosition;
	}
	public void Taunt(double delta)
	{
		
	}
	private Player CheckPlayerLineOfSight(Vector3 targetGlobalPosition)
	{
		_checkForPlayer.TargetPosition = _checkForPlayer.ToLocal(targetGlobalPosition);
		_checkForPlayer.ForceRaycastUpdate();

		if (!_checkForPlayer.IsColliding())
			return null;

		var collider = _checkForPlayer.GetCollider();

		if (collider is Player player)
			return player;

		if (collider is VisibilityRadius visibilityRadius)
			return visibilityRadius.Player;

		return null;
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
		return player.SoundLevel / (1 + 0.4 * GlobalPosition.DistanceSquaredTo(player.GlobalPosition)) * 5000;
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

	// --- Chase audio helpers ---

	/// <summary>
	/// Starts the chase-sound loop by playing a random clip on player A.
	/// Safe to call even if no clips/players are assigned - it just does nothing.
	/// </summary>
	private void StartChaseAudio()
	{
		Rpc(nameof(RpcStartChaseAudio));
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, CallLocal = true)]
	private void RpcStartChaseAudio()
	{
		if (_chaseAudioA == null || _chaseAudioB == null || _chaseSounds == null || _chaseSounds.Count == 0)
			return;

		_chaseAudioActive = true;

		PlayRandomChaseSound(_activeChasePlayer);
	}

	/// <summary>
	/// Call every frame while in the Chase state. Watches the currently playing clip and,
	/// once it's _chaseOverlapFraction of the way through, starts a new random clip on the
	/// other player so the two overlap instead of leaving a gap.
	/// </summary>
	private void UpdateChaseAudio(double delta)
	{
		if (_activeChasePlayer == null || _inactiveChasePlayer == null)
			return;

		if (!_chaseAudioActive)
			return;

		if (_activeChasePlayer.Playing && _activeChasePlayer.Stream != null)
		{
			double length = _activeChasePlayer.Stream.GetLength();
			if (length > 0)
			{
				double progress = _activeChasePlayer.GetPlaybackPosition() / length;
				if (progress >= _chaseOverlapFraction && !_inactiveChasePlayer.Playing)
				{
					PlayRandomChaseSound(_inactiveChasePlayer);
					// the one we just started becomes the new "active" (i.e. the one we watch for the next crossfade point)
					(_activeChasePlayer, _inactiveChasePlayer) = (_inactiveChasePlayer, _activeChasePlayer);
				}
			}
		}
		else if (!_inactiveChasePlayer.Playing)
		{
			// fallback in case a stream's length was 0/unreadable and we never crossfaded in time -
			// avoids total silence if something slipped through
			PlayRandomChaseSound(_activeChasePlayer);
		}
	}

	private void StopChaseAudio()
	{
		Rpc(nameof(RpcStopChaseAudio));
	}
 
	[Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, CallLocal = true)]
	private void RpcStopChaseAudio()
	{
		_chaseAudioActive = false;
	}


	private void PlayRandomChaseSound(AudioStreamPlayer3D player)
	{
		int soundIndex = GD.RandRange(0, _chaseSounds.Count - 1);
		player.Stream = _chaseSounds[soundIndex];
		player.Play();
	}

}