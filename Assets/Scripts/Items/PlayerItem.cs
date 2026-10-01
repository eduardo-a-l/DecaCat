using UnityEngine;

public class PlayerItem : MonoBehaviour
{
    [SerializeField] private ItemData startingItem;

    private ItemData currentItem;
    private int currentDurability;

    public ItemData CurrentItem => currentItem;
    public int CurrentDurability => currentDurability;

    private void Start()
    {
        if (startingItem != null)
        {
            EquipItem(startingItem);
        }
    }

    public void EquipItem(ItemData item)
    {
        if (item == null)
            return;

        currentItem = item;
        currentDurability = item.MaxDurability;

        Debug.Log("Equipped: " + item.ItemName);
    }

    public bool UseItem()
    {
        if (currentItem == null)
            return false;

        currentDurability--;

        Debug.Log(currentItem.ItemName + ": " + currentDurability
            + "/" + currentItem.MaxDurability);

        if (currentDurability <= 0)
        {
            Debug.Log(currentItem.ItemName + " broke");
            currentItem = null;
            currentDurability = 0;
        }

        return true;
    }

    public ItemData DropItem()
    {
        if (currentItem == null)
            return null;

        ItemData droppedItem = currentItem;

        Debug.Log("Dropped: " + currentItem.ItemName);

        currentItem = null;
        currentDurability = 0;

        return droppedItem;
    }
}