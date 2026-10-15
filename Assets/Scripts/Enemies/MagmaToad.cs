using UnityEngine;

public class MagmaToad : Enemy
{
    private enum Mode
    {
        Resting,
        Swelling,
        Hopping
    }

    private const float SwellSeconds = 0.9f;
    private const float HopSeconds = 0.55f;
    private const float RestSeconds = 0.9f;
    private const float FireballSpeed = 5.5f;
    private const float FireballSize = 0.34f;
    private const float HopHeightScale = 0.35f;
    private const float MaxHopSpeed = 12f;
    private const float StuckDistanceFactor = 0.25f;
    private const int StuckSteps = 3;

    private static readonly Color FireballColor = new Color(1f, 0.5f, 0.1f, 1f);

    private Room room;
    private Mode mode = Mode.Resting;
    private float timer;
    private float hopElapsed;
    private Vector2 hopTarget;
    private Vector2 hopVelocity;
    private Vector2 lastPosition;
    private int stuckSteps;
    private Vector3 baseScale;

    protected override string DefaultDescription =>
        "A squat toad that swells up, then leaps to where you were standing. " +
        "When it lands it spits four fireballs out in an X shape.";

    public override string[] GetIndexStats()
    {
        return new[]
        {
            "Health 15",
            "Contact damage 1",
            "Hops to your last position",
            "Lands with an X of 4 fireballs"
        };
    }

    protected override void Awake()
    {
        base.Awake();

        maxHealth = ScaleHealth(15);
        currentHealth = maxHealth;
        contactDamage = ScaleDamage(1);
        timer = Random.Range(0.4f, 1.2f);
        baseScale = transform.localScale;
    }

    private void Start()
    {
        room = GetComponentInParent<Room>();
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
                {
                    mode = Mode.Swelling;
                    timer = SwellSeconds;

                    PlayTelegraph(SwellSeconds, new Color(1f, 0.55f, 0.2f, 1f), 0.45f);
                }

                break;

            case Mode.Swelling:
                Body.linearVelocity = Vector2.zero;
                timer -= step;

                if (timer <= 0f)
                    BeginHop();

                break;

            case Mode.Hopping:
                UpdateHop(step);
                break;
        }
    }

    private void BeginHop()
    {
        Transform chased = GetTarget();

        hopTarget = chased != null ? (Vector2)chased.position : Body.position;

        if (room == null)
            room = GetComponentInParent<Room>();

        if (room != null)
        {
            Rect bounds = room.PlayBounds;

            hopTarget.x = Mathf.Clamp(hopTarget.x, bounds.xMin + 0.5f, bounds.xMax - 0.5f);
            hopTarget.y = Mathf.Clamp(hopTarget.y, bounds.yMin + 0.5f, bounds.yMax - 0.5f);
        }

        hopElapsed = 0f;
        stuckSteps = 0;
        lastPosition = Body.position;
        hopVelocity = Vector2.ClampMagnitude((hopTarget - Body.position) / HopSeconds, MaxHopSpeed);
        mode = Mode.Hopping;

        SetSpawnGrace(HopSeconds);
    }

    private void UpdateHop(float step)
    {
        hopElapsed += step;

        float progress = Mathf.Clamp01(hopElapsed / HopSeconds);

        transform.localScale = baseScale * (1f + HopHeightScale * Mathf.Sin(progress * Mathf.PI));

        float moved = (Body.position - lastPosition).magnitude;
        float expected = hopVelocity.magnitude * step;

        lastPosition = Body.position;

        if (expected > 0.0001f && moved < expected * StuckDistanceFactor)
            stuckSteps++;
        else
            stuckSteps = 0;

        if (progress < 1f && stuckSteps < StuckSteps)
        {
            Body.linearVelocity = hopVelocity;
            return;
        }

        transform.localScale = baseScale;
        Body.linearVelocity = Vector2.zero;

        Land();
    }

    private void Land()
    {
        for (int i = 0; i < 4; i++)
        {
            Vector2 direction = PowerFx.Rotate(Vector2.right, 45f + 90f * i);

            EnemyProjectile.Spawn(
                Body.position, direction, FireballSpeed, ScaleDamage(1),
                FireballSize, FireballColor, DamageElement.Fire
            );
        }

        ZoneEffect.Spawn(
            Body.position, 1f, new Color(1f, 0.5f, 0.1f, 0.6f), 0.3f, true, 0.3f, 6
        );

        mode = Mode.Resting;
        timer = RestSeconds;
    }
}
