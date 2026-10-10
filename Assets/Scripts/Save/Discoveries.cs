using System.Collections.Generic;

public static class Discoveries
{
    public const string ItemKind = "item";
    public const string EnemyKind = "enemy";
    public const string BossKind = "boss";

    public static bool Add(string kind, string id, string displayName = null)
    {
        if (!SaveSession.IsActive || string.IsNullOrEmpty(id))
            return false;

        SaveData data = SaveSession.Data;

        if (data.discoveries == null)
            data.discoveries = new List<string>();

        string key = Key(kind, id);

        if (data.discoveries.Contains(key))
            return false;

        data.discoveries.Add(key);

        AchievementToast.Show(
            "NEW INDEX ENTRY",
            string.IsNullOrEmpty(displayName) ? id : displayName,
            GetKindLabel(kind)
        );

        SaveSession.Save();

        return true;
    }

    public static void AddEnemy(Enemy enemy)
    {
        if (enemy != null)
            Add(enemy.IsBoss ? BossKind : EnemyKind, enemy.IndexId, enemy.DisplayName);
    }

    public static bool Has(SaveData data, string kind, string id)
    {
        return data != null &&
               data.discoveries != null &&
               data.discoveries.Contains(Key(kind, id));
    }

    private static string GetKindLabel(string kind)
    {
        if (kind == BossKind)
            return "Boss";

        return kind == EnemyKind ? "Enemy" : "Item";
    }

    private static string Key(string kind, string id)
    {
        return kind + ":" + id;
    }
}
