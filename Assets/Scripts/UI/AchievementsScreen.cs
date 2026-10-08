using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementsScreen : MonoBehaviour
{
    private const float ColumnX = 470f;
    private const float RowAreaTop = 250f;
    private const float RowAreaHeight = 600f;
    private const float MaxRowHeight = 104f;

    private static readonly Color BackgroundColor = new Color(0.04f, 0.04f, 0.06f, 0.97f);
    private static readonly Color UnlockedColor = new Color(0.42f, 0.74f, 0.55f, 1f);
    private static readonly Color LockedColor = new Color(0.4f, 0.4f, 0.46f, 1f);
    private static readonly Color UnlockedFill = new Color(0.42f, 0.74f, 0.55f, 0.12f);
    private static readonly Color LockedFill = new Color(1f, 1f, 1f, 0.03f);
    private static readonly Color DescriptionColor = new Color(1f, 1f, 1f, 0.6f);
    private static readonly Color TabSelected = new Color(0.95f, 0.78f, 0.3f, 1f);
    private static readonly Color TabNormal = new Color(0.2f, 0.2f, 0.2f, 1f);

    private int slot;
    private Action onClose;
    private RectTransform content;

    public static GameObject Create(int startSlot, Action onClose)
    {
        GameObject root = UIFactory.CreateOverlayCanvas("Achievements", 100);

        AchievementsScreen screen = root.AddComponent<AchievementsScreen>();
        screen.slot = Mathf.Clamp(startSlot, 0, SaveSlots.SlotCount - 1);
        screen.onClose = onClose;
        screen.Build();

        return root;
    }

    private void Build()
    {
        if (content != null)
            Destroy(content.gameObject);

        content = UIFactory.CreateRect(
            transform, "Content", Vector2.zero, Vector2.zero
        );

        content.anchorMin = Vector2.zero;
        content.anchorMax = Vector2.one;
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;

        UIFactory.CreatePanel(content, "Background", BackgroundColor);

        UIFactory.CreateText(
            content, "Title", "ACHIEVEMENTS", 80f, Color.white,
            new Vector2(0f, 465f), new Vector2(1200f, 110f)
        );

        DrawTabs();

        SaveData data = GetData();

        if (data == null)
        {
            UIFactory.CreateText(
                content, "Empty", "This slot is empty", 50f, DescriptionColor,
                new Vector2(0f, 0f), new Vector2(1000f, 80f)
            );
        }
        else
        {
            DrawSummary(data);
            DrawRows(data);
        }

        UIFactory.CreateButton(
            content, "CloseButton", "Close",
            new Vector2(0f, -470f), new Vector2(300f, 80f), onClose, 44f
        );
    }

    private void DrawTabs()
    {
        for (int i = 0; i < SaveSlots.SlotCount; i++)
        {
            int tabSlot = i;

            Button tab = UIFactory.CreateButton(
                content, "Tab" + i, "Slot " + (i + 1),
                new Vector2((i - 1) * 250f, 385f), new Vector2(220f, 64f),
                () => SelectSlot(tabSlot), 34f
            );

            if (i != slot)
                continue;

            ColorBlock colors = tab.colors;
            colors.normalColor = TabSelected;
            colors.highlightedColor = TabSelected;
            colors.selectedColor = TabSelected;
            tab.colors = colors;

            TextMeshProUGUI label = tab.GetComponentInChildren<TextMeshProUGUI>();

            if (label != null)
                label.color = Color.black;
        }
    }

    private void DrawSummary(SaveData data)
    {
        UIFactory.CreateText(
            content, "Summary",
            "Unlocked " + data.unlockedAchievements.Count + " / " +
            Achievements.All.Length,
            34f, Color.white, new Vector2(0f, 308f), new Vector2(800f, 50f)
        );
    }

    private void DrawRows(SaveData data)
    {
        int count = Achievements.All.Length;
        int rows = Mathf.CeilToInt(count / 2f);
        float rowHeight = Mathf.Min(MaxRowHeight, RowAreaHeight / rows);

        for (int i = 0; i < count; i++)
        {
            int column = i / rows;
            int row = i % rows;

            float x = column == 0 ? -ColumnX : ColumnX;
            float y = RowAreaTop - rowHeight * (row + 0.5f);

            DrawRow(Achievements.All[i], data, new Vector2(x, y), rowHeight - 10f);
        }
    }

    private void DrawRow(
        AchievementDefinition achievement, SaveData data,
        Vector2 position, float height)
    {
        bool unlocked = data.IsUnlocked(achievement.Id);
        int value = Mathf.Min(data.GetStat(achievement.Stat), achievement.Target);
        Vector2 size = new Vector2(900f, height);

        RectTransform row = UIFactory.CreateRect(
            content, achievement.Id, position, size
        );

        Image fill = row.gameObject.AddComponent<Image>();
        fill.color = unlocked ? UnlockedFill : LockedFill;
        fill.raycastTarget = false;

        UIFactory.AddOutline(row, size, 3f, unlocked ? UnlockedColor : LockedColor);

        UIFactory.CreateText(
            row, "Title", achievement.Title, 34f,
            unlocked ? UnlockedColor : Color.white,
            new Vector2(-100f, height * 0.2f), new Vector2(640f, 44f),
            null, TextAlignmentOptions.Left
        );

        UIFactory.CreateText(
            row, "Description", achievement.Description, 24f, DescriptionColor,
            new Vector2(-100f, -height * 0.22f), new Vector2(640f, 34f),
            null, TextAlignmentOptions.Left
        );

        UIFactory.CreateText(
            row, "Progress",
            unlocked ? "Unlocked" : value + "/" + achievement.Target, 30f,
            unlocked ? UnlockedColor : DescriptionColor,
            new Vector2(350f, 0f), new Vector2(180f, 50f),
            null, TextAlignmentOptions.Right
        );
    }

    private SaveData GetData()
    {
        if (SaveSession.IsActive && SaveSession.ActiveSlot == slot)
            return SaveSession.Data;

        return SaveSlots.Load(slot);
    }

    private void SelectSlot(int newSlot)
    {
        slot = newSlot;

        Build();
    }
}
