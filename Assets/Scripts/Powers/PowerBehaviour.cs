using UnityEngine;

public struct AttackContext
{
    public Vector2 Origin;
    public Vector2 Direction;
    public float Angle;
    public int BaseDamage;
    public int Damage;
}

public abstract class PowerBehaviour
{
    protected PlayerPower Owner { get; private set; }
    public PowerDefinition Definition { get; private set; }

    public virtual float SpeedMultiplier => 1f;
    public virtual float DamageMultiplier => 1f;
    public virtual float CooldownMultiplier => 1f;
    public virtual bool CanAttack => true;
    public virtual bool IsSpecialBlocked => false;
    public virtual float SpecialCooldown => 5f;

    public void Attach(PlayerPower owner, PowerDefinition definition)
    {
        Owner = owner;
        Definition = definition;
    }

    public virtual void OnGained()
    {
    }

    public virtual void OnLost()
    {
    }

    public virtual void Tick(float step)
    {
    }

    public virtual void OnAttackBlocked()
    {
    }

    public virtual void OnHit(AttackContext attack, IDamageable target)
    {
    }

    public virtual void OnAttackEnded(AttackContext attack, int hits)
    {
    }

    public virtual bool TryAbsorbDamage(DamageElement element)
    {
        return false;
    }

    public virtual void OnDamaged()
    {
    }

    public virtual void OnEnemyKilled(Enemy enemy, bool wasBurning, Vector3 position)
    {
    }

    public virtual int ModifyDroppedDurability(int durability)
    {
        return durability;
    }

    public virtual string GetStatusText()
    {
        return string.Empty;
    }

    public abstract bool UseSpecial(Vector2 direction);
}
