using UnityEngine;

public class Hedgehog : Enemy
{
    private enum Mode
    {
        Idle,
        Moving,
        Aiming
    }

    private const float Speed = 5.5f;
    private const float ShootChance = 0.45f;
    private const float AimSeconds = 0.7f;
    private const float SpikeSpeed = 6f;
    private const float SpikeSpread = 22f;

    private static readonly Color SpikeColor = new Color(0.55f, 0.4f, 0.25f, 1f);

    private Room room;
    private Mode mode = Mode.Idle;
    private Vector2 destination;
    private float timer;

    protected override string DefaultDescription =>
        "Darts quickly to random spots in the room. Now and then it stops and " +
        "shoots three spikes at once, the middle one aimed right at you.";

    public override string[] GetIndexStats()
    {
        return new[]
        {
            "Health 15",
            "Contact damage 1",
            "Fires 3 spikes when it stops"
        };
    }

    protected override void Awake()
    {
        base.Awake();

        maxHealth = ScaleHealth(15);
        currentHealth = maxHealth;
        contactDamage = ScaleDamage(1);
        moveSpeed = ScaleSpeed(Speed);
        timer = Random.Range(0.3f, 1f);
    }

    private void Start()
    {
        room = GetComponentInParent<Room>();
    }

    protected override void MoveTowardsTarget()
    {
        float step = Time.fixedDeltaTime;

        if (mode == Mode.Moving)
        {
            Vector2 toDestination = destination - Body.position;
            timer -= step;

            if (toDestination.magnitude < 0.3f || timer <= 0f)
            {
                mode = Mode.Idle;
                timer = Random.Range(0.5f, 1.2f);
                Body.linearVelocity = Vector2.zero;
                return;
            }

            Body.linearVelocity = toDestination.normalized * moveSpeed * SpeedMultiplier;
            return;
        }

        Body.linearVelocity = Vector2.zero;
        timer -= step;

        if (timer > 0f)
            return;

        if (mode == Mode.Aiming)
        {
            FireSpikes();
            mode = Mode.Idle;
            timer = 0.7f;
            return;
        }

        if (Random.value < ShootChance)
        {
            mode = Mode.Aiming;
            timer = AimSeconds;

            PlayTelegraph(AimSeconds, new Color(1f, 0.3f, 0.25f, 1f), 0.3f);
            return;
        }

        destination = room != null
            ? room.GetRandomFloorPoint()
            : Body.position + Random.insideUnitCircle * 4f;

        mode = Mode.Moving;
        timer = 3f;
    }

    private void FireSpikes()
    {
        Transform target = GetTarget();

        if (target == null)
            return;

        Vector2 aim = (Vector2)target.position - Body.position;

        for (int i = -1; i <= 1; i++)
        {
            EnemyProjectile.Spawn(
                Body.position, PowerFx.Rotate(aim, i * SpikeSpread),
                SpikeSpeed, ScaleDamage(1), 0.22f, SpikeColor
            );
        }
    }
}
