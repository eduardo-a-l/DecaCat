using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
public class ItemPickup : MonoBehaviour
{
    [SerializeField] private ItemData itemData;

    private SpriteRenderer spriteRenderer;
    private int durability;
    private bool durabilitySet;

    public ItemData ItemData => itemData;
    public int Durability => durability;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        GetComponent<CircleCollider2D>().isTrigger = true;
    }

    private void Start()
    {
        if (!durabilitySet && itemData != null)
            durability = itemData.MaxDurability;

        UpdateSprite();
    }

    public void SetItem(ItemData item, int remainingDurability)
    {
        itemData = item;
        durability = remainingDurability;
        durabilitySet = true;

        UpdateSprite();
    }

    private void UpdateSprite()
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.sprite =
            itemData != null ? itemData.ItemSprite : null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerItem player = other.GetComponent<PlayerItem>();

        if (player != null)
            player.EnterPickupRange(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerItem player = other.GetComponent<PlayerItem>();

        if (player != null)
            player.ExitPickupRange(this);
    }
}