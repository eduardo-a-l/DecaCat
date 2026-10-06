using UnityEngine;

public class GroundMarker : MonoBehaviour
{
    private const int SortingOrder = -3;
    private const float OuterAlpha = 0.5f;

    private Transform fill;
    private SpriteRenderer outerRenderer;
    private SpriteRenderer fillRenderer;
    private float duration;
    private float elapsed;

    public static void Spawn(
        Vector3 position, float radius, float duration, Color color)
    {
        GameObject markerObject = new GameObject("GroundMarker");
        markerObject.transform.position = position;
        markerObject.transform.localScale = Vector3.one * (radius * 2f);

        GroundMarker marker = markerObject.AddComponent<GroundMarker>();
        marker.Begin(color, Mathf.Max(0.05f, duration));
    }

    private void Begin(Color color, float markerDuration)
    {
        duration = markerDuration;

        outerRenderer = CreateCircle("Outer", transform, color, OuterAlpha);

        fillRenderer = CreateCircle("Fill", transform, color, 1f);
        fill = fillRenderer.transform;
        fill.localScale = Vector3.zero;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        float progress = Mathf.Clamp01(elapsed / duration);

        fill.localScale = Vector3.one * progress;

        if (progress >= 1f)
            Destroy(gameObject);
    }

    private static SpriteRenderer CreateCircle(
        string objectName, Transform parent, Color color, float alphaScale)
    {
        GameObject circle = new GameObject(objectName);
        circle.transform.SetParent(parent, false);

        SpriteRenderer spriteRenderer = circle.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = UIFactory.CircleSprite;
        spriteRenderer.sortingOrder = SortingOrder;

        Color tint = color;
        tint.a = color.a * alphaScale;
        spriteRenderer.color = tint;

        return spriteRenderer;
    }
}
