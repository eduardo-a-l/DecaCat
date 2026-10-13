using UnityEngine;

public class Turtle : Enemy
{
    private const float Speed = 0.9f;
    private const float FireInterval = 2.2f;
    private const float WindupSeconds = 0.4f;
    private const float ProjectileSpeed = 7.5f;
    private const float ProjectileSize = 0.3f;

    private static readonly Color ProjectileColor = new Color(0.35f, 0.85f, 0.3f, 1f);

    private float fireTimer;
    private float windupTimer;
    private bool diagonal;

    protected override string DefaultDescription =>
        "Plods slowly toward you. Every couple of seconds it fires four fast " +
        "shots, switching between the straight and diagonal directions.";

    public override string[] GetIndexStats()
    {
        return new[]
        {
            "Health 40",
            "Contact damage 1",
            "Fires 4 fast shots, + then x"
        };
    }

    protected override void Awake()
    {
        base.Awake();

        maxHealth = ScaleHealth(40);
        currentHealth = maxHealth;
        contactDamage = ScaleDamage(1);
        moveSpeed = ScaleSpeed(Speed);
        fireTimer = Random.Range(1f, 2f);
        diagonal = Random.value < 0.5f;
    }

    protected override void MoveTowardsTarget()
    {
        float step = Time.fixedDeltaTime;

        if (windupTimer > 0f)
        {
            Body.linearVelocity = Vector2.zero;
            windupTimer -= step;

            if (windupTimer <= 0f)
                FireVolley();

            return;
        }

        fireTimer -= step;

        if (fireTimer <= 0f)
        {
            fireTimer = FireInterval;
            windupTimer = WindupSeconds;
            Body.linearVelocity = Vector2.zero;

            PlayTelegraph(WindupSeconds, new Color(1f, 0.95f, 0.4f, 1f), 0.2f);
            return;
        }

        base.MoveTowardsTarget();
    }

    private void FireVolley()
    {
        float offset = diagonal ? 45f : 0f;

        for (int i = 0; i < 4; i++)
        {
            Vector2 direction = PowerFx.Rotate(Vector2.right, offset + 90f * i);

            EnemyProjectile.Spawn(
                Body.position, direction, ProjectileSpeed,
                ScaleDamage(1), ProjectileSize, ProjectileColor
            );
        }

        diagonal = !diagonal;
    }
}
