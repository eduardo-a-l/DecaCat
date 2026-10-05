using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameHud : MonoBehaviour
{
    private const int IconPixels = 64;

    [Header("Drag your sprites here (empty = placeholder)")]
    [SerializeField] private Sprite pauseSprite;
    [SerializeField] private Sprite mapSprite;

    [Header("Text")]
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private string floorFormat = "Floor {0}";

    [SerializeField] private GameManager gameManager;

    private TextMeshProUGUI floorLabel;
    private int shownFloor = -1;

    private void Awake()
    {
        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        BuildFloorLabel();
        BuildPauseButton();
        BuildMapButton();
    }

    private void Update()
    {
        int floor = gameManager != null ? gameManager.CurrentFloor : 1;

        if (floor == shownFloor)
            return;

        shownFloor = floor;
        floorLabel.text = string.Format(floorFormat, floor);
    }

    private void BuildFloorLabel()
    {
        RectTransform rect = CreateTopRect(
            "FloorLabel", new Vector2(0.5f, 1f),
            new Vector2(0f, -90f), new Vector2(700f, 90f)
        );

        floorLabel = rect.gameObject.AddComponent<TextMeshProUGUI>();
        floorLabel.fontSize = 56f;
        floorLabel.alignment = TextAlignmentOptions.Center;
        floorLabel.color = Color.white;
        floorLabel.raycastTarget = false;

        if (font != null)
            floorLabel.font = font;
    }

    private void BuildPauseButton()
    {
        Sprite sprite = pauseSprite != null ? pauseSprite : CreatePauseSprite();

        CreateIconButton(
            "PauseButton", sprite,
            new Vector2(-91f, -103f), new Vector2(90f, 90f),
            () =>
            {
                if (gameManager != null)
                    gameManager.TogglePause();
            }
        );
    }

    private void BuildMapButton()
    {
        Sprite sprite = mapSprite != null ? mapSprite : CreateMapSprite();

        CreateIconButton(
            "MapButton", sprite,
            new Vector2(-91f, -213f), new Vector2(80f, 70f),
            () =>
            {
                if (gameManager != null)
                    gameManager.ToggleMap();
            }
        );

        RectTransform label = CreateTopRect(
            "MapLabel", new Vector2(1f, 1f),
            new Vector2(-91f, -268f), new Vector2(140f, 40f)
        );

        TextMeshProUGUI text = label.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = "Map";
        text.fontSize = 34f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;

        if (font != null)
            text.font = font;
    }

    private RectTransform CreateTopRect(
        string objectName, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject rectObject = new GameObject(objectName, typeof(RectTransform));
        rectObject.transform.SetParent(transform, false);

        RectTransform rect = (RectTransform)rectObject.transform;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        return rect;
    }

    private void CreateIconButton(
        string objectName, Sprite sprite, Vector2 position, Vector2 size,
        UnityEngine.Events.UnityAction onClick)
    {
        RectTransform rect = CreateTopRect(
            objectName, new Vector2(1f, 1f), position, size
        );

        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.65f, 0.65f, 0.65f, 1f);
        button.colors = colors;

        button.onClick.AddListener(onClick);
    }

    private static Sprite CreatePauseSprite()
    {
        Texture2D texture = NewIconTexture();
        Color white = Color.white;
        float radius = IconPixels * 0.5f;
        Vector2 center = new Vector2(radius, radius);

        for (int y = 0; y < IconPixels; y++)
        {
            for (int x = 0; x < IconPixels; x++)
            {
                float distance = Vector2.Distance(
                    new Vector2(x + 0.5f, y + 0.5f), center
                );

                float ring =
                    Mathf.Clamp01(radius - 2f - distance) *
                    Mathf.Clamp01(distance - (radius - 8f));

                bool bar =
                    y >= 20 && y <= 43 &&
                    ((x >= 22 && x <= 28) || (x >= 36 && x <= 42));

                float alpha = bar ? 1f : ring;

                texture.SetPixel(x, y, new Color(white.r, white.g, white.b, alpha));
            }
        }

        return FinishIcon(texture);
    }

    private static Sprite CreateMapSprite()
    {
        Texture2D texture = NewIconTexture();
        Color fill = new Color(1f, 0.72f, 0.42f, 1f);
        Color fold = new Color(0.85f, 0.55f, 0.25f, 1f);

        for (int y = 0; y < IconPixels; y++)
        {
            for (int x = 0; x < IconPixels; x++)
            {
                bool inside = x >= 6 && x <= 57 && y >= 12 && y <= 51;
                bool seam = x == 23 || x == 24 || x == 39 || x == 40;

                Color color = inside ? (seam ? fold : fill) : Color.clear;

                texture.SetPixel(x, y, color);
            }
        }

        return FinishIcon(texture);
    }

    private static Texture2D NewIconTexture()
    {
        Texture2D texture = new Texture2D(
            IconPixels, IconPixels, TextureFormat.RGBA32, false
        );

        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        return texture;
    }

    private static Sprite FinishIcon(Texture2D texture)
    {
        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, IconPixels, IconPixels),
            new Vector2(0.5f, 0.5f),
            IconPixels
        );
    }
}
