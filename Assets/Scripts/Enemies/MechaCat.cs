using UnityEngine;

public class MechaCat : Enemy
{
    private enum Mode
    {
        Resting,
        Sliding,
        Winding,
        Attacking
    }

    private const float SlideSpeed = 11f;
    private const float WindupSeconds = 0.7f;
    private const float AttackSeconds = 2.4f;
    private const float RestSeconds = 0.6f;
    private const float SpiralInterval = 0.14f;
    private const float SpiralStep = 19f;
    private const float SpiralSpeed = 5.5f;
    private const float DiagonalInterval = 0.45f;
    private const float DiagonalSpeed = 6.5f;
    private const float ProjectileSize = 0.32f;
    private const float StuckDistanceFactor = 0.2f;
    private const int StuckSteps = 3;

    private static readonly Vector2[] Directions =
    {
        Vector2.left, Vector2.right, Vector2.up, Vector2.down
    };

    private static readonly Color SpiralColor = new Color(0.5f, 0.9f, 1f, 1f);
    private static readonly Color DiagonalColor = new Color(1f, 0.6f, 0.25f, 1f);

    [Header("Stats")]
    [SerializeField] private int health = 200;
    [SerializeField] private int touchDamage = 1;

    [Header("Falling crates")]
    [SerializeField] private Crate cratePrefab;
    [SerializeField] private float crateInterval = 30f;
    [SerializeField] private int cratesPerDrop = 2;

    private readonly BossCrateDropper crates = new BossCrateDropper();

    private Room room;
    private Mode mode = Mode.Resting;
    private Vector2 slideDirection;
    private Vector2 lastPosition;
    private float timer = 1f;
    private int slidesLeft;
    private int stuckSteps;
    private float attackTimer;
    private float spiralTimer;
    private float diagonalTimer;
    private float spiralAngle;

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
        "A huge robot cat that slides at great speed, only ever in straight " +
        "lines. After a few slides it stops and unleashes a spinning stream of " +
        "shots together with bursts of diagonal shots.";

    public override string[] GetIndexStats()
    {
        return new[]
        {
            "Health " + health,
            "Contact damage " + touchDamage,
            "Slides in straight lines only",
            "Rotating and diagonal shots together"
        };
    }

    protected override void Awake()
    {
        base.Awake();

        maxHealth = ScaleHealth(health);
        currentHealth = maxHealth;
        contactDamage = ScaleDamage(touchDamage);
        moveSpeed = ScaleSpeed(SlideSpeed);
        slidesLeft = Random.Range(2, 4);
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
            Time.fixedDeltaTime, crateInterval, cratesPerDrop, cratePrefab, CurrentRoom
        );

        base.FixedUpdate();
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
                    BeginSlide();

                break;

            case Mode.Sliding:
                UpdateSlide(step);
                break;

            case Mode.Winding:
                Body.linearVelocity = Vector2.zero;
                timer -= step;

                if (timer <= 0f)
                    BeginAttack();

                break;

            case Mode.Attacking:
                Body.linearVelocity = Vector2.zero;
                UpdateAttack(step);
                break;
        }
    }

    private void BeginSlide()
    {
        slidesLeft--;

        slideDirection = ChooseDirection();
        timer = Random.Range(0.45f, 0.8f);
        stuckSteps = 0;
        lastPosition = Body.position;
        mode = Mode.Sliding;
    }

    private Vector2 ChooseDirection()
    {
        Transform chased = GetTarget();

        if (chased != null && Random.value < 0.65f)
        {
            Vector2 delta = (Vector2)chased.position - Body.position;

            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                return delta.x > 0f ? Vector2.right : Vector2.left;

            return delta.y > 0f ? Vector2.up : Vector2.down;
        }

        return Directions[Random.Range(0, Directions.Length)];
    }

    private void UpdateSlide(float step)
    {
        Body.linearVelocity = slideDirection * moveSpeed * SpeedMultiplier;

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

        if (slidesLeft > 0)
        {
            BeginSlide();
            return;
        }

        Body.linearVelocity = Vector2.zero;
        mode = Mode.Winding;
        timer = WindupSeconds;

        PlayTelegraph(WindupSeconds, new Color(1f, 0.9f, 0.4f, 1f), 0.15f);
    }

    private void BeginAttack()
    {
        mode = Mode.Attacking;
        attackTimer = AttackSeconds;
        spiralTimer = 0f;
        diagonalTimer = 0f;
        spiralAngle = Random.Range(0f, 360f);
    }

    private void UpdateAttack(float step)
    {
        attackTimer -= step;
        spiralTimer -= step;
        diagonalTimer -= step;

        if (spiralTimer <= 0f)
        {
            spiralTimer += SpiralInterval;

            for (int i = 0; i < 2; i++)
            {
                Vector2 direction =
                    PowerFx.Rotate(Vector2.right, spiralAngle + 180f * i);

                EnemyProjectile.Spawn(
                    Body.position, direction, SpiralSpeed,
                    ScaleDamage(1), ProjectileSize, SpiralColor
                );
            }

            spiralAngle += SpiralStep;
        }

        if (diagonalTimer <= 0f)
        {
            diagonalTimer += DiagonalInterval;

            for (int i = 0; i < 4; i++)
            {
                Vector2 direction = PowerFx.Rotate(Vector2.right, 45f + 90f * i);

                EnemyProjectile.Spawn(
                    Body.position, direction, DiagonalSpeed,
                    ScaleDamage(1), ProjectileSize, DiagonalColor
                );
            }
        }

        if (attackTimer > 0f)
            return;

        slidesLeft = Random.Range(2, 4);
        mode = Mode.Resting;
        timer = RestSeconds;
    }
}
