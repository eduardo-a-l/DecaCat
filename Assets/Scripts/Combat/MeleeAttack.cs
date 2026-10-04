using System.Collections;
using UnityEngine;

public class MeleeAttack : MonoBehaviour
{
    [SerializeField] private PlayerItem playerItem;
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private WeaponAiming weaponAiming;

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

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) &&
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

        Vector3 mousePosition =
            Camera.main.ScreenToWorldPoint(
                new Vector3(
                    Input.mousePosition.x,
                    Input.mousePosition.y,
                    -Camera.main.transform.position.z
                )
            );

        Vector2 direction =
            (Vector2)(mousePosition - transform.position);

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

        Vector2 attackPosition =
            (Vector2)transform.position +
            direction * item.AttackRange;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            attackPosition, item.AttackRadius
        );

        foreach (Collider2D hit in hits)
        {
            TestTarget target =
                hit.GetComponentInParent<TestTarget>();

            if (target != null)
            {
                target.TakeDamage(item.Damage);
            }
        }

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