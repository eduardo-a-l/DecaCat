using UnityEngine;

public class CloudPower : PowerBehaviour
{
    private const float FogRadius = 3.2f;
    private const float FogSlow = 0.65f;
    private const float FogRefresh = 0.15f;
    private const float KnockbackDistance = 1.6f;
    private const float RainSeconds = 20f;
    private const float ThunderChance = 0.2f;

    private SpriteRenderer fog;
    private float fogTimer;

    public override float SpecialCooldown => Mathf.Max(0.5f, RainEffect.Remaining);

    public override bool IsSpecialBlocked => RainEffect.IsActive;

    public override void OnGained()
    {
        fog = PowerFx.CreateSprite(
            "Fog", PowerFx.Circle, new Color(1f, 1f, 1f, 0.13f),
            Owner.Position, Vector2.one * FogRadius * 2f, -2, Owner.transform
        );
    }

    public override void OnLost()
    {
        if (fog != null)
            Object.Destroy(fog.gameObject);
    }

    public override void Tick(float step)
    {
        fogTimer -= step;

        if (fogTimer > 0f)
            return;

        fogTimer = FogRefresh;

        foreach (Enemy enemy in PowerFx.GetEnemiesInRadius(Owner.Position, FogRadius))
            enemy.ApplySlow(FogSlow, 0.4f);
    }

    public override void OnHit(AttackContext attack, IDamageable target)
    {
        IPushable pushable = target as IPushable;

        if (pushable != null)
            pushable.Push(attack.Origin, KnockbackDistance);
    }

    public override int ModifyDroppedDurability(int durability)
    {
        return Mathf.Max(1, durability / 2);
    }

    public override bool UseSpecial(Vector2 direction)
    {
        if (RainEffect.IsActive)
            return false;

        RainEffect.Begin(RainSeconds, Random.value < ThunderChance);

        return true;
    }

    public override string GetStatusText()
    {
        return RainEffect.IsActive
            ? "Rain " + Mathf.CeilToInt(RainEffect.Remaining) + "s"
            : string.Empty;
    }
}
