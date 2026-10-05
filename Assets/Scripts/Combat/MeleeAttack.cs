using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeAttack : MonoBehaviour
{
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

        Vector2 attackPosition =
            (Vector2)transform.position +
            direction * item.AttackRange;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            attackPosition, item.AttackRadius
        );

        HashSet<IDamageable> damaged = new HashSet<IDamageable>();
        bool spendDurability = false;

        foreach (Collider2D hit in hits)
        {
            IDamageable target =
                hit.GetComponentInParent<IDamageable>();

            if (target == null || !damaged.Add(target))
                continue;

            if (target.CostsDurability)
                spendDurability = true;

            target.TakeDamage(item.Damage);
        }

        if (spendDurability)
            playerItem.UseItem();
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