using UnityEngine;

public class BunnyPower : PowerBehaviour
{
    private const float ShieldBreakSeconds = 30f;
    private const float ShieldGrace = 0.6f;
    private const float DashDistance = 5.5f;
    private const float DashSeconds = 0.25f;
    private const float DashRadius = 0.9f;
    private const int DashDamage = 8;
    private const float DashInvulnerability = 5f;

    private SpriteRenderer shield;
    private bool shieldUp = true;
    private float shieldReadyAt;

    public override float SpecialCooldown => 8f;

    public override void OnGained()
    {
        shieldUp = true;

        shield = PowerFx.CreateSprite(
            "Shield", PowerFx.Ring, new Color(0.8f, 0.95f, 1f, 0.8f),
            Owner.Position, Vector2.one * 1.5f, 3, Owner.transform
        );
    }

    public override void OnLost()
    {
        if (shield != null)
            Object.Destroy(shield.gameObject);
    }

    public override void Tick(float step)
    {
        if (!shieldUp && Time.time >= shieldReadyAt)
        {
            shieldUp = true;
            shield.enabled = true;

            ZoneEffect.Spawn(Owner.Position, 1f, new Color(0.8f, 0.95f, 1f, 0.7f), 0.4f, true, 0.4f, 6);
        }

        if (shieldUp && shield != null)
        {
            float pulse = 1.5f + 0.06f * Mathf.Sin(Time.time * 5f);
            float native = Mathf.Max(0.0001f, PowerFx.Ring.bounds.size.x);

            shield.transform.localScale = Vector3.one * (pulse / native);
        }
    }

    public override bool TryAbsorbDamage(DamageElement element)
    {
        if (!shieldUp)
            return false;

        shieldUp = false;
        shieldReadyAt = Time.time + ShieldBreakSeconds;

        if (shield != null)
            shield.enabled = false;

        if (Owner.Health != null)
            Owner.Health.GrantInvulnerability(ShieldGrace);

        ZoneEffect.Spawn(Owner.Position, 1.3f, new Color(0.8f, 0.95f, 1f, 0.8f), 0.4f, true, 0.4f, 6);

        return true;
    }

    public override bool UseSpecial(Vector2 direction)
    {
        System.Collections.Generic.HashSet<Enemy> struck =
            new System.Collections.Generic.HashSet<Enemy>();

        Owner.Health.GrantInvulnerability(DashInvulnerability);

        Owner.StartDash(direction, DashDistance, DashSeconds, false, position =>
        {
            ZoneEffect.Spawn(position, 0.55f, new Color(1f, 1f, 1f, 0.55f), 0.3f, false, 0.4f, 6);

            foreach (Enemy enemy in PowerFx.GetEnemiesInRadius(position, DashRadius))
            {
                if (struck.Add(enemy))
                    enemy.TakeDamage(DashDamage);
            }
        });

        return true;
    }

    public override string GetStatusText()
    {
        if (shieldUp)
            return "Shield ready";

        return "Shield " + Mathf.CeilToInt(shieldReadyAt - Time.time) + "s";
    }
}
