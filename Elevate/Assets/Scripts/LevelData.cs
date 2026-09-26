using System.Collections.Generic;
using UnityEngine;

public class LevelData
{
    public string Title;
    public string Hint;
    public int Par;
    public List<string> Map = new List<string>();

    public static List<LevelData> LoadAll()
    {
        var asset = Resources.Load<TextAsset>("levels");
        var levels = new List<LevelData>();
        LevelData current = null;
        bool inMap = false;
        foreach (var raw in asset.text.Replace("\r", "").Split('\n'))
        {
            if (inMap)
            {
                if (raw.Trim() == "end")
                {
                    inMap = false;
                    levels.Add(current);
                }
                else current.Map.Add(raw);
                continue;
            }
            if (raw.StartsWith("level:")) current = new LevelData { Title = raw.Substring(6).Trim() };
            else if (raw.StartsWith("hint:")) current.Hint = raw.Substring(5).Trim();
            else if (raw.StartsWith("par:")) current.Par = int.Parse(raw.Substring(4).Trim());
            else if (raw.Trim() == "map:") inMap = true;
        }
        return levels;
    }
}
