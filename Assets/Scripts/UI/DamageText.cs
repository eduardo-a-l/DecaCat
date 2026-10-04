using TMPro;
using UnityEngine;

public class DamageText : MonoBehaviour
{
    private const float Lifetime = 0.8f;
    private const float RiseDistance = 0.4f;
    private const float FontSize = 4f;
    private const int SortingOrder = 100;

    private static readonly Color TextColor = new Color(1f, 0.4f, 0.35f, 1f);

    private TextMeshPro text;
    private Vector3 startPosition;
    private float elapsed;

    public static void Spawn(Vector3 position, int amount)
    {
        GameObject textObject = new GameObject(
            "DamageText", typeof(RectTransform)
        );

        textObject.transform.position = position;

        DamageText damageText = textObject.AddComponent<DamageText>();
        damageText.Setup(amount);
    }

    private void Setup(int amount)
    {
        startPosition = transform.position;

        text = gameObject.AddComponent<TextMeshPro>();
        text.text = "-" + amount;
        text.fontSize = FontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.color = TextColor;
        text.sortingOrder = SortingOrder;
        text.rectTransform.sizeDelta = new Vector2(3f, 1f);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        float progress = Mathf.Clamp01(elapsed / Lifetime);

        transform.position =
            startPosition + Vector3.up * (RiseDistance * progress);

        Color color = TextColor;
        color.a = Mathf.Clamp01((1f - progress) * 2f);
        text.color = color;

        if (progress >= 1f)
            Destroy(gameObject);
    }
}
