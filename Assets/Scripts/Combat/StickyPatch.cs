using System.Collections.Generic;
using UnityEngine;

public class StickyPatch : MonoBehaviour
{
    private const float CheckInterval = 0.15f;
    private const float FadeTime = 0.6f;
    private const float MergeDistance = 0.35f;
    private const int MaxPatches = 14;

    private static readonly List<StickyPatch> active = new List<StickyPatch>();

    private SpriteRenderer spriteRenderer;
    private Color color;
    private float radius;
    private float lifetime;
    private float slowFactor;
    private float age;
    private float nextCheck;

    public static StickyPatch Spawn(
        Vector2 position, float radius, float lifetime, float slowFactor)
    {
        active.RemoveAll(patch => patch == null);

        foreach (StickyPatch existing in active)
        {
            if (((Vector2)existing.transform.position - position).magnitude < MergeDistance)
            {
                existing.age = 0f;
                return existing;
            }
        }

        while (active.Count >= MaxPatches)
        {
            Destroy(active[0].gameObject);
            active.RemoveAt(0);
        }

        Color color = new Color(1f, 0.45f, 0.75f, 0.55f);

        SpriteRenderer renderer = PowerFx.CreateSprite(
            "StickyPatch", PowerFx.Circle, color, position,
            Vector2.one * radius * 2f, -4, PowerFx.EffectParent
        );

        StickyPatch patch = renderer.gameObject.AddComponent<StickyPatch>();
        patch.spriteRenderer = renderer;
        patch.color = color;
        patch.radius = radius;
        patch.lifetime = Mathf.Max(0.5f, lifetime);
        patch.slowFactor = slowFactor;

        active.Add(patch);

        return patch;
    }

    private void OnDestroy()
    {
        active.Remove(this);
    }

    private void Update()
    {
        age += Time.deltaTime;

        if (age >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        float fade = Mathf.Clamp01((lifetime - age) / FadeTime);

        Color current = color;
        current.a = color.a * fade;
        spriteRenderer.color = current;

        if (Time.time < nextCheck)
            return;

        nextCheck = Time.time + CheckInterval;

        foreach (Enemy enemy in PowerFx.GetEnemiesInRadius(transform.position, radius))
            enemy.ApplySlow(slowFactor, 0.4f);
    }
}
