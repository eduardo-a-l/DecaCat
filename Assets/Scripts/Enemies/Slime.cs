using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
public class Slime : Enemy
{
    [SerializeField] private SlimeData data;

    private SpriteRenderer spriteRenderer;
    private CircleCollider2D circleCollider;
    private bool isBig;

    public bool IsBig => isBig;

    protected override string DefaultDescription =>
        "A small bouncy blob that chases you. When two slimes touch they " +
        "merge into one big slime that is slower but much sturdier, so do not " +
        "let them pile up.";

    public override string[] GetIndexStats()
    {
        if (data == null)
            return base.GetIndexStats();

        return new[]
        {
            "Health " + data.Small.Health + " (" + data.Big.Health + " merged)",
            "Contact damage " + data.Small.Damage
        };
    }

    public override void GetIndexVisual(out Sprite sprite, out Color color)
    {
        SlimeStage stage = data != null ? data.Small : new SlimeStage();

        sprite = stage.Sprite != null ? stage.Sprite : PlaceholderSprite;
        color = stage.Sprite != null ? Color.white : stage.PlaceholderColor;
    }

    protected override void Awake()
    {
        base.Awake();

        spriteRenderer = GetComponent<SpriteRenderer>();
        circleCollider = GetComponent<CircleCollider2D>();

        if (data == null)
        {
            Debug.LogWarning(name + " has no SlimeData assigned, using defaults");
            data = ScriptableObject.CreateInstance<SlimeData>();
        }

        ApplyStage(data.Small, false, ScaleHealth(data.Small.Health));
    }

    protected override void HandleContact(Collision2D collision)
    {
        base.HandleContact(collision);

        if (isDead)
            return;

        TryMerge(collision.collider.GetComponentInParent<Slime>());
    }

    private void TryMerge(Slime other)
    {
        if (other == null || other == this)
            return;

        if (isBig || other.isBig || isDead || other.isDead)
            return;

        Merge(other);
    }

    private void Merge(Slime other)
    {
        int combinedHealth = currentHealth + other.currentHealth;
        Vector2 midpoint = (Body.position + other.Body.position) * 0.5f;

        other.Absorb();

        Body.position = midpoint;
        transform.position = midpoint;
        Body.linearVelocity = Vector2.zero;

        ApplyStage(data.Big, true, combinedHealth);

        GameStats.Add(StatType.SlimeMerges);
    }

    protected override void RecordDefeat()
    {
        GameStats.Add(StatType.SlimesDefeated);
    }

    private void Absorb()
    {
        isDead = true;
        Destroy(gameObject);
    }

    private void ApplyStage(SlimeStage stage, bool big, int health)
    {
        isBig = big;
        maxHealth = ScaleHealth(stage.Health);
        currentHealth = Mathf.Clamp(health, 1, maxHealth);
        contactDamage = ScaleDamage(stage.Damage);
        moveSpeed = ScaleSpeed(stage.MoveSpeed);

        bool hasSprite = stage.Sprite != null;
        Sprite sprite = hasSprite ? stage.Sprite : PlaceholderSprite;

        spriteRenderer.sprite = sprite;
        spriteRenderer.color = hasSprite ? Color.white : stage.PlaceholderColor;

        transform.localScale = Vector3.one * stage.Scale;

        circleCollider.radius =
            Mathf.Min(sprite.bounds.extents.x, sprite.bounds.extents.y);
        circleCollider.offset = sprite.bounds.center;
    }
}
