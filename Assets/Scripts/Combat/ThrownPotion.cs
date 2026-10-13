using System;
using UnityEngine;

public class ThrownPotion : MonoBehaviour
{
    private const float Speed = 9f;
    private const float MaxRange = 6.5f;
    private const float HitRadius = 0.25f;
    private const float SpinSpeed = 540f;
    private const float VisualSize = 0.6f;

    private ItemData item;
    private Vector2 direction;
    private int damage;
    private float travelled;
    private bool exploded;
    private Action<Enemy> onHit;

    public static ThrownPotion Throw(
        Vector2 position, Vector2 direction, ItemData item, int damage,
        Action<Enemy> onHit)
    {
        Sprite sprite = item.ItemSprite != null ? item.ItemSprite : PowerFx.Circle;

        Vector2 native = sprite.bounds.size;
        float scale = VisualSize / Mathf.Max(native.x, native.y, 0.0001f);

        SpriteRenderer renderer = PowerFx.CreateSprite(
            "Potion", sprite, Color.white, position, native * scale, 5,
            PowerFx.EffectParent
        );

        ThrownPotion potion = renderer.gameObject.AddComponent<ThrownPotion>();
        potion.item = item;
        potion.direction = direction.normalized;
        potion.damage = damage;
        potion.onHit = onHit;

        return potion;
    }

    private void Update()
    {
        if (exploded)
            return;

        float step = Speed * Time.deltaTime;
        Vector2 previous = transform.position;
        Vector2 position = previous + direction * step;

        transform.position = position;
        transform.Rotate(0f, 0f, SpinSpeed * Time.deltaTime);

        travelled += step;

        if (HitsWall(position))
        {
            Explode(previous);
            return;
        }

        if (HitsEnemy(position) || travelled >= MaxRange)
            Explode(position);
    }

    private bool HitsEnemy(Vector2 position)
    {
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(position, HitRadius))
        {
            Enemy enemy = hit.GetComponentInParent<Enemy>();

            if (enemy != null && enemy.CurrentHealth > 0)
                return true;
        }

        return false;
    }

    private static bool HitsWall(Vector2 position)
    {
        int mask = PhysicsLayers.WallMask;

        return mask != 0 && Physics2D.OverlapCircle(position, HitRadius * 0.7f, mask) != null;
    }

    private void Explode(Vector2 position)
    {
        if (exploded)
            return;

        exploded = true;

        float radius = item.AreaRadius;
        Color color = GetColor(item.PotionEffect);

        ZoneEffect.Spawn(
            position, radius, new Color(color.r, color.g, color.b, 0.4f),
            0.45f, false, 0.2f, 4
        );

        ZoneEffect.Spawn(position, radius, color, 0.35f, true, 0.3f, 6);

        foreach (Enemy enemy in PowerFx.GetEnemiesInRadius(position, radius))
            ApplyEffect(enemy);

        Destroy(gameObject);
    }

    private void ApplyEffect(Enemy enemy)
    {
        switch (item.PotionEffect)
        {
            case PotionEffect.Damage:
                enemy.TakeDamage(damage);
                NotifyHit(enemy);
                break;

            case PotionEffect.Slow:
                enemy.ApplySlow(item.SlowFactor, item.EffectSeconds);
                break;

            case PotionEffect.Poison:
                enemy.ApplyDot(
                    "poison", item.EffectSeconds, item.EffectDamage,
                    SpecialWeapons.PoisonTint
                );
                break;

            case PotionEffect.Fire:
                enemy.TakeDamage(damage);
                enemy.Burn(item.EffectSeconds, item.EffectDamage);
                NotifyHit(enemy);
                break;
        }
    }

    private void NotifyHit(Enemy enemy)
    {
        if (onHit != null)
            onHit(enemy);
    }

    private static Color GetColor(PotionEffect effect)
    {
        switch (effect)
        {
            case PotionEffect.Slow:
                return new Color(0.4f, 0.75f, 1f, 1f);

            case PotionEffect.Poison:
                return new Color(0.55f, 1f, 0.4f, 1f);

            case PotionEffect.Fire:
                return new Color(1f, 0.5f, 0.15f, 1f);

            default:
                return new Color(1f, 0.35f, 0.45f, 1f);
        }
    }
}
