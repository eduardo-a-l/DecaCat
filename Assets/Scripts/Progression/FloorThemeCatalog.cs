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

    public FloorTheme GetTheme(int index)
    {
        if (themes == null || index < 0 || index >= themes.Length)
            return null;

        return themes[index];
    }
}
