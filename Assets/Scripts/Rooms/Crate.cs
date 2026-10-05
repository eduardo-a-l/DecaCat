using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Crate : MonoBehaviour, IDamageable
{
    private const int PlaceholderPixels = 16;
    private const float PlaceholderSize = 0.8f;

    private static Sprite placeholderSprite;

    [SerializeField] private int health = 1;
    [SerializeField] private ItemData[] loot;
    [SerializeField] private ItemPickup pickupPrefab;

    private bool isBroken;

    public bool CostsDurability => false;

    private static Sprite PlaceholderSprite
    {
        get
        {
            if (placeholderSprite == null)
                placeholderSprite = CreatePlaceholderSprite();

            return placeholderSprite;
        }
    }

    private void Awake()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer.sprite == null)
            spriteRenderer.sprite = PlaceholderSprite;

        BoxCollider2D box = GetComponent<BoxCollider2D>();

        if (box == null)
            box = gameObject.AddComponent<BoxCollider2D>();

        box.size = spriteRenderer.sprite.bounds.size;
        box.offset = spriteRenderer.sprite.bounds.center;
    }

    public void TakeDamage(int damage)
    {
        if (isBroken || damage <= 0)
            return;

        health -= damage;

        if (health <= 0)
            Break();
    }

    private void Break()
    {
        isBroken = true;

        DropLoot();
        Destroy(gameObject);
    }

    private void DropLoot()
    {
        if (pickupPrefab == null || loot == null || loot.Length == 0)
            return;

        ItemData item = loot[Random.Range(0, loot.Length)];

        if (item == null)
            return;

        ItemPickup pickup = Instantiate(
            pickupPrefab, transform.position, Quaternion.identity, transform.parent
        );

        pickup.SetItem(item, item.MaxDurability);
    }

    private static Sprite CreatePlaceholderSprite()
    {
        Texture2D texture = new Texture2D(
            PlaceholderPixels, PlaceholderPixels, TextureFormat.RGBA32, false
        );

        texture.filterMode = FilterMode.Point;

        Color border = new Color(0.33f, 0.2f, 0.09f, 1f);
        Color fill = new Color(0.62f, 0.42f, 0.2f, 1f);
        Color plank = new Color(0.5f, 0.32f, 0.14f, 1f);
        int last = PlaceholderPixels - 1;

        for (int y = 0; y < PlaceholderPixels; y++)
        {
            for (int x = 0; x < PlaceholderPixels; x++)
            {
                bool edge = x == 0 || y == 0 || x == last || y == last;
                bool seam = y == PlaceholderPixels / 2;

                texture.SetPixel(x, y, edge ? border : (seam ? plank : fill));
            }
        }

        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, PlaceholderPixels, PlaceholderPixels),
            new Vector2(0.5f, 0.5f),
            PlaceholderPixels / PlaceholderSize
        );
    }
}
