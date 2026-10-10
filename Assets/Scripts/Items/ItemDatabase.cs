using System.Collections.Generic;
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

    public static List<ItemData> All
    {
        get
        {
            List<ItemData> result = new List<ItemData>();

            if (Instance == null || Instance.items == null)
                return result;

            foreach (ItemData item in Instance.items)
            {
                if (item != null)
                    result.Add(item);
            }

            return result;
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
