using UnityEngine;

public class FirePower : PowerBehaviour
{
    private const float TrailInterval = 0.25f;
    private const float TrailMinDistance = 0.25f;
    private const float PatchRadius = 0.45f;
    private const float PatchSeconds = 5f;
    private const float BurnSeconds = 2f;
    private const float EvadeChance = 0.8f;
    private const float SpecialRadius = 2.4f;
    private const int SpecialDamage = 20;
    private const float DeathBurstRadius = 1.6f;
    private const int DeathBurstDamage = 4;

    private static readonly Color PatchColor = new Color(1f, 0.5f, 0.1f, 0.6f);

    private float trailTimer;
    private Vector2 lastPatch = new Vector2(float.MaxValue, float.MaxValue);

    public override float DamageMultiplier => 1.2f;
    public override float SpecialCooldown => 5f;

    public override void Tick(float step)
    {
        trailTimer -= step;

        if (!Owner.IsMoving || trailTimer > 0f)
            return;

        Vector2 position = Owner.Position;

        if (Vector2.Distance(position, lastPatch) < TrailMinDistance)
            return;

        FirePatch.Spawn(position, PatchRadius, PatchSeconds, PatchColor, BurnSeconds);

        trailTimer = TrailInterval;
        lastPatch = position;
    }

    public override bool TryAbsorbDamage(DamageElement element)
    {
        if (element != DamageElement.Fire || Random.value >= EvadeChance)
            return false;

        ZoneEffect.Spawn(Owner.Position, 0.8f, Definition.Accent, 0.4f, true, 0.3f, 6);

        return true;
    }

    public override bool UseSpecial(Vector2 direction)
    {
        Vector2 center = Owner.Position;

        foreach (Enemy enemy in PowerFx.GetEnemiesInRadius(center, SpecialRadius))
        {
            enemy.TakeDamage(SpecialDamage);
            enemy.Burn(BurnSeconds);
        }

        ZoneEffect.Spawn(center, SpecialRadius, new Color(1f, 0.45f, 0.1f, 0.6f), 0.5f, false, 0.2f, 6);
        ZoneEffect.Spawn(center, SpecialRadius, new Color(1f, 0.85f, 0.3f, 0.9f), 0.5f, true, 0.2f, 6);

        return true;
    }

    public override void OnEnemyKilled(Enemy enemy, bool wasBurning, Vector3 position)
    {
        if (!wasBurning)
            return;

        foreach (Enemy other in PowerFx.GetEnemiesInRadius(position, DeathBurstRadius))
        {
            if (other != enemy)
                other.TakeDamage(DeathBurstDamage);
        }

        float playerDistance = Vector2.Distance(Owner.Position, position);

        if (playerDistance <= DeathBurstRadius && Owner.Health != null)
            Owner.Health.TakeDamage(1);

        ZoneEffect.Spawn(position, DeathBurstRadius, new Color(1f, 0.5f, 0.15f, 0.65f), 0.35f, false, 0.2f, 6);
    }
}
