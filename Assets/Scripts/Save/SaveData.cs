using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StatEntry
{
    public string key;
    public int value;
}

[Serializable]
public class SaveData
{
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;
    public int floor = 1;
    public int floorSeed;
    public string loadoutItem = string.Empty;
    public int loadoutDurability;
    public float playTimeSeconds;
    public string lastSaved = string.Empty;
    public List<StatEntry> stats = new List<StatEntry>();
    public List<string> usedWeapons = new List<string>();
    public List<string> unlockedAchievements = new List<string>();

    public int GetStat(StatType type)
    {
        string key = type.ToString();

        foreach (StatEntry entry in stats)
        {
            if (entry.key == key)
                return entry.value;
        }

        return 0;
    }

    public void SetStat(StatType type, int value)
    {
        string key = type.ToString();

        foreach (StatEntry entry in stats)
        {
            if (entry.key == key)
            {
                entry.value = value;
                return;
            }
        }

        StatEntry created = new StatEntry();
        created.key = key;
        created.value = value;

        stats.Add(created);
    }

    public bool IsUnlocked(string achievementId)
    {
        return unlockedAchievements.Contains(achievementId);
    }

    public string FormatPlayTime()
    {
        int total = Mathf.FloorToInt(playTimeSeconds);
        int hours = total / 3600;
        int minutes = (total % 3600) / 60;

        if (hours > 0)
            return hours + "h " + minutes.ToString("00") + "m";

        return minutes + "m";
    }
}
