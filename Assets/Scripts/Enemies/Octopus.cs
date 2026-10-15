using UnityEngine;

public class Octopus : Enemy
{
    private const float Speed = 2f;
    private const float ArriveDistance = 0.3f;
    private const float BurstInterval = 0.4f;
    private const int BurstsPerRun = 3;
    private const float RunPauseSeconds = 0.8f;
    private const float AngleStep = 24f;
    private const float ProjectileSpeed = 4.5f;
    private const float ProjectileSize = 0.3f;

    private static readonly Color ProjectileColor = new Color(0.9f, 0.55f, 1f, 1f);

    private Room room;
    private Vector2 home;
    private bool hasHome;
    private bool arrived;
    private float timer = 0.5f;
    private float angle;
    private int burstsLeft = BurstsPerRun;
    private float rotation = 1f;

    protected override string DefaultDescription =>
        "Settles near the middle of the room and fires three shots at a time in " +
        "a spiral, three bursts turning clockwise and then three turning the " +
        "other way.";

    public override string[] GetIndexStats()
    {
        return new[]
        {
            "Health 25",
            "Contact damage 1",
            "Spiral bursts of 3 shots",
            "Alternates clockwise and anticlockwise"
        };
    }

    protected override void Awake()
    {
        base.Awake();

        maxHealth = ScaleHealth(25);
        currentHealth = maxHealth;
        contactDamage = ScaleDamage(1);
        moveSpeed = ScaleSpeed(Speed);
        angle = Random.Range(0f, 360f);
    }

    private void Start()
    {
        room = GetComponentInParent<Room>();
    }

    protected override void MoveTowardsTarget()
    {
        if (!arrived)
        {
            WalkHome();
            return;
        }

        Body.linearVelocity = Vector2.zero;

        timer -= Time.fixedDeltaTime;

        if (timer > 0f)
            return;

        FireBurst();
    }

    private void WalkHome()
    {
        if (!hasHome)
        {
            if (room == null)
                room = GetComponentInParent<Room>();

            if (room == null)
            {
                arrived = true;
                return;
            }

            home = (Vector2)room.Center + new Vector2(
                Random.Range(-1.5f, 1.5f), Random.Range(-1f, 1f)
            );

            hasHome = true;
        }

        Vector2 toHome = home - Body.position;

        if (toHome.magnitude <= ArriveDistance)
        {
            arrived = true;
            Body.linearVelocity = Vector2.zero;
            return;
        }

        Body.linearVelocity = toHome.normalized * moveSpeed * SpeedMultiplier;
    }

    private void FireBurst()
    {
        for (int i = 0; i < 3; i++)
        {
            Vector2 direction = PowerFx.Rotate(Vector2.right, angle + 120f * i);

            EnemyProjectile.Spawn(
                Body.position, direction, ProjectileSpeed,
                ScaleDamage(1), ProjectileSize, ProjectileColor
            );
        }

        angle += AngleStep * rotation;
        burstsLeft--;
        timer = BurstInterval;

        if (burstsLeft > 0)
            return;

        burstsLeft = BurstsPerRun;
        rotation = -rotation;
        timer = RunPauseSeconds;

        PlayTelegraph(RunPauseSeconds, new Color(1f, 0.7f, 1f, 1f), 0.15f);
    }
}
