using System.Collections.Generic;
using Godot;

public class PropData
{
    public string Set;
    public string Shape;
    public PackedScene Scene;

    public PropData(string set, string shape, PackedScene scene)
    {
        Set = set;
        Shape = shape;
        Scene = scene;
    }
    

}

public class PropLoader
{
    public IReadOnlyList<PropData> Props => _props;

    private readonly List<PropData> _props = new();
    private string _jsonFile;

    public PropLoader()
    {
        _props = new List<PropData>();

        string jsonPath = "res://Json/Furniture.json";
        FileAccess file = FileAccess.Open(jsonPath, FileAccess.ModeFlags.Read);
        _jsonFile = file.GetAsText();
        file.Close();

        // Parse JSON string
        Variant parsed = Json.ParseString(_jsonFile);
        if (parsed.VariantType != Variant.Type.Dictionary)
        {
            GD.PrintErr("rooms.json root is not a dictionary.");
            return;
        }

        var dict = parsed.AsGodotDictionary();

        foreach (var entry in dict)
        {
            string name = (string)entry.Key;
            var roomData = entry.Value.AsGodotDictionary();

            if (!roomData.ContainsKey("scene") ||
                !roomData.ContainsKey("shape") ||
                !roomData.ContainsKey("set"))
            {
                GD.PrintErr($"Invalid entry: {name}");
                continue;
            }

            string scenePath = roomData["scene"].AsString();
            string shape = roomData["shape"].AsString();
            string set = roomData["set"].AsString();

            var scene = ResourceLoader.Load<PackedScene>(scenePath);

            if (scene == null)
            {
                GD.PrintErr($"Failed to load scene: {scenePath}");
                continue;
            }

            _props.Add(new PropData(set, shape, scene));
        }
    }
}

