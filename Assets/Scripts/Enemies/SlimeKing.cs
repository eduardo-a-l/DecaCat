using System.Collections.Generic;
using UnityEngine;

public class SlimeKing : Enemy
{
    private enum State
    {
        Following,
        Telegraphing,
        Jumping,
        Recovering
    }

    private const float MinTelegraph = 0.45f;
    private const float MinJump = 0.35f;
    private const float MinRecover = 0.3f;
    private const float CrateFallHeight = 8f;
    private const float CrateFallTime = 0.6f;
    private const float CornerClearRadius = 0.45f;
    private const float SummonSpacing = 1f;
    private const float SummonRingGap = 0.9f;
    private const float SummonRingDepth = 0.8f;
    private const float ShadowAlpha = 0.3f;
    private const int ShadowSortingOrder = -4;

    private static readonly Color MarkerColor = new Color(0.9f, 0.2f, 0.2f, 0.6f);
    private static readonly Color CrateMarkerColor = new Color(1f, 0.9f, 0.3f, 0.5f);

    [Header("Stats")]
    [SerializeField] private int health = 200;
    [SerializeField] private int touchDamage = 1;

    [Header("Movement (speed and jump rate grow as health drops)")]
    [SerializeField] private float followSpeed = 0.7f;
    [SerializeField] private float rageBonus = 1f;

    [Header("Jump")]
    [SerializeField] private float jumpInterval = 3.5f;
    [SerializeField] private float telegraphTime = 1f;
    [SerializeField] private float jumpTime = 0.6f;
    [SerializeField] private float recoverTime = 0.9f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float landingRadius = 1.6f;

    [Header("Summons")]
    [SerializeField] private Slime slimePrefab;
    [SerializeField] private int slimesPerSummon = 2;
    [SerializeField] private float summonDelay = 1.2f;
    [SerializeField] private float summonMarkerRadius = 0.6f;
    [SerializeField] private int[] summonHealthThresholds = { 150, 100, 50 };

    [Header("Falling crates")]
    [SerializeField] private Crate cratePrefab;
    [SerializeField] private float crateInterval = 30f;
    [SerializeField] private int cratesPerDrop = 2;

    private class PendingSummon
    {
        public Vector3 Position;
        public float Timer;
    }

    private readonly Dictionary<Crate, Vector3> droppedCrates =
        new Dictionary<Crate, Vector3>();

    private readonly List<PendingSummon> pendingSummons =
        new List<PendingSummon>();

    private CircleCollider2D circleCollider;
    private Transform visual;
    private Transform shadow;
    private Vector3 shadowBaseScale;
    private bool[] summoned;
    private State state;
    private float stateTimer;
    private float stateDuration;
    private float crateTimer;
    private Vector2 jumpStart;
    private Vector2 jumpTarget;

    protected override bool CanBeKnockedBack => false;

    private float Rage =>
        1f + (1f - (float)currentHealth / maxHealth) * rageBonus;

    protected override void Awake()
    {
        base.Awake();

        maxHealth = ScaleHealth(health);
        currentHealth = maxHealth;
        contactDamage = ScaleDamage(touchDamage);

        circleCollider = GetComponent<CircleCollider2D>();

        SpriteRenderer bodyRenderer = GetComponentInChildren<SpriteRenderer>();

        if (bodyRenderer != null && bodyRenderer.transform != transform)
            visual = bodyRenderer.transform;

        summoned = new bool[summonHealthThresholds.Length];

        CreateShadow();
        EnterState(State.Following, jumpInterval);
    }

    private void Update()
    {
        if (isDead)
            return;

        float lift = 0f;
        float height01 = 0f;

        if (state == State.Jumping)
        {
            float extra = Mathf.Min(
                Time.time - Time.fixedTime, Time.fixedDeltaTime
            );

            float t = Mathf.Clamp01((stateTimer + extra) / stateDuration);

            height01 = Mathf.Sin(t * Mathf.PI);
            lift = height01 * jumpHeight;
        }

        if (visual != null)
            visual.localPosition = new Vector3(0f, lift, 0f);

        if (shadow != null)
            shadow.localScale = shadowBaseScale * (1f - 0.35f * height01);
    }

