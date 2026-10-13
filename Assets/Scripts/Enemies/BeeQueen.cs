using System.Collections.Generic;
using UnityEngine;

public class BeeQueen : Enemy
{
    private const float ArriveDistance = 0.6f;
    private const float MaxTravelSeconds = 4f;
    private const float MinTravelDistance = 4f;
    private const float AngryBonus = 0.35f;

    private static readonly Color SummonColor = new Color(1f, 0.85f, 0.2f, 0.5f);

    [Header("Stats")]
    [SerializeField] private int health = 150;
    [SerializeField] private int touchDamage = 1;

    [Header("Zigzag flight")]
    [SerializeField] private float flySpeed = 3.2f;
    [SerializeField] private float zigzagFrequency = 3.5f;
    [SerializeField] private float zigzagStrength = 0.9f;

    [Header("Bee summons")]
    [SerializeField] private Bee beePrefab;
    [SerializeField] private float summonInterval = 7f;
    [SerializeField] private float firstSummonDelay = 3f;
    [SerializeField] private int beesPerSummon = 3;
    [SerializeField] private int maxBees = 10;
    [SerializeField] private float summonDelay = 1f;
    [SerializeField] private float summonMarkerRadius = 0.5f;

    [Header("Falling crates")]
    [SerializeField] private Crate cratePrefab;
    [SerializeField] private float crateInterval = 30f;
    [SerializeField] private int cratesPerDrop = 2;

    private class PendingBee
    {
        public Vector3 Position;
        public float Timer;
    }

    private readonly List<PendingBee> pendingBees = new List<PendingBee>();
    private readonly BossCrateDropper crates = new BossCrateDropper();

    private Room room;
    private Vector2 destination;
    private bool hasDestination;
    private float travelTimer;
    private float zigzagClock;
    private float summonTimer;
    private bool angry;

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
        "The queen of the bees. She zigzags around the room and keeps calling " +
        "new bees. Hurting her angers every bee in the room, just like hurting " +
        "a normal bee does.";

    public override string[] GetIndexStats()
    {
        return new[]
        {
            "Health " + health,
            "Contact damage " + touchDamage,
            "Summons bees",
            "Hurting her angers all bees"
        };
    }

    protected override void Awake()
    {
        base.Awake();

        maxHealth = ScaleHealth(health);
        currentHealth = maxHealth;
        contactDamage = ScaleDamage(touchDamage);
        moveSpeed = ScaleSpeed(flySpeed);
        summonTimer = firstSummonDelay;
        zigzagClock = Random.Range(0f, 6f);
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

        crates.Tick(step, crateInterval, cratesPerDrop, cratePrefab, CurrentRoom);

        UpdateSummons(step);

        base.FixedUpdate();
    }

    public override void TakeDamage(int amount)
    {
        base.TakeDamage(amount);

        Provoke();
    }

    private void Provoke()
    {
        angry = true;

        Bee.AngerRoom(CurrentRoom);
    }

    protected override void MoveTowardsTarget()
    {
        float step = Time.fixedDeltaTime;

        travelTimer -= step;
        zigzagClock += step;

        if (!hasDestination ||
            travelTimer <= 0f ||
            Vector2.Distance(Body.position, destination) <= ArriveDistance)
            ChooseDestination();

        Vector2 direction = (destination - Body.position).normalized;
        Vector2 sideways = new Vector2(-direction.y, direction.x);

        float speed = moveSpeed * (angry ? 1f + AngryBonus : 1f);
        float wave = Mathf.Sin(zigzagClock * zigzagFrequency) * zigzagStrength;

        Vector2 velocity = direction * speed + sideways * (speed * wave);

        Body.linearVelocity =
            Vector2.ClampMagnitude(velocity, speed * 1.5f) * SpeedMultiplier;
    }

    private void ChooseDestination()
    {
        Room current = CurrentRoom;

        hasDestination = true;
        travelTimer = MaxTravelSeconds;

        if (current == null)
        {
            destination = Body.position + Random.insideUnitCircle * 5f;
            return;
        }

        Vector2 best = current.GetRandomFloorPoint();

        for (int i = 0; i < 12; i++)
        {
            Vector2 candidate = current.GetRandomFloorPoint();

            if (Vector2.Distance(candidate, Body.position) >= MinTravelDistance)
            {
                best = candidate;
                break;
            }

            best = candidate;
        }

        destination = best;
    }

    private int CountBees()
    {
        Room current = CurrentRoom;

        if (current == null)
            return 0;

        int count = 0;

        foreach (Enemy enemy in current.Enemies)
        {
            if (enemy is Bee && enemy != null)
                count++;
        }

        return count;
    }

    private void UpdateSummons(float step)
    {
        for (int i = pendingBees.Count - 1; i >= 0; i--)
        {
            PendingBee pending = pendingBees[i];

            pending.Timer -= step;

            if (pending.Timer > 0f)
                continue;

            SpawnBee(pending.Position);
            pendingBees.RemoveAt(i);
        }

        summonTimer -= step;

        if (summonTimer > 0f)
            return;

        summonTimer = summonInterval;

        SummonBees();
    }

    private void SummonBees()
    {
        Room current = CurrentRoom;

        if (beePrefab == null || current == null)
            return;

        int free = maxBees - CountBees() - pendingBees.Count;
        int count = Mathf.Min(beesPerSummon, free);

        for (int i = 0; i < count; i++)
        {
            Vector3 position = current.GetRandomFloorPoint();

            GroundMarker.Spawn(position, summonMarkerRadius, summonDelay, SummonColor);

            pendingBees.Add(new PendingBee
            {
                Position = position,
                Timer = summonDelay
            });
        }
    }

    private void SpawnBee(Vector3 position)
    {
        Bee bee = Spawn(beePrefab, position, transform.parent, Scaling);

        Room current = CurrentRoom;

        if (current != null)
            current.AddEnemy(bee);

        if (angry)
            bee.BecomeAggressive();
    }
}
