using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PowerHud : MonoBehaviour
{
    private const int MaxCharges = PlayerPower.MaxCharges;

    private static readonly Color BackColor = new Color(0f, 0f, 0f, 0.5f);
    private static readonly Color CooldownColor = new Color(0f, 0f, 0f, 0.65f);
    private static readonly Color BlockedColor = new Color(0.6f, 0.1f, 0.1f, 0.65f);
    private static readonly Color PanelColor = new Color(0.04f, 0.04f, 0.07f, 0.88f);

    private static PowerHud instance;

    private PlayerPower power;
    private GameObject statusRoot;
    private Image icon;
    private Image cooldown;
    private TextMeshProUGUI charges;
    private TextMeshProUGUI status;
    private GameObject panelRoot;
    private Image panelIcon;
    private TextMeshProUGUI panelTitle;
    private TextMeshProUGUI panelBody;
    private TextMeshProUGUI panelHint;
    private PowerAltar shownAltar;
    private PowerDefinition shownDefinition;
    private bool shownReplace;

    public static void Create(PlayerPower owner)
    {
        if (instance != null)
            return;

        GameObject root = UIFactory.CreateOverlayCanvas("PowerHud", 8);

        PowerHud hud = root.AddComponent<PowerHud>();
        hud.power = owner;
        hud.Build(root.transform);

        instance = hud;
    }

    public static void ShowAltar(PowerAltar altar, bool replaces)
    {
        if (instance != null)
            instance.DisplayAltar(altar, replaces);
    }

    public static void Hide(PowerAltar altar)
    {
        if (instance != null && instance.shownAltar == altar)
            instance.DisplayAltar(null, false);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void Build(Transform root)
    {
        statusRoot = UIFactory.CreateRect(
            root, "Status", new Vector2(0f, 0f), new Vector2(520f, 140f)
        ).gameObject;

        RectTransform statusRect = (RectTransform)statusRoot.transform;
        statusRect.anchorMin = new Vector2(0f, 1f);
        statusRect.anchorMax = new Vector2(0f, 1f);
        statusRect.pivot = new Vector2(0f, 1f);
        statusRect.anchoredPosition = new Vector2(40f, -170f);

        CreateAnchoredImage(
            statusRoot.transform, "Back", new Vector2(60f, -70f),
            new Vector2(120f, 120f), BackColor, UIFactory.CircleSprite
        );

        icon = CreateAnchoredImage(statusRoot.transform, "Icon", new Vector2(60f, -70f), new Vector2(84f, 84f), Color.white, null);
        cooldown = CreateAnchoredImage(statusRoot.transform, "Cooldown", new Vector2(60f, -70f), new Vector2(120f, 120f), CooldownColor, UIFactory.CircleSprite);

        cooldown.type = Image.Type.Filled;
        cooldown.fillMethod = Image.FillMethod.Radial360;
        cooldown.fillOrigin = (int)Image.Origin360.Top;
        cooldown.fillClockwise = false;
        cooldown.fillAmount = 0f;

        charges = CreateAnchoredText(statusRoot.transform, "Charges", new Vector2(250f, -40f), new Vector2(240f, 56f), 44f, Color.white);
        status = CreateAnchoredText(statusRoot.transform, "Text", new Vector2(250f, -92f), new Vector2(400f, 44f), 28f, new Color(1f, 1f, 1f, 0.8f));

        charges.alignment = TextAlignmentOptions.Left;
        status.alignment = TextAlignmentOptions.Left;

        BuildPanel(root);

        statusRoot.SetActive(false);
        panelRoot.SetActive(false);
    }

    private void BuildPanel(Transform root)
    {
        RectTransform rect = UIFactory.CreateRect(
            root, "AltarPanel", Vector2.zero, new Vector2(1060f, 410f)
        );

        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -290f);

        panelRoot = rect.gameObject;

        Image back = panelRoot.AddComponent<Image>();
        back.color = PanelColor;
        back.raycastTarget = false;

        panelIcon = UIFactory.CreateImage(rect, "Icon", new Vector2(-440f, 120f), new Vector2(110f, 110f), Color.white);

        panelTitle = UIFactory.CreateText(
            rect, "Title", string.Empty, 50f, Color.white,
            new Vector2(40f, 150f), new Vector2(800f, 70f),
            null, TextAlignmentOptions.Left
        );

        panelBody = UIFactory.CreateText(
            rect, "Body", string.Empty, 26f, Color.white,
            new Vector2(0f, -45f), new Vector2(980f, 230f),
            null, TextAlignmentOptions.TopLeft
        );

        panelBody.enableAutoSizing = true;
        panelBody.fontSizeMin = 16f;
        panelBody.fontSizeMax = 26f;

        panelHint = UIFactory.CreateText(
            rect, "Hint", string.Empty, 28f, new Color(1f, 0.85f, 0.4f, 1f),
            new Vector2(0f, -182f), new Vector2(980f, 40f)
        );
    }

    private void Update()
    {
        bool has = power != null && power.HasPower;

        if (statusRoot.activeSelf != has)
            statusRoot.SetActive(has);

        if (!has)
            return;

        PowerDefinition definition = power.Current;

        icon.sprite = definition.Icon;
        icon.preserveAspect = true;
        icon.enabled = definition.Icon != null;

        charges.text = power.Charges + "/" + MaxCharges;

        float total = Mathf.Max(0.01f, power.SpecialCooldownTotal);
        float fraction = power.SpecialCooldownRemaining / total;

        cooldown.color = power.SpecialBlocked ? BlockedColor : CooldownColor;
        cooldown.fillAmount = power.SpecialBlocked ? 1f : Mathf.Clamp01(fraction);

        string line = power.StatusText;

        if (string.IsNullOrEmpty(line) && !Application.isMobilePlatform)
            line = "Special: Space";

        status.text = line;
    }

    private void DisplayAltar(PowerAltar altar, bool replaces)
    {
        if (altar == shownAltar && replaces == shownReplace)
            return;

        shownAltar = altar;
        shownReplace = replaces;

        if (altar == null || altar.Definition == null)
        {
            panelRoot.SetActive(false);
            return;
        }

        PowerDefinition definition = altar.Definition;

        if (definition != shownDefinition)
            shownDefinition = definition;

        panelIcon.sprite = definition.Icon;
        panelIcon.preserveAspect = true;
        panelIcon.enabled = definition.Icon != null;

        panelTitle.text = definition.DisplayName;
        panelTitle.color = Color.Lerp(Color.white, definition.Accent, 0.7f);

        panelBody.text =
            "<b><color=#8fe3ff>Passive</color></b>  " + definition.Passive + "\n" +
            "<b><color=#9dff9d>Special</color></b>  " + definition.Special + "\n" +
            "<b><color=#ff9a9a>Negative</color></b>  " + definition.Negative;

        string take = Application.isMobilePlatform ? "Tap E to take it" : "Press E to take it";

        panelHint.text = replaces ? take + "  (replaces your current power)" : take;

        panelRoot.SetActive(true);
    }

    private static Image CreateAnchoredImage(
        Transform parent, string objectName, Vector2 position, Vector2 size,
        Color color, Sprite sprite)
    {
        Image image = UIFactory.CreateImage(parent, objectName, position, size, color, sprite);
        AnchorTopLeft(image.rectTransform);
        image.rectTransform.anchoredPosition = position;

        return image;
    }

    private static TextMeshProUGUI CreateAnchoredText(
        Transform parent, string objectName, Vector2 position, Vector2 size,
        float fontSize, Color color)
    {
        TextMeshProUGUI text = UIFactory.CreateText(parent, objectName, string.Empty, fontSize, color, position, size);
        AnchorTopLeft(text.rectTransform);
        text.rectTransform.anchoredPosition = position;

        return text;
    }

    private static void AnchorTopLeft(RectTransform rect)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
    }
}