    protected override void FixedUpdate()
    {
        if (isDead)
            return;

        float step = Time.fixedDeltaTime;

        UpdateCrateTimer(step);
        UpdateSummons(step);

        stateTimer += step;

        switch (state)
        {
            case State.Following:
                moveSpeed = ScaleSpeed(followSpeed) * Rage;
                MoveTowardsTarget();

                if (stateTimer >= stateDuration)
                    BeginTelegraph();

                break;

            case State.Telegraphing:
                Body.linearVelocity = Vector2.zero;

                if (stateTimer >= stateDuration)
                    BeginJump();

                break;

            case State.Jumping:
                UpdateJump();
                break;

            case State.Recovering:
                Body.linearVelocity = Vector2.zero;

                if (stateTimer >= stateDuration)
                    EnterState(State.Following, jumpInterval / Rage);

                break;
        }
    }

    public override void TakeDamage(int amount)
    {
        base.TakeDamage(amount);

        if (isDead)
            return;

        for (int i = 0; i < summonHealthThresholds.Length; i++)
        {
            if (summoned[i] || currentHealth > GetSummonThreshold(i))
                continue;

            summoned[i] = true;
            SummonSlimes();
        }
    }

    private int GetSummonThreshold(int index)
    {
        if (health <= 0)
            return summonHealthThresholds[index];

        return Mathf.RoundToInt(
            summonHealthThresholds[index] * (float)maxHealth / health
        );
    }

    protected override void RecordDefeat()
    {
        GameStats.Add(StatType.BossesDefeated);
    }

    private void EnterState(State next, float duration)
    {
        state = next;
        stateTimer = 0f;
        stateDuration = Mathf.Max(0.01f, duration);
    }

    private void BeginTelegraph()
    {
        Transform chased = GetTarget();

        if (chased == null)
        {
            EnterState(State.Following, jumpInterval / Rage);
            return;
        }

        jumpTarget = chased.position;

        float duration = Mathf.Max(MinTelegraph, telegraphTime / Rage);

        GroundMarker.Spawn(jumpTarget, landingRadius, duration, MarkerColor);
        EnterState(State.Telegraphing, duration);
    }

    private void BeginJump()
    {
        jumpStart = Body.position;
        Body.linearVelocity = Vector2.zero;
        circleCollider.enabled = false;

        EnterState(State.Jumping, Mathf.Max(MinJump, jumpTime / Rage));
    }

    private void UpdateJump()
    {
        float t = Mathf.Clamp01(stateTimer / stateDuration);

        Body.position = Vector2.Lerp(
            jumpStart, jumpTarget, Mathf.SmoothStep(0f, 1f, t)
        );

        if (t >= 1f)
            Land();
    }

    private void Land()
    {
        Body.position = jumpTarget;
        Body.linearVelocity = Vector2.zero;
        circleCollider.enabled = true;

        ResolveLanding();

        EnterState(State.Recovering, Mathf.Max(MinRecover, recoverTime / Rage));
    }

    private void ResolveLanding()
    {
        Transform chased = GetTarget();

        if (chased != null &&
            Vector2.Distance(chased.position, jumpTarget) <= landingRadius)
        {
            PlayerHealth player = chased.GetComponent<PlayerHealth>();

            if (player != null)
                player.TakeDamage(contactDamage);
        }

        foreach (Collider2D hit in
                 Physics2D.OverlapCircleAll(jumpTarget, landingRadius))
        {
            Crate crate = hit.GetComponent<Crate>();

            if (crate != null)
                crate.TakeDamage(int.MaxValue);
        }
    }

    private void SummonSlimes()
    {
        if (slimePrefab == null || slimesPerSummon <= 0)
            return;

        foreach (Vector3 position in FindSummonPositions())
        {
            GroundMarker.Spawn(
                position, summonMarkerRadius, summonDelay, MarkerColor
            );

            PendingSummon pending = new PendingSummon();
            pending.Position = position;
            pending.Timer = summonDelay;

            pendingSummons.Add(pending);
        }
    }

