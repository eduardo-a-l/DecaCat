using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeAttack : MonoBehaviour
{
    private const int SweepSamples = 5;
    private const float PlayerDistanceTieBreak = 0.001f;

    [SerializeField] private PlayerItem playerItem;
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private WeaponAiming weaponAiming;

    [Header("Unarmed")]
    public const int DefaultUnarmedDamage = 1;
    public const float DefaultUnarmedCooldown = 0.25f;

    [SerializeField] private int unarmedDamage = DefaultUnarmedDamage;
    [SerializeField] private float unarmedCooldown = DefaultUnarmedCooldown;
    [SerializeField] private float unarmedRange = 0.6f;
    [SerializeField] private float unarmedRadius = 0.4f;
    [SerializeField] private float unarmedSwingAngle = 80f;
    [SerializeField] private float unarmedSwingDuration = 0.1f;
    [SerializeField] private Color unarmedEffectColor =
        new Color(1f, 1f, 1f, 0.6f);

    private PlayerInputReader inputReader;
    private float nextAttackTime;
    private bool exhausted;
    private float exhaustTotal;
    private Coroutine swingCoroutine;

    public float CooldownRemaining
    {
        get
        {
            return Mathf.Max(0f, nextAttackTime - Time.time);
        }
    }

    public bool CooldownExhausted => exhausted && CooldownRemaining > 0f;

    public float ExhaustTotal => exhaustTotal;

    public float CurrentCooldown
    {
        get
        {
            if (playerItem == null || playerItem.CurrentItem == null)
                return unarmedCooldown;

            return playerItem.CurrentItem.Cooldown;
        }
    }

    private void Awake()
    {
        inputReader = GetComponent<PlayerInputReader>();
    }

    private void OnEnable()
    {
        if (playerItem != null)
            playerItem.ItemChanged += OnItemChanged;
    }

    private void OnDisable()
    {
        if (playerItem != null)
            playerItem.ItemChanged -= OnItemChanged;
    }

    private void OnItemChanged()
    {
        float remaining = CooldownRemaining;

        exhausted = remaining > 0.05f;
        exhaustTotal = remaining;
    }

    private void Update()
    {
        if (inputReader == null ||
            !inputReader.AttackPressed ||
            Time.time < nextAttackTime)
            return;

        PlayerPower power = PlayerPower.Instance;

        if (power != null && !power.CanAttack)
        {
            inputReader.AcknowledgeAttack();
            power.OnAttackBlocked();
            return;
        }

        Attack();
    }

    private void Attack()
    {
        if (playerItem == null)
            return;

        inputReader.AcknowledgeAttack();

        ItemData item = playerItem.CurrentItem;

        if (item == null)
        {
            Punch();
            return;
        }

        PlayerPower power = PlayerPower.Instance;

        exhausted = false;
        nextAttackTime = Time.time + item.Cooldown *
            (power != null ? power.CooldownMultiplier : 1f);

        Vector3 aimPosition = inputReader.AimWorldPosition;

        Vector2 direction =
            (Vector2)(aimPosition - transform.position);

        if (direction.sqrMagnitude < 0.001f)
            direction = Vector2.right;

        direction.Normalize();

        float angle = Mathf.Atan2(
            direction.y, direction.x
        ) * Mathf.Rad2Deg;

        if (SpecialWeapons.TryUse(item, playerItem, power, transform.position, direction, angle))
            return;

        if (item.IsRanged)
        {
            FireRanged(item, power, direction, angle);
            return;
        }

        if (swingCoroutine != null)
            StopCoroutine(swingCoroutine);

        if (weaponAiming != null)
            weaponAiming.BeginSwing();

        swingCoroutine = StartCoroutine(
            Swing(angle, item.SwingAngle, item.SwingDuration)
        );

        SwingEffect.Play(transform, angle, item);

        List<IDamageable> targets = CollectTargets(
            angle, item.AttackRange, item.AttackRadius,
            item.SwingAngle, item.HitsMultipleTargets
        );
        bool spendDurability = false;

        int damage = power != null ? power.ModifyDamage(item.Damage) : item.Damage;

        AttackContext context = new AttackContext
        {
            Origin = transform.position,
            Direction = direction,
            Angle = angle,
            BaseDamage = item.Damage,
            Damage = damage
        };

        foreach (IDamageable target in targets)
        {
            if (target.CostsDurability)
                spendDurability = true;

            target.TakeDamage(damage);

            if (power != null)
                power.OnHit(context, target);

            if (item.Knockback > 0f && target is IPushable pushable)
                pushable.Push(transform.position, item.Knockback);
        }

        if (power != null)
            power.OnAttackEnded(context, targets.Count);

        if (SpecialWeapons.AfterSwing(item, transform.position, direction))
            spendDurability = true;

        if (spendDurability)
            playerItem.UseItem();
    }

    private void FireRanged(
        ItemData item, PlayerPower power, Vector2 direction, float angle)
    {
        int damage = power != null ? power.ModifyDamage(item.Damage) : item.Damage;

        AttackContext context = new AttackContext
        {
            Origin = transform.position,
            Direction = direction,
            Angle = angle,
            BaseDamage = item.Damage,
            Damage = damage
        };

        ProjectileSettings shot = new ProjectileSettings
        {
            Direction = direction,
            Speed = item.ProjectileSpeed,
            Damage = damage,
            Radius = Mathf.Max(item.ProjectileSize.x, item.ProjectileSize.y) * 0.5f,
            Size = item.ProjectileSize,
            Lifetime = item.ProjectileLifetime,
            Color = item.ProjectileColor,
            Bounces = item.Bounces,
            OnEnemyHit = enemy =>
            {
                if (power != null)
                    power.OnHit(context, enemy);

                SpecialWeapons.ApplySuction(item, enemy);
            }
        };

        if (item.AimLock)
        {
            Enemy locked = FindLockTarget(transform.position, direction);

            if (locked != null)
            {
                shot.Homing = true;
                shot.LockTarget = locked;
                shot.HomingTurn = 3f;
            }
        }

        PowerProjectile.Spawn((Vector2)transform.position + direction * 0.6f, shot);

        if (power != null)
            power.OnAttackEnded(context, 0);

        playerItem.UseShot();
    }

    private static Enemy FindLockTarget(Vector2 origin, Vector2 direction)
    {
        Enemy best = null;
        float bestAngle = 25f;

        foreach (Enemy enemy in PowerFx.GetRoomEnemies())
        {
            Vector2 toEnemy = (Vector2)enemy.transform.position - origin;

            if (toEnemy.magnitude > 12f)
                continue;

            float angle = Vector2.Angle(direction, toEnemy);

            if (angle < bestAngle)
            {
                bestAngle = angle;
                best = enemy;
            }
        }

        return best;
    }

    private void Punch()
    {
        PlayerPower power = PlayerPower.Instance;

        exhausted = false;
        nextAttackTime = Time.time + unarmedCooldown *
            (power != null ? power.CooldownMultiplier : 1f);

        float angle = GetAimAngle();
        float radians = angle * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

        SwingEffect.Play(
            transform, angle, unarmedRange, unarmedRadius,
            unarmedSwingAngle, unarmedSwingDuration,
            null, unarmedEffectColor
        );

        List<IDamageable> targets = CollectTargets(
            angle, unarmedRange, unarmedRadius, unarmedSwingAngle, false
        );

        int damage = power != null ? power.ModifyDamage(unarmedDamage) : unarmedDamage;

        AttackContext context = new AttackContext
        {
            Origin = transform.position,
            Direction = direction,
            Angle = angle,
            BaseDamage = unarmedDamage,
            Damage = damage
        };

        foreach (IDamageable target in targets)
        {
            target.TakeDamage(damage);

            if (power != null)
                power.OnHit(context, target);
        }

        if (power != null)
            power.OnAttackEnded(context, targets.Count);
    }

    private float GetAimAngle()
    {
        Vector2 direction =
            (Vector2)(inputReader.AimWorldPosition - transform.position);

        if (direction.sqrMagnitude < 0.001f)
            direction = Vector2.right;

        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    private List<IDamageable> CollectTargets(
        float aimAngle, float attackRange, float attackRadius,
        float swingAngle, bool hitsMultipleTargets)
    {
        Dictionary<IDamageable, float> scores =
            new Dictionary<IDamageable, float>();

        Vector2 origin = transform.position;
        int samples = hitsMultipleTargets ? SweepSamples : 1;

        for (int i = 0; i < samples; i++)
        {
            float angle = aimAngle;

            if (samples > 1)
            {
                float t = (float)i / (samples - 1);
                angle = aimAngle - swingAngle / 2f + swingAngle * t;
            }

            float radians = angle * Mathf.Deg2Rad;

            Vector2 center =
                origin +
                new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) *
                attackRange;

            Collider2D[] hits =
                Physics2D.OverlapCircleAll(center, attackRadius);

            foreach (Collider2D hit in hits)
            {
                IDamageable target = hit.GetComponentInParent<IDamageable>();

                if (target == null)
                    continue;

                float score =
                    Vector2.Distance(center, hit.ClosestPoint(center)) +
                    Vector2.Distance(origin, hit.bounds.center) *
                    PlayerDistanceTieBreak;

                if (!scores.TryGetValue(target, out float best) || score < best)
                    scores[target] = score;
            }
        }

        if (hitsMultipleTargets)
            return new List<IDamageable>(scores.Keys);

        IDamageable closest = null;
        float closestScore = float.MaxValue;

        foreach (KeyValuePair<IDamageable, float> pair in scores)
        {
            if (pair.Value < closestScore)
            {
                closestScore = pair.Value;
                closest = pair.Key;
            }
        }

        List<IDamageable> result = new List<IDamageable>();

        if (closest != null)
            result.Add(closest);

        return result;
    }

    private IEnumerator Swing(
        float angle, float swingAngle, float swingDuration)
    {
        float elapsed = 0f;

        while (elapsed < swingDuration)
        {
            float progress = elapsed / swingDuration;

            float currentAngle =
                angle - swingAngle / 2f +
                swingAngle * progress;

            weaponPivot.rotation =
                Quaternion.Euler(0f, 0f, currentAngle);

            elapsed += Time.deltaTime;
            yield return null;
        }

        weaponPivot.rotation =
            Quaternion.Euler(0f, 0f, angle);

        swingCoroutine = null;

        if (weaponAiming != null)
            weaponAiming.EndSwing();
    }

}
