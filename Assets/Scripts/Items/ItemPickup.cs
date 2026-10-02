using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
public class ItemPickup : MonoBehaviour
{
    [SerializeField] private ItemData itemData;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        CircleCollider2D itemCollider =
            GetComponent<CircleCollider2D>();

        itemCollider.isTrigger = true;
    }

    private void Start()
    {
        UpdateSprite();
    }

    public void SetItem(ItemData item)
    {
        itemData = item;
        UpdateSprite();
    }

    private void UpdateSprite()
    {
        if (spriteRenderer != null && itemData != null)
        {
            spriteRenderer.sprite = itemData.ItemSprite;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerItem playerItem = other.GetComponent<PlayerItem>();

        if (playerItem == null || itemData == null)
            return;

        playerItem.PickUpItem(itemData);
        Destroy(gameObject);
    }
}