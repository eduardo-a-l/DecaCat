using System.Collections.Generic;
using UnityEngine;

public class PlayerItem : MonoBehaviour
{
    [SerializeField] private ItemData startingItem;
    [SerializeField] private ItemPickup droppedItemPrefab;

    private PlayerInputReader inputReader;
    private ItemData currentItem;
    private int currentDurability;

    private readonly HashSet<ItemPickup> nearbyPickups =
        new HashSet<ItemPickup>();

    public ItemData CurrentItem => currentItem;
    public int CurrentDurability => currentDurability;

    private void Awake()
    {
        inputReader = GetComponent<PlayerInputReader>();
    }

    private void Start()
    {
        if (startingItem != null)
            EquipItem(startingItem);
    }

    private void Update()
    {
        if (inputReader == null)
            return;

        if (inputReader.InteractPressed)
            Interact();

        if (inputReader.DropPressed)
            DropCurrentItem();
    }

    public void EnterPickupRange(ItemPickup pickup)
    {
        if (pickup != null)
            nearbyPickups.Add(pickup);
    }

    public void ExitPickupRange(ItemPickup pickup)
    {
        nearbyPickups.Remove(pickup);
    }

    private void Interact()
    {
        ItemPickup pickup = GetClosestPickup();

        if (pickup == null)
            return;

        if (currentItem != null && !DropCurrentItem())
            return;

        ItemData newItem = pickup.ItemData;
        int durability = pickup.Durability;

        nearbyPickups.Remove(pickup);
        Destroy(pickup.gameObject);

        EquipItem(newItem, durability);
    }

    private ItemPickup GetClosestPickup()
    {
        ItemPickup closest = null;
        float closestDistance = float.MaxValue;

        foreach (ItemPickup pickup in nearbyPickups)
        {
            if (pickup == null || pickup.ItemData == null ||
                !pickup.isActiveAndEnabled)
                continue;

            float distance =
                (pickup.transform.position - transform.position)
                .sqrMagnitude;

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = pickup;
            }
        }

        return closest;
    }

    private void EquipItem(ItemData item, int durability = -1)
    {
        if (item == null)
            return;

        currentItem = item;

        currentDurability = durability < 0
            ? item.MaxDurability
            : Mathf.Clamp(durability, 0, item.MaxDurability);

        Debug.Log("Equipped: " + item.ItemName +
                  " (" + currentDurability + " uses)");
    }

    public bool UseItem()
    {
        if (currentItem == null || currentDurability <= 0)
            return false;

        currentDurability--;

        Debug.Log(currentItem.ItemName + ": " +
                  currentDurability + "/" +
                  currentItem.MaxDurability);

        if (currentDurability <= 0)
        {
            Debug.Log(currentItem.ItemName + " broke");
            currentItem = null;
            currentDurability = 0;
        }

        return true;
    }

    private bool DropCurrentItem()
    {
        if (currentItem == null)
            return false;

        if (droppedItemPrefab == null)
        {
            Debug.LogWarning("No dropped item prefab assigned");
            return false;
        }

        Vector3 dropPosition =
            transform.position + Vector3.down * 0.6f;

        ItemPickup dropped = Instantiate(
            droppedItemPrefab,
            dropPosition,
            Quaternion.identity
        );

        dropped.SetItem(currentItem, currentDurability);
        EnterPickupRange(dropped);

        Debug.Log("Dropped: " + currentItem.ItemName);

        currentItem = null;
        currentDurability = 0;

        return true;
    }
    public void DropItem()
    {
        DropCurrentItem();
    }
}
