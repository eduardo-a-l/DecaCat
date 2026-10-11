using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class AllyMinion : MonoBehaviour
{
    private const int MaxHealth = 5;
    private const int ContactDamage = 3;
    private const float InvulnerabilitySeconds = 1.5f;
    private const float HitInterval = 0.6f;
    private const float Speed = 4.8f;
    private const float Lifetime = 30f;
    private const float FollowDistance = 1.8f;
    private const float TeleportDistance = 9f;
    private const float Diameter = 0.5f;

    private static readonly List<AllyMinion> All = new List<AllyMinion>();

    private readonly Dictionary<Enemy, float> nextHit = new Dictionary<Enemy, float>();

    private Rigidbody2D body;
    private CircleCollider2D circle;
    private SpriteRenderer spriteRenderer;
    private Transform player;
    private Color baseColor;
    private int health = MaxHealth;
    private float invulnerableUntil;
    private float age;

    public static AllyMinion Spawn(
        Vector2 position, Transform player, Collider2D[] playerColliders, Color color)
    {
        GameObject root = new GameObject("Minion");
        root.transform.position = position;

        Rigidbody2D rigidbody = root.AddComponent<Rigidbody2D>();
        rigidbody.gravityScale = 0f;
        rigidbody.freezeRotation = true;
        rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D collider = root.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;

        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = PowerFx.Circle;
        renderer.color = color;
        renderer.sortingOrder = 2;

        float native = Mathf.Max(0.0001f, renderer.sprite.bounds.size.x);
        root.transform.localScale = Vector3.one * (Diameter / native);

        AllyMinion minion = root.AddComponent<AllyMinion>();
        minion.body = rigidbody;
        minion.circle = collider;
        minion.spriteRenderer = renderer;
        minion.baseColor = color;
        minion.player = player;

        foreach (Collider2D playerCollider in playerColliders)
        {
            if (playerCollider != null)
                Physics2D.IgnoreCollision(collider, playerCollider, true);
        }

        foreach (AllyMinion other in All)
            Physics2D.IgnoreCollision(collider, other.circle, true);

        ZoneEffect.Spawn(position, 0.6f, color, 0.35f, true);

        return minion;
    }

    public static void DestroyAll()
    {
        foreach (AllyMinion minion in new List<AllyMinion>(All))
        {
            if (minion != null)
                Destroy(minion.gameObject);
        }
    }

    public void TakeHit(int damage)
    {
        if (Time.time < invulnerableUntil || damage <= 0)
            return;

        health -= damage;
        invulnerableUntil = Time.time + InvulnerabilitySeconds;

        if (health <= 0)
            Pop();
    }

    private void OnEnable()
    {
        All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    private void Update()
    {
        age += Time.deltaTime;

        if (age >= Lifetime)
        {
            Pop();
            return;
        }

        bool invulnerable = Time.time < invulnerableUntil;

        spriteRenderer.color = invulnerable
            ? new Color(baseColor.r, baseColor.g, baseColor.b,
                Mathf.PingPong(Time.time * 10f, 1f) > 0.5f ? 0.9f : 0.3f)
            : baseColor;
    }

    private void FixedUpdate()
    {
        if (player == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 position = body.position;
        Vector2 toPlayer = (Vector2)player.position - position;

        if (toPlayer.magnitude > TeleportDistance)
        {
            body.position = (Vector2)player.position + Random.insideUnitCircle.normalized;
            body.linearVelocity = Vector2.zero;
            return;
        }

        Enemy enemy = PowerFx.GetClosestEnemy(position, 30f);
        Vector2 desired;

        if (enemy != null)
        {
            desired = ((Vector2)enemy.transform.position - position).normalized * Speed;
        }
        else if (toPlayer.magnitude > FollowDistance)
        {
            desired = toPlayer.normalized * Speed;
        }
        else
        {
            desired = Vector2.zero;
        }

        body.linearVelocity = Vector2.MoveTowards(
            body.linearVelocity, desired, Speed * 6f * Time.fixedDeltaTime
        );
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        Enemy enemy = collision.collider.GetComponentInParent<Enemy>();

        if (enemy == null || enemy.CurrentHealth <= 0)
            return;

        if (nextHit.TryGetValue(enemy, out float ready) && Time.time < ready)
            return;

        nextHit[enemy] = Time.time + HitInterval;

        enemy.TakeDamage(ContactDamage);
        enemy.Push(body.position, 0.6f);
    }

    private void Pop()
    {
        ZoneEffect.Spawn(transform.position, 0.7f, baseColor, 0.3f, true);
        Destroy(gameObject);
    }
}
