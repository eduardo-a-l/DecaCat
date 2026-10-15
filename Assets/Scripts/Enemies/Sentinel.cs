using UnityEngine;

public class Sentinel : Enemy
{
    private enum Phase
    {
        Cross,
        Charging,
        Firing,
        Recovering
    }

    private const int ArmCount = 4;
    private const float ArmLength = 3f;
    private const float ArmWidth = 0.24f;
    private const float RotationSpeed = 50f;
    private const float ArmHitCooldown = 0.9f;
    private const float MaxBeamLength = 22f;
    private const float ChargeSeconds = 1.6f;
    private const float FireSeconds = 0.4f;
    private const float RecoverSeconds = 0.8f;
    private const float BeamWidth = 1f;
    private const float TileSize = 0.9f;
    private const float TileSeconds = 3f;
    private const float BeamHitCooldown = 0.5f;

    private static readonly Color ArmColor = new Color(0.55f, 0.85f, 1f, 0.9f);
    private static readonly Color ChargeColor = new Color(1f, 0.95f, 0.25f, 1f);

    [Header("Stats")]
    [SerializeField] private int health = 500;
    [SerializeField] private int touchDamage = 1;

    [Header("Beam timing")]
    [SerializeField] private float crossMinSeconds = 5f;
    [SerializeField] private float crossMaxSeconds = 8f;

    [Header("Falling crates")]
    [SerializeField] private Crate cratePrefab;
    [SerializeField] private float crateInterval = 30f;
    [SerializeField] private int cratesPerDrop = 2;

    private readonly BossCrateDropper crates = new BossCrateDropper();
    private readonly SpriteRenderer[] arms = new SpriteRenderer[ArmCount];

    private Room room;
    private SpriteRenderer beam;
    private Phase phase = Phase.Cross;
    private float timer;
    private float crossAngle;
    private float armHitTimer;
    private float beamHitTimer;
    private float beamLength;
    private Vector2 beamDirection = Vector2.right;
    private bool centered;

    public override bool IsBoss => true;
    protected override bool CanBeKnockedBack => false;

    private Transform VisualParent =>
        transform.parent != null ? transform.parent : PowerFx.EffectParent;

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
        "A watchful sentinel that never leaves the middle of the room. It spins " +
        "a cross of electric beams, and now and then locks onto you and charges " +
        "a huge yellow beam that leaves the floor electrified for a few seconds.";

    public override string[] GetIndexStats()
    {
        return new[]
        {
            "Health " + health,
            "Contact damage " + touchDamage,
            "Spinning cross of electric beams",
            "Charged beam electrifies the floor"
        };
    }

    protected override void Awake()
    {
        base.Awake();

        maxHealth = ScaleHealth(health);
        currentHealth = maxHealth;
        contactDamage = ScaleDamage(touchDamage);
        moveSpeed = 0f;

        timer = Random.Range(crossMinSeconds, crossMaxSeconds);

        for (int i = 0; i < ArmCount; i++)
        {
            arms[i] = PowerFx.CreateSprite(
                "Arm" + i, PowerFx.Square, ArmColor, transform.position,
                new Vector2(ArmLength, ArmWidth), 2, VisualParent
            );
        }

        beam = PowerFx.CreateSprite(
            "ChargeBeam", PowerFx.Square, ChargeColor, transform.position,
            new Vector2(1f, 0.1f), 3, VisualParent
        );

        beam.enabled = false;
    }

    private void OnDestroy()
    {
        foreach (SpriteRenderer arm in arms)
        {
            if (arm != null)
                Destroy(arm.gameObject);
        }

        if (beam != null)
            Destroy(beam.gameObject);
    }

    protected override void RecordDefeat()
    {
        GameStats.Add(StatType.BossesDefeated);
    }

    protected override void FixedUpdate()
    {
        if (isDead)
            return;

        float step = Time.fixedDeltaTime;

        if (!centered)
            MoveToCenter();

        crates.Tick(step, crateInterval, cratesPerDrop, cratePrefab, CurrentRoom);

        UpdatePhase(step);

        base.FixedUpdate();
    }

    private void MoveToCenter()
    {
        centered = true;

        Room current = CurrentRoom;

        if (current == null)
            return;

        Vector2 center = current.Center;

        Body.position = center;
        transform.position = center;
    }

    protected override void MoveTowardsTarget()
    {
        Body.linearVelocity = Vector2.zero;
    }

    private void Update()
    {
        if (isDead)
            return;

        UpdateVisuals();
    }

