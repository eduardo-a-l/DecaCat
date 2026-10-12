using UnityEngine;

public class Bee : Enemy
{
    private const float ChaseSpeed = 3.4f;
    private const float OrbitMaxSpeed = 4.5f;

    private Room room;
    private Vector2 center;
    private float radius;
    private float angle;
    private float angularSpeed;
    private bool aggressive;

    public override bool CanWalkOverObstacles => true;

    protected override string DefaultDescription =>
        "Buzzes around the room in circles. Hit one and every bee in the room " +
        "swarms after you. It has no ranged attack.";

    public override string[] GetIndexStats()
    {
        return new[]
        {
            "Health 5",
            "Contact damage 1",
            "Hurting one angers all bees"
        };
    }

    protected override void Awake()
    {
        base.Awake();

        maxHealth = ScaleHealth(5);
        currentHealth = maxHealth;
        contactDamage = ScaleDamage(1);
        moveSpeed = ScaleSpeed(ChaseSpeed);

        radius = Random.Range(1.4f, 3.4f);
        angle = Random.Range(0f, Mathf.PI * 2f);
        angularSpeed = Random.Range(1.1f, 2f) * (Random.value < 0.5f ? -1f : 1f);
    }

    private void Start()
    {
        room = GetComponentInParent<Room>();

        center = room != null
            ? room.GetRandomFloorPoint()
            : (Vector2)transform.position;
    }

    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);

        Anger();
    }

    private void Anger()
    {
        aggressive = true;

        if (room == null)
            return;

        foreach (Enemy enemy in room.Enemies)
        {
            Bee bee = enemy as Bee;

            if (bee != null)
                bee.aggressive = true;
        }
    }

    protected override void MoveTowardsTarget()
    {
        if (aggressive)
        {
            Transform target = GetTarget();

            if (target != null)
            {
                Vector2 toTarget = (Vector2)target.position - Body.position;

                Body.linearVelocity = toTarget.normalized * moveSpeed * SpeedMultiplier;
            }

            return;
        }

        angle += angularSpeed * Time.fixedDeltaTime;

        Vector2 goal = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        Vector2 delta = goal - Body.position;

        Body.linearVelocity = Vector2.ClampMagnitude(
            delta * 6f, ScaleSpeed(OrbitMaxSpeed)
        ) * SpeedMultiplier;
    }
}
