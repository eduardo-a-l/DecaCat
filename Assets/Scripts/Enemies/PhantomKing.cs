using System.Collections.Generic;
using UnityEngine;

public class PhantomKing : Enemy
{
    private enum State
    {
        Roaming,
        Warning,
        Vanished
    }

    private const float ReappearGraceSeconds = 0.6f;
    private const float ShieldPulseSpeed = 6f;
    private const float DeflectCooldown = 0.25f;
    private const float SpawnClearFromPlayer = 3f;

    private static readonly Color WarningColor = new Color(0.55f, 0.75f, 1f, 0.55f);
    private static readonly Color SummonColor = new Color(0.7f, 0.85f, 1f, 0.5f);
    private static readonly Color ShieldColor = new Color(0.4f, 0.9f, 1f, 0.8f);

    [Header("Stats")]
    [SerializeField] private int health = 150;
    [SerializeField] private int touchDamage = 1;

    [Header("Movement (speed grows with every living ghost)")]
    [SerializeField] private float followSpeed = 1.1f;
    [SerializeField] private float ghostSpeedBonus = 0.2f;
    [SerializeField] private float maxSpeedMultiplier = 3f;

    [Header("Teleport")]
    [SerializeField] private float teleportInterval = 7f;
    [SerializeField] private float warningTime = 1.1f;
    [SerializeField] private float vanishTime = 0.45f;
    [SerializeField] private float minTeleportDistance = 3f;
    [SerializeField] private float warningRadius = 1.2f;

    [Header("Invulnerability")]
    [SerializeField, Range(0f, 1f)] private float invulnerableChance = 0.4f;
    [SerializeField] private float invulnerableSeconds = 10f;
    [SerializeField] private float invulnerableCooldown = 20f;

    [Header("Ghost summons")]
    [SerializeField] private Ghost ghostPrefab;
    [SerializeField] private float summonInterval = 9f;
    [SerializeField] private float firstSummonDelay = 4f;
    [SerializeField] private int ghostsPerSummon = 2;
    [SerializeField] private int maxGhosts = 8;
    [SerializeField] private float summonDelay = 1.2f;
    [SerializeField] private float summonMarkerRadius = 0.6f;

    [Header("Falling crates")]
    [SerializeField] private Crate cratePrefab;
    [SerializeField] private float crateInterval = 30f;
    [SerializeField] private int cratesPerDrop = 2;

    private class PendingGhost
    {
        public Vector3 Position;
        public float Timer;
    }

    private readonly List<PendingGhost> pendingGhosts = new List<PendingGhost>();
    private readonly BossCrateDropper crates = new BossCrateDropper();

    private Room room;
    private SpriteRenderer bodyRenderer;
    private SpriteRenderer shield;
    private State state = State.Roaming;
    private Vector2 destination;
    private float stateTimer;
    private float teleportTimer;
    private float summonTimer;
    private float invulnerableTimer;
    private float invulnerableCooldownTimer;
    private float deflectTimer;

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
        "A ghost with a crown. It warns you, then teleports around the room, " +
        "and sometimes becomes invulnerable for ten seconds. It summons ghosts " +
        "and moves faster for each one alive. It has no projectiles.";

    public override string[] GetIndexStats()
    {
        return new[]
        {
            "Health " + health,
            "Contact damage " + touchDamage,
            "Teleports with a warning",
            "Invulnerable for 10s at times",
            "Faster with every ghost alive"
        };
    }

    protected override void Awake()
    {
        base.Awake();

        maxHealth = ScaleHealth(health);
        currentHealth = maxHealth;
        contactDamage = ScaleDamage(touchDamage);
        moveSpeed = ScaleSpeed(followSpeed);

        bodyRenderer = GetComponent<SpriteRenderer>();
        teleportTimer = teleportInterval;
        summonTimer = firstSummonDelay;
    }

    protected override void RecordDefeat()
    {
        GameStats.Add(StatType.BossesDefeated);
    }

    protected override void FixedUpdate()
    {
        if (isDead)
            return;

        Tick(Time.fixedDeltaTime);

        base.FixedUpdate();
    }

    private void Update()
    {
        if (isDead)
            return;

        if (shield != null)
        {
            float pulse = 0.55f + 0.35f * Mathf.Sin(Time.time * ShieldPulseSpeed);
            Color color = ShieldColor;
            color.a = pulse;
            shield.color = color;
        }
    }

    public override void TakeDamage(int amount)
    {
        if (isDead)
            return;

        if (invulnerableTimer > 0f && amount < int.MaxValue / 2)
        {
            Deflect();
            return;
        }

        base.TakeDamage(amount);
    }

    protected override void MoveTowardsTarget()
    {
        if (state != State.Roaming)
        {
            Body.linearVelocity = Vector2.zero;
            return;
        }

        base.MoveTowardsTarget();
    }

    private void Tick(float step)
    {
        deflectTimer -= step;

        UpdateInvulnerability(step);
        UpdateSpeed();

        crates.Tick(step, crateInterval, cratesPerDrop, cratePrefab, CurrentRoom);

        UpdateSummons(step);
        UpdateTeleport(step);
    }

    private void UpdateSpeed()
    {
        float multiplier = Mathf.Min(
            maxSpeedMultiplier, 1f + ghostSpeedBonus * CountGhosts()
        );

        moveSpeed = ScaleSpeed(followSpeed) * multiplier;
    }

    private int CountGhosts()
    {
        Room current = CurrentRoom;

        if (current == null)
            return 0;

        int count = 0;

        foreach (Enemy enemy in current.Enemies)
        {
            if (enemy is Ghost && enemy != null)
                count++;
        }

        return count;
    }

