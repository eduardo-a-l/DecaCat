using System.Collections.Generic;
using UnityEngine;

public class CrystalObstacle : MonoBehaviour
{
    private const float Size = 0.9f;
    private const float BreakDistance = 1.15f;
    private const float BlinkTime = 1.5f;

    private static readonly List<CrystalObstacle> Active = new List<CrystalObstacle>();

    private static readonly Color BodyColor = new Color(0.95f, 0.65f, 1f, 1f);

    private SpriteRenderer spriteRenderer;
    private Transform breaker;
    private float lifetime;
    private float age;

    public static int ActiveCount
    {
        get
        {
            Active.RemoveAll(crystal => crystal == null);

            return Active.Count;
        }
    }

    public static CrystalObstacle Spawn(Vector2 position, float lifetime, Transform breaker)
    {
        SpriteRenderer renderer = PowerFx.CreateSprite(
            "CrystalObstacle", PowerFx.Square, BodyColor, position,
            Vector2.one * Size, 1, PowerFx.EffectParent
        );

        renderer.transform.rotation = Quaternion.Euler(0f, 0f, 45f);

        GameObject root = renderer.gameObject;

        PhysicsLayers.SetLayer(root, PhysicsLayers.ObstacleLayer);

        BoxCollider2D box = root.AddComponent<BoxCollider2D>();
        box.size = Vector2.one * 0.8f;

        CrystalObstacle crystal = root.AddComponent<CrystalObstacle>();
        crystal.spriteRenderer = renderer;
        crystal.breaker = breaker;
        crystal.lifetime = lifetime;

        Active.Add(crystal);

        ZoneEffect.Spawn(position, 0.7f, new Color(1f, 0.8f, 1f, 0.6f), 0.35f, true, 0.3f, 6);

        return crystal;
    }

    private void OnDestroy()
    {
        Active.Remove(this);
    }

    private void Update()
    {
        age += Time.deltaTime;

        if (age >= lifetime)
        {
            Shatter();
            return;
        }

        if (breaker != null &&
            Vector2.Distance(breaker.position, transform.position) < BreakDistance)
        {
            Shatter();
            return;
        }

        float remaining = lifetime - age;

        Color color = BodyColor;

        if (remaining < BlinkTime)
            color.a = 0.5f + 0.5f * Mathf.Sin(Time.time * 20f);

        spriteRenderer.color = color;
    }

    private void Shatter()
    {
        ZoneEffect.Spawn(
            transform.position, 0.9f, new Color(1f, 0.8f, 1f, 0.7f), 0.3f, true, 0.3f, 6
        );

        Destroy(gameObject);
    }
}
