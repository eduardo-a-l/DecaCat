using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FloorThemes", menuName = "DecaCat/Floors/Floor Theme Catalog")]
public class FloorThemeCatalog : ScriptableObject
{
    private const string ResourceName = "FloorThemes";

    private static FloorThemeCatalog instance;

    [Header("Order of the first 6 groups: Blue, Yellow, Orange, Red, Green, Purple")]
    [SerializeField] private FloorTheme[] themes;

    [SerializeField] private ProgressionTuning tuning = new ProgressionTuning();

    public static FloorThemeCatalog Instance
    {
        get
        {
            if (instance == null)
                instance = Resources.Load<FloorThemeCatalog>(ResourceName);

            return instance;
        }
    }

    public ProgressionTuning Tuning => tuning ?? new ProgressionTuning();

    public List<Enemy> GetAllEnemies()
    {
        List<Enemy> result = new List<Enemy>();
        HashSet<string> seen = new HashSet<string>();

        if (themes == null)
            return result;

        foreach (FloorTheme theme in themes)
        {
            if (theme == null)
                continue;

            if (theme.Enemies != null)
            {
                foreach (ThemeEnemy entry in theme.Enemies)
                {
                    if (entry != null)
                        AddUnique(result, seen, entry.Prefab);
                }
            }

            foreach (Enemy boss in theme.Bosses)
                AddUnique(result, seen, boss);
        }

        return result;
    }

    private static void AddUnique(List<Enemy> list, HashSet<string> seen, Enemy enemy)
    {
        if (enemy != null && seen.Add(enemy.IndexId))
            list.Add(enemy);
    }

    public FloorTheme GetTheme(int index)
    {
        if (themes == null || index < 0 || index >= themes.Length)
            return null;

        return themes[index];
    }
}
