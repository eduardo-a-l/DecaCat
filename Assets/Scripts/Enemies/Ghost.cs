using System.Collections.Generic;
using UnityEngine;

public class Ghost : Enemy
{
    private static readonly List<Ghost> ActiveGhosts = new List<Ghost>();

    [Header("Stats")]
    [SerializeField] private int health = 15;
    [SerializeField] private int touchDamage = 1;

    [Header("Movement")]
    [SerializeField] private float closeSpeed = 0.4f;
    [SerializeField] private float farSpeed = 3.5f;
    [SerializeField] private float farDistance = 8f;
    [SerializeField] private float crowdSpeedBonus = 0.25f;

    protected override void Awake()
    {
        base.Awake();

        maxHealth = ScaleHealth(health);
        currentHealth = maxHealth;
        contactDamage = ScaleDamage(touchDamage);
    }

    private void OnEnable()
    {
        ActiveGhosts.Add(this);
    }

    private void OnDisable()
    {
        ActiveGhosts.Remove(this);
    }

    protected override void RecordDefeat()
    {
        GameStats.Add(StatType.GhostsDefeated);
    }

    protected override void MoveTowardsTarget()
    {
        Transform chased = GetTarget();

        if (chased == null)
        {
            Body.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 offset = (Vector2)chased.position - Body.position;
        float distance = offset.magnitude;

        if (distance < 0.0001f)
        {
            Body.linearVelocity = Vector2.zero;
            return;
        }

        float distanceFactor = Mathf.Clamp01(distance / farDistance);
        float crowdFactor =
            1f + crowdSpeedBonus * Mathf.Max(0, ActiveGhosts.Count - 1);

        float speed =
            Mathf.Lerp(closeSpeed, farSpeed, distanceFactor) * crowdFactor *
            Scaling.Speed;

        Body.linearVelocity = offset / distance * speed;
    }
}
