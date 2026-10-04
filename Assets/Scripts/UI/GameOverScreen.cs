using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverScreen : MonoBehaviour
{
    private const float FadeDuration = 0.5f;
    private const int SortingOrder = 100;

    private static readonly Color BackgroundColor = new Color(0f, 0f, 0f, 0.8f);
    private static readonly Color TitleColor = new Color(0.9f, 0.25f, 0.25f, 1f);
    private static readonly Color HintColor = new Color(1f, 1f, 1f, 0.6f);
    private static readonly Color ButtonNormal = new Color(0.2f, 0.2f, 0.2f, 1f);
    private static readonly Color ButtonHighlighted = new Color(0.35f, 0.35f, 0.35f, 1f);
    private static readonly Color ButtonPressed = new Color(0.12f, 0.12f, 0.12f, 1f);

    private CanvasGroup canvasGroup;
    private float elapsed;

    public static GameOverScreen Create(Action onRestart)
    {
        GameObject root = new GameObject(
            "GameOverScreen",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup)
        );

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        CanvasGroup group = root.GetComponent<CanvasGroup>();
        group.alpha = 0f;

        GameOverScreen screen = root.AddComponent<GameOverScreen>();
        screen.canvasGroup = group;

        CreateBackground(root.transform);

        CreateText(
            root.transform, "Title", "GAME OVER", 120f, TitleColor,
            new Vector2(0f, 140f), new Vector2(1200f, 160f)
        );

        CreateButton(root.transform, onRestart);

        CreateText(
            root.transform, "Hint", "or press R", 32f, HintColor,
            new Vector2(0f, -170f), new Vector2(600f, 50f)
        );

        return screen;
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        canvasGroup.alpha = Mathf.Clamp01(elapsed / FadeDuration);
    }

    private static void CreateBackground(Transform parent)
    {
        GameObject background = new GameObject(
            "Background", typeof(RectTransform), typeof(Image)
        );

        background.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)background.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        background.GetComponent<Image>().color = BackgroundColor;
    }

    private static void CreateButton(Transform parent, Action onRestart)
    {
        GameObject buttonObject = new GameObject(
            "RestartButton",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button)
        );

        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)buttonObject.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -40f);
        rect.sizeDelta = new Vector2(420f, 100f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = Color.white;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = ButtonNormal;
        colors.highlightedColor = ButtonHighlighted;
        colors.selectedColor = ButtonHighlighted;
        colors.pressedColor = ButtonPressed;
        button.colors = colors;

        button.onClick.AddListener(() =>
        {
            if (onRestart != null)
                onRestart();
        });

        CreateText(
            buttonObject.transform, "Label", "Restart", 52f, Color.white,
            Vector2.zero, rect.sizeDelta
        );
    }

    private static void CreateText(
        Transform parent, string objectName, string content,
        float fontSize, Color color, Vector2 position, Vector2 size)
    {
        GameObject textObject = new GameObject(
            objectName, typeof(RectTransform)
        );

        textObject.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)textObject.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.raycastTarget = false;
    }
}
