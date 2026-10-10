using System.Collections.Generic;

public static class Discoveries
{
    public const string ItemKind = "item";
    public const string EnemyKind = "enemy";
    public const string BossKind = "boss";

    public static bool Add(string kind, string id)
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

        return true;
    }

    public static void AddEnemy(Enemy enemy)
    {
        if (enemy != null)
            Add(enemy.IsBoss ? BossKind : EnemyKind, enemy.IndexId);
    }

    public static bool Has(SaveData data, string kind, string id)
    {
        return data != null &&
               data.discoveries != null &&
               data.discoveries.Contains(Key(kind, id));
    }

    private static string Key(string kind, string id)
    {
        return kind + ":" + id;
    }
}
