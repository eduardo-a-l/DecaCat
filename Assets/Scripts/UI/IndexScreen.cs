using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IndexScreen : MonoBehaviour
{
    private const float RowHeight = 92f;
    private const float ListWidth = 780f;
    private const float ListHeight = 610f;
    private const float DetailWidth = 940f;

    private static readonly Vector2 ListCenter = new Vector2(-490f, -90f);
    private static readonly Vector2 DetailCenter = new Vector2(410f, -90f);

    private static readonly Color BackgroundColor = new Color(0.04f, 0.04f, 0.06f, 0.97f);
    private static readonly Color PanelColor = new Color(1f, 1f, 1f, 0.04f);
    private static readonly Color RowColor = new Color(1f, 1f, 1f, 0.06f);
    private static readonly Color RowSelectedColor = new Color(0.95f, 0.78f, 0.3f, 0.28f);
    private static readonly Color DimColor = new Color(1f, 1f, 1f, 0.6f);
    private static readonly Color LockedColor = new Color(1f, 1f, 1f, 0.35f);
    private static readonly Color TabSelected = new Color(0.95f, 0.78f, 0.3f, 1f);

    private static readonly string[] CategoryNames = { "Items", "Enemies", "Bosses" };

    private class Entry
    {
        public string Name;
        public string Subtitle;
        public string Description;
        public string[] Stats;
        public Sprite Sprite;
        public Color Tint = Color.white;
        public bool Discovered;
    }

    private int slot;
    private int category;
    private int selected;
    private Action onClose;
    private RectTransform content;
    private List<Entry> entries;
    private readonly List<Image> rowImages = new List<Image>();

    private Image detailIcon;
    private TextMeshProUGUI detailName;
    private TextMeshProUGUI detailSubtitle;
    private TextMeshProUGUI detailStats;
    private TextMeshProUGUI detailDescription;

    public static GameObject Create(int startSlot, Action onClose)
    {
        GameObject root = UIFactory.CreateOverlayCanvas("Index", 100);

        IndexScreen screen = root.AddComponent<IndexScreen>();
        screen.slot = Mathf.Clamp(startSlot, 0, SaveSlots.SlotCount - 1);
        screen.onClose = onClose;
        screen.Build();

        return root;
    }

    private void Build()
    {
        if (content != null)
            Destroy(content.gameObject);

        rowImages.Clear();

        content = UIFactory.CreateRect(
            transform, "Content", Vector2.zero, Vector2.zero
        );

        content.anchorMin = Vector2.zero;
        content.anchorMax = Vector2.one;
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;

        UIFactory.CreatePanel(content, "Background", BackgroundColor);

        UIFactory.CreateText(
            content, "Title", "INDEX", 80f, Color.white,
            new Vector2(0f, 465f), new Vector2(1200f, 110f)
        );

        DrawSlotTabs();
        DrawCategoryTabs();

        SaveData data = GetData();

        entries = data == null ? new List<Entry>() : BuildEntries(data);

        int found = 0;

        foreach (Entry entry in entries)
        {
            if (entry.Discovered)
                found++;
        }

        UIFactory.CreateText(
            content, "Summary", "Discovered " + found + " / " + entries.Count,
            32f, DimColor, new Vector2(0f, 255f), new Vector2(800f, 44f)
        );

        DrawList();
        DrawDetail();

        selected = Mathf.Clamp(selected, 0, Mathf.Max(0, entries.Count - 1));
        ShowEntry(selected);

        UIFactory.CreateButton(
            content, "CloseButton", "Close",
            new Vector2(0f, -470f), new Vector2(300f, 80f), onClose, 44f
        );
    }

    private void DrawSlotTabs()
    {
        for (int i = 0; i < SaveSlots.SlotCount; i++)
        {
            int tabSlot = i;

            Button tab = UIFactory.CreateButton(
                content, "SlotTab" + i, "Slot " + (i + 1),
                new Vector2((i - 1) * 250f, 385f), new Vector2(220f, 64f),
                () => SelectSlot(tabSlot), 34f
            );

            StyleTab(tab, i == slot);
        }
    }

    private void DrawCategoryTabs()
    {
        for (int i = 0; i < CategoryNames.Length; i++)
        {
            int tabCategory = i;

            Button tab = UIFactory.CreateButton(
                content, "CategoryTab" + i, CategoryNames[i],
                new Vector2((i - 1) * 300f, 315f), new Vector2(260f, 60f),
                () => SelectCategory(tabCategory), 32f
            );

            StyleTab(tab, i == category);
        }
    }

    private static void StyleTab(Button tab, bool isSelected)
    {
        if (!isSelected)
            return;

        ColorBlock colors = tab.colors;
        colors.normalColor = TabSelected;
        colors.highlightedColor = TabSelected;
        colors.selectedColor = TabSelected;
        tab.colors = colors;

        TextMeshProUGUI label = tab.GetComponentInChildren<TextMeshProUGUI>();

        if (label != null)
            label.color = Color.black;
    }

    private void DrawList()
    {
        RectTransform view = UIFactory.CreateRect(
            content, "List", ListCenter, new Vector2(ListWidth, ListHeight)
        );

        Image viewImage = view.gameObject.AddComponent<Image>();
        viewImage.color = PanelColor;

        view.gameObject.AddComponent<RectMask2D>();

        RectTransform listContent = UIFactory.CreateRect(
            view, "Content", Vector2.zero, Vector2.zero
        );

        listContent.anchorMin = new Vector2(0f, 1f);
        listContent.anchorMax = new Vector2(1f, 1f);
        listContent.pivot = new Vector2(0.5f, 1f);
        listContent.anchoredPosition = Vector2.zero;
        listContent.sizeDelta = new Vector2(0f, entries.Count * RowHeight);

        ScrollRect scroll = view.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = view;
        scroll.content = listContent;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.scrollSensitivity = 45f;

        if (entries.Count == 0)
        {
            UIFactory.CreateText(
                view, "Empty", "This slot is empty", 40f, DimColor,
                Vector2.zero, new Vector2(700f, 70f)
            );

            return;
        }

        for (int i = 0; i < entries.Count; i++)
            DrawRow(listContent, i);
    }

    private void DrawRow(RectTransform parent, int index)
    {
        Entry entry = entries[index];

        GameObject rowObject = new GameObject(
            "Row" + index, typeof(RectTransform), typeof(Image), typeof(Button)
        );

        rowObject.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)rowObject.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -index * RowHeight - 4f);
        rect.sizeDelta = new Vector2(-12f, RowHeight - 8f);

        Image image = rowObject.GetComponent<Image>();
        image.color = RowColor;
        rowImages.Add(image);

        Button button = rowObject.GetComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.85f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.7f);
        colors.selectedColor = Color.white;
        button.colors = colors;

        int rowIndex = index;
        button.onClick.AddListener(() => ShowEntry(rowIndex));

        Vector2 size = rect.sizeDelta;
        size.x = ListWidth - 12f;

        if (entry.Discovered && entry.Sprite != null)
        {
            UIFactory.CreateImage(
                rect, "Icon", new Vector2(-size.x / 2f + 50f, 0f),
                new Vector2(64f, 64f), entry.Tint, entry.Sprite
            );
        }

        UIFactory.CreateText(
            rect, "Name", entry.Discovered ? entry.Name : "???", 38f,
            entry.Discovered ? Color.white : LockedColor,
            new Vector2(40f, 0f), new Vector2(560f, 60f),
            null, TextAlignmentOptions.Left
        );
    }

    private void DrawDetail()
    {
        RectTransform detail = UIFactory.CreateRect(
            content, "Detail", DetailCenter, new Vector2(DetailWidth, ListHeight)
        );

        Image detailImage = detail.gameObject.AddComponent<Image>();
        detailImage.color = PanelColor;
        detailImage.raycastTarget = false;

        detailIcon = UIFactory.CreateImage(
            detail, "Icon", new Vector2(-330f, 170f),
            new Vector2(240f, 240f), Color.white
        );

        detailName = UIFactory.CreateText(
            detail, "Name", string.Empty, 56f, Color.white,
            new Vector2(110f, 215f), new Vector2(640f, 80f),
            null, TextAlignmentOptions.Left
        );

        detailSubtitle = UIFactory.CreateText(
            detail, "Subtitle", string.Empty, 30f, TabSelected,
            new Vector2(110f, 150f), new Vector2(640f, 44f),
            null, TextAlignmentOptions.Left
        );

        detailStats = UIFactory.CreateText(
            detail, "Stats", string.Empty, 30f, DimColor,
            new Vector2(110f, 40f), new Vector2(640f, 170f),
            null, TextAlignmentOptions.TopLeft
        );

        detailDescription = UIFactory.CreateText(
            detail, "Description", string.Empty, 34f, Color.white,
            new Vector2(0f, -170f), new Vector2(DetailWidth - 80f, 250f),
            null, TextAlignmentOptions.TopLeft
        );
    }

    private void ShowEntry(int index)
    {
        if (entries == null || entries.Count == 0)
        {
            detailIcon.enabled = false;
            detailName.text = string.Empty;
            detailSubtitle.text = string.Empty;
            detailStats.text = string.Empty;
            detailDescription.text = "Nothing here yet.";
            return;
        }

        selected = Mathf.Clamp(index, 0, entries.Count - 1);

        for (int i = 0; i < rowImages.Count; i++)
            rowImages[i].color = i == selected ? RowSelectedColor : RowColor;

        Entry entry = entries[selected];

        detailIcon.enabled = entry.Discovered && entry.Sprite != null;
        detailIcon.sprite = entry.Sprite;
        detailIcon.color = entry.Tint;
        detailIcon.preserveAspect = true;

        detailName.text = entry.Discovered ? entry.Name : "???";
        detailSubtitle.text = entry.Discovered ? entry.Subtitle : CategoryNames[category];
        detailStats.text = entry.Discovered ? string.Join("\n", entry.Stats) : string.Empty;

        detailDescription.text = entry.Discovered
            ? entry.Description
            : "Not discovered yet. Keep exploring.";
    }

    private List<Entry> BuildEntries(SaveData data)
    {
        if (category == 0)
            return BuildItemEntries(data);

        return BuildEnemyEntries(data, category == 2);
    }

    private static List<Entry> BuildItemEntries(SaveData data)
    {
        List<Entry> result = new List<Entry>();

        result.Add(new Entry
        {
            Name = "Bare Hands",
            Subtitle = "Item",
            Description =
                "Your own paws. Always with you and they never break. " +
                "Quick, but weak.",
            Stats = new[]
            {
                "Damage " + MeleeAttack.DefaultUnarmedDamage,
                "Cooldown " + MeleeAttack.DefaultUnarmedCooldown.ToString("0.##") + "s",
                "Never breaks"
            },
            Discovered = true
        });

        foreach (ItemData item in ItemDatabase.All)
        {
            result.Add(new Entry
            {
                Name = item.ItemName,
                Subtitle = "Item",
                Description = string.IsNullOrEmpty(item.Description)
                    ? "No description yet."
                    : item.Description,
                Stats = GetItemStats(item),
                Sprite = item.ItemSprite,
                Discovered = Discoveries.Has(
                    data, Discoveries.ItemKind, item.ItemName
                )
            });
        }

        return result;
    }

    private static string[] GetItemStats(ItemData item)
    {
        List<string> stats = new List<string>
        {
            "Damage " + item.Damage,
            "Cooldown " + item.Cooldown.ToString("0.##") + "s",
            "Durability " + item.MaxDurability,
            "Reach " + item.AttackRange.ToString("0.##")
        };

        if (item.HitsMultipleTargets)
            stats.Add("Hits every enemy in the swing");

        if (item.Knockback > 0f)
            stats.Add("Knocks enemies back");

        return stats.ToArray();
    }

    private static List<Entry> BuildEnemyEntries(SaveData data, bool bosses)
    {
        List<Entry> result = new List<Entry>();
        FloorThemeCatalog catalog = FloorThemeCatalog.Instance;

        if (catalog == null)
            return result;

        foreach (Enemy enemy in catalog.GetAllEnemies())
        {
            if (enemy.IsBoss != bosses)
                continue;

            enemy.GetIndexVisual(out Sprite sprite, out Color tint);

            result.Add(new Entry
            {
                Name = enemy.DisplayName,
                Subtitle = bosses ? "Boss" : "Enemy",
                Description = enemy.Description,
                Stats = enemy.GetIndexStats(),
                Sprite = sprite,
                Tint = tint,
                Discovered = Discoveries.Has(
                    data,
                    bosses ? Discoveries.BossKind : Discoveries.EnemyKind,
                    enemy.IndexId
                )
            });
        }

        return result;
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
        selected = 0;

        Build();
    }

    private void SelectCategory(int newCategory)
    {
        category = newCategory;
        selected = 0;

        Build();
    }
}
