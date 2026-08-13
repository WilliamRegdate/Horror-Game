using System.Collections.Generic;
using Godot;

public partial class BoneLookAtVectorModifier : SkeletonModifier3D
{
    private Skeleton3D _skeleton;
    private List<Node3D> targets = new();

    private Node3D _target;
    private int _boneIndex = -1;
    public BoneLookAtVectorModifier(Skeleton3D skeleton, string boneName)
    {
        _skeleton = skeleton;
        if (_skeleton != null)
        {
            _boneIndex = _skeleton.FindBone(boneName);
        }
    }
    public void AddTarget(Node3D node)
    {
        targets.Add(node);
    }
    private Quaternion _currentRot = Quaternion.Identity;

    public override void _ProcessModificationWithDelta(double delta)
    {
        if (!Active || _skeleton == null || targets.Count == 0 || _boneIndex == -1)
            return;

        Transform3D currentGlobalPose = _skeleton.GetBoneGlobalPose(_boneIndex);
        Vector3 currentPos = currentGlobalPose.Origin;

        //get bone global position relative to root node
        Vector3 globalBonePos = currentPos + GlobalPosition;

        //look at closest target
        float closest = float.PositiveInfinity;
        foreach (var target in targets)
        {
            if (closest > (target.Position - globalBonePos).LengthSquared())
            {
                closest = (target.Position - globalBonePos).LengthSquared();
                _target = target;
            }
        }

        Vector3 targetLocal = _skeleton.GlobalTransform.AffineInverse() * _target.GlobalPosition;

        Transform3D rotationToTarget = Transform3D.Identity;
        rotationToTarget = rotationToTarget.LookingAt(targetLocal - currentPos, Vector3.Up);

        Quaternion targetRot = rotationToTarget.Basis.GetRotationQuaternion();
        float weight = 1f - Mathf.Exp(-(float)delta * 10);
        Quaternion flip = new Quaternion(Vector3.Up, Mathf.Pi);
        targetRot *= flip;
        _currentRot = _currentRot.Slerp(targetRot, weight);
        currentGlobalPose.Basis = new Basis(_currentRot);
        _skeleton.SetBoneGlobalPose(_boneIndex, currentGlobalPose);
    }
}
