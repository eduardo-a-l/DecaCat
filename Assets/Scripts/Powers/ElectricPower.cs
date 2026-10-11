using System.Collections.Generic;
using UnityEngine;

public class ElectricPower : PowerBehaviour
{
    private const float ChargePerSecond = 0.4f;
    private const float StunChance = 0.5f;
    private const float StunSeconds = 1.5f;
    private const float EnemySpeedBonus = 1.3f;
    private const float DashDistance = 6.5f;
    private const float DashSeconds = 0.22f;
    private const float DashRadius = 1.3f;
    private const int DashDamage = 18;

    private static readonly Color BoltColor = new Color(1f, 0.95f, 0.3f, 0.9f);

    private float energy;

    public override float CooldownMultiplier => 0.5f;
    public override float DamageMultiplier => 1f + energy;
    public override float SpecialCooldown => 6f;

    public override void OnGained()
    {
        Enemy.GlobalSpeedMultiplier = EnemySpeedBonus;
    }

    public override void OnLost()
    {
        Enemy.GlobalSpeedMultiplier = 1f;

        if (Owner.Renderer != null)
            Owner.Renderer.color = Color.white;
    }

    public override void Tick(float step)
    {
        if (Owner.IsMoving)
            energy = Mathf.Min(1f, energy + ChargePerSecond * step);
    }

    public override void OnHit(AttackContext attack, IDamageable target)
    {
        Enemy enemy = target as Enemy;

        if (enemy != null && Random.value < energy * StunChance)
            enemy.Stun(StunSeconds);
    }

    public override void OnAttackEnded(AttackContext attack, int hits)
    {
        if (energy <= 0.05f)
            return;

        ZoneEffect.Spawn(attack.Origin, 0.5f + energy, BoltColor, 0.3f, true, 0.3f, 6);

        energy = 0f;
    }

    public override bool UseSpecial(Vector2 direction)
    {
        HashSet<Enemy> struck = new HashSet<Enemy>();

        Owner.Health.GrantInvulnerability(DashSeconds + 0.3f);

        if (Owner.Renderer != null)
            Owner.Renderer.color = BoltColor;

        Owner.StartDash(direction, DashDistance, DashSeconds, true, position =>
        {
            ZoneEffect.Spawn(position, 0.6f, BoltColor, 0.3f, false, 0.4f, 6);

            foreach (Enemy enemy in PowerFx.GetEnemiesInRadius(position, DashRadius))
            {
                if (!struck.Add(enemy))
                    continue;

                enemy.TakeDamage(DashDamage);
                enemy.Stun(1f);
            }
        });

        Owner.StartCoroutine(RestoreColor());

        return true;
    }

    public override string GetStatusText()
    {
        return "Charge " + Mathf.RoundToInt(energy * 100f) + "%";
    }

    private System.Collections.IEnumerator RestoreColor()
    {
        yield return new WaitForSeconds(DashSeconds + 0.1f);

        if (Owner != null && Owner.Renderer != null)
            Owner.Renderer.color = Color.white;
    }
}
