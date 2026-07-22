using Godot;

public partial class BoneLookAtVectorModifier : SkeletonModifier3D
{
    public Node3D Target;
    private Skeleton3D _skeleton;
    private int _boneIndex = -1;
    public BoneLookAtVectorModifier(Skeleton3D skeleton, Node3D target, string boneName)
    {
        if (target == null) GD.PrintErr("target pos is null");
        Target = target;
        
        _skeleton = skeleton;
        if (_skeleton != null)
        {
            _boneIndex = _skeleton.FindBone(boneName);
        }
    }
    private Quaternion _currentRot = Quaternion.Identity;

    public override void _ProcessModificationWithDelta(double delta)
    {
        if (!Active || _skeleton == null || Target == null || _boneIndex == -1)
            return;

        Vector3 targetLocal = _skeleton.GlobalTransform.AffineInverse() * Target.GlobalPosition;
        Transform3D currentGlobalPose = _skeleton.GetBoneGlobalPose(_boneIndex);
        Vector3 currentPos = currentGlobalPose.Origin;

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
