using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthDisplay : MonoBehaviour
{
    private const int HeartsPerRow = 6;
    private const float RowGap = 10f;

    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image[] hearts;

    [SerializeField] private Sprite fullHeart;
    [SerializeField] private Sprite emptyHeart;

    private readonly List<Image> slots = new List<Image>();
    private Vector2 origin;
    private Vector2 columnStep = new Vector2(120f, 0f);
    private Vector2 rowStep = new Vector2(0f, -110f);

    private void Awake()
    {
        if (hearts == null)
            return;

        foreach (Image heart in hearts)
        {
            if (heart != null)
                slots.Add(heart);
        }

        if (slots.Count == 0)
            return;

        origin = slots[0].rectTransform.anchoredPosition;
        rowStep = new Vector2(0f, -(slots[0].rectTransform.sizeDelta.y + RowGap));

        if (slots.Count > 1)
        {
            columnStep =
                slots[1].rectTransform.anchoredPosition - origin;
        }
    }

    private void Update()
    {
        if (playerHealth == null || slots.Count == 0)
            return;

        int max = playerHealth.MaxHealth;

        while (slots.Count < max)
            AddHeart();

        for (int i = 0; i < slots.Count; i++)
        {
            bool visible = i < max;

            if (slots[i].gameObject.activeSelf != visible)
                slots[i].gameObject.SetActive(visible);

            bool isFull = i < playerHealth.CurrentHealth;

            slots[i].sprite = isFull ? fullHeart : emptyHeart;
        }
    }

    private void AddHeart()
    {
        Image template = slots[0];
        Image copy = Instantiate(template, template.transform.parent);

        copy.name = "Heart" + (slots.Count + 1);
        copy.rectTransform.anchoredPosition = GetPosition(slots.Count);

        slots.Add(copy);
    }

    private Vector2 GetPosition(int index)
    {
        int column = index % HeartsPerRow;
        int row = index / HeartsPerRow;

        return origin + columnStep * column + rowStep * row;
    }
}
