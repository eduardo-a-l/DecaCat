using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SlotSelectScreen : MonoBehaviour
{
    private const float CardSpacing = 500f;
    private const float CardY = 40f;
    private const float ButtonRowY = -330f;

    private static readonly Vector2 CardSize = new Vector2(440f, 620f);
    private static readonly Color BackgroundColor = new Color(0.05f, 0.05f, 0.07f, 1f);
    private static readonly Color CardNormal = new Color(0.1f, 0.1f, 0.13f, 0.92f);
    private static readonly Color CardHighlighted = new Color(0.2f, 0.2f, 0.26f, 0.96f);
    private static readonly Color CardPressed = new Color(0.07f, 0.07f, 0.09f, 1f);
    private static readonly Color DimColor = new Color(1f, 1f, 1f, 0.45f);
    private static readonly Color DetailColor = new Color(1f, 1f, 1f, 0.8f);
    private static readonly Color SuccessColor = new Color(0.42f, 0.74f, 0.55f, 1f);
    private static readonly Color ErrorColor = new Color(0.9f, 0.35f, 0.35f, 1f);

    private Action<int, bool> onSelect;
    private Action onBack;
    private RectTransform content;
    private GameObject dialog;
    private string statusMessage = string.Empty;
    private bool statusIsError;

    public static GameObject Create(Action<int, bool> onSelect, Action onBack)
    {
        GameObject root = UIFactory.CreateOverlayCanvas("SlotSelect", 50);

        SlotSelectScreen screen = root.AddComponent<SlotSelectScreen>();
        screen.onSelect = onSelect;
        screen.onBack = onBack;
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
            content, "Title", "SELECT SLOT", 90f, Color.white,
            new Vector2(0f, 440f), new Vector2(1000f, 120f)
        );

        for (int slot = 0; slot < SaveSlots.SlotCount; slot++)
            DrawSlot(slot);

        UIFactory.CreateText(
            content, "Status", statusMessage, 30f,
            statusIsError ? ErrorColor : SuccessColor,
            new Vector2(0f, -405f), new Vector2(1700f, 50f)
        );

        UIFactory.CreateButton(
            content, "BackButton", "Back",
            new Vector2(0f, -475f), new Vector2(300f, 80f), onBack, 44f
        );
    }

    private void DrawSlot(int slot)
    {
        SaveData data = SaveSlots.Load(slot);
        bool isEmpty = data == null;
        float x = (slot - 1) * CardSpacing;

        RectTransform card = UIFactory.CreateRect(
            content, "Slot" + (slot + 1), new Vector2(x, CardY), CardSize
        );

        Image fill = card.gameObject.AddComponent<Image>();
        fill.color = Color.white;

        Button button = card.gameObject.AddComponent<Button>();
        button.targetGraphic = fill;

        ColorBlock colors = button.colors;
        colors.normalColor = CardNormal;
        colors.highlightedColor = CardHighlighted;
        colors.selectedColor = CardNormal;
        colors.pressedColor = CardPressed;
        button.colors = colors;

        button.onClick.AddListener(() => onSelect(slot, isEmpty));

        UIFactory.AddOutline(card, CardSize, 5f, isEmpty ? DimColor : Color.white);

        UIFactory.CreateText(
            card, "Number", (slot + 1) + ".", 84f,
            isEmpty ? DimColor : Color.white,
            new Vector2(-130f, -250f), new Vector2(160f, 110f),
            null, TextAlignmentOptions.Left
        );

        if (isEmpty)
        {
            UIFactory.CreateText(
                card, "NewGame", "NEW GAME", 54f, Color.white,
                Vector2.zero, new Vector2(400f, 100f)
            );
        }
        else
        {
            DrawDetails(card, data);
        }

        Button exportButton = UIFactory.CreateButton(
            content, "Export" + slot, "Export",
            new Vector2(x - 145f, ButtonRowY), new Vector2(135f, 56f),
            () => ExportSlot(slot), 28f
        );

        exportButton.interactable = !isEmpty;

        UIFactory.CreateButton(
            content, "Import" + slot, "Import",
            new Vector2(x, ButtonRowY), new Vector2(135f, 56f),
            () => ImportSlot(slot), 28f
        );

        Button deleteButton = UIFactory.CreateButton(
            content, "Delete" + slot, "Delete",
            new Vector2(x + 145f, ButtonRowY), new Vector2(135f, 56f),
            () => DeleteSlot(slot), 28f
        );

        deleteButton.interactable = !isEmpty;
    }

    private void DrawDetails(RectTransform card, SaveData data)
    {
        UIFactory.CreateText(
            card, "Floor", "FLOOR " + data.floor, 64f, Color.white,
            new Vector2(0f, 210f), new Vector2(400f, 90f)
        );

        ItemData weapon = ItemDatabase.FindItem(data.loadoutItem);

        if (weapon != null)
        {
            UIFactory.CreateImage(
                card, "Weapon", new Vector2(0f, 100f), new Vector2(120f, 120f),
                Color.white, weapon.ItemSprite
            );

            UIFactory.CreateText(
                card, "WeaponName",
                weapon.ItemName + "  " + data.loadoutDurability + "/" +
                weapon.MaxDurability,
                32f, DetailColor, new Vector2(0f, 20f), new Vector2(420f, 50f)
            );
        }
        else
        {
            UIFactory.CreateText(
                card, "WeaponName", "No weapon", 32f, DimColor,
                new Vector2(0f, 60f), new Vector2(420f, 50f)
            );
        }

        UIFactory.CreateText(
            card, "Deaths", "Deaths: " + data.GetStat(StatType.Deaths), 32f,
            DetailColor, new Vector2(0f, -60f), new Vector2(420f, 50f)
        );

        UIFactory.CreateText(
            card, "Achievements",
            "Achievements: " + data.unlockedAchievements.Count + "/" +
            Achievements.All.Length,
            32f, DetailColor, new Vector2(0f, -110f), new Vector2(420f, 50f)
        );

        UIFactory.CreateText(
            card, "PlayTime", data.FormatPlayTime(), 34f, DetailColor,
            new Vector2(110f, -255f), new Vector2(200f, 60f),
            null, TextAlignmentOptions.Right
        );
    }

    private void ExportSlot(int slot)
    {
        bool success = SaveSlots.Export(slot, out string message);

        SetStatus(message, !success);
    }

    private void ImportSlot(int slot)
    {
        if (!SaveSlots.TryParseExportCode(
                GUIUtility.systemCopyBuffer, out SaveData parsed, out string error))
        {
            SetStatus(error, true);
            return;
        }

        if (!SaveSlots.Exists(slot))
        {
            FinishImport(slot, parsed);
            return;
        }

        OpenDialog(
            "Replace slot " + (slot + 1) + " with the save from the clipboard?",
            "Replace",
            () =>
            {
                CloseDialog();
                FinishImport(slot, parsed);
            }
        );
    }

    private void FinishImport(int slot, SaveData data)
    {
        SaveSlots.Save(slot, data);

        SetStatus("Imported the clipboard save into slot " + (slot + 1), false);
    }

    private void DeleteSlot(int slot)
    {
        OpenDialog(
            "Delete slot " + (slot + 1) + "?\nThis can't be undone.",
            "Delete",
            () =>
            {
                CloseDialog();
                SaveSlots.Delete(slot);

                SetStatus("Slot " + (slot + 1) + " deleted", false);
            }
        );
    }

    private void SetStatus(string message, bool isError)
    {
        statusMessage = message;
        statusIsError = isError;

        Build();
    }

    private void OpenDialog(string message, string confirmLabel, Action onConfirm)
    {
        CloseDialog();

        dialog = ConfirmDialog.Create(message, confirmLabel, onConfirm, CloseDialog);
    }

    private void CloseDialog()
    {
        if (dialog != null)
            Destroy(dialog);

        dialog = null;
    }

    private void OnDestroy()
    {
        CloseDialog();
    }
}
