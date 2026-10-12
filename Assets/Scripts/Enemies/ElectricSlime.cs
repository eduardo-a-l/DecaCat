using UnityEngine;

public class ElectricSlime : Enemy
{
    private const float MoveSpeed = 2.2f;
    private const float KnockbackDistance = 2.4f;

    private static int pendingStack;

    [SerializeField] private Sprite stack1Sprite;
    [SerializeField] private Sprite stack2Sprite;
    [SerializeField] private Sprite stack4Sprite;

    private int stack = 4;

    public override DamageElement AttackElement => DamageElement.Electric;

    protected override string DefaultDescription =>
        "A tower of four electric slimes. When its health runs out it breaks into " +
        "two stacks of two, which break into single slimes. They never merge back, " +
        "and their shocks knock you away.";

    public override string[] GetIndexStats()
    {
        return new[]
        {
            "Health 5 per stack",
            "Contact damage 1",
            "Splits 4 > 2 > 1",
            "Knocks you back"
        };
    }

    public override void GetIndexVisual(out Sprite sprite, out Color color)
    {
        sprite = stack4Sprite != null ? stack4Sprite : PlaceholderSprite;
        color = Color.white;
    }

    protected override void Awake()
    {
        base.Awake();

        stack = pendingStack > 0 ? pendingStack : 4;
        pendingStack = 0;

        maxHealth = ScaleHealth(5);
        currentHealth = maxHealth;
        contactDamage = ScaleDamage(1);
        moveSpeed = ScaleSpeed(MoveSpeed);

        ApplyStackVisual();
    }

    protected override void HandleHitPlayer(PlayerHealth player)
    {
        PlayerMovement movement = player.GetComponent<PlayerMovement>();

        if (movement != null)
            movement.Knockback(transform.position, KnockbackDistance);
    }

    protected override void Die()
    {
        if (stack > 1)
            SplitInTwo();

        base.Die();
    }

    private void SplitInTwo()
    {
        Room room = GetComponentInParent<Room>();

        for (int i = 0; i < 2; i++)
        {
            pendingStack = stack / 2;

            Vector2 offset = new Vector2(i == 0 ? -0.5f : 0.5f, 0f);

            ElectricSlime child = Spawn(
                this, (Vector2)transform.position + offset, transform.parent, Scaling
            );

            if (room != null)
                room.AddEnemy(child);
        }

        pendingStack = 0;
    }

    private void ApplyStackVisual()
    {
        Sprite sprite = stack >= 4 ? stack4Sprite : stack >= 2 ? stack2Sprite : stack1Sprite;
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null || sprite == null)
            return;

        spriteRenderer.sprite = sprite;

        CircleCollider2D circle = GetComponent<CircleCollider2D>();

        if (circle != null)
        {
            circle.radius = Mathf.Max(0.2f, sprite.bounds.extents.x * 0.8f);
            circle.offset = new Vector2(0f, -sprite.bounds.extents.y * 0.35f);
        }
    }
}
