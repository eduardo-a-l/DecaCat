using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemDisplay : MonoBehaviour
{
    [SerializeField] private PlayerItem playerItem;
    [SerializeField] private Image itemImage;

    [Header("Durability")]
    [SerializeField] private TMP_Text durabilityText;
    [SerializeField] private float fontSize = 28f;
    [SerializeField] private float textHeight = 32f;
    [SerializeField] private float textGap = 4f;

    private int shownDurability = -1;
    private int shownMaxDurability = -1;

    private void Awake()
    {
        if (durabilityText == null && itemImage != null)
            durabilityText = CreateDurabilityText();
    }

    private void Update()
    {
        if (playerItem == null || itemImage == null)
            return;

        ItemData item = playerItem.CurrentItem;

        if (item == null)
        {
            itemImage.enabled = false;
            HideDurability();
        }
        else
        {
            itemImage.enabled = true;
            itemImage.sprite = item.ItemSprite;
            ShowDurability(playerItem.CurrentDurability, item.MaxDurability);
        }
    }

    public void ShiftBy(Vector2 delta)
    {
        if (itemImage == null)
            return;

        itemImage.rectTransform.anchoredPosition += delta;

        Transform parent = itemImage.transform.parent;
        Transform background =
            parent != null ? parent.Find("ItemSlotBackground") : null;

        if (background != null)
            ((RectTransform)background).anchoredPosition += delta;

        if (durabilityText != null)
            durabilityText.rectTransform.anchoredPosition += delta;
    }

    private void ShowDurability(int current, int max)
    {
        if (durabilityText == null)
            return;

        durabilityText.enabled = true;

        if (current == shownDurability && max == shownMaxDurability)
            return;

        shownDurability = current;
        shownMaxDurability = max;
        durabilityText.text = current + "/" + max;
    }

    private void HideDurability()
    {
        shownDurability = -1;
        shownMaxDurability = -1;

        if (durabilityText != null)
            durabilityText.enabled = false;
    }

    private TMP_Text CreateDurabilityText()
    {
        RectTransform iconRect = itemImage.rectTransform;

        GameObject textObject = new GameObject(
            "DurabilityText", typeof(RectTransform)
        );

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(iconRect.parent, false);
        rect.anchorMin = iconRect.anchorMin;
        rect.anchorMax = iconRect.anchorMax;
        rect.pivot = new Vector2(0.5f, 0f);

        float centerX =
            iconRect.anchoredPosition.x +
            iconRect.rect.width * (0.5f - iconRect.pivot.x);

        float top =
            iconRect.anchoredPosition.y +
            iconRect.rect.height * (1f - iconRect.pivot.y);

        rect.anchoredPosition = new Vector2(centerX, top + textGap);
        rect.sizeDelta = new Vector2(iconRect.rect.width, textHeight);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Bottom;
        text.color = Color.white;
        text.raycastTarget = false;
        text.enabled = false;

        return text;
    }
}