using UnityEngine;

public class MiniRobot : Enemy
{
    private enum Mode
    {
        Resting,
        Moving,
        Winding,
        Fuse
    }

    private const float Speed = 6.5f;
    private const float ProjectileSpeed = 12f;
    private const float ProjectileSize = 0.25f;
    private const float WindupSeconds = 0.35f;
    private const float FuseSeconds = 1.1f;
    private const float ExplosionRadius = 2.8f;
    private const float ExplodeChance = 0.07f;
    private const float StuckDistanceFactor = 0.2f;
    private const int StuckSteps = 3;

    private static readonly Vector2[] Directions =
    {
        Vector2.left, Vector2.right, Vector2.up, Vector2.down
    };

    private static readonly Color ProjectileColor = new Color(1f, 0.85f, 0.2f, 1f);
    private static readonly Color ExplosionColor = new Color(1f, 0.4f, 0.1f, 1f);

    private Mode mode = Mode.Resting;
    private Vector2 moveDirection;
    private Vector2 lastPosition;
    private float timer;
    private int movesLeft;
    private int stuckSteps;

    protected override string DefaultDescription =>
        "A tiny, very fast robot that only runs in straight lines. After a few " +
        "dashes it stops and fires one quick shot. Very rarely it overloads " +
        "and blows up in a big blast.";

    public override string[] GetIndexStats()
    {
        return new[]
        {
            "Health 10",
            "Contact damage 1",
            "Fast single shot",
            "Rarely explodes"
        };
    }

    protected override void Awake()
    {
        base.Awake();

        maxHealth = ScaleHealth(10);
        currentHealth = maxHealth;
        contactDamage = ScaleDamage(1);
        moveSpeed = ScaleSpeed(Speed);
        movesLeft = Random.Range(2, 4);
        timer = Random.Range(0.2f, 0.6f);
    }

    protected override void MoveTowardsTarget()
    {
        float step = Time.fixedDeltaTime;

        switch (mode)
        {
            case Mode.Resting:
                Body.linearVelocity = Vector2.zero;
                timer -= step;

                if (timer <= 0f)
                    BeginMove();

                break;

            case Mode.Moving:
                UpdateMove(step);
                break;

            case Mode.Winding:
                Body.linearVelocity = Vector2.zero;
                timer -= step;

                if (timer <= 0f)
                {
                    Shoot();
                    movesLeft = Random.Range(2, 4);
                    mode = Mode.Resting;
                    timer = 0.5f;
                }

                break;

            case Mode.Fuse:
                Body.linearVelocity = Vector2.zero;
                timer -= step;

                if (timer <= 0f)
                    Explode();

                break;
        }
    }

    private void BeginMove()
    {
        movesLeft--;

        moveDirection = Directions[Random.Range(0, Directions.Length)];
        timer = Random.Range(0.35f, 0.75f);
        stuckSteps = 0;
        lastPosition = Body.position;
        mode = Mode.Moving;
    }

    private void UpdateMove(float step)
    {
        Body.linearVelocity = moveDirection * moveSpeed * SpeedMultiplier;

        timer -= step;

        float moved = (Body.position - lastPosition).magnitude;
        float expected = moveSpeed * SpeedMultiplier * step;

        lastPosition = Body.position;

        if (moved < expected * StuckDistanceFactor)
            stuckSteps++;
        else
            stuckSteps = 0;

        if (timer > 0f && stuckSteps < StuckSteps)
            return;

        if (movesLeft > 0)
        {
            BeginMove();
            return;
        }

        BeginStop();
    }

    private void BeginStop()
    {
        Body.linearVelocity = Vector2.zero;

        if (Random.value < ExplodeChance)
        {
            mode = Mode.Fuse;
            timer = FuseSeconds;

            PlayTelegraph(FuseSeconds, new Color(1f, 0.2f, 0.15f, 1f), 0.4f);
            GroundMarker.Spawn(
                Body.position, ExplosionRadius, FuseSeconds,
                new Color(1f, 0.25f, 0.2f, 0.45f)
            );

            return;
        }

        mode = Mode.Winding;
        timer = WindupSeconds;

        PlayTelegraph(WindupSeconds, new Color(1f, 0.95f, 0.4f, 1f), 0.2f);
    }

    private void Shoot()
    {
        Transform chased = GetTarget();

        if (chased == null)
            return;

        Vector2 toTarget = (Vector2)chased.position - Body.position;

        EnemyProjectile.Spawn(
            Body.position, toTarget, ProjectileSpeed,
            ScaleDamage(1), ProjectileSize, ProjectileColor
        );
    }

    private void Explode()
    {
        Vector2 center = Body.position;

        ZoneEffect.Spawn(
            center, ExplosionRadius, new Color(1f, 0.5f, 0.15f, 0.55f),
            0.5f, false, 0.2f, 4
        );

        ZoneEffect.Spawn(center, ExplosionRadius, ExplosionColor, 0.4f, true, 0.3f, 6);

        PlayerHealth hitPlayer = null;

        foreach (Collider2D hit in Physics2D.OverlapCircleAll(center, ExplosionRadius))
        {
            PlayerHealth player = hit.GetComponentInParent<PlayerHealth>();

            if (player != null)
                hitPlayer = player;
        }

        if (hitPlayer != null)
            hitPlayer.TakeDamage(ScaleDamage(1), DamageElement.None);

        Die();
    }
}
