using UnityEngine;

public class CrystalSlime : Enemy
{
    private enum Mode
    {
        Following,
        Winding,
        Slamming
    }

    private const float FollowSpeed = 1.4f;
    private const float SlamIntervalMin = 5f;
    private const float SlamIntervalMax = 7f;
    private const float WindupSeconds = 0.9f;
    private const float WaveInterval = 0.35f;
    private const int WaveCount = 2;
    private const int ProjectilesPerWave = 14;
    private const float ProjectileSpeed = 5f;
    private const float ProjectileSize = 0.32f;
    private const float RecoverSeconds = 0.7f;
    private const int CrystalsPerSlam = 3;
    private const int MaxCrystals = 8;
    private const float CrystalSeconds = 14f;
    private const float CrystalClearFromPlayer = 2.5f;
    private const float CrystalClearFromSelf = 2.2f;

    private static readonly Color ProjectileColor = new Color(0.95f, 0.6f, 1f, 1f);

    [Header("Stats")]
    [SerializeField] private int health = 400;
    [SerializeField] private int touchDamage = 1;

    [Header("Falling crates")]
    [SerializeField] private Crate cratePrefab;
    [SerializeField] private float crateInterval = 30f;
    [SerializeField] private int cratesPerDrop = 2;

    private readonly BossCrateDropper crates = new BossCrateDropper();

    private Room room;
    private Mode mode = Mode.Following;
    private float slamTimer;
    private float timer;
    private int wavesLeft;
    private float waveOffset;

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
        "A slow crystal giant. It slams the ground to send rings of shots " +
        "outward and raises crystal blocks that wall you in. Only the slime " +
        "itself can break them, by rolling over them.";

    public override string[] GetIndexStats()
    {
        return new[]
        {
            "Health " + health,
            "Contact damage " + touchDamage,
            "Slams to send out rings of shots",
            "Crystals only break when it moves over them"
        };
    }

    protected override void Awake()
    {
        base.Awake();

        maxHealth = ScaleHealth(health);
        currentHealth = maxHealth;
        contactDamage = ScaleDamage(touchDamage);
        moveSpeed = ScaleSpeed(FollowSpeed);
        slamTimer = Random.Range(3f, 4.5f);
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
            case Mode.Following:
                base.MoveTowardsTarget();

                slamTimer -= step;

                if (slamTimer <= 0f)
                {
                    mode = Mode.Winding;
                    timer = WindupSeconds;
                    Body.linearVelocity = Vector2.zero;

                    PlayTelegraph(WindupSeconds, new Color(1f, 0.8f, 1f, 1f), 0.25f);
                }

                break;

            case Mode.Winding:
                Body.linearVelocity = Vector2.zero;
                timer -= step;

                if (timer <= 0f)
                    BeginSlam();

                break;

            case Mode.Slamming:
                Body.linearVelocity = Vector2.zero;
                timer -= step;

                if (timer > 0f)
                    break;

                if (wavesLeft > 0)
                {
                    FireWave();
                    break;
                }

                mode = Mode.Following;
                slamTimer = Random.Range(SlamIntervalMin, SlamIntervalMax);

                break;
        }
    }

    private void BeginSlam()
    {
        mode = Mode.Slamming;
        wavesLeft = WaveCount;
        waveOffset = 0f;

        ZoneEffect.Spawn(
            Body.position, 2.4f, new Color(1f, 0.7f, 1f, 0.6f), 0.5f, true, 0.2f, 6
        );

        RaiseCrystals();
        FireWave();
    }

    private void FireWave()
    {
        float step = 360f / ProjectilesPerWave;

        for (int i = 0; i < ProjectilesPerWave; i++)
        {
            Vector2 direction = PowerFx.Rotate(Vector2.right, waveOffset + step * i);

            EnemyProjectile.Spawn(
                Body.position, direction, ProjectileSpeed, ScaleDamage(1),
                ProjectileSize, ProjectileColor
            );
        }

        waveOffset += step * 0.5f;
        wavesLeft--;
        timer = wavesLeft > 0 ? WaveInterval : RecoverSeconds;
    }

    private void RaiseCrystals()
    {
        Room current = CurrentRoom;

        if (current == null)
            return;

        Transform chased = GetTarget();

        for (int i = 0; i < CrystalsPerSlam; i++)
        {
            if (CrystalObstacle.ActiveCount >= MaxCrystals)
                return;

            Vector2 position = current.GetRandomFloorPoint();

            for (int attempt = 0; attempt < 12; attempt++)
            {
                Vector2 candidate = current.GetRandomFloorPoint();

                bool farFromPlayer = chased == null ||
                    Vector2.Distance(candidate, chased.position) >= CrystalClearFromPlayer;

                bool farFromSelf =
                    Vector2.Distance(candidate, Body.position) >= CrystalClearFromSelf;

                position = candidate;

                if (farFromPlayer && farFromSelf)
                    break;
            }

            CrystalObstacle.Spawn(position, CrystalSeconds, transform);
        }
    }
}
