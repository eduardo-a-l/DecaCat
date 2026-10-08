public class AchievementDefinition
{
    public AchievementDefinition(
        string id, string title, string description, StatType stat, int target)
    {
        Id = id;
        Title = title;
        Description = description;
        Stat = stat;
        Target = target;
    }

    public string Id { get; }
    public string Title { get; }
    public string Description { get; }
    public StatType Stat { get; }
    public int Target { get; }
}

public static class Achievements
{
    public static readonly AchievementDefinition[] All =
    {
        new AchievementDefinition(
            "first_blood", "First Blood",
            "Defeat your first enemy", StatType.EnemiesDefeated, 1),

        new AchievementDefinition(
            "exterminator", "Exterminator",
            "Defeat 100 enemies", StatType.EnemiesDefeated, 100),

        new AchievementDefinition(
            "slime_mixer", "Slime Mixer",
            "Merge slimes together 10 times", StatType.SlimeMerges, 10),

        new AchievementDefinition(
            "ghostbuster", "Ghostbuster",
            "Defeat 20 ghosts", StatType.GhostsDefeated, 20),

        new AchievementDefinition(
            "crate_smasher", "Crate Smasher",
            "Break 10 crates", StatType.CratesBroken, 10),

        new AchievementDefinition(
            "pack_rat", "Pack Rat",
            "Pick up 25 items", StatType.ItemsPickedUp, 25),

        new AchievementDefinition(
            "collector", "Collector",
            "Use 5 different weapons", StatType.DistinctWeapons, 5),

        new AchievementDefinition(
            "jack_of_all_trades", "Jack of All Trades",
            "Use all 9 weapons", StatType.DistinctWeapons, 9),

        new AchievementDefinition(
            "wear_and_tear", "Wear and Tear",
            "Break 5 weapons", StatType.WeaponsBroken, 5),

        new AchievementDefinition(
            "dethroned", "Dethroned",
            "Defeat the Slime King", StatType.BossesDefeated, 1),

        new AchievementDefinition(
            "going_down", "Going Down",
            "Reach floor 2", StatType.HighestFloor, 2),

        new AchievementDefinition(
            "deep_diver", "Deep Diver",
            "Reach floor 5", StatType.HighestFloor, 5),

        new AchievementDefinition(
            "untouchable", "Untouchable",
            "Clear a floor without taking damage", StatType.NoDamageFloors, 1),

        new AchievementDefinition(
            "try_again", "Try Again",
            "Die for the first time", StatType.Deaths, 1)
    };
}
