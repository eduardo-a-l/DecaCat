using System;
using UnityEngine;

[Serializable]
public class ThemeEnemy
{
    [SerializeField] private Enemy prefab;
    [SerializeField, Range(1, 5)] private int unlockFloor = 1;

    public Enemy Prefab => prefab;

    public int UnlockFloor =>
        Mathf.Clamp(unlockFloor, 1, FloorProgression.FloorsPerGroup);
}

[Serializable]
public class FloorTheme
{
    public const int EnemySlots = 5;
    public const int BossSlots = 3;

    public static readonly string[] DefaultNames =
        { "Blue", "Yellow", "Orange", "Red", "Green", "Purple" };

    public static readonly Color[] DefaultTints =
    {
        new Color(1f, 1f, 1f, 1f),
        new Color(1f, 0.92f, 0.55f, 1f),
        new Color(1f, 0.7f, 0.4f, 1f),
        new Color(1f, 0.5f, 0.5f, 1f),
        new Color(0.6f, 1f, 0.65f, 1f),
        new Color(0.8f, 0.6f, 1f, 1f)
    };

    [SerializeField] private string displayName = "Theme";
    [SerializeField] private Color floorTint = Color.white;
    [SerializeField] private Color wallTint = Color.white;

    [Header("5 new enemies. Unlock floor = first floor (1-5) of the group they can appear on")]
    [SerializeField] private ThemeEnemy[] enemies = new ThemeEnemy[EnemySlots];

    [Header("3 bosses. Each one appears at least once in the group of 5 floors")]
    [SerializeField] private Enemy[] bosses = new Enemy[BossSlots];

    public string DisplayName => displayName;
    public Color FloorTint => floorTint;
    public Color WallTint => wallTint;
    public ThemeEnemy[] Enemies => enemies;

    public Enemy GetBoss(int slot)
    {
        if (bosses == null || bosses.Length == 0)
            return null;

        if (slot >= 0 && slot < bosses.Length && bosses[slot] != null)
            return bosses[slot];

        foreach (Enemy boss in bosses)
        {
            if (boss != null)
                return boss;
        }

        return null;
    }
}