    private void UpdatePhase(float step)
    {
        if (armHitTimer > 0f)
            armHitTimer -= step;

        if (beamHitTimer > 0f)
            beamHitTimer -= step;

        timer -= step;

        switch (phase)
        {
            case Phase.Cross:
                crossAngle += RotationSpeed * step;

                DamageFromArms();

                if (timer <= 0f)
                    BeginCharge();

                break;

            case Phase.Charging:
                if (timer <= 0f)
                    BeginFire();

                break;

            case Phase.Firing:
                DamageFromBeam();

                if (timer <= 0f)
                {
                    phase = Phase.Recovering;
                    timer = RecoverSeconds;
                }

                break;

            case Phase.Recovering:
                if (timer <= 0f)
                {
                    phase = Phase.Cross;
                    timer = Random.Range(crossMinSeconds, crossMaxSeconds);
                }

                break;
        }
    }

    private void BeginCharge()
    {
        Transform chased = GetTarget();

        Vector2 toTarget = chased != null
            ? (Vector2)chased.position - Body.position
            : Vector2.right;

        beamDirection = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector2.right;

        RaycastHit2D hit = Physics2D.Raycast(
            Body.position, beamDirection, MaxBeamLength, PhysicsLayers.WallMask
        );

        beamLength = hit.collider != null ? hit.distance : MaxBeamLength;

        phase = Phase.Charging;
        timer = ChargeSeconds;

        PlayTelegraph(ChargeSeconds, ChargeColor, 0.1f);
    }

    private void BeginFire()
    {
        phase = Phase.Firing;
        timer = FireSeconds;
        beamHitTimer = 0f;

        for (float distance = 1.2f; distance < beamLength; distance += TileSize)
        {
            ElectrifiedTile.Spawn(
                Body.position + beamDirection * distance, TileSize,
                TileSeconds, ScaleDamage(1)
            );
        }

        ZoneEffect.Spawn(
            Body.position, 1.8f, new Color(1f, 0.95f, 0.3f, 0.8f), 0.35f, true, 0.3f, 6
        );
    }

    private void DamageFromArms()
    {
        if (armHitTimer > 0f)
            return;

        for (int i = 0; i < ArmCount; i++)
        {
            float angle = crossAngle + 90f * i;
            Vector2 direction = PowerFx.Rotate(Vector2.right, angle);
            Vector2 center = Body.position + direction * (ArmLength * 0.5f);

            if (HitPlayerInBox(center, new Vector2(ArmLength, ArmWidth), angle))
            {
                armHitTimer = ArmHitCooldown;
                return;
            }
        }
    }

    private void DamageFromBeam()
    {
        if (beamHitTimer > 0f)
            return;

        Vector2 center = Body.position + beamDirection * (beamLength * 0.5f);
        float angle = Mathf.Atan2(beamDirection.y, beamDirection.x) * Mathf.Rad2Deg;

        if (HitPlayerInBox(center, new Vector2(beamLength, BeamWidth), angle))
            beamHitTimer = BeamHitCooldown;
    }

    private bool HitPlayerInBox(Vector2 center, Vector2 size, float angle)
    {
        foreach (Collider2D hit in Physics2D.OverlapBoxAll(center, size, angle))
        {
            PlayerHealth player = hit.GetComponentInParent<PlayerHealth>();

            if (player == null)
                continue;

            player.TakeDamage(ScaleDamage(1), DamageElement.Electric);

            return true;
        }

        return false;
    }

    private void UpdateVisuals()
    {
        bool showArms = phase == Phase.Cross;

        for (int i = 0; i < ArmCount; i++)
        {
            SpriteRenderer arm = arms[i];

            if (arm == null)
                continue;

            arm.enabled = showArms;

            if (!showArms)
                continue;

            float angle = crossAngle + 90f * i;
            Vector2 direction = PowerFx.Rotate(Vector2.right, angle);
            float flicker = 0.7f + 0.3f * Mathf.Sin(Time.time * 45f + i);

            Color color = ArmColor;
            color.a = ArmColor.a * flicker;

            arm.color = color;
            arm.transform.position = Body.position + direction * (ArmLength * 0.5f);
            arm.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            arm.transform.localScale = new Vector3(ArmLength, ArmWidth, 1f);
        }

        bool showBeam = phase == Phase.Charging || phase == Phase.Firing;

        beam.enabled = showBeam;

        if (!showBeam)
            return;

        float width;
        float alpha;

        if (phase == Phase.Charging)
        {
            float progress = 1f - Mathf.Clamp01(timer / ChargeSeconds);

            width = Mathf.Lerp(0.08f, 0.5f, progress);
            alpha = Mathf.Lerp(0.3f, 1f, progress) *
                    (0.7f + 0.3f * Mathf.Sin(Time.time * 40f));
        }
        else
        {
            width = BeamWidth;
            alpha = 1f;
        }

        Color beamColor = ChargeColor;
        beamColor.a = alpha;

        beam.color = beamColor;
        beam.transform.position = Body.position + beamDirection * (beamLength * 0.5f);
        beam.transform.rotation =
            Quaternion.Euler(0f, 0f, Mathf.Atan2(beamDirection.y, beamDirection.x) * Mathf.Rad2Deg);
        beam.transform.localScale = new Vector3(beamLength, width, 1f);
    }
}
