using System.Collections.Generic;
using Godot;


public partial class GetRoomSet : MeshInstance3D
{
	private string _propSet;
	public string GetSet()
	{
		return _propSet;
	}
	public override void _EnterTree()
	{
		switch (GD.RandRange(0,2))
		{
			case 0:
			_propSet = "server";
				break;
			case 1:
			_propSet = "office";
				break;
			case 2:
			_propSet = "lab";
				break;
		}
	}

}
