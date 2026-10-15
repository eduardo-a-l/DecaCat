using UnityEngine;

public class FireSpirit : Enemy
{
    private enum Mode
    {
        Following,
        Winding,
        Anchored
    }

    private const float Speed = 2.2f;
    private const float TrailInterval = 0.45f;
    private const float TrailRadius = 0.3f;
    private const float TrailSeconds = 2f;
    private const float AreaWindupSeconds = 0.8f;
    private const float AreaRadius = 1.6f;
    private const float AreaSeconds = 5f;

    private static readonly Color MarkerColor = new Color(1f, 0.45f, 0.1f, 0.5f);

    private Mode mode = Mode.Following;
    private float trailTimer = TrailInterval;
    private float areaTimer;
    private float windupTimer;
    private FireHazard circle;

    protected override string DefaultDescription =>
        "A flickering spirit that follows you and leaves small fires behind it " +
        "that burn for two seconds. Now and then it stops to ignite a big " +
        "circle of fire for five seconds and guards it until it burns out.";

    public override string[] GetIndexStats()
    {
        return new[]
        {
            "Health 10",
            "Contact damage 1",
            "Leaves a trail of fire",
            "Sometimes ignites a big circle"
        };
    }

    protected override void Awake()
    {
        base.Awake();

        maxHealth = ScaleHealth(10);
        currentHealth = maxHealth;
        contactDamage = ScaleDamage(1);
        moveSpeed = ScaleSpeed(Speed);
        areaTimer = Random.Range(5f, 9f);
    }

    protected override void MoveTowardsTarget()
    {
        float step = Time.fixedDeltaTime;

        switch (mode)
        {
            case Mode.Following:
                Follow(step);
                break;

            case Mode.Winding:
                Body.linearVelocity = Vector2.zero;
                windupTimer -= step;

                if (windupTimer <= 0f)
                    IgniteCircle();

                break;

            case Mode.Anchored:
                Body.linearVelocity = Vector2.zero;

                if (circle == null)
                {
                    mode = Mode.Following;
                    areaTimer = Random.Range(7f, 11f);
                }

                break;
        }
    }

    private void Follow(float step)
    {
        base.MoveTowardsTarget();

        trailTimer -= step;

        if (trailTimer <= 0f)
        {
            trailTimer = TrailInterval;

            FireHazard.Spawn(Body.position, TrailRadius, TrailSeconds, ScaleDamage(1));
        }

        areaTimer -= step;

        if (areaTimer > 0f)
            return;

        mode = Mode.Winding;
        windupTimer = AreaWindupSeconds;
        Body.linearVelocity = Vector2.zero;

        PlayTelegraph(AreaWindupSeconds, new Color(1f, 0.5f, 0.15f, 1f), 0.3f);
        GroundMarker.Spawn(Body.position, AreaRadius, AreaWindupSeconds, MarkerColor);
    }

    private void IgniteCircle()
    {
        circle = FireHazard.Spawn(Body.position, AreaRadius, AreaSeconds, ScaleDamage(1));
        mode = Mode.Anchored;
    }
}
