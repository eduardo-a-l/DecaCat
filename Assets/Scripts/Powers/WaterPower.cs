using UnityEngine;

public class WaterPower : PowerBehaviour
{
    private const int StacksNeeded = 4;
    private const float LockSeconds = 10f;
    private const int MinionCount = 3;

    private static readonly Color MinionColor = new Color(0.2f, 0.85f, 0.85f, 1f);

    private int energy;
    private float specialLockUntil;

    public override float SpecialCooldown => 8f;
    public override bool IsSpecialBlocked => Time.time < specialLockUntil;

    public override void OnDamaged()
    {
        specialLockUntil = Time.time + LockSeconds;
    }

    public override void OnAttackEnded(AttackContext attack, int hits)
    {
        if (energy >= StacksNeeded)
        {
            energy = 0;
            FireWave(attack);
        }
        else if (hits > 0)
        {
            energy++;
        }
    }

    public override bool UseSpecial(Vector2 direction)
    {
        AllyMinion.DestroyAll();

        for (int i = 0; i < MinionCount; i++)
        {
            Vector2 offset = PowerFx.Rotate(Vector2.right, i * 360f / MinionCount) * 0.9f;

            AllyMinion.Spawn(
                Owner.Position + offset, Owner.transform,
                Owner.PlayerColliders, MinionColor
            );
        }

        return true;
    }

    public override string GetStatusText()
    {
        float lockLeft = specialLockUntil - Time.time;

        if (lockLeft > 0f)
            return "Special locked " + Mathf.CeilToInt(lockLeft) + "s";

        return energy >= StacksNeeded
            ? "Tidal wave ready"
            : "Energy " + energy + "/" + StacksNeeded;
    }

    private void FireWave(AttackContext attack)
    {
        ProjectileSettings wave = new ProjectileSettings
        {
            Direction = attack.Direction,
            Speed = 9f,
            Damage = Mathf.Max(2, attack.BaseDamage * 2),
            Radius = 1f,
            Size = new Vector2(0.7f, 2.2f),
            Lifetime = 1.1f,
            Color = new Color(0.3f, 0.85f, 1f, 0.75f),
            Pierce = true,
            IgnoreWalls = true,
            Knockback = 3f
        };

        PowerProjectile.Spawn(attack.Origin + attack.Direction * 0.8f, wave);
    }
}
