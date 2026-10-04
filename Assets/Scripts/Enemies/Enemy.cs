using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class Enemy : MonoBehaviour, IDamageable
{
    private const int PlaceholderPixels = 64;
    private const float PlaceholderDiameter = 0.6f;

    private static Sprite placeholderSprite;

    private Rigidbody2D body;
    private Transform target;

    protected int maxHealth;
    protected int currentHealth;
    protected int contactDamage;
    protected float moveSpeed;
    protected bool isDead;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    protected Rigidbody2D Body => body;

    protected static Sprite PlaceholderSprite
    {
        get
        {
            if (placeholderSprite == null)
                placeholderSprite = CreatePlaceholderSprite();

            return placeholderSprite;
        }
    }

    protected virtual void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
    }

    protected virtual void FixedUpdate()
    {
        if (isDead)
            return;

        MoveTowardsTarget();
    }

    protected virtual void MoveTowardsTarget()
    {
        if (target == null)
        {
            PlayerHealth player = FindFirstObjectByType<PlayerHealth>();

            if (player != null)
                target = player.transform;
        }

        if (target == null)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 direction = (Vector2)target.position - body.position;

        if (direction.sqrMagnitude < 0.0001f)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        body.linearVelocity = direction.normalized * moveSpeed;
    }

    public virtual void TakeDamage(int damage)
    {
        if (isDead || damage <= 0)
            return;

        currentHealth -= damage;

        Debug.Log(name + " health: " + Mathf.Max(0, currentHealth));

        if (currentHealth <= 0)
            Die();
    }

    protected virtual void Die()
    {
        isDead = true;
        Destroy(gameObject);
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
