using System.Collections;
using UnityEngine;

public class MeleeAttack : MonoBehaviour
{
    [SerializeField] private PlayerItem playerItem;
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private WeaponAiming weaponAiming;

    [SerializeField] private float attackRange = 0.8f;
    [SerializeField] private float attackRadius = 0.5f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private float swingAngle = 100f;
    [SerializeField] private float swingDuration = 0.2f;

    private float nextAttackTime;
    private Coroutine swingCoroutine;

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

        nextAttackTime = Time.time + attackCooldown;

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

        weaponAiming.BeginSwing();
        swingCoroutine = StartCoroutine(
            Swing(angle)
        );

        Vector2 attackPosition =
            (Vector2)transform.position +
            direction * attackRange;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            attackPosition, attackRadius
        );

        foreach (Collider2D hit in hits)
        {
            TestTarget target =
                hit.GetComponentInParent<TestTarget>();

            if (target != null)
            {
                target.TakeDamage(damage);
            }
        }

        playerItem.UseItem();
    }

    private IEnumerator Swing(float angle)
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

        weaponAiming.EndSwing();

        swingCoroutine = null;
    }
}