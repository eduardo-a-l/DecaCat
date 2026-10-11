using UnityEngine;

public class ZoneEffect : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Color color;
    private float duration;
    private float age;
    private float startScale;
    private float endScale;

    public static ZoneEffect Spawn(
        Vector2 position, float radius, Color color, float duration,
        bool ring = false, float startFactor = 0.2f, int sortingOrder = -3)
    {
        Sprite sprite = ring ? PowerFx.Ring : PowerFx.Circle;

        SpriteRenderer renderer = PowerFx.CreateSprite(
            "Zone", sprite, color, position, Vector2.one * radius * 2f,
            sortingOrder, PowerFx.EffectParent
        );

        ZoneEffect effect = renderer.gameObject.AddComponent<ZoneEffect>();
        effect.spriteRenderer = renderer;
        effect.color = color;
        effect.duration = Mathf.Max(0.05f, duration);
        effect.endScale = renderer.transform.localScale.x;
        effect.startScale = effect.endScale * Mathf.Clamp01(startFactor);

        return effect;
    }

    private void Update()
    {
        age += Time.deltaTime;

        float t = Mathf.Clamp01(age / duration);
        float eased = 1f - (1f - t) * (1f - t);
        float scale = Mathf.Lerp(startScale, endScale, eased);

        transform.localScale = new Vector3(scale, scale, 1f);

        Color faded = color;
        faded.a = color.a * (1f - t);
        spriteRenderer.color = faded;

        if (age >= duration)
            Destroy(gameObject);
    }
}
