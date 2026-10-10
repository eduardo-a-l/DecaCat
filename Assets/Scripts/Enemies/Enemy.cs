using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class Enemy : MonoBehaviour, IDamageable, IPushable
{
    [Header("Hit Flash")]
    [SerializeField] private float flashInterval = 0.04f;
    [SerializeField] private int flashCount = 3;

    private const int PlaceholderPixels = 64;
    private const float PlaceholderDiameter = 0.6f;
    private const float DamageTextGap = 0.15f;
    private const float KnockbackDuration = 0.2f;

    private static Sprite placeholderSprite;
    private static EnemyScaling pendingScaling = EnemyScaling.None;

    private Rigidbody2D body;
    private Transform target;
    private SpriteRenderer flashRenderer;
    private Collider2D bodyCollider;
    private Coroutine flashRoutine;
    private Vector2 knockbackVelocity;
    private float knockbackTimer;

    protected int maxHealth;
    protected int currentHealth;
    protected int contactDamage;
    protected float moveSpeed;
    protected bool isDead;

    public event System.Action<Enemy> Died;

    public bool CostsDurability => true;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    protected Rigidbody2D Body => body;
    protected EnemyScaling Scaling { get; private set; }

    protected virtual bool CanBeKnockedBack => true;

    protected static Sprite PlaceholderSprite
    {
        get
        {
            if (placeholderSprite == null)
                placeholderSprite = CreatePlaceholderSprite();

            return placeholderSprite;
        }
    }

    public static T Spawn<T>(
        T prefab, Vector3 position, Transform parent, EnemyScaling scaling)
        where T : Enemy
    {
        EnemyScaling previous = pendingScaling;
        pendingScaling = scaling;

        try
        {
            return Instantiate(prefab, position, Quaternion.identity, parent);
        }
        finally
        {
            pendingScaling = previous;
        }
    }

    protected int ScaleHealth(int value)
    {
        return Mathf.Max(1, Mathf.RoundToInt(value * Scaling.Health));
    }

    protected int ScaleDamage(int value)
    {
        return value + Scaling.DamageBonus;
    }

    protected float ScaleSpeed(float value)
    {
        return value * Scaling.Speed;
    }

    protected virtual void Awake()
    {
        Scaling = pendingScaling;

        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;

        flashRenderer = GetComponentInChildren<SpriteRenderer>();
        bodyCollider = GetComponent<Collider2D>();
    }

    protected virtual void FixedUpdate()
    {
        if (isDead)
            return;

        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.fixedDeltaTime;

            body.linearVelocity =
                knockbackVelocity *
                Mathf.Clamp01(knockbackTimer / KnockbackDuration);

            return;
        }

        MoveTowardsTarget();
    }

    public void Push(Vector2 source, float distance)
    {
        if (isDead || !CanBeKnockedBack || distance <= 0f || body == null)
            return;

        Vector2 direction = body.position - source;

        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.right;

        knockbackVelocity =
            direction.normalized * (2f * distance / KnockbackDuration);

        knockbackTimer = KnockbackDuration;
    }

    protected Transform GetTarget()
    {
        if (target == null)
        {
            PlayerHealth player = FindFirstObjectByType<PlayerHealth>();

            if (player != null)
                target = player.transform;
        }

        return target;
    }

    protected virtual void MoveTowardsTarget()
    {
        Transform chased = GetTarget();

        if (chased == null)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 direction = (Vector2)chased.position - body.position;

        if (direction.sqrMagnitude < 0.0001f)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        body.linearVelocity = direction.normalized * moveSpeed;
    }

    public void SetFrozen(bool frozen)
    {
        enabled = !frozen;

        if (body == null)
            return;

        if (frozen)
            body.linearVelocity = Vector2.zero;

        body.simulated = !frozen;
    }

    public virtual void TakeDamage(int damage)
    {
        if (isDead || damage <= 0)
            return;

        currentHealth -= damage;

        Debug.Log(name + " health: " + Mathf.Max(0, currentHealth));

        DamageText.Spawn(GetDamageTextPosition(), damage);

        if (currentHealth <= 0)
            Die();
        else
            StartFlash();
    }

    private Vector3 GetDamageTextPosition()
    {
        if (bodyCollider == null)
            return transform.position + Vector3.up * 0.5f;

        Bounds bounds = bodyCollider.bounds;

        return new Vector3(
            bounds.center.x,
            bounds.max.y + DamageTextGap,
            transform.position.z
        );
    }

    private void StartFlash()
    {
        if (flashRenderer == null)
            return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashCoroutine());
    }

    private IEnumerator FlashCoroutine()
    {
        for (int i = 0; i < flashCount; i++)
        {
            flashRenderer.enabled = false;
            yield return new WaitForSeconds(flashInterval);

            flashRenderer.enabled = true;
            yield return new WaitForSeconds(flashInterval);
        }

        flashRoutine = null;
    }

    protected virtual void Die()
    {
        isDead = true;

        GameStats.Add(StatType.EnemiesDefeated);
        RecordDefeat();

        Died?.Invoke(this);
        Destroy(gameObject);
    }

    protected virtual void RecordDefeat()
    {
    }

    protected virtual void HandleContact(Collision2D collision)
    {
        if (isDead)
            return;

        PlayerHealth player =
            collision.collider.GetComponentInParent<PlayerHealth>();

        if (player != null)
            player.TakeDamage(contactDamage);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleContact(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        HandleContact(collision);
    }

    private static Sprite CreatePlaceholderSprite()
    {
        Texture2D texture = new Texture2D(
            PlaceholderPixels, PlaceholderPixels,
            TextureFormat.RGBA32, false
        );

        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        float radius = PlaceholderPixels * 0.5f;
        Color[] pixels = new Color[PlaceholderPixels * PlaceholderPixels];

        for (int y = 0; y < PlaceholderPixels; y++)
        {
            for (int x = 0; x < PlaceholderPixels; x++)
            {
                float dx = x + 0.5f - radius;
                float dy = y + 0.5f - radius;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(radius - distance);

                pixels[y * PlaceholderPixels + x] =
                    new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, PlaceholderPixels, PlaceholderPixels),
            new Vector2(0.5f, 0.5f),
            PlaceholderPixels / PlaceholderDiameter
        );
    }
}
