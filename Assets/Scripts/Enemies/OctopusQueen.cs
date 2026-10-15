using UnityEngine;

public class OctopusQueen : Enemy
{
    private enum Phase
    {
        Settling,
        Warning,
        Attacking,
        Resting,
        Hidden
    }

    private const float Speed = 2.5f;
    private const float ArriveDistance = 0.3f;
    private const float WarningSeconds = 1f;
    private const float TentacleSeconds = 3.5f;
    private const float RestSeconds = 1.5f;
    private const float HiddenSeconds = 4f;
    private const float HiddenTentacleInterval = 1.4f;
    private const int TentaclesPerAttack = 4;
    private const int TentaclesWhileHidden = 3;
    private const float HideChance = 0.4f;
    private const float BurstInterval = 0.35f;
    private const int BurstsPerRun = 3;
    private const float AngleStep = 22f;
    private const float ProjectileSpeed = 4.6f;
    private const float ProjectileSize = 0.3f;
    private const float MarkerRadius = 0.55f;

    private static readonly Color ProjectileColor = new Color(0.9f, 0.5f, 1f, 1f);
    private static readonly Color MarkerColor = new Color(0.8f, 0.4f, 1f, 0.5f);

    [Header("Stats")]
    [SerializeField] private int health = 300;
    [SerializeField] private int touchDamage = 1;

    [Header("Tentacles")]
    [SerializeField] private Sprite tentacleSprite;

    [Header("Falling crates")]
    [SerializeField] private Crate cratePrefab;
    [SerializeField] private float crateInterval = 30f;
    [SerializeField] private int cratesPerDrop = 2;

    private readonly BossCrateDropper crates = new BossCrateDropper();
    private readonly Vector2[] pending = new Vector2[TentaclesPerAttack];

    private Room room;
    private SpriteRenderer bodyRenderer;
    private Phase phase = Phase.Settling;
    private Vector2 home;
    private bool hasHome;
    private float timer;
    private float burstTimer;
    private float angle;
    private float rotation = 1f;
    private int burstsLeft = BurstsPerRun;
    private float hiddenTentacleTimer;
    private int attacksSinceHide;

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
        "The octopus queen holds the middle of the room. Tentacles burst from " +
        "the floor at marked spots while she fires spirals of shots. Sometimes " +
        "she sinks into the ground and cannot be hurt.";

    public override string[] GetIndexStats()
    {
        return new[]
        {
            "Health " + health,
            "Contact damage " + touchDamage,
            "Tentacles from marked floor spots",
            "Spiral shots while tentacles are out",
            "Sometimes hides underground"
        };
    }

    protected override void Awake()
    {
        base.Awake();

        maxHealth = ScaleHealth(health);
        currentHealth = maxHealth;
        contactDamage = ScaleDamage(touchDamage);
        moveSpeed = ScaleSpeed(Speed);

        bodyRenderer = GetComponent<SpriteRenderer>();
        angle = Random.Range(0f, 360f);
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

        RunPhase();

        base.FixedUpdate();
    }

    public override void TakeDamage(int amount)
    {
        if (isDead)
            return;

        if (phase == Phase.Hidden && amount < int.MaxValue / 2)
            return;

        base.TakeDamage(amount);
    }

    protected override void MoveTowardsTarget()
    {
    }

    private void RunPhase()
    {
        float step = Time.fixedDeltaTime;

        switch (phase)
        {
            case Phase.Settling:
                Settle();
                break;

            case Phase.Warning:
                Body.linearVelocity = Vector2.zero;
                timer -= step;

                if (timer <= 0f)
                    ReleaseTentacles();

                break;

            case Phase.Attacking:
                Body.linearVelocity = Vector2.zero;
                timer -= step;
                burstTimer -= step;

                if (burstTimer <= 0f)
                    FireBurst();

                if (timer <= 0f)
                {
                    phase = Phase.Resting;
                    timer = RestSeconds;
                    attacksSinceHide++;
                }

                break;

            case Phase.Resting:
                Body.linearVelocity = Vector2.zero;
                timer -= step;

                if (timer > 0f)
                    break;

                if (attacksSinceHide >= 2 && Random.value < HideChance)
                    Hide();
                else
                    BeginWarning();

                break;

            case Phase.Hidden:
                Body.linearVelocity = Vector2.zero;
                timer -= step;
                hiddenTentacleTimer -= step;

                if (hiddenTentacleTimer <= 0f)
                {
                    hiddenTentacleTimer = HiddenTentacleInterval;

                    MarkAndRaise(TentaclesWhileHidden, true);
                }

                if (timer <= 0f)
                    Surface();

                break;
        }
    }

