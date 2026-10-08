using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "DecaCat/Items/Item Database")]
public class ItemDatabase : ScriptableObject
{
    private static ItemDatabase instance;

    [SerializeField] private ItemData[] items;

    private static ItemDatabase Instance
    {
        get
        {
            if (instance == null)
                instance = Resources.Load<ItemDatabase>("ItemDatabase");

            return instance;
        }
    }

    public static ItemData FindItem(string itemName)
    {
        if (string.IsNullOrEmpty(itemName) || Instance == null || Instance.items == null)
            return null;

        foreach (ItemData item in Instance.items)
        {
            if (item != null && item.ItemName == itemName)
                return item;
        }

        return null;
    }
}
