using UnityEngine;

public class CrystalPower : PowerBehaviour
{
    private const float HeatPerAttack = 15f;
    private const float CoolPerSecond = 22f;
    private const float OverheatAt = 70f;
    private const float ClearAt = 25f;
    private const float StrainHeat = 10f;
    private const float MaxHeat = 100f;
    private const float PrismSeconds = 10f;

    private float heat;
    private bool overheated;

    public override float SpecialCooldown => 14f;
    public override bool CanAttack => !overheated;

    public override void Tick(float step)
    {
        heat = Mathf.Max(0f, heat - CoolPerSecond * step);

        if (overheated && heat <= ClearAt)
            overheated = false;
    }

    public override void OnAttackBlocked()
    {
        heat += StrainHeat;

        ZoneEffect.Spawn(Owner.Position, 0.6f, new Color(1f, 0.4f, 0.3f, 0.7f), 0.25f, true, 0.4f, 6);

        if (heat < MaxHeat)
            return;

        heat = 60f;

        if (Owner.Health != null)
            Owner.Health.TakeDamage(1);
    }

    public override void OnAttackEnded(AttackContext attack, int hits)
    {
        FireCrystal(attack);

        heat += HeatPerAttack;

        if (heat >= OverheatAt)
            overheated = true;
    }

    public override bool UseSpecial(Vector2 direction)
    {
        CrystalPrism.Spawn(Owner.Position, PrismSeconds, Definition.Accent);

        return true;
    }

    public override string GetStatusText()
    {
        return overheated
            ? "OVERHEATED"
            : "Heat " + Mathf.RoundToInt(heat) + "%";
    }

    private void FireCrystal(AttackContext attack)
    {
        ProjectileSettings crystal = new ProjectileSettings
        {
            Direction = attack.Direction,
            Speed = 12f,
            Damage = Mathf.Max(1, attack.BaseDamage / 2),
            Radius = 0.25f,
            Size = new Vector2(0.5f, 0.22f),
            Lifetime = 1.3f,
            Color = Definition.Accent,
            Sprite = PowerFx.Square,
            SplitCount = 2,
            SplitDamage = Mathf.Max(1, attack.BaseDamage / 4),
            Amplifiable = true
        };

        PowerProjectile.Spawn(attack.Origin + attack.Direction * 0.6f, crystal);
    }
}
