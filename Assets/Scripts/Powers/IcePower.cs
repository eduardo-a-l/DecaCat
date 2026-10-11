using UnityEngine;

public class IcePower : PowerBehaviour
{
    private const float SlowFactor = 0.5f;
    private const float SlowSeconds = 5f;
    private const float FreezeChance = 0.1f;
    private const float FreezeSeconds = 10f;
    private const float EvadeChance = 0.8f;
    private const float SpecialSlowFactor = 0.4f;
    private const float SpecialSeconds = 20f;
    private const float PlayerSpeedPenalty = 0.5f;

    public override float SpeedMultiplier => PlayerSpeedPenalty;
    public override float SpecialCooldown => 8f;

    public override void OnHit(AttackContext attack, IDamageable target)
    {
        Enemy enemy = target as Enemy;

        if (enemy == null)
            return;

        enemy.ApplySlow(SlowFactor, SlowSeconds);

        if (Random.value < FreezeChance)
            enemy.Freeze(FreezeSeconds);
    }

    public override bool TryAbsorbDamage(DamageElement element)
    {
        if (element != DamageElement.Ice || Random.value >= EvadeChance)
            return false;

        ZoneEffect.Spawn(Owner.Position, 0.8f, Definition.Accent, 0.4f, true, 0.3f, 6);

        return true;
    }

    public override bool UseSpecial(Vector2 direction)
    {
        var enemies = PowerFx.GetRoomEnemies();

        if (enemies.Count == 0)
            return false;

        foreach (Enemy enemy in enemies)
            enemy.ApplySlow(SpecialSlowFactor, SpecialSeconds);

        Color wave = Definition.Accent;
        wave.a = 0.35f;

        ZoneEffect.Spawn(Owner.Position, 10f, wave, 0.9f, false, 0.1f, 6);
        ZoneEffect.Spawn(Owner.Position, 10f, Definition.Accent, 0.9f, true, 0.1f, 6);

        return true;
    }
}
