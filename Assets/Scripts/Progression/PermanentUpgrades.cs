using UnityEngine;

public enum PermanentUpgradeType
{
    MaxHeart,
    Damage
}

public static class PermanentUpgrades
{
    public const int FloorInterval = 10;
    public const float DamagePerStack = 1.5f;

    public static int BonusHearts =>
        SaveSession.IsActive ? SaveSession.Data.bonusHearts : 0;

    public static int DamageStacks =>
        SaveSession.IsActive ? SaveSession.Data.damageUpgrades : 0;

    public static float DamageMultiplier =>
        Mathf.Pow(DamagePerStack, DamageStacks);

    public static bool IsUpgradeFloor(int floor)
    {
        return floor > 1 && (floor - 1) % FloorInterval == 0;
    }

    public static bool IsChosenFor(int floor)
    {
        return SaveSession.IsActive &&
               SaveSession.Data.upgradeFloorsChosen.Contains(floor);
    }

    public static void Apply(PermanentUpgradeType type)
    {
        if (!SaveSession.IsActive)
            return;

        SaveData data = SaveSession.Data;

        if (type == PermanentUpgradeType.MaxHeart)
        {
            data.bonusHearts++;

            PlayerHealth health = Object.FindFirstObjectByType<PlayerHealth>();

            if (health != null)
                health.SetBonusHealth(data.bonusHearts);
        }
        else
        {
            data.damageUpgrades++;
        }

        SaveSession.Save();
    }

    public static void ResetAll()
    {
        if (!SaveSession.IsActive)
            return;

        SaveData data = SaveSession.Data;

        data.bonusHearts = 0;
        data.damageUpgrades = 0;
        data.upgradeFloorsChosen.Clear();

        PlayerHealth health = Object.FindFirstObjectByType<PlayerHealth>();

        if (health != null)
            health.SetBonusHealth(0);

        SaveSession.Save();
    }

    public static string GetName(PermanentUpgradeType type)
    {
        return type == PermanentUpgradeType.MaxHeart ? "+1 Max Heart" : "1.5x Damage";
    }

    public static Color GetAccent(PermanentUpgradeType type)
    {
        return type == PermanentUpgradeType.MaxHeart
            ? new Color(1f, 0.35f, 0.4f, 1f)
            : new Color(1f, 0.75f, 0.25f, 1f);
    }
}
