using System.Collections.Generic;
using UnityEngine;

public class CrystalPrism : MonoBehaviour
{
    private const float Radius = 0.7f;

    private static readonly List<CrystalPrism> Active = new List<CrystalPrism>();

    private SpriteRenderer body;
    private SpriteRenderer glow;
    private Vector3 bodyScale;
    private Color accent;
    private float lifetime;
    private float age;

    public static CrystalPrism Spawn(Vector2 position, float lifetime, Color accent)
    {
        GameObject root = new GameObject("CrystalPrism");

        if (PowerFx.EffectParent != null)
            root.transform.SetParent(PowerFx.EffectParent, true);

        root.transform.position = position;

        CrystalPrism prism = root.AddComponent<CrystalPrism>();
        prism.lifetime = lifetime;
        prism.accent = accent;

        prism.glow = PowerFx.CreateSprite(
            "Glow", PowerFx.Circle, new Color(accent.r, accent.g, accent.b, 0.25f),
            position, Vector2.one * Radius * 2.4f, 3, root.transform
        );

        prism.body = PowerFx.CreateSprite(
            "Body", PowerFx.Square, Color.Lerp(accent, Color.white, 0.4f),
            position, new Vector2(0.55f, 0.55f), 4, root.transform
        );

        prism.body.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
        prism.bodyScale = prism.body.transform.localScale;

        return prism;
    }

    public static bool TryAmplify(PowerProjectile projectile)
    {
        foreach (CrystalPrism prism in Active)
        {
            float distance =
                Vector2.Distance(projectile.transform.position, prism.transform.position);

            if (distance > Radius)
                continue;

            prism.Amplify(projectile);

            return true;
        }

        return false;
    }

    private void OnEnable()
    {
        Active.Add(this);
    }

    private void OnDisable()
    {
        Active.Remove(this);
    }

    private void Update()
    {
        age += Time.deltaTime;

        if (age >= lifetime)
        {
            ZoneEffect.Spawn(transform.position, 1f, accent, 0.4f, true);
            Destroy(gameObject);
            return;
        }

        float pulse = 1f + 0.12f * Mathf.Sin(age * 6f);
        float fade = Mathf.Clamp01((lifetime - age) / 1f);

        body.transform.rotation = Quaternion.Euler(0f, 0f, 45f + age * 40f);
        body.transform.localScale = bodyScale * pulse;
        body.color = new Color(1f, 1f, 1f, 0.55f + 0.45f * fade);
        glow.color = new Color(accent.r, accent.g, accent.b, 0.25f * fade);
    }

    private void Amplify(PowerProjectile projectile)
    {
        ProjectileSettings source = projectile.Settings;
        Vector2 direction = projectile.Velocity.normalized;

        for (int i = -1; i <= 1; i++)
        {
            ProjectileSettings amplified = source.Clone();

            amplified.Direction = PowerFx.Rotate(direction, i * 16f);
            amplified.Damage = Mathf.Max(1, Mathf.RoundToInt(source.Damage * 1.5f));
            amplified.SplitCount = 4;
            amplified.SplitDamage = source.SplitDamage + 1;
            amplified.Amplified = true;
            amplified.Amplifiable = false;
            amplified.Size = source.Size * 1.3f;
            amplified.Color = accent;
            amplified.Lifetime = Mathf.Max(source.Lifetime, 1.2f);

            PowerProjectile.Spawn(transform.position, amplified);
        }

        ZoneEffect.Spawn(transform.position, 0.8f, accent, 0.3f, true, 0.2f, 6);

        Destroy(projectile.gameObject);
    }
}
