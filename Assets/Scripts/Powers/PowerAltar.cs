using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class PowerAltar : MonoBehaviour
{
    private const float TriggerRadius = 0.95f;
    private const float IconWorldSize = 0.95f;
    private const float IconHeight = 0f;
    private const float PromptGap = 0.45f;
    private const float PromptSize = 0.5f;
    private const int PromptSortingOrder = 10;

    private static readonly Color PromptBackground = new Color(0f, 0f, 0f, 0.75f);

    private PowerDefinition definition;
    private PermanentUpgradeType upgrade;
    private string label;
    private Action<PowerAltar> onTaken;
    private SpriteRenderer iconRenderer;
    private GameObject promptObject;
    private TextMeshPro promptText;
    private float bobOffset;

    public bool Taken { get; private set; }
    public bool IsUpgrade => definition == null;
    public PermanentUpgradeType Upgrade => upgrade;
    public PowerType PowerType => definition.Type;
    public PowerDefinition Definition => definition;

    public static PowerAltar Spawn(
        Vector2 position, Transform parent, PowerDefinition definition,
        Action<PowerAltar> onTaken)
    {
        PowerAltar altar = Build(
            position, parent, definition.Icon, definition.Accent,
            definition.DisplayName, onTaken
        );

        altar.definition = definition;

        return altar;
    }

    public static PowerAltar SpawnUpgrade(
        Vector2 position, Transform parent, PermanentUpgradeType type,
        Action<PowerAltar> onTaken)
    {
        PowerAltar altar = Build(
            position, parent, PermanentUpgradeCatalog.GetIcon(type),
            PermanentUpgrades.GetAccent(type), PermanentUpgrades.GetName(type),
            onTaken
        );

        altar.upgrade = type;

        return altar;
    }

    private static PowerAltar Build(
        Vector2 position, Transform parent, Sprite icon, Color accent,
        string label, Action<PowerAltar> onTaken)
    {
        GameObject root = new GameObject("PowerAltar");
        root.transform.SetParent(parent, false);
        root.transform.position = position;

        CircleCollider2D trigger = root.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = TriggerRadius;

        PowerFx.CreateSprite(
            "Pedestal", PowerFx.Circle, new Color(0.12f, 0.12f, 0.18f, 0.95f),
            position, Vector2.one * 1.15f, -3, root.transform
        );

        PowerFx.CreateSprite(
            "Ring", PowerFx.Ring, new Color(accent.r, accent.g, accent.b, 0.9f),
            position, Vector2.one * 1.4f, -2, root.transform
        );

        GameObject iconObject = new GameObject("Icon");
        iconObject.transform.SetParent(root.transform, false);
        iconObject.transform.localPosition = new Vector3(0f, IconHeight, 0f);

        SpriteRenderer iconRenderer = iconObject.AddComponent<SpriteRenderer>();
        iconRenderer.sprite = icon;
        iconRenderer.sortingOrder = 2;

        if (icon != null)
        {
            Vector2 size = icon.bounds.size;
            float scale = IconWorldSize / Mathf.Max(size.x, size.y, 0.0001f);

            iconObject.transform.localScale = Vector3.one * scale;
        }

        PowerAltar altar = root.AddComponent<PowerAltar>();
        altar.label = label;
        altar.onTaken = onTaken;
        altar.iconRenderer = iconRenderer;
        altar.bobOffset = UnityEngine.Random.value * 6f;
        altar.CreateNameLabel(root.transform);

        return altar;
    }

    public void Take()
    {
        if (Taken)
            return;

        Taken = true;

        if (onTaken != null)
            onTaken(this);

        Vanish();
    }

    public void Vanish()
    {
        Taken = true;

        SetPromptVisible(false, string.Empty);

        PlayerPower power = PlayerPower.Instance;

        if (power != null)
            power.ExitAltar(this);

        StartCoroutine(FadeOut());
    }

    public void SetPromptVisible(bool visible, string keyLabel)
    {
        if (!visible)
        {
            if (promptObject != null)
                promptObject.SetActive(false);

            return;
        }

        if (promptObject == null)
            CreatePrompt();

        promptObject.transform.localPosition =
            new Vector3(0f, IconHeight + IconWorldSize / 2f + PromptGap, 0f);

        if (promptText.text != keyLabel)
            promptText.text = keyLabel;

        promptObject.SetActive(true);
    }

    private void Update()
    {
        if (Taken || iconRenderer == null)
            return;

        float bob = Mathf.Sin(Time.time * 2.2f + bobOffset) * 0.08f;

        iconRenderer.transform.localPosition = new Vector3(0f, IconHeight + bob, 0f);
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();

        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;

            float alpha = 1f - elapsed / 0.5f;

            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                Color color = renderer.color;
                color.a = Mathf.Min(color.a, alpha);
                renderer.color = color;
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    private void CreateNameLabel(Transform parent)
    {
        GameObject labelObject = new GameObject("Name", typeof(RectTransform));
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.localPosition = new Vector3(0f, -0.8f, 0f);

        TextMeshPro text = labelObject.AddComponent<TextMeshPro>();
        text.text = label;
        text.fontSize = 2.6f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.sortingOrder = PromptSortingOrder;
        text.rectTransform.sizeDelta = new Vector2(3f, 0.6f);
    }

    private void CreatePrompt()
    {
        promptObject = new GameObject("Prompt");
        promptObject.transform.SetParent(transform, false);

        GameObject background = new GameObject("Background");
        background.transform.SetParent(promptObject.transform, false);
        background.transform.localScale = Vector3.one * PromptSize;

        SpriteRenderer backgroundRenderer = background.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = UIFactory.CircleSprite;
        backgroundRenderer.color = PromptBackground;
        backgroundRenderer.sortingOrder = PromptSortingOrder;

        GameObject keyObject = new GameObject("Key", typeof(RectTransform));
        keyObject.transform.SetParent(promptObject.transform, false);

        promptText = keyObject.AddComponent<TextMeshPro>();
        promptText.fontSize = 3.2f;
        promptText.fontStyle = FontStyles.Bold;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.color = Color.white;
        promptText.sortingOrder = PromptSortingOrder + 1;
        promptText.rectTransform.sizeDelta = new Vector2(PromptSize, PromptSize);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerPower power = other.GetComponentInParent<PlayerPower>();

        if (power != null && !Taken)
            power.EnterAltar(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerPower power = other.GetComponentInParent<PlayerPower>();

        if (power != null)
            power.ExitAltar(this);
    }
}
