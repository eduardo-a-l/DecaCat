using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthBar : MonoBehaviour
{
    private const float BarWidth = 920f;
    private const float BarHeight = 34f;
    private const float FrameSize = 5f;
    private const float FadeSpeed = 3f;
    private const float ChipSpeed = 0.35f;
    private const float ChipDelay = 0.4f;

    private static readonly Color FrameColor = new Color(0f, 0f, 0f, 0.85f);
    private static readonly Color BackColor = new Color(0.12f, 0.05f, 0.05f, 0.9f);
    private static readonly Color ChipColor = new Color(1f, 0.9f, 0.75f, 0.95f);
    private static readonly Color FillColor = new Color(0.86f, 0.17f, 0.17f, 1f);

    private RoomManager roomManager;
    private CanvasGroup group;
    private RectTransform chipRect;
    private RectTransform fillRect;
    private TextMeshProUGUI nameLabel;
    private Enemy boss;
    private float ratio = 1f;
    private float chipRatio = 1f;
    private float chipTimer;
    private float alpha;

    public static BossHealthBar Create(
        Transform parent, TMP_FontAsset font, RoomManager manager)
    {
        GameObject root = new GameObject("BossHealthBar", typeof(RectTransform));
        root.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)root.transform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -250f);
        rect.sizeDelta = new Vector2(BarWidth + FrameSize * 2f, 100f);

        BossHealthBar bar = root.AddComponent<BossHealthBar>();
        bar.roomManager = manager;
        bar.Build(rect, font);

        return bar;
    }

    private void Build(RectTransform root, TMP_FontAsset font)
    {
        group = root.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        RectTransform nameRect = NewRect(root, "Name", new Vector2(0f, 30f),
            new Vector2(BarWidth, 44f));

        nameLabel = nameRect.gameObject.AddComponent<TextMeshProUGUI>();
        nameLabel.fontSize = 38f;
        nameLabel.alignment = TextAlignmentOptions.Center;
        nameLabel.color = Color.white;
        nameLabel.raycastTarget = false;

        if (font != null)
            nameLabel.font = font;

        RectTransform frame = NewRect(root, "Frame", new Vector2(0f, -14f),
            new Vector2(BarWidth + FrameSize * 2f, BarHeight + FrameSize * 2f));

        Fill(frame.gameObject, FrameColor);

        RectTransform back = NewRect(root, "Back", new Vector2(0f, -14f),
            new Vector2(BarWidth, BarHeight));

        Fill(back.gameObject, BackColor);

        chipRect = NewStretch(back, "Chip", ChipColor);
        fillRect = NewStretch(back, "Fill", FillColor);
    }

    private void Update()
    {
        Enemy found = FindBoss();

        if (found != boss)
        {
            if (found == null && boss != null)
                ratio = 0f;

            boss = found;

            if (boss != null)
            {
                nameLabel.text = boss.DisplayName;
                ratio = GetRatio(boss);
                chipRatio = ratio;
            }
        }

        if (boss != null)
        {
            float newRatio = GetRatio(boss);

            if (newRatio < ratio)
                chipTimer = ChipDelay;

            ratio = newRatio;
        }

        chipTimer -= Time.unscaledDeltaTime;

        if (chipRatio < ratio)
            chipRatio = ratio;
        else if (chipTimer <= 0f)
            chipRatio = Mathf.MoveTowards(chipRatio, ratio, ChipSpeed * Time.unscaledDeltaTime);

        SetWidth(fillRect, ratio);
        SetWidth(chipRect, chipRatio);

        float targetAlpha = boss != null ? 1f : 0f;

        alpha = Mathf.MoveTowards(alpha, targetAlpha, FadeSpeed * Time.unscaledDeltaTime);

        group.alpha = alpha;
    }

    private Enemy FindBoss()
    {
        Room room = roomManager != null ? roomManager.CurrentRoom : null;

        if (room == null || room.State != RoomState.Active)
            return null;

        foreach (Enemy enemy in room.Enemies)
        {
            if (enemy != null && enemy.IsBoss && enemy.CurrentHealth > 0)
                return enemy;
        }

        return null;
    }

    private static float GetRatio(Enemy enemy)
    {
        if (enemy.MaxHealth <= 0)
            return 0f;

        return Mathf.Clamp01((float)enemy.CurrentHealth / enemy.MaxHealth);
    }

    private static void SetWidth(RectTransform rect, float value)
    {
        rect.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
    }

    private static RectTransform NewRect(
        Transform parent, string objectName, Vector2 position, Vector2 size)
    {
        GameObject rectObject = new GameObject(objectName, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)rectObject.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        return rect;
    }

    private static RectTransform NewStretch(
        RectTransform parent, string objectName, Color color)
    {
        GameObject rectObject = new GameObject(objectName, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)rectObject.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Fill(rectObject, color);

        return rect;
    }

    private static void Fill(GameObject target, Color color)
    {
        Image image = target.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }
}
