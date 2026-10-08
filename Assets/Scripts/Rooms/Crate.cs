using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Crate : MonoBehaviour, IDamageable
{
    private const int PlaceholderPixels = 16;
    private const float PlaceholderSize = 0.8f;
    private const int FallSortingOrder = 5;

    private static Sprite placeholderSprite;

    [SerializeField] private int health = 1;
    [SerializeField] private ItemData[] loot;
    [SerializeField] private ItemPickup pickupPrefab;

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    private int restingSortingOrder;
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
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer.sprite == null)
            spriteRenderer.sprite = PlaceholderSprite;

        restingSortingOrder = spriteRenderer.sortingOrder;

        boxCollider = GetComponent<BoxCollider2D>();

        if (boxCollider == null)
            boxCollider = gameObject.AddComponent<BoxCollider2D>();

        boxCollider.size = spriteRenderer.sprite.bounds.size;
        boxCollider.offset = spriteRenderer.sprite.bounds.center;
    }

    public void BeginFall(float height, float duration)
    {
        StartCoroutine(FallCoroutine(height, duration));
    }

    public void TakeDamage(int damage)
    {
        if (isBroken || damage <= 0)
            return;

        health -= damage;

        if (health <= 0)
            Break();
    }

    private IEnumerator FallCoroutine(float height, float duration)
    {
        Vector3 landed = transform.position;
        float elapsed = 0f;

        boxCollider.enabled = false;
        spriteRenderer.sortingOrder = FallSortingOrder;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            transform.position = landed + Vector3.up * (height * (1f - t * t));

            yield return null;
        }

        transform.position = landed;
        spriteRenderer.sortingOrder = restingSortingOrder;
        boxCollider.enabled = true;
    }

    private void Break()
    {
        isBroken = true;

        GameStats.Add(StatType.CratesBroken);

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
