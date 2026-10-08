public static class GameStats
{
    public static void Add(StatType type, int amount = 1)
    {
        if (!SaveSession.IsActive || amount <= 0)
            return;

        SaveData data = SaveSession.Data;

        data.SetStat(type, data.GetStat(type) + amount);

        Evaluate(data);
    }

    public static void SetMax(StatType type, int value)
    {
        if (!SaveSession.IsActive)
            return;

        SaveData data = SaveSession.Data;

        if (value <= data.GetStat(type))
            return;

        data.SetStat(type, value);

        Evaluate(data);
    }

    public static void RecordWeaponUsed(string itemName)
    {
        if (!SaveSession.IsActive || string.IsNullOrEmpty(itemName))
            return;

        SaveData data = SaveSession.Data;

        if (data.usedWeapons.Contains(itemName))
            return;

        data.usedWeapons.Add(itemName);
        data.SetStat(StatType.DistinctWeapons, data.usedWeapons.Count);

        Evaluate(data);
    }

    private static void Evaluate(SaveData data)
    {
        bool unlockedAny = false;

        foreach (AchievementDefinition achievement in Achievements.All)
        {
            if (data.IsUnlocked(achievement.Id))
                continue;

            if (data.GetStat(achievement.Stat) < achievement.Target)
                continue;

            data.unlockedAchievements.Add(achievement.Id);
            unlockedAny = true;

            AchievementToast.Show(achievement);
        }

        if (unlockedAny)
            SaveSession.Save();
    }
}