    private void Settle()
    {
        if (!hasHome)
        {
            Room current = CurrentRoom;

            if (current == null)
            {
                BeginWarning();
                return;
            }

            home = (Vector2)current.Center + new Vector2(
                Random.Range(-1f, 1f), Random.Range(-0.6f, 0.6f)
            );

            hasHome = true;
        }

        Vector2 toHome = home - Body.position;

        if (toHome.magnitude <= ArriveDistance)
        {
            Body.linearVelocity = Vector2.zero;
            BeginWarning();
            return;
        }

        Body.linearVelocity = toHome.normalized * moveSpeed * SpeedMultiplier;
    }

    private void BeginWarning()
    {
        phase = Phase.Warning;
        timer = WarningSeconds;

        PlayTelegraph(WarningSeconds, new Color(1f, 0.7f, 1f, 1f), 0.15f);

        MarkPositions(pending, TentaclesPerAttack, WarningSeconds);
    }

    private void MarkPositions(Vector2[] positions, int count, float seconds)
    {
        Transform chased = GetTarget();
        Room current = CurrentRoom;

        for (int i = 0; i < count; i++)
        {
            Vector2 position;

            if (i < 2 && chased != null)
                position = (Vector2)chased.position + Random.insideUnitCircle * 1.5f;
            else if (current != null)
                position = current.GetRandomFloorPoint();
            else
                position = Body.position + Random.insideUnitCircle * 4f;

            if (current != null)
            {
                Rect bounds = current.PlayBounds;

                position.x = Mathf.Clamp(position.x, bounds.xMin + 0.5f, bounds.xMax - 0.5f);
                position.y = Mathf.Clamp(position.y, bounds.yMin + 0.5f, bounds.yMax - 0.5f);
            }

            positions[i] = position;

            GroundMarker.Spawn(position, MarkerRadius, seconds, MarkerColor);
        }
    }

    private void ReleaseTentacles()
    {
        for (int i = 0; i < TentaclesPerAttack; i++)
            TentacleHazard.Spawn(pending[i], tentacleSprite, TentacleSeconds, ScaleDamage(1));

        phase = Phase.Attacking;
        timer = TentacleSeconds;
        burstTimer = 0.2f;
        burstsLeft = BurstsPerRun;
    }

    private void MarkAndRaise(int count, bool nearPlayer)
    {
        Vector2[] positions = new Vector2[count];

        MarkPositions(positions, count, 0.9f);

        StartCoroutine(RaiseAfterDelay(positions, 0.9f));
    }

    private System.Collections.IEnumerator RaiseAfterDelay(Vector2[] positions, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (isDead)
            yield break;

        foreach (Vector2 position in positions)
            TentacleHazard.Spawn(position, tentacleSprite, 1.6f, ScaleDamage(1));
    }

    private void FireBurst()
    {
        for (int i = 0; i < 3; i++)
        {
            Vector2 direction = PowerFx.Rotate(Vector2.right, angle + 120f * i);

            EnemyProjectile.Spawn(
                Body.position, direction, ProjectileSpeed, ScaleDamage(1),
                ProjectileSize, ProjectileColor
            );
        }

        angle += AngleStep * rotation;
        burstsLeft--;
        burstTimer = BurstInterval;

        if (burstsLeft > 0)
            return;

        burstsLeft = BurstsPerRun;
        rotation = -rotation;
    }

    private void Hide()
    {
        phase = Phase.Hidden;
        timer = HiddenSeconds;
        hiddenTentacleTimer = 0.6f;
        attacksSinceHide = 0;

        ZoneEffect.Spawn(
            transform.position, 1.6f, MarkerColor, 0.4f, true, 0.3f, 6
        );

        if (bodyRenderer != null)
            bodyRenderer.enabled = false;

        BeginGrace(HiddenSeconds + 0.1f, true);
    }

    private void Surface()
    {
        Room current = CurrentRoom;

        if (current != null)
        {
            home = (Vector2)current.Center + new Vector2(
                Random.Range(-1.5f, 1.5f), Random.Range(-1f, 1f)
            );

            Body.position = home;
            transform.position = home;
        }

        if (bodyRenderer != null)
            bodyRenderer.enabled = true;

        ZoneEffect.Spawn(
            transform.position, 1.6f, MarkerColor, 0.4f, true, 0.3f, 6
        );

        phase = Phase.Resting;
        timer = 0.8f;

        SetSpawnGrace(0.6f);
    }
}
