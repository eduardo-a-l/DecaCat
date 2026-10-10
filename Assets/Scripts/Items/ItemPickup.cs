using TMPro;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
public class ItemPickup : MonoBehaviour
{
    private const float PromptGap = 0.4f;
    private const float PromptSize = 0.5f;
    private const float PromptFontSize = 3.2f;
    private const int PromptSortingOrder = 10;

    private static readonly Color PromptBackground = new Color(0f, 0f, 0f, 0.75f);

    [SerializeField] private ItemData itemData;

    private SpriteRenderer spriteRenderer;
    private GameObject promptObject;
    private TextMeshPro promptText;
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

        if (itemData != null)
            Discoveries.Add(Discoveries.ItemKind, itemData.ItemName);

        UpdateSprite();
    }

    public void SetItem(ItemData item, int remainingDurability)
    {
        itemData = item;
        durability = remainingDurability;
        durabilitySet = true;

        UpdateSprite();
    }

    public void SetPromptVisible(bool visible, string keyLabel)
    {
        if (!visible)
        {
            if (promptObject != null)
                promptObject.SetActive(false);

            return;
        }

        if (promptObject == null)
            CreatePrompt();

        promptObject.transform.localPosition =
            new Vector3(0f, GetPromptHeight(), 0f);

        if (promptText.text != keyLabel)
            promptText.text = keyLabel;

        promptObject.SetActive(true);
    }

    private float GetPromptHeight()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
            return PromptGap + PromptSize / 2f;

        return spriteRenderer.sprite.bounds.extents.y + PromptGap;
    }

    private void CreatePrompt()
    {
        promptObject = new GameObject("Prompt");
        promptObject.transform.SetParent(transform, false);

        GameObject background = new GameObject("Background");
        background.transform.SetParent(promptObject.transform, false);
        background.transform.localScale = Vector3.one * PromptSize;

        SpriteRenderer backgroundRenderer =
            background.AddComponent<SpriteRenderer>();

        backgroundRenderer.sprite = UIFactory.CircleSprite;
        backgroundRenderer.color = PromptBackground;
        backgroundRenderer.sortingOrder = PromptSortingOrder;

        GameObject keyObject = new GameObject("Key", typeof(RectTransform));
        keyObject.transform.SetParent(promptObject.transform, false);

        promptText = keyObject.AddComponent<TextMeshPro>();
        promptText.fontSize = PromptFontSize;
        promptText.fontStyle = FontStyles.Bold;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.color = Color.white;
        promptText.sortingOrder = PromptSortingOrder + 1;
        promptText.rectTransform.sizeDelta = new Vector2(PromptSize, PromptSize);
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
