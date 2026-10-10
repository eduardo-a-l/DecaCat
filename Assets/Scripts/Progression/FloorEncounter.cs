using System.Collections.Generic;
using UnityEngine;

public class FloorEncounter
{
    private readonly List<Enemy> pool = new List<Enemy>();
    private readonly List<float> weights = new List<float>();
    private float totalWeight;

    public int Floor { get; private set; }
    public int ThemeIndex { get; private set; }
    public string ThemeName { get; private set; }
    public Color FloorTint { get; private set; }
    public Color WallTint { get; private set; }
    public EnemyScaling Scaling { get; private set; }
    public Enemy BossPrefab { get; private set; }
    public int PoolSize => pool.Count;

    public static FloorEncounter Create(int floor, int runSeed)
    {
        FloorThemeCatalog catalog = FloorThemeCatalog.Instance;

        ProgressionTuning tuning =
            catalog != null ? catalog.Tuning : new ProgressionTuning();

        int[] order = FloorProgression.GetThemeOrder(
            FloorProgression.GetCycle(floor), runSeed
        );

        int position = FloorProgression.GetCyclePosition(floor);
        int themeIndex = order[position];

        FloorTheme theme = catalog != null ? catalog.GetTheme(themeIndex) : null;

        FloorEncounter encounter = new FloorEncounter();

        encounter.Floor = floor;
        encounter.ThemeIndex = themeIndex;
        encounter.Scaling = FloorProgression.GetScaling(floor, tuning);

        encounter.ThemeName =
            theme != null && !string.IsNullOrEmpty(theme.DisplayName)
                ? theme.DisplayName
                : FloorTheme.DefaultNames[themeIndex];

        encounter.FloorTint =
            theme != null ? theme.FloorTint : FloorTheme.DefaultTints[themeIndex];

        encounter.WallTint =
            theme != null ? theme.WallTint : FloorTheme.DefaultTints[themeIndex];

        if (catalog != null)
        {
            encounter.BuildPool(catalog, tuning, order, position, floor);

            if (theme != null)
            {
                encounter.BossPrefab = theme.GetBoss(
                    FloorProgression.GetBossSlot(floor, runSeed)
                );
            }
        }

        return encounter;
    }

    public Enemy PickEnemy()
    {
        if (pool.Count == 0)
            return null;

        float roll = Random.value * totalWeight;

        for (int i = 0; i < pool.Count; i++)
        {
            roll -= weights[i];

            if (roll <= 0f)
                return pool[i];
        }

        return pool[pool.Count - 1];
    }

    private void BuildPool(
        FloorThemeCatalog catalog, ProgressionTuning tuning,
        int[] order, int position, int floor)
    {
        int floorInGroup = FloorProgression.GetFloorInGroup(floor) + 1;

        for (int index = 0; index <= position; index++)
        {
            FloorTheme theme = catalog.GetTheme(order[index]);

            if (theme == null || theme.Enemies == null)
                continue;

            int age = position - index;
            float weight = Mathf.Pow(tuning.OlderEnemyWeight, age);

            foreach (ThemeEnemy entry in theme.Enemies)
            {
                if (entry == null || entry.Prefab == null)
                    continue;

                if (age == 0 && entry.UnlockFloor > floorInGroup)
                    continue;

                pool.Add(entry.Prefab);
                weights.Add(weight);
                totalWeight += weight;
            }
        }
    }
}
