using System.Collections.Generic;
using UnityEngine;

public class JellyfishQueen : Enemy
{
    private enum Phase
    {
        Firing,
        Holding,
        Resting
    }

    private const float TopMargin = 1.6f;
    private const float FollowSpeed = 5f;
    private const float FireSeconds = 4f;
    private const float HoldSeconds = 0.7f;
    private const float RestSeconds = 2.2f;
    private const float PairInterval = 0.1f;
    private const float ProjectileSpeed = 4.2f;
    private const float Amplitude = 0.8f;
    private const float Frequency = 7f;
    private const float ProjectileSize = 0.3f;
    private const float ScatterMinSpeed = 2.5f;
    private const float ScatterMaxSpeed = 7f;

    private static readonly Color StrandColor = new Color(0.35f, 0.8f, 1f, 1f);
    private static readonly Color StrandColorAlt = new Color(0.7f, 0.9f, 1f, 1f);

    [Header("Stats")]
    [SerializeField] private int health = 200;
    [SerializeField] private int touchDamage = 1;

    [Header("Falling crates (bottom corners only)")]
    [SerializeField] private Crate cratePrefab;
    [SerializeField] private float crateInterval = 30f;
    [SerializeField] private int cratesPerDrop = 2;

    private readonly BossCrateDropper crates = new BossCrateDropper();
    private readonly List<WaveProjectile> strands = new List<WaveProjectile>();

    private Room room;
    private Phase phase = Phase.Resting;
    private float timer = 1.5f;
    private float pairTimer;
    private int pairIndex;

    public override bool IsBoss => true;
    protected override bool CanBeKnockedBack => false;

    private Room CurrentRoom
    {
        get
        {
            if (room == null)
                room = GetComponentInParent<Room>();

            return room;
        }
    }

    protected override string DefaultDescription =>
        "The jellyfish queen drifts along the top of the room, following you " +
        "sideways, and sprays a double helix of wavy shots downward. When the " +
        "stream ends, every shot flies apart across the room at its own speed.";

    public override string[] GetIndexStats()
    {
        return new[]
        {
            "Health " + health,
            "Contact damage " + touchDamage,
            "DNA-like sine and cosine shots",
            "Shots scatter when the attack ends"
        };
    }

    protected override void Awake()
    {
        base.Awake();

        maxHealth = ScaleHealth(health);
        currentHealth = maxHealth;
        contactDamage = ScaleDamage(touchDamage);
        moveSpeed = ScaleSpeed(FollowSpeed);
    }

    protected override void RecordDefeat()
    {
        GameStats.Add(StatType.BossesDefeated);
    }

    protected override void FixedUpdate()
    {
        if (isDead)
            return;

        crates.Tick(
            Time.fixedDeltaTime, crateInterval, cratesPerDrop, cratePrefab,
            CurrentRoom, true
        );

        UpdatePhase(Time.fixedDeltaTime);

        base.FixedUpdate();
    }

    protected override void MoveTowardsTarget()
    {
        Room current = CurrentRoom;
        Transform chased = GetTarget();

        if (current == null)
        {
            Body.linearVelocity = Vector2.zero;
            return;
        }

        Rect bounds = current.PlayBounds;

        float targetX = chased != null
            ? Mathf.Clamp(chased.position.x, bounds.xMin + 1.2f, bounds.xMax - 1.2f)
            : Body.position.x;

        Vector2 destination = new Vector2(targetX, bounds.yMax - TopMargin);
        Vector2 toDestination = destination - Body.position;

        float speed = moveSpeed * SpeedMultiplier;

        Body.linearVelocity = toDestination.magnitude > 0.1f
            ? Vector2.ClampMagnitude(toDestination * 4f, speed)
            : Vector2.zero;
    }

    private void UpdatePhase(float step)
    {
        timer -= step;

        switch (phase)
        {
            case Phase.Firing:
                pairTimer -= step;

                if (pairTimer <= 0f)
                {
                    pairTimer += PairInterval;
                    FirePair();
                }

                if (timer <= 0f)
                {
                    phase = Phase.Holding;
                    timer = HoldSeconds;

                    PlayTelegraph(HoldSeconds, new Color(0.7f, 0.95f, 1f, 1f), 0.15f);
                }

                break;

            case Phase.Holding:
                if (timer <= 0f)
                {
                    ScatterStrands();
                    phase = Phase.Resting;
                    timer = RestSeconds;
                }

                break;

            case Phase.Resting:
                if (timer <= 0f)
                {
                    phase = Phase.Firing;
                    timer = FireSeconds;
                    pairTimer = 0f;
                    pairIndex = 0;
                }

                break;
        }
    }

    private void FirePair()
    {
        bool cosine = pairIndex % 2 == 1;
        Color color = cosine ? StrandColorAlt : StrandColor;

        for (int i = 0; i < 2; i++)
        {
            float phaseShift = Mathf.PI * i;

            strands.Add(
                WaveProjectile.Spawn(
                    Body.position, Vector2.down, ProjectileSpeed, Amplitude,
                    Frequency, phaseShift, cosine, ScaleDamage(1),
                    ProjectileSize, color, 12f
                )
            );
        }

        pairIndex++;
    }

    private void ScatterStrands()
    {
        foreach (WaveProjectile strand in strands)
        {
            if (strand == null)
                continue;

            float angle = Random.Range(215f, 325f);
            float speed = Random.Range(ScatterMinSpeed, ScatterMaxSpeed);

            strand.Release(PowerFx.Rotate(Vector2.right, angle) * speed);
        }

        strands.Clear();
    }
}
