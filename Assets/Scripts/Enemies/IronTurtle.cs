using UnityEngine;

public class IronTurtle : Enemy
{
    private enum Mode
    {
        Sliding,
        Emerging,
        Firing,
        Resting
    }

    private const float SlideSpeed = 8f;
    private const float EmergeSeconds = 0.6f;
    private const float WaveInterval = 0.35f;
    private const int WaveCount = 3;
    private const float RestSeconds = 0.8f;
    private const float ProjectileSpeed = 6.5f;
    private const float ProjectileSize = 0.34f;
    private const float StuckDistanceFactor = 0.25f;
    private const float ShellOffset = 0f;
    private const float OpenOffset = -0.3f;

    private static readonly Color ProjectileColor = new Color(0.7f, 0.85f, 1f, 1f);

    [Header("Stats")]
    [SerializeField] private int health = 400;
    [SerializeField] private int touchDamage = 1;

    [Header("Sprites")]
    [SerializeField] private Sprite shellSprite;
    [SerializeField] private Sprite openSprite;

    [Header("Movement")]
    [SerializeField] private float slideMinSeconds = 3.5f;
    [SerializeField] private float slideMaxSeconds = 5f;

    [Header("Falling crates")]
    [SerializeField] private Crate cratePrefab;
    [SerializeField] private float crateInterval = 30f;
    [SerializeField] private int cratesPerDrop = 2;

    private readonly BossCrateDropper crates = new BossCrateDropper();

    private Room room;
    private SpriteRenderer bodyRenderer;
    private CircleCollider2D shellCollider;
    private Mode mode = Mode.Resting;
    private Vector2 slideVelocity;
    private Vector2 lastPosition;
    private float timer = 1f;
    private int wavesLeft;
    private bool justLaunched;

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
        "A heavy iron turtle. It tucks into its shell and ricochets around the " +
        "room like a bouncing logo. When it stops it pops out and fires shots " +
        "in all four straight directions.";

    public override string[] GetIndexStats()
    {
        return new[]
        {
            "Health " + health,
            "Contact damage " + touchDamage,
            "Bounces around in its shell",
            "Fires 4-way shots when it stops"
        };
    }

    public override void GetIndexVisual(out Sprite sprite, out Color color)
    {
        sprite = openSprite != null ? openSprite : PlaceholderSprite;
        color = Color.white;
    }

    protected override void Awake()
    {
        base.Awake();

        maxHealth = ScaleHealth(health);
        currentHealth = maxHealth;
        contactDamage = ScaleDamage(touchDamage);
        moveSpeed = ScaleSpeed(SlideSpeed);

        bodyRenderer = GetComponent<SpriteRenderer>();
        shellCollider = GetComponent<CircleCollider2D>();

        ShowShell();
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

            case Mode.Emerging:
                Body.linearVelocity = Vector2.zero;
                timer -= step;

                if (timer <= 0f)
                {
                    mode = Mode.Firing;
                    wavesLeft = WaveCount;
                    timer = 0f;
                }

                break;

            case Mode.Firing:
                Body.linearVelocity = Vector2.zero;
                timer -= step;

                if (timer <= 0f)
                    FireWave();

                break;
        }
    }

    private void BeginSlide()
    {
        ShowShell();

        float angle = 45f + 90f * Random.Range(0, 4) + Random.Range(-18f, 18f);

        slideVelocity = PowerFx.Rotate(Vector2.right, angle).normalized;
        timer = Random.Range(slideMinSeconds, slideMaxSeconds);
        lastPosition = Body.position;
        justLaunched = true;
        mode = Mode.Sliding;
    }

    private void UpdateSlide(float step)
    {
        float speed = moveSpeed * SpeedMultiplier;

        if (!justLaunched)
        {
            Vector2 moved = Body.position - lastPosition;
            Vector2 expected = slideVelocity * speed * step;

            if (Mathf.Abs(expected.x) > 0.0001f &&
                Mathf.Abs(moved.x) < Mathf.Abs(expected.x) * StuckDistanceFactor)
                slideVelocity.x = -slideVelocity.x;

            if (Mathf.Abs(expected.y) > 0.0001f &&
                Mathf.Abs(moved.y) < Mathf.Abs(expected.y) * StuckDistanceFactor)
                slideVelocity.y = -slideVelocity.y;
        }

        justLaunched = false;
        lastPosition = Body.position;

        Body.linearVelocity = slideVelocity * speed;

        timer -= step;

        if (timer > 0f)
            return;

        Body.linearVelocity = Vector2.zero;
        mode = Mode.Emerging;
        timer = EmergeSeconds;

        ShowOpen();
        PlayTelegraph(EmergeSeconds, new Color(1f, 0.9f, 0.4f, 1f), 0.12f);
    }

    private void FireWave()
    {
        for (int i = 0; i < 4; i++)
        {
            Vector2 direction = PowerFx.Rotate(Vector2.right, 90f * i);

            EnemyProjectile.Spawn(
                Body.position, direction, ProjectileSpeed,
                ScaleDamage(1), ProjectileSize, ProjectileColor
            ).PassThroughObstacles();
        }

        wavesLeft--;
        timer = WaveInterval;

        if (wavesLeft > 0)
            return;

        mode = Mode.Resting;
        timer = RestSeconds;
    }

    private void ShowShell()
    {
        if (bodyRenderer != null && shellSprite != null)
            bodyRenderer.sprite = shellSprite;

        if (shellCollider != null)
            shellCollider.offset = new Vector2(0f, ShellOffset);
    }

    private void ShowOpen()
    {
        if (bodyRenderer != null && openSprite != null)
            bodyRenderer.sprite = openSprite;

        if (shellCollider != null)
            shellCollider.offset = new Vector2(0f, OpenOffset);
    }
}