    private void UpdateInvulnerability(float step)
    {
        if (invulnerableCooldownTimer > 0f)
            invulnerableCooldownTimer -= step;

        if (invulnerableTimer <= 0f)
            return;

        invulnerableTimer -= step;

        if (invulnerableTimer <= 0f)
            EndInvulnerability();
    }

    private void BeginInvulnerability()
    {
        invulnerableTimer = invulnerableSeconds;
        invulnerableCooldownTimer = invulnerableSeconds + invulnerableCooldown;

        if (shield == null)
        {
            shield = PowerFx.CreateSprite(
                "Shield", PowerFx.Ring, ShieldColor, transform.position,
                Vector2.one * 3.2f, 6, transform
            );
        }

        shield.enabled = true;

        ZoneEffect.Spawn(
            transform.position, 2.2f, ShieldColor, 0.5f, true, 0.3f, 6
        );
    }

    private void EndInvulnerability()
    {
        invulnerableTimer = 0f;

        if (shield != null)
            shield.enabled = false;
    }

    private void Deflect()
    {
        if (deflectTimer > 0f)
            return;

        deflectTimer = DeflectCooldown;

        ZoneEffect.Spawn(
            transform.position, 1.6f, new Color(0.5f, 0.95f, 1f, 0.7f),
            0.25f, true, 0.6f, 7
        );
    }

    private void UpdateTeleport(float step)
    {
        switch (state)
        {
            case State.Roaming:
                teleportTimer -= step;

                if (teleportTimer <= 0f)
                    BeginWarning();

                break;

            case State.Warning:
                stateTimer -= step;

                if (stateTimer <= 0f)
                    Vanish();

                break;

            case State.Vanished:
                stateTimer -= step;

                if (stateTimer <= 0f)
                    Reappear();

                break;
        }
    }

    private void BeginWarning()
    {
        destination = PickTeleportPoint();
        state = State.Warning;
        stateTimer = warningTime;

        GroundMarker.Spawn(destination, warningRadius, warningTime, WarningColor);
        PlayTelegraph(warningTime, new Color(0.6f, 0.8f, 1f, 1f), 0.12f);
    }

    private void Vanish()
    {
        state = State.Vanished;
        stateTimer = vanishTime;

        ZoneEffect.Spawn(
            transform.position, 1.6f, WarningColor, 0.35f, true, 0.3f, 6
        );

        if (bodyRenderer != null)
            bodyRenderer.enabled = false;

        if (shield != null)
            shield.enabled = false;

        BeginGrace(vanishTime + 0.1f, true);

        Body.position = destination;
        transform.position = destination;
    }

    private void Reappear()
    {
        state = State.Roaming;
        teleportTimer = Random.Range(teleportInterval * 0.8f, teleportInterval * 1.2f);

        if (bodyRenderer != null)
            bodyRenderer.enabled = true;

        if (shield != null && invulnerableTimer > 0f)
            shield.enabled = true;

        ZoneEffect.Spawn(
            transform.position, 1.6f, WarningColor, 0.35f, true, 0.3f, 6
        );

        SetSpawnGrace(ReappearGraceSeconds);

        if (invulnerableTimer <= 0f &&
            invulnerableCooldownTimer <= 0f &&
            Random.value < invulnerableChance)
            BeginInvulnerability();
    }

    private Vector2 PickTeleportPoint()
    {
        Room current = CurrentRoom;

        if (current == null)
            return Body.position + Random.insideUnitCircle * 4f;

        Vector2 best = current.GetRandomFloorPoint();

        for (int i = 0; i < 14; i++)
        {
            Vector2 candidate = current.GetRandomFloorPoint();

            if (Vector2.Distance(candidate, Body.position) >= minTeleportDistance)
                return candidate;

            best = candidate;
        }

        return best;
    }

    private void UpdateSummons(float step)
    {
        for (int i = pendingGhosts.Count - 1; i >= 0; i--)
        {
            PendingGhost pending = pendingGhosts[i];

            pending.Timer -= step;

            if (pending.Timer > 0f)
                continue;

            SpawnGhost(pending.Position);
            pendingGhosts.RemoveAt(i);
        }

        summonTimer -= step;

        if (summonTimer > 0f)
            return;

        summonTimer = summonInterval;

        SummonGhosts();
    }

    private void SummonGhosts()
    {
        Room current = CurrentRoom;

        if (ghostPrefab == null || current == null)
            return;

        int free = maxGhosts - CountGhosts() - pendingGhosts.Count;
        int count = Mathf.Min(ghostsPerSummon, free);

        for (int i = 0; i < count; i++)
        {
            Vector3 position = PickSummonPoint(current);

            GroundMarker.Spawn(position, summonMarkerRadius, summonDelay, SummonColor);

            pendingGhosts.Add(new PendingGhost
            {
                Position = position,
                Timer = summonDelay
            });
        }
    }

    private Vector3 PickSummonPoint(Room current)
    {
        Transform chased = GetTarget();
        Vector2 fallback = current.GetRandomFloorPoint();

        for (int i = 0; i < 12; i++)
        {
            Vector2 candidate = current.GetRandomFloorPoint();

            if (chased == null ||
                Vector2.Distance(candidate, chased.position) >= SpawnClearFromPlayer)
                return candidate;

            fallback = candidate;
        }

        return fallback;
    }

    private void SpawnGhost(Vector3 position)
    {
        Ghost ghost = Spawn(ghostPrefab, position, transform.parent, Scaling);

        Room current = CurrentRoom;

        if (current != null)
            current.AddEnemy(ghost);

        ghost.SetSpawnGrace(0.6f);
    }
}
