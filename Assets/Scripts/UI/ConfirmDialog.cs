using System;
using UnityEngine;
using UnityEngine.UI;

public static class ConfirmDialog
{
    private static readonly Vector2 BoxSize = new Vector2(1000f, 420f);
    private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.75f);
    private static readonly Color BoxColor = new Color(0.08f, 0.08f, 0.1f, 1f);

    public static GameObject Create(
        string message, string confirmLabel, Action onConfirm, Action onCancel)
    {
        GameObject root = UIFactory.CreateOverlayCanvas("ConfirmDialog", 200);

        UIFactory.CreatePanel(root.transform, "Dim", DimColor);

        RectTransform box = UIFactory.CreateRect(
            root.transform, "Box", Vector2.zero, BoxSize
        );

        Image fill = box.gameObject.AddComponent<Image>();
        fill.color = BoxColor;

        UIFactory.AddOutline(box, BoxSize, 5f, Color.white);

        UIFactory.CreateText(
            box, "Message", message, 44f, Color.white,
            new Vector2(0f, 70f), new Vector2(900f, 200f)
        );

        UIFactory.CreateButton(
            box, "Confirm", confirmLabel,
            new Vector2(-190f, -120f), new Vector2(340f, 90f), onConfirm, 44f
        );

        UIFactory.CreateButton(
            box, "Cancel", "Cancel",
            new Vector2(190f, -120f), new Vector2(340f, 90f), onCancel, 44f
        );

        return root;
    }
}
