using UnityEngine;

public class RockSlime : Enemy
{
    private enum Mode
    {
        Resting,
        Winding,
        Charging
    }

    private const float ChargeSpeed = 9f;
    private const float WindupSeconds = 0.65f;
    private const float RestSeconds = 1f;
    private const int MaxBounces = 2;
    private const float MaxChargeSeconds = 3f;
    private const float ProbeRadius = 0.4f;

    private Mode mode = Mode.Resting;
    private Vector2 direction = Vector2.right;
    private float timer;
    private int bounces;

    protected override string DefaultDescription =>
        "A heavy rock slime. It shakes, then charges straight at you and can " +
        "ricochet off walls up to two times before it runs out of steam.";

    public override string[] GetIndexStats()
    {
        return new[]
        {
            "Health 50",
            "Contact damage 1",
            "Charges, bounces off walls twice"
        };
    }

    protected override void Awake()
    {
        base.Awake();

        maxHealth = ScaleHealth(50);
        currentHealth = maxHealth;
        contactDamage = ScaleDamage(1);
        moveSpeed = ScaleSpeed(ChargeSpeed);
        timer = Random.Range(0.4f, 1f);
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
                    mode = Mode.Winding;
                    timer = WindupSeconds;

                    PlayTelegraph(WindupSeconds, new Color(0.8f, 0.7f, 1f, 1f), 0.25f);
                }

                break;

            case Mode.Winding:
                Body.linearVelocity = Vector2.zero;
                timer -= step;

                if (timer <= 0f)
                    BeginCharge();

                break;

            case Mode.Charging:
                UpdateCharge(step);
                break;
        }
    }

    private void BeginCharge()
    {
        Transform chased = GetTarget();

        if (chased != null)
        {
            Vector2 toTarget = (Vector2)chased.position - Body.position;

            if (toTarget.sqrMagnitude > 0.0001f)
                direction = toTarget.normalized;
        }

        bounces = 0;
        timer = MaxChargeSeconds;
        mode = Mode.Charging;
    }

    private void UpdateCharge(float step)
    {
        float speed = moveSpeed * SpeedMultiplier;

        timer -= step;

        RaycastHit2D hit = Physics2D.CircleCast(
            Body.position, ProbeRadius, direction, speed * step + 0.08f,
            PhysicsLayers.WallMask
        );

        if (hit.collider != null)
        {
            if (bounces >= MaxBounces)
            {
                EndCharge();
                return;
            }

            direction = Vector2.Reflect(direction, hit.normal).normalized;
            bounces++;
        }

        Body.linearVelocity = direction * speed;

        if (timer <= 0f)
            EndCharge();
    }

    private void EndCharge()
    {
        Body.linearVelocity = Vector2.zero;
        mode = Mode.Resting;
        timer = RestSeconds;
    }
}
