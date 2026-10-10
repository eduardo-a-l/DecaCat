using UnityEngine;

public static class FloorProgression
{
    public const int FloorsPerGroup = 5;
    public const int GroupsPerCycle = 6;

    private const int ThemeSalt = 100;
    private const int BossSalt = 5000;

    public static int GetGroup(int floor)
    {
        return (Mathf.Max(1, floor) - 1) / FloorsPerGroup;
    }

    public static int GetFloorInGroup(int floor)
    {
        return (Mathf.Max(1, floor) - 1) % FloorsPerGroup;
    }

    public static int GetCycle(int floor)
    {
        return GetGroup(floor) / GroupsPerCycle;
    }

    public static int GetCyclePosition(int floor)
    {
        return GetGroup(floor) % GroupsPerCycle;
    }

    public static int[] GetThemeOrder(int cycle, int runSeed)
    {
        int[] order = { 0, 1, 2, 3, 4, 5 };

        for (int current = 1; current <= cycle; current++)
            order = ShuffleThemes(current, runSeed, order[GroupsPerCycle - 1]);

        return order;
    }

    public static int GetBossSlot(int floor, int runSeed)
    {
        int group = GetGroup(floor);

        StableRandom random = new StableRandom(runSeed, BossSalt + group);

        int[] slots = { 0, 1, 2, random.Next(3), random.Next(3) };

        for (int i = slots.Length - 1; i > 0; i--)
            Swap(slots, i, random.Next(i + 1));

        if (group == 0)
            Swap(slots, 0, System.Array.IndexOf(slots, 0));

        return slots[GetFloorInGroup(floor)];
    }

    public static EnemyScaling GetScaling(int floor, ProgressionTuning tuning)
    {
        int floorsPassed = Mathf.Max(1, floor) - 1;
        int group = GetGroup(floor);

        float health =
            1f + tuning.HealthPerFloor * floorsPassed +
            tuning.HealthPerGroup * group;

        float speed = Mathf.Min(
            tuning.MaxSpeedMultiplier,
            1f + tuning.SpeedPerFloor * floorsPassed +
            tuning.SpeedPerGroup * group
        );

        int damageBonus = group / tuning.GroupsPerDamageBonus;

        return new EnemyScaling(health, speed, damageBonus);
    }

    private static int[] ShuffleThemes(int cycle, int runSeed, int previousLast)
    {
        int[] order = { 0, 1, 2, 3, 4, 5 };

        StableRandom random = new StableRandom(runSeed, ThemeSalt + cycle);

        for (int i = order.Length - 1; i > 0; i--)
            Swap(order, i, random.Next(i + 1));

        if (order[0] == previousLast)
            Swap(order, 0, 1 + random.Next(order.Length - 1));

        return order;
    }

    private static void Swap(int[] array, int a, int b)
    {
        int temp = array[a];
        array[a] = array[b];
        array[b] = temp;
    }
}
