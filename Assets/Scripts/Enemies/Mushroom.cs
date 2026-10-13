using UnityEngine;

public class Mushroom : Enemy
{
    private static readonly int[] Health = { 5, 10, 15 };
    private static readonly float[] ProjectileSize = { 0.28f, 0.36f, 0.46f };

    private const float ShootRange = 12f;
    private const float ProjectileSpeed = 2.8f;
    private static readonly Color ProjectileColor = new Color(0.4f, 0.9f, 0.25f, 1f);

    [SerializeField] private Sprite smallSprite;
    [SerializeField] private Sprite mediumSprite;
    [SerializeField] private Sprite bigSprite;

    private int size;
    private const float WindupSeconds = 0.6f;

    private float shootTimer;
    private float windupTimer;

    public override int GroupSize => 3;

    protected override string DefaultDescription =>
        "Rooted in place, it spits one slow green spore at a time toward you. " +
        "They grow in groups of three sizes, and the bigger ones are tougher.";

    public override string[] GetIndexStats()
    {
        return new[]
        {
            "Health 5 / 10 / 15 by size",
            "Contact damage 1",
            "Fires a slow green projectile"
        };
    }

    public override void GetIndexVisual(out Sprite sprite, out Color color)
    {
        sprite = mediumSprite != null ? mediumSprite : PlaceholderSprite;
        color = Color.white;
    }

    protected override void Awake()
    {
        base.Awake();

        size = Random.Range(0, 3);

        maxHealth = ScaleHealth(Health[size]);
        currentHealth = maxHealth;
        contactDamage = ScaleDamage(1);
        moveSpeed = 0f;

        Sprite sprite = size == 0 ? smallSprite : size == 1 ? mediumSprite : bigSprite;
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null && sprite != null)
        {
            spriteRenderer.sprite = sprite;

            CircleCollider2D circle = GetComponent<CircleCollider2D>();

            if (circle != null)
            {
                circle.radius = Mathf.Max(0.15f, sprite.bounds.extents.x * 0.85f);
                circle.offset = Vector2.zero;
            }
        }

        shootTimer = Random.Range(1f, 2.5f);
    }

    protected override void MoveTowardsTarget()
    {
        Body.linearVelocity = Vector2.zero;

        if (IsImmobilized)
            return;

        if (windupTimer > 0f)
        {
            windupTimer -= Time.fixedDeltaTime;

            if (windupTimer <= 0f)
                Shoot();

            return;
        }

        shootTimer -= Time.fixedDeltaTime;

        if (shootTimer > 0f)
            return;

        shootTimer = Random.Range(2f, 3.2f);

        Transform target = GetTarget();

        if (target == null)
            return;

        if (((Vector2)target.position - Body.position).magnitude > ShootRange)
            return;

        windupTimer = WindupSeconds;
        PlayTelegraph(WindupSeconds, new Color(0.75f, 1f, 0.35f, 1f), 0.3f);
    }

    private void Shoot()
    {
        Transform target = GetTarget();

        if (target == null)
            return;

        Vector2 toTarget = (Vector2)target.position - Body.position;

        EnemyProjectile.Spawn(
            Body.position, toTarget, ProjectileSpeed * SpeedMultiplier,
            ScaleDamage(1), ProjectileSize[size], ProjectileColor
        );
    }
}
