using System.Collections.Generic;
using UnityEngine;

public class PlayerItem : MonoBehaviour
{
    [SerializeField] private ItemData startingItem;
    [SerializeField] private ItemPickup droppedItemPrefab;

    private PlayerInputReader inputReader;
    private ItemData currentItem;
    private int currentDurability;
    private bool loadoutApplied;

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
        if (startingItem != null && !loadoutApplied)
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

        UpdatePrompts();
    }

    public void EnterPickupRange(ItemPickup pickup)
    {
        if (pickup != null)
            nearbyPickups.Add(pickup);
    }

    public void ExitPickupRange(ItemPickup pickup)
    {
        if (pickup != null)
            pickup.SetPromptVisible(false, string.Empty);

        nearbyPickups.Remove(pickup);
    }

    private void UpdatePrompts()
    {
        ItemPickup closest = GetClosestPickup();
        string key = inputReader.InteractKeyLabel;

        foreach (ItemPickup pickup in nearbyPickups)
        {
            if (pickup != null)
                pickup.SetPromptVisible(pickup == closest, key);
        }
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

        GameStats.Add(StatType.ItemsPickedUp);

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

    public void ApplyLoadout(ItemData item, int durability)
    {
        loadoutApplied = true;

        if (item != null)
            EquipItem(item, durability);
    }

    private void EquipItem(ItemData item, int durability = -1)
    {
        if (item == null)
            return;

        currentItem = item;

        Discoveries.Add(Discoveries.ItemKind, item.ItemName, item.ItemName);

        GameStats.RecordWeaponUsed(item.ItemName);

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
            GameStats.Add(StatType.WeaponsBroken);
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

        ItemPickup dropped = Instantiate(
            droppedItemPrefab,
            transform.position,
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
