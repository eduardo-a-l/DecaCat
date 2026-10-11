using System.Collections.Generic;
using UnityEngine;

public class ProjectileSettings
{
    public Vector2 Direction = Vector2.right;
    public float Speed = 10f;
    public int Damage = 1;
    public float Radius = 0.25f;
    public Vector2 Size = new Vector2(0.4f, 0.4f);
    public float Lifetime = 1.5f;
    public Color Color = Color.white;
    public Sprite Sprite;
    public bool Homing;
    public bool IgnoreWalls;
    public bool Pierce;
    public float Knockback;
    public float StunSeconds;
    public int SplitCount;
    public int SplitDamage = 1;
    public bool Amplifiable;
    public bool Amplified;
    public Enemy Ignore;

    public ProjectileSettings Clone()
    {
        return (ProjectileSettings)MemberwiseClone();
    }
}

public class PowerProjectile : MonoBehaviour
{
    private const float RetargetInterval = 0.2f;
    private const float IgnoreSeconds = 0.3f;
    private const float HomingRange = 9f;

    private readonly HashSet<Enemy> hitEnemies = new HashSet<Enemy>();

    private ProjectileSettings settings;
    private Vector2 velocity;
    private float age;
    private float nextRetarget;
    private Enemy homingTarget;

    public ProjectileSettings Settings => settings;
    public Vector2 Velocity => velocity;

    public static PowerProjectile Spawn(Vector2 position, ProjectileSettings settings)
    {
        Sprite sprite = settings.Sprite != null ? settings.Sprite : PowerFx.Circle;

        SpriteRenderer renderer = PowerFx.CreateSprite(
            "Projectile", sprite, settings.Color, position, settings.Size,
            5, PowerFx.EffectParent
        );

        PowerProjectile projectile = renderer.gameObject.AddComponent<PowerProjectile>();
        projectile.settings = settings;
        projectile.velocity = settings.Direction.normalized * settings.Speed;
        projectile.Face();

        return projectile;
    }

    private void Update()
    {
        float step = Time.deltaTime;

        age += step;

        if (age >= settings.Lifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (settings.Amplifiable && !settings.Amplified && CrystalPrism.TryAmplify(this))
            return;

        if (settings.Homing)
            Steer(step);

        Vector2 position = (Vector2)transform.position + velocity * step;

        transform.position = position;
        Face();

        if (CheckEnemies(position))
            return;

        if (!settings.IgnoreWalls)
            CheckWalls(position);
    }

    private bool CheckEnemies(Vector2 position)
    {
        foreach (Collider2D collider in Physics2D.OverlapCircleAll(position, settings.Radius))
        {
            Enemy enemy = collider.GetComponentInParent<Enemy>();

            if (enemy == null || enemy.CurrentHealth <= 0 || hitEnemies.Contains(enemy))
                continue;

            if (enemy == settings.Ignore && age < IgnoreSeconds)
                continue;

            hitEnemies.Add(enemy);

            enemy.TakeDamage(settings.Damage);

            if (settings.Knockback > 0f)
                enemy.Push(position - velocity.normalized * 0.5f, settings.Knockback);

            if (settings.StunSeconds > 0f)
                enemy.Stun(settings.StunSeconds);

            if (settings.Pierce)
                continue;

            Split(position, velocity.normalized, enemy);
            Destroy(gameObject);

            return true;
        }

        return false;
    }

    private void CheckWalls(Vector2 position)
    {
        int mask = PhysicsLayers.WallMask;

        if (mask == 0 || Physics2D.OverlapCircle(position, settings.Radius * 0.7f, mask) == null)
            return;

        Split(position, -velocity.normalized, null);
        Destroy(gameObject);
    }

    private void Split(Vector2 position, Vector2 baseDirection, Enemy ignore)
    {
        int count = settings.SplitCount;

        if (count <= 0)
            return;

        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : (float)i / (count - 1);
            float angle = Mathf.Lerp(-60f, 60f, t);

            Color beamColor = Color.Lerp(settings.Color, Color.white, 0.5f);

            ProjectileSettings beam = new ProjectileSettings
            {
                Direction = PowerFx.Rotate(baseDirection, angle),
                Speed = 9f,
                Damage = settings.SplitDamage,
                Radius = 0.2f,
                Size = new Vector2(0.28f, 0.14f),
                Lifetime = 1.6f,
                Color = beamColor,
                Homing = true,
                Ignore = ignore
            };

            Spawn(position, beam);
        }

        ZoneEffect.Spawn(position, 0.55f, settings.Color, 0.25f, true, 0.3f, 6);
    }

    private void Steer(float step)
    {
        if (Time.time >= nextRetarget)
        {
            nextRetarget = Time.time + RetargetInterval;

            Enemy exclude = age < IgnoreSeconds ? settings.Ignore : null;

            homingTarget = PowerFx.GetClosestEnemy(transform.position, HomingRange, exclude);
        }

        if (homingTarget == null)
            return;

        Vector2 desired =
            ((Vector2)homingTarget.transform.position - (Vector2)transform.position)
            .normalized * settings.Speed;

        velocity = Vector2.MoveTowards(velocity, desired, settings.Speed * 9f * step);
    }

    private void Face()
    {
        if (velocity.sqrMagnitude < 0.0001f)
            return;

        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}
