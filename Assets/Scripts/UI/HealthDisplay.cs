using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthDisplay : MonoBehaviour
{
    private const float LeftEdge = 46f;
    private const float TopEdge = -53f;
    private const float BandHeight = 100f;
    private const float RowGap = 6f;
    private const float ColumnGapRatio = 0.21f;
    private const float MaxHeartSize = 100f;
    private const float MinHeartSize = 22f;
    private const float FloorLabelHalfWidth = 350f;
    private const float RightMargin = 30f;
    private const float MinBandWidth = 260f;

    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image[] hearts;

    [SerializeField] private Sprite fullHeart;
    [SerializeField] private Sprite emptyHeart;

    private readonly List<Image> slots = new List<Image>();
    private int laidOutCount = -1;
    private float laidOutWidth = -1f;

    private void Awake()
    {
        if (hearts == null)
            return;

        foreach (Image heart in hearts)
        {
            if (heart != null)
                slots.Add(heart);
        }
    }

    private void Update()
    {
        if (playerHealth == null || slots.Count == 0)
            return;

        int max = playerHealth.MaxHealth;

        while (slots.Count < max)
            AddHeart();

        float bandWidth = GetBandWidth();

        if (max != laidOutCount || !Mathf.Approximately(bandWidth, laidOutWidth))
        {
            Layout(max, bandWidth);

            laidOutCount = max;
            laidOutWidth = bandWidth;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            bool visible = i < max;

            if (slots[i].gameObject.activeSelf != visible)
                slots[i].gameObject.SetActive(visible);

            bool isFull = i < playerHealth.CurrentHealth;

            slots[i].sprite = isFull ? fullHeart : emptyHeart;
        }
    }

    private float GetBandWidth()
    {
        RectTransform parent = slots[0].rectTransform.parent as RectTransform;

        if (parent == null || parent.rect.width <= 1f)
            return MinBandWidth;

        float width = parent.rect.width * 0.5f - FloorLabelHalfWidth - RightMargin - LeftEdge;

        return Mathf.Max(MinBandWidth, width);
    }

    private void AddHeart()
    {
        Image template = slots[0];
        Image copy = Instantiate(template, template.transform.parent);

        copy.name = "Heart" + (slots.Count + 1);

        slots.Add(copy);

        laidOutCount = -1;
    }

    private void Layout(int count, float bandWidth)
    {
        int rows = 1;
        float size = MaxHeartSize;

        for (; rows <= count; rows++)
        {
            size = Mathf.Min(
                MaxHeartSize,
                (BandHeight - RowGap * (rows - 1)) / rows
            );

            size = Mathf.Max(MinHeartSize, size);

            int columns = Mathf.CeilToInt((float)count / rows);
            float pitch = size * (1f + ColumnGapRatio);

            if (columns * pitch - size * ColumnGapRatio <= bandWidth)
                break;
        }

        rows = Mathf.Min(rows, count);

        int perRow = Mathf.CeilToInt((float)count / rows);
        float columnPitch = size * (1f + ColumnGapRatio);
        float rowPitch = size + RowGap;

        for (int i = 0; i < slots.Count; i++)
        {
            RectTransform rect = slots[i].rectTransform;

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);

            int column = i % perRow;
            int row = i / perRow;

            rect.anchoredPosition = new Vector2(
                LeftEdge + size * 0.5f + column * columnPitch,
                TopEdge - size * 0.5f - row * rowPitch
            );
        }
    }
}
