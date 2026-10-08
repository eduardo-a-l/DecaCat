using UnityEngine;

public class SwingEffect : MonoBehaviour
{
    private const int SortingOrder = 2;
    private const float FadeStart = 0.35f;
    private const int SpritePixels = 64;
    private const float CutOffset = 0.22f;

    private static Sprite defaultSprite;

    private Transform follow;
    private SpriteRenderer slashRenderer;
    private Color baseColor;
    private float startAngle;
    private float endAngle;
    private float duration;
    private float elapsed;

    private static Sprite DefaultSprite
    {
        get
        {
            if (defaultSprite == null)
                defaultSprite = CreateCrescentSprite();

            return defaultSprite;
        }
    }

    public static void Play(Transform owner, float aimAngle, ItemData item)
    {
        Play(
            owner, aimAngle, item.AttackRange, item.AttackRadius,
            item.SwingAngle, item.SwingDuration,
            item.SwingEffectSprite, item.SwingEffectColor
        );
    }

    public static void Play(
        Transform owner, float aimAngle, float range, float radius,
        float swingAngle, float swingDuration, Sprite sprite, Color color)
    {
        GameObject pivot = new GameObject("SwingEffect");
        pivot.transform.position = owner.position;

        SwingEffect effect = pivot.AddComponent<SwingEffect>();

        effect.Begin(
            owner, aimAngle, range, radius,
            swingAngle, swingDuration, sprite, color
        );
    }

    private void Begin(
        Transform owner, float aimAngle, float range, float radius,
        float swingAngle, float swingDuration, Sprite effectSprite, Color color)
    {
        follow = owner;
        startAngle = aimAngle - swingAngle / 2f;
        endAngle = aimAngle + swingAngle / 2f;
        duration = Mathf.Max(0.05f, swingDuration);
        baseColor = color;

        Sprite sprite = effectSprite != null ? effectSprite : DefaultSprite;

        GameObject slash = new GameObject("Slash");
        slash.transform.SetParent(transform, false);
        slash.transform.localPosition = new Vector3(range, 0f, 0f);

        float width = Mathf.Max(0.01f, sprite.bounds.size.x);
        slash.transform.localScale = Vector3.one * (radius * 2f / width);

        slashRenderer = slash.AddComponent<SpriteRenderer>();
        slashRenderer.sprite = sprite;
        slashRenderer.color = baseColor;
        slashRenderer.sortingOrder = SortingOrder;

        ApplyAngle(0f);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        float progress = Mathf.Clamp01(elapsed / duration);

        if (follow != null)
            transform.position = follow.position;

        ApplyAngle(progress);

        Color color = baseColor;
        color.a = baseColor.a * (1f - Mathf.InverseLerp(FadeStart, 1f, progress));
        slashRenderer.color = color;

        if (progress >= 1f)
            Destroy(gameObject);
    }

    private void ApplyAngle(float progress)
    {
        transform.rotation = Quaternion.Euler(
            0f, 0f, Mathf.Lerp(startAngle, endAngle, progress)
        );
    }

    private static Sprite CreateCrescentSprite()
    {
        Texture2D texture = new Texture2D(
            SpritePixels, SpritePixels, TextureFormat.RGBA32, false
        );

        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        float radius = SpritePixels * 0.5f;
        Vector2 outerCenter = new Vector2(radius, radius);
        Vector2 cutCenter = new Vector2(radius - CutOffset * SpritePixels, radius);
        Color[] pixels = new Color[SpritePixels * SpritePixels];

        for (int y = 0; y < SpritePixels; y++)
        {
            for (int x = 0; x < SpritePixels; x++)
            {
                Vector2 point = new Vector2(x + 0.5f, y + 0.5f);

                float outer = Mathf.Clamp01(
                    radius - Vector2.Distance(point, outerCenter)
                );

                float cut = Mathf.Clamp01(
                    Vector2.Distance(point, cutCenter) - radius + 0.5f
                );

                pixels[y * SpritePixels + x] =
                    new Color(1f, 1f, 1f, outer * cut);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, SpritePixels, SpritePixels),
            new Vector2(0.5f, 0.5f),
            SpritePixels
        );
    }
}
