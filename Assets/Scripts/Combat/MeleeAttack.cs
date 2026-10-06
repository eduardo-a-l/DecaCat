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

    private PlayerInputReader inputReader;
    private float nextAttackTime;
    private Coroutine swingCoroutine;

    public float CooldownRemaining
    {
        get
        {
            return Mathf.Max(0f, nextAttackTime - Time.time);
        }
    }

    public float CurrentCooldown
    {
        get
        {
            if (playerItem == null || playerItem.CurrentItem == null)
                return 0f;

            return playerItem.CurrentItem.Cooldown;
        }
    }

    private void Awake()
    {
        inputReader = GetComponent<PlayerInputReader>();
    }

    private void Update()
    {
        if (inputReader != null &&
            inputReader.AttackPressed &&
            Time.time >= nextAttackTime)
        {
            Attack();
        }
    }

    private void Attack()
    {
        if (playerItem == null ||
            playerItem.CurrentItem == null)
            return;

        ItemData item = playerItem.CurrentItem;

        nextAttackTime = Time.time + item.Cooldown;

        Vector3 aimPosition = inputReader.AimWorldPosition;

        Vector2 direction =
            (Vector2)(aimPosition - transform.position);

        if (direction.sqrMagnitude < 0.001f)
            direction = Vector2.right;

        direction.Normalize();

        float angle = Mathf.Atan2(
            direction.y, direction.x
        ) * Mathf.Rad2Deg;

        if (swingCoroutine != null)
            StopCoroutine(swingCoroutine);

        if (weaponAiming != null)
            weaponAiming.BeginSwing();

        swingCoroutine = StartCoroutine(
            Swing(angle, item.SwingAngle, item.SwingDuration)
        );

        SwingEffect.Play(transform, angle, item);

        List<IDamageable> targets = CollectTargets(item, angle);
        bool spendDurability = false;

        foreach (IDamageable target in targets)
        {
            if (target.CostsDurability)
                spendDurability = true;

            target.TakeDamage(item.Damage);

            if (item.Knockback > 0f && target is IPushable pushable)
                pushable.Push(transform.position, item.Knockback);
        }

        if (spendDurability)
            playerItem.UseItem();
    }

    private List<IDamageable> CollectTargets(ItemData item, float aimAngle)
    {
        Dictionary<IDamageable, float> scores =
            new Dictionary<IDamageable, float>();

        Vector2 origin = transform.position;
        int samples = item.HitsMultipleTargets ? SweepSamples : 1;

        for (int i = 0; i < samples; i++)
        {
            float angle = aimAngle;

            if (samples > 1)
            {
                float t = (float)i / (samples - 1);
                angle = aimAngle - item.SwingAngle / 2f + item.SwingAngle * t;
            }

            float radians = angle * Mathf.Deg2Rad;

            Vector2 center =
                origin +
                new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) *
                item.AttackRange;

            Collider2D[] hits =
                Physics2D.OverlapCircleAll(center, item.AttackRadius);

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

        if (item.HitsMultipleTargets)
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
