using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
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
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

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
        Color color, Vector2 position, Vector2 size, TMP_FontAsset font = null,
        TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        RectTransform rect = CreateRect(parent, objectName, position, size);

        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;

        if (font != null)
            text.font = font;

        return text;
    }

    public static Button CreateButton(
        Transform parent, string objectName, string label,
        Vector2 position, Vector2 size, Action onClick, float fontSize = 52f)
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

        CreateText(rect, "Label", label, fontSize, Color.white, Vector2.zero, size);

        return button;
    }

    public static Image CreateImage(
        Transform parent, string objectName, Vector2 position, Vector2 size,
        Color color, Sprite sprite = null)
    {
        RectTransform rect = CreateRect(parent, objectName, position, size);

        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        image.preserveAspect = sprite != null;

        return image;
    }

    public static void AddOutline(
        RectTransform parent, Vector2 size, float thickness, Color color)
    {
        float halfWidth = size.x / 2f;
        float halfHeight = size.y / 2f;
        float half = thickness / 2f;

        CreateImage(
            parent, "OutlineTop", new Vector2(0f, halfHeight - half),
            new Vector2(size.x, thickness), color
        );

        CreateImage(
            parent, "OutlineBottom", new Vector2(0f, -halfHeight + half),
            new Vector2(size.x, thickness), color
        );

        CreateImage(
            parent, "OutlineLeft", new Vector2(-halfWidth + half, 0f),
            new Vector2(thickness, size.y), color
        );

        CreateImage(
            parent, "OutlineRight", new Vector2(halfWidth - half, 0f),
            new Vector2(thickness, size.y), color
        );
    }

    public static Slider CreateSlider(
        Transform parent, string objectName, Vector2 position, Vector2 size,
        float value, UnityAction<float> onChanged)
    {
        RectTransform root = CreateRect(parent, objectName, position, size);

        RectTransform background = CreateStretched(
            root, "Background", new Vector2(0f, 0.3f), new Vector2(1f, 0.7f)
        );

        Image backgroundImage = background.gameObject.AddComponent<Image>();
        backgroundImage.color = new Color(0.25f, 0.25f, 0.3f, 1f);

        RectTransform fillArea = CreateStretched(
            root, "Fill Area", new Vector2(0f, 0.3f), new Vector2(1f, 0.7f)
        );

        RectTransform fill = CreateStretched(
            fillArea, "Fill", Vector2.zero, Vector2.one
        );

        Image fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.color = new Color(0.42f, 0.74f, 0.55f, 1f);

        RectTransform handleArea = CreateStretched(
            root, "Handle Slide Area", Vector2.zero, Vector2.one
        );

        float handleWidth = Application.isMobilePlatform ? 56f : 24f;

        handleArea.offsetMin = new Vector2(handleWidth / 2f, 0f);
        handleArea.offsetMax = new Vector2(-handleWidth / 2f, 0f);

        RectTransform handle = CreateStretched(
            handleArea, "Handle", Vector2.zero, Vector2.one
        );

        handle.sizeDelta = new Vector2(handleWidth, 0f);

        Image handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = Color.white;

        Slider slider = root.gameObject.AddComponent<Slider>();
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.SetValueWithoutNotify(value);
        slider.onValueChanged.AddListener(onChanged);

        return slider;
    }

    private static RectTransform CreateStretched(
        Transform parent, string objectName, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject rectObject = new GameObject(objectName, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)rectObject.transform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return rect;
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
