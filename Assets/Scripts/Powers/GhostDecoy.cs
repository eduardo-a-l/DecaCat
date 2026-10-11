using UnityEngine;

public class GhostDecoy : MonoBehaviour
{
    private const float TauntRange = 10f;
    private const float ExplosionRadius = 2.4f;
    private const int ExplosionDamage = 12;
    private const float FireSeconds = 3f;

    private static readonly Color FireColor = new Color(0.4f, 1f, 0.7f, 0.45f);

    private SpriteRenderer spriteRenderer;
    private float lifetime;
    private float age;

    public static GhostDecoy Spawn(Vector2 position, Sprite sprite, float seconds)
    {
        GameObject root = new GameObject("GhostDecoy");

        if (PowerFx.EffectParent != null)
            root.transform.SetParent(PowerFx.EffectParent, true);

        root.transform.position = position;

        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite != null ? sprite : PowerFx.Circle;
        renderer.color = new Color(0.8f, 1f, 0.9f, 0.6f);
        renderer.sortingOrder = 1;

        GhostDecoy decoy = root.AddComponent<GhostDecoy>();
        decoy.spriteRenderer = renderer;
        decoy.lifetime = seconds;

        foreach (Enemy enemy in PowerFx.GetRoomEnemies())
        {
            if (Vector2.Distance(enemy.transform.position, position) <= TauntRange)
                enemy.Taunt(root.transform, seconds);
        }

        return decoy;
    }

    private void Update()
    {
        age += Time.deltaTime;

        float remaining = lifetime - age;
        float blink = remaining < 1f ? Mathf.PingPong(age * 8f, 1f) : 1f;

        spriteRenderer.color = new Color(0.8f, 1f, 0.9f, 0.35f + 0.3f * blink);

        if (remaining <= 0f)
            Explode();
    }

    private void Explode()
    {
        Vector2 position = transform.position;

        foreach (Enemy enemy in PowerFx.GetEnemiesInRadius(position, ExplosionRadius))
        {
            enemy.TakeDamage(ExplosionDamage);
            enemy.Burn(2f);
        }

        FirePatch.Spawn(position, ExplosionRadius * 0.9f, FireSeconds, FireColor, 2f);
        ZoneEffect.Spawn(position, ExplosionRadius, new Color(0.6f, 1f, 0.8f, 0.8f), 0.45f, true, 0.2f, 6);

        Destroy(gameObject);
    }
}
