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
		switch (GD.RandRange(0,1))
		{
			case 0:
			_propSet = "server";
				break;
			case 1:
		_propSet = "office";
			break;
		}
	}

}
