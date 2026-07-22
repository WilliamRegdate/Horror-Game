using Godot;
using System;

public partial class HeadController : MeshInstance3D
{
	[Export] private MeshInstance3D _head;
	[Export] private Skeleton3D _skeleton;
	private int boneIndex;
	private Transform3D _neckTransform;
	private Vector3 _neckPos;

	
	public override void _Ready()
	{	AddChild(_head);
		boneIndex = _skeleton.FindBone("Neck");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		//parent head to neck bone
		_neckTransform = _skeleton.GetBoneGlobalPose(boneIndex);
		Position = _neckTransform.Origin;


	}
}
