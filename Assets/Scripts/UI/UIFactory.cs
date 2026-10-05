using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class UIFactory
{
    private const int CirclePixels = 64;

    private static readonly Color ButtonNormal = new Color(0.2f, 0.2f, 0.2f, 1f);
    private static readonly Color ButtonHighlighted = new Color(0.35f, 0.35f, 0.35f, 1f);
    private static readonly Color ButtonPressed = new Color(0.12f, 0.12f, 0.12f, 1f);

    private static Sprite circleSprite;

    public static Sprite CircleSprite
    {
        get
        {
            if (circleSprite == null)
                circleSprite = CreateCircleSprite();

            return circleSprite;
        }
    }

    public static GameObject CreateOverlayCanvas(string objectName, int sortingOrder)
    {
        GameObject root = new GameObject(
            objectName,
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return root;
    }

    public static Image CreatePanel(Transform parent, string objectName, Color color)
    {
        GameObject panel = new GameObject(
            objectName, typeof(RectTransform), typeof(Image)
        );

        panel.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)panel.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = panel.GetComponent<Image>();
        image.color = color;

        return image;
    }

    public static RectTransform CreateRect(
        Transform parent, string objectName, Vector2 position, Vector2 size)
    {
        GameObject rectObject = new GameObject(objectName, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)rectObject.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        return rect;
    }

    public static TextMeshProUGUI CreateText(
        Transform parent, string objectName, string content, float fontSize,
        Color color, Vector2 position, Vector2 size, TMP_FontAsset font = null)
    {
        RectTransform rect = CreateRect(parent, objectName, position, size);

        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.raycastTarget = false;

        if (font != null)
            text.font = font;

        return text;
    }

    public static Button CreateButton(
        Transform parent, string objectName, string label,
        Vector2 position, Vector2 size, Action onClick)
    {
        RectTransform rect = CreateRect(parent, objectName, position, size);

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = Color.white;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = ButtonNormal;
        colors.highlightedColor = ButtonHighlighted;
        colors.selectedColor = ButtonHighlighted;
        colors.pressedColor = ButtonPressed;
        button.colors = colors;

        button.onClick.AddListener(() =>
        {
            if (onClick != null)
                onClick();
        });

        CreateText(rect, "Label", label, 52f, Color.white, Vector2.zero, size);

        return button;
    }

    private static Sprite CreateCircleSprite()
    {
        Texture2D texture = new Texture2D(
            CirclePixels, CirclePixels, TextureFormat.RGBA32, false
        );

        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        float radius = CirclePixels * 0.5f;
        Vector2 center = new Vector2(radius, radius);
        Color[] pixels = new Color[CirclePixels * CirclePixels];

        for (int y = 0; y < CirclePixels; y++)
        {
            for (int x = 0; x < CirclePixels; x++)
            {
                Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                float alpha = Mathf.Clamp01(radius - Vector2.Distance(point, center));

                pixels[y * CirclePixels + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, CirclePixels, CirclePixels),
            new Vector2(0.5f, 0.5f),
            CirclePixels
        );
    }
}
