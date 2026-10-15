using UnityEngine;

public class Jellyfish : Enemy
{
    private enum Mode
    {
        Hovering,
        Winding,
        Firing,
        Resting
    }

    private const float Speed = 2.6f;
    private const float HoverHeight = 3.5f;
    private const float HoverMinSeconds = 0.9f;
    private const float AlignTolerance = 1f;
    private const float MaxHoverSeconds = 3.5f;
    private const float WindupSeconds = 0.35f;
    private const int ShotsPerAttack = 3;
    private const float ShotInterval = 0.2f;
    private const int AttacksBeforeRest = 3;
    private const float RestSeconds = 1.4f;
    private const float ProjectileSpeed = 4.2f;
    private const float Amplitude = 0.9f;
    private const float Frequency = 5f;
    private const float ProjectileSize = 0.3f;

    private static readonly Color ProjectileColor = new Color(0.4f, 0.9f, 1f, 1f);

    private Room room;
    private Mode mode = Mode.Hovering;
    private float timer = HoverMinSeconds;
    private float hoverClock;
    private int shotsLeft;
    private int attacksDone;
    private bool useCosine;

    public override bool CanWalkOverObstacles => true;

    protected override string DefaultDescription =>
        "Floats above you and drops three glowing shots that wave like a sine " +
        "curve, then a cosine curve on the next attack. After three attacks it " +
        "stops to rest before floating back over you.";

    public override string[] GetIndexStats()
    {
        return new[]
        {
            "Health 10",
            "Contact damage 1",
            "Wavy shots, sine and cosine",
            "Rests after 3 attacks"
        };
    }

    protected override void Awake()
    {
        base.Awake();

        maxHealth = ScaleHealth(10);
        currentHealth = maxHealth;
        contactDamage = ScaleDamage(1);
        moveSpeed = ScaleSpeed(Speed);
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
            case Mode.Hovering:
                Hover(step);
                break;

            case Mode.Winding:
                Body.linearVelocity = Vector2.zero;
                timer -= step;

                if (timer <= 0f)
                {
                    mode = Mode.Firing;
                    shotsLeft = ShotsPerAttack;
                    timer = 0f;
                }

                break;

            case Mode.Firing:
                Body.linearVelocity = Vector2.zero;
                timer -= step;

                if (timer <= 0f)
                    FireShot();

                break;

            case Mode.Resting:
                Body.linearVelocity = Vector2.zero;
                timer -= step;

                if (timer <= 0f)
                {
                    mode = Mode.Hovering;
                    timer = HoverMinSeconds;
                    hoverClock = 0f;
                }

                break;
        }
    }

    private void Hover(float step)
    {
        Transform chased = GetTarget();

        if (chased == null)
        {
            Body.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 desired = (Vector2)chased.position + Vector2.up * HoverHeight;

        if (room == null)
            room = GetComponentInParent<Room>();

        if (room != null)
        {
            Rect bounds = room.PlayBounds;

            desired.x = Mathf.Clamp(desired.x, bounds.xMin + 0.5f, bounds.xMax - 0.5f);
            desired.y = Mathf.Clamp(desired.y, bounds.yMin + 0.5f, bounds.yMax - 0.5f);
        }

        Vector2 toDesired = desired - Body.position;

        Body.linearVelocity = toDesired.magnitude > 0.3f
            ? toDesired.normalized * moveSpeed * SpeedMultiplier
            : Vector2.zero;

        timer -= step;
        hoverClock += step;

        bool aligned = Mathf.Abs(chased.position.x - Body.position.x) <= AlignTolerance;

        if ((timer <= 0f && aligned) || hoverClock >= MaxHoverSeconds)
        {
            Body.linearVelocity = Vector2.zero;
            mode = Mode.Winding;
            timer = WindupSeconds;

            PlayTelegraph(WindupSeconds, new Color(0.6f, 1f, 1f, 1f), 0.2f);
        }
    }

    private void FireShot()
    {
        WaveProjectile.Spawn(
            Body.position, Vector2.down, ProjectileSpeed, Amplitude, Frequency,
            0f, useCosine, ScaleDamage(1), ProjectileSize, ProjectileColor
        );

        shotsLeft--;
        timer = ShotInterval;

        if (shotsLeft > 0)
            return;

        useCosine = !useCosine;
        attacksDone++;

        if (attacksDone >= AttacksBeforeRest)
        {
            attacksDone = 0;
            mode = Mode.Resting;
            timer = RestSeconds;
            return;
        }

        mode = Mode.Hovering;
        timer = HoverMinSeconds;
        hoverClock = 0f;
    }
}
