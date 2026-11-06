using Godot;

namespace OrbRunner;

/// <summary>Stores the best score in the user folder (user://), which survives game updates.</summary>
public static class SaveData
{
    public static string FilePath { get; set; } = "user://save.cfg";

    public static int LoadBest()
    {
        var config = new ConfigFile();
        return config.Load(FilePath) == Error.Ok ? (int)config.GetValue("score", "best", 0) : 0;
    }

    public static void SaveBest(int best)
    {
        var config = new ConfigFile();
        config.SetValue("score", "best", best);
        config.Save(FilePath);
    }
}
