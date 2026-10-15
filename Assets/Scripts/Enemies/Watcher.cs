using UnityEngine;

public class Watcher : Enemy
{
    private enum Phase
    {
        Tracking,
        Locked,
        Firing,
        Cooling
    }

    private const float TrackSeconds = 1.4f;
    private const float LockSeconds = 0.5f;
    private const float CoolSeconds = 1.6f;
    private const int ShotCount = 5;
    private const float ShotInterval = 0.12f;
    private const float ProjectileSpeed = 7.5f;
    private const float ProjectileSize = 0.3f;
    private const float MaxLineLength = 14f;
    private const float LineWidth = 0.09f;

    private static readonly Color ProjectileColor = new Color(1f, 0.3f, 0.25f, 1f);

    private Phase phase = Phase.Tracking;
    private Vector2 aim = Vector2.right;
    private float timer = TrackSeconds;
    private int shotsLeft;
    private SpriteRenderer line;

    protected override bool CanBeKnockedBack => false;

    protected override string DefaultDescription =>
        "Never moves. It follows you with its eye, then locks its aim and fires " +
        "five shots down that line. Step out of the line before it locks.";

    public override string[] GetIndexStats()
    {
        return new[]
        {
            "Health 50",
            "Contact damage 1",
            "Locks aim, then fires 5 shots"
        };
    }

    protected override void Awake()
    {
        base.Awake();

        maxHealth = ScaleHealth(50);
        currentHealth = maxHealth;
        contactDamage = ScaleDamage(1);
        moveSpeed = 0f;
        timer = Random.Range(0.6f, TrackSeconds);

        line = PowerFx.CreateSprite(
            "AimLine", PowerFx.Circle, new Color(1f, 0.2f, 0.2f, 0f),
            transform.position, new Vector2(0.1f, LineWidth), 1,
            transform.parent != null ? transform.parent : PowerFx.EffectParent
        );
    }

    private void OnDestroy()
    {
        if (line != null)
            Destroy(line.gameObject);
    }

    private void Update()
    {
        if (isDead || line == null)
            return;

        bool visible = phase == Phase.Tracking || phase == Phase.Locked;

        if (!visible)
        {
            line.enabled = false;
            return;
        }

        line.enabled = true;

        Vector2 origin = Body.position;
        float length = MaxLineLength;

        RaycastHit2D hit = Physics2D.Raycast(
            origin, aim, MaxLineLength, PhysicsLayers.WallMask
        );

        if (hit.collider != null)
            length = hit.distance;

        float alpha = phase == Phase.Locked
            ? 0.55f + 0.4f * Mathf.Sin(Time.time * 40f)
            : 0.25f;

        line.color = new Color(1f, 0.2f, 0.2f, alpha);

        Transform lineTransform = line.transform;

        lineTransform.position = origin + aim * (length * 0.5f);
        lineTransform.rotation =
            Quaternion.Euler(0f, 0f, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg);
        lineTransform.localScale = new Vector3(length, LineWidth, 1f);
    }

    protected override void MoveTowardsTarget()
    {
        Body.linearVelocity = Vector2.zero;

        float step = Time.fixedDeltaTime;

        switch (phase)
        {
            case Phase.Tracking:
                Transform chased = GetTarget();

                if (chased != null)
                {
                    Vector2 toTarget = (Vector2)chased.position - Body.position;

                    if (toTarget.sqrMagnitude > 0.0001f)
                        aim = toTarget.normalized;
                }

                timer -= step;

                if (timer <= 0f)
                {
                    phase = Phase.Locked;
                    timer = LockSeconds;

                    PlayTelegraph(LockSeconds, new Color(1f, 0.25f, 0.2f, 1f), 0.2f);
                }

                break;

            case Phase.Locked:
                timer -= step;

                if (timer <= 0f)
                {
                    phase = Phase.Firing;
                    shotsLeft = ShotCount;
                    timer = 0f;
                }

                break;

            case Phase.Firing:
                timer -= step;

                if (timer <= 0f)
                {
                    EnemyProjectile.Spawn(
                        Body.position + aim * 0.4f, aim, ProjectileSpeed,
                        ScaleDamage(1), ProjectileSize, ProjectileColor
                    );

                    shotsLeft--;
                    timer = ShotInterval;

                    if (shotsLeft <= 0)
                    {
                        phase = Phase.Cooling;
                        timer = CoolSeconds;
                    }
                }

                break;

            case Phase.Cooling:
                timer -= step;

                if (timer <= 0f)
                {
                    phase = Phase.Tracking;
                    timer = TrackSeconds;
                }

                break;
        }
    }
}
