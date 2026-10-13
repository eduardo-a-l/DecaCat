using UnityEngine;

[CreateAssetMenu(fileName = "PermanentUpgrades", menuName = "DecaCat/Permanent Upgrade Catalog")]
public class PermanentUpgradeCatalog : ScriptableObject
{
    private const string ResourceName = "PermanentUpgrades";

    private static PermanentUpgradeCatalog instance;

    [SerializeField] private Sprite maxHeartIcon;
    [SerializeField] private Sprite damageIcon;

    public static Sprite GetIcon(PermanentUpgradeType type)
    {
        if (instance == null)
            instance = Resources.Load<PermanentUpgradeCatalog>(ResourceName);

        if (instance == null)
            return null;

        return type == PermanentUpgradeType.MaxHeart
            ? instance.maxHeartIcon
            : instance.damageIcon;
    }
}
