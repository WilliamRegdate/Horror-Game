
using System.ComponentModel;
using Godot;

public class Leg
{
    public Node3D IKTarget;
    public Node3D IKPole;
    public Node3D Foot;
    public Vector3 RestPosition;
    public int[] AdjacentIndices;

    public Vector3 TargetPosition = new();
    public Vector3 StepStartPosition;
    public float StepProgress;
    public Vector3 LastStepTarget;
    public bool IsMoving;
    public float StepHeight;

    public Leg(Node3D ikTarget, Node3D ikPole, Node3D foot, Vector3 restPosition, int[] adjacentIndices)
    {
        IKTarget = ikTarget;
        IKPole = ikPole;
        Foot = foot;
        RestPosition = restPosition;
        AdjacentIndices = adjacentIndices;
    }
}

public partial class Monster : Node3D
{
    public Node3D Player;
    private BoneLookAtVectorModifier _lookAtPlayerModifier;

    [Export] public Node3D[] _ikTargets;
    [Export] private Node3D[] _ikPoles;
    [Export] private Node3D[] _feet;
    public float StepDuration = 0.3f;
    public float StepHeight = 0.5f;

    public Leg[] _legs = new Leg[4];
    private float _moveFootThreshold = 6;
    private Node3D _parent;
    [Export] private Node3D _spineTarget;

    public override void _Ready()
    {
        _parent = GetParent() as Node3D;
        Node grandParent = _parent.GetParent();
        _moveFootThreshold *= _moveFootThreshold;

        _legs[0] = new Leg(_ikTargets[0], _ikPoles[0], _feet[0], new Vector3(-1f, 0, -2), new[] { 1, 2 }); // front-left  → front-right, back-left
        _legs[1] = new Leg(_ikTargets[1], _ikPoles[1], _feet[1], new Vector3( 1f, 0, -2), new[] { 0, 3 }); // front-right → front-left, back-right
        _legs[2] = new Leg(_ikTargets[2], _ikPoles[2], _feet[2], new Vector3(-1f, 0,  2), new[] { 0, 3 }); // back-left   → front-left, back-right
        _legs[3] = new Leg(_ikTargets[3], _ikPoles[3], _feet[3], new Vector3( 1f, 0,  2), new[] { 1, 2 }); // back-right  → front-right, back-left

        var ik = GetChild(0).GetChild(0).GetChild(2) as TwoBoneIK3D;
        for (int i = 0; i < 4; i++)
        {
            _legs[i].Foot.RotationDegrees += Vector3.Left * 90;
            _legs[i].IKTarget.CallDeferred(Node.MethodName.Reparent, grandParent);
            CallDeferred(MethodName.RefreshIKTarget, ik, i);
        }
        //Position = new Vector3(0, -3f, -1);
        Position = new Vector3(0, -4.5f, 0);
    }
    //calls once world is ready and set up
    public void OnStartGame()
	{
        Skeleton3D skeleton = GetChild(0).GetChild(0) as Skeleton3D;
        if (skeleton == null)
        {
            GD.PrintErr("_Skeleton is null");
            return;
        }
		Player = GetTree().GetFirstNodeInGroup("Player") as Node3D;
        if (Player == null)
        {
            GD.PrintErr("Player not found in group");
            return;
        }
        _lookAtPlayerModifier = new(skeleton, Player, "Head");
        skeleton.AddChild(_lookAtPlayerModifier);
	}

    public override void _Process(double delta)
    {
        float frontTotal = 0;
        float backTotal = 0;
        for (int i = 0; i < 4; i++)
        {
            Leg leg = _legs[i];
            MoveLeg(leg, delta);

            var spaceState = GetWorld3D().DirectSpaceState;
            var query = PhysicsRayQueryParameters3D.Create(
                leg.IKTarget.GlobalPosition + Vector3.Up * 3,
                leg.IKTarget.GlobalPosition + Vector3.Down * 5
            );

            var result = spaceState.IntersectRay(query);

        

            Vector3 direction = Quaternion * leg.RestPosition;
            int[] adjacent = leg.AdjacentIndices;

            //is leg far enough away to move
            if ((direction + GlobalPosition - leg.IKTarget.Position - Position).LengthSquared() > _moveFootThreshold
                && !_legs[adjacent[0]].IsMoving
                && !_legs[adjacent[1]].IsMoving
                && !_legs[i].IsMoving)
            {
                Vector3 newTargetWorld = direction + GlobalPosition - Position;
                var stepResult = spaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(
                newTargetWorld + Vector3.Up * 2,
                newTargetWorld + Vector3.Down * 5));

                leg.TargetPosition = direction + GlobalPosition - Position;
                if (stepResult.Count > 0)
                    leg.TargetPosition.Y = stepResult["position"].AsVector3().Y + 0.3f;
            }

            // align feet with the floor
            leg.Foot.GlobalPosition = leg.IKTarget.GlobalPosition;

            if (result.Count > 0)
            {
                Vector3 normal = ((Vector3)result["normal"]).Normalized();
                //rotate normal according to monster rotation
                normal = Quaternion.Inverse() * normal;
                Basis basis;
                if (Mathf.Abs(normal.Dot(Vector3.Forward)) > 0.999f)
                    basis = Basis.LookingAt(-normal - Vector3.Up * 0.1f, Vector3.Right);
                else
                    basis = Basis.LookingAt(-normal - Vector3.Up * 0.1f, Vector3.Forward);

                leg.Foot.Quaternion = basis.GetRotationQuaternion();
            }
            if (i > 1)
                backTotal += _ikTargets[i].GlobalPosition.Y;
            else
                frontTotal += _ikTargets[i].GlobalPosition.Y;
        }
        frontTotal *= 0.5f;
        backTotal *= 0.5f;
        GlobalPosition = new Vector3(GlobalPosition.X,frontTotal*0.5f + backTotal*0.5f -6.5f, GlobalPosition.Z) ;
        _spineTarget.GlobalPosition = new Vector3(_spineTarget.GlobalPosition.X, frontTotal -5 - Position.Y , _spineTarget.GlobalPosition.Z) ;
        
    }

    private void MoveLeg(Leg leg, double delta)
    {

        if (!leg.LastStepTarget.IsEqualApprox(leg.TargetPosition))
        {
            leg.IsMoving = true;
            leg.StepStartPosition = leg.IKTarget.Position;
            leg.LastStepTarget = leg.TargetPosition;
            leg.StepProgress = 0f;   
            Vector3 dir = leg.TargetPosition - leg.StepStartPosition;
            dir = dir.Normalized();
            dir = dir.Normalized();
            leg.StepHeight = StepHeight + StepHeight * dir.Dot(Vector3.Up);
        }
        if (leg.StepProgress >= 1.0f)
        {
            leg.IsMoving = false;
            return;
        }

        leg.StepProgress = Mathf.Min(leg.StepProgress + (float)delta / StepDuration, 1.0f);

        Vector3 flatPos = leg.StepStartPosition.Lerp(leg.TargetPosition, leg.StepProgress);
        float lift = Mathf.Sin(leg.StepProgress * Mathf.Pi) * leg.StepHeight;

        leg.IKTarget.Position = flatPos + new Vector3(0, lift, 0);
    }

    private void RefreshIKTarget(TwoBoneIK3D ik, int i)
    {
        ik.SetTargetNode(i, _legs[i].IKTarget.GetPath());
    }
}