    private void UpdateSummons(float step)
    {
        for (int i = pendingSummons.Count - 1; i >= 0; i--)
        {
            PendingSummon pending = pendingSummons[i];

            pending.Timer -= step;

            if (pending.Timer > 0f)
                continue;

            SpawnSlime(pending.Position);
            pendingSummons.RemoveAt(i);
        }
    }

    private void SpawnSlime(Vector3 position)
    {
        Slime slime = Spawn(slimePrefab, position, transform.parent, Scaling);

        Room room = GetComponentInParent<Room>();

        if (room != null)
            room.AddEnemy(slime);
    }

    private List<Vector3> FindSummonPositions()
    {
        List<Vector3> positions = new List<Vector3>();

        float minDistance = circleCollider.radius + SummonRingGap;
        float maxDistance = minDistance + SummonRingDepth;
        int attempts = slimesPerSummon * 20;

        for (int i = 0; i < attempts && positions.Count < slimesPerSummon; i++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = Random.Range(minDistance, maxDistance);

            Vector3 candidate =
                transform.position +
                new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * distance;

            if (IsSummonSpotFree(candidate, positions))
                positions.Add(candidate);
        }

        return positions;
    }

    private bool IsSummonSpotFree(Vector3 position, List<Vector3> chosen)
    {
        foreach (Vector3 other in chosen)
        {
            if (Vector2.Distance(other, position) < SummonSpacing)
                return false;
        }

        foreach (Collider2D hit in
                 Physics2D.OverlapCircleAll(position, summonMarkerRadius))
        {
            if (hit.isTrigger || hit.GetComponentInParent<SlimeKing>() == this)
                continue;

            return false;
        }

        return true;
    }

    private void UpdateCrateTimer(float step)
    {
        crateTimer += step;

        if (crateTimer < crateInterval)
            return;

        crateTimer = 0f;
        DropCrates();
    }

    private void DropCrates()
    {
        Room room = GetComponentInParent<Room>();

        if (room == null || cratePrefab == null)
            return;

        List<Vector3> corners = room.GetCornerPositions();

        for (int i = corners.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Vector3 temp = corners[i];
            corners[i] = corners[j];
            corners[j] = temp;
        }

        int dropped = 0;

        foreach (Vector3 corner in corners)
        {
            if (dropped >= cratesPerDrop)
                break;

            if (IsCornerOccupied(corner))
                continue;

            Crate crate = Instantiate(
                cratePrefab, corner, Quaternion.identity, room.transform
            );

            crate.BeginFall(CrateFallHeight, CrateFallTime);
            droppedCrates[crate] = corner;

            GroundMarker.Spawn(corner, 0.5f, CrateFallTime, CrateMarkerColor);

            dropped++;
        }
    }

    private bool IsCornerOccupied(Vector3 corner)
    {
        foreach (KeyValuePair<Crate, Vector3> pair in droppedCrates)
        {
            if (pair.Key != null &&
                Vector2.Distance(pair.Value, corner) < CornerClearRadius)
                return true;
        }

        foreach (Collider2D hit in
                 Physics2D.OverlapCircleAll(corner, CornerClearRadius))
        {
            if (hit.GetComponent<Crate>() != null)
                return true;
        }

        return false;
    }

    private void CreateShadow()
    {
        GameObject shadowObject = new GameObject("Shadow");
        shadowObject.transform.SetParent(transform, false);
        shadowObject.transform.localPosition = new Vector3(0f, -0.2f, 0f);

        float diameter = circleCollider.radius * 2.2f;
        shadowBaseScale = new Vector3(diameter, diameter * 0.6f, 1f);
        shadowObject.transform.localScale = shadowBaseScale;

        SpriteRenderer shadowRenderer =
            shadowObject.AddComponent<SpriteRenderer>();

        shadowRenderer.sprite = UIFactory.CircleSprite;
        shadowRenderer.color = new Color(0f, 0f, 0f, ShadowAlpha);
        shadowRenderer.sortingOrder = ShadowSortingOrder;

        shadow = shadowObject.transform;
    }
}
