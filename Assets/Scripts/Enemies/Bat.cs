using UnityEngine;

public class Bat : Enemy
{
    private enum Mode
    {
        Resting,
        Moving,
        Winding
    }

    private const float Speed = 3.6f;
    private const float SafeRadius = 5.5f;
    private const float ArriveDistance = 0.25f;
    private const float MaxTravelSeconds = 3.5f;
    private const float WindupSeconds = 0.55f;
    private const float ProjectileSpeed = 4.8f;
    private const float ProjectileSize = 0.3f;
    private const int DestinationAttempts = 16;

    private static readonly Color ProjectileColor = new Color(0.75f, 0.35f, 0.95f, 1f);

    private Room room;
    private Mode mode = Mode.Resting;
    private Vector2 destination;
    private float timer;

    public override bool CanWalkOverObstacles => true;

    protected override string DefaultDescription =>
        "Keeps its distance and flutters to a new spot whenever you get too " +
        "close. When it lands it screeches, then fires three shots spread " +
        "evenly around itself.";

    public override string[] GetIndexStats()
    {
        return new[]
        {
            "Health 15",
            "Contact damage 1",
            "Fires 3 shots in a circle"
        };
    }

    protected override void Awake()
    {
        base.Awake();

        maxHealth = ScaleHealth(15);
        currentHealth = maxHealth;
        contactDamage = ScaleDamage(1);
        moveSpeed = ScaleSpeed(Speed);
        timer = Random.Range(0.2f, 0.7f);
    }

    private void Start()
    {
        room = GetComponentInParent<Room>();
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
                {
                    destination = ChooseDestination();
                    mode = Mode.Moving;
                    timer = MaxTravelSeconds;
                }

                break;

            case Mode.Moving:
                Vector2 toDestination = destination - Body.position;
                timer -= step;

                if (toDestination.magnitude <= ArriveDistance || timer <= 0f)
                {
                    Body.linearVelocity = Vector2.zero;
                    mode = Mode.Winding;
                    timer = WindupSeconds;

                    PlayTelegraph(WindupSeconds, new Color(1f, 0.35f, 0.9f, 1f), 0.25f);
                    break;
                }

                Body.linearVelocity =
                    toDestination.normalized * moveSpeed * SpeedMultiplier;

                break;

            case Mode.Winding:
                Body.linearVelocity = Vector2.zero;
                timer -= step;

                if (timer <= 0f)
                {
                    Fire();
                    mode = Mode.Resting;
                    timer = Random.Range(0.6f, 1.2f);
                }

                break;
        }
    }

    private Vector2 ChooseDestination()
    {
        Transform chased = GetTarget();

        if (room == null)
            return Body.position + Random.insideUnitCircle * 4f;

        if (chased == null)
            return room.GetRandomFloorPoint();

        Vector2 player = chased.position;
        Vector2 farthest = room.GetRandomFloorPoint();
        float farthestDistance = Vector2.Distance(farthest, player);
        int valid = 0;
        Vector2 picked = farthest;

        for (int i = 0; i < DestinationAttempts; i++)
        {
            Vector2 candidate = room.GetRandomFloorPoint();
            float distance = Vector2.Distance(candidate, player);

            if (distance > farthestDistance)
            {
                farthest = candidate;
                farthestDistance = distance;
            }

            if (distance < SafeRadius)
                continue;

            valid++;

            if (Random.Range(0, valid) == 0)
                picked = candidate;
        }

        return valid > 0 ? picked : farthest;
    }

    private void Fire()
    {
        Transform chased = GetTarget();

        Vector2 aim = chased != null
            ? (Vector2)chased.position - Body.position
            : Vector2.right;

        for (int i = 0; i < 3; i++)
        {
            EnemyProjectile.Spawn(
                Body.position, PowerFx.Rotate(aim, i * 120f),
                ProjectileSpeed, ScaleDamage(1), ProjectileSize, ProjectileColor
            );
        }
    }
}
