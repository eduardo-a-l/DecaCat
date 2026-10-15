using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class Enemy : MonoBehaviour, IDamageable, IPushable
{
    [Header("Hit Flash")]
    [SerializeField] private float flashInterval = 0.04f;
    [SerializeField] private int flashCount = 3;

    [Header("Index (leave empty to use the built-in text)")]
    [SerializeField] private string displayName;
    [SerializeField, TextArea(2, 6)] private string description;

    private const int PlaceholderPixels = 64;
    private const float PlaceholderDiameter = 0.6f;
    private const float DamageTextGap = 0.15f;
    private const float KnockbackDuration = 0.2f;
    private const float BurnTickSeconds = 1f;
    private const float BossStatusScale = 0.4f;
    private const float ClimbSpeed = 5f;

    private static readonly Color SlowTint = new Color(0.65f, 0.85f, 1f, 1f);
    private static readonly Color FreezeTint = new Color(0.45f, 0.75f, 1f, 1f);
    private static readonly Color BurnTint = new Color(1f, 0.55f, 0.25f, 1f);
    private static readonly Color StunTint = new Color(1f, 1f, 0.4f, 1f);

    private sealed class DotEffect
    {
        public string Id;
        public int TicksLeft;
        public float Timer;
        public int Damage;
        public Color Tint;
    }

    private static Sprite placeholderSprite;
    private static EnemyScaling pendingScaling = EnemyScaling.None;

    private Rigidbody2D body;
    private Transform target;
    private SpriteRenderer flashRenderer;
    private Collider2D bodyCollider;
    private Coroutine flashRoutine;
    private Vector2 knockbackVelocity;
    private float knockbackTimer;
    private float slowFactor = 1f;
    private float slowTimer;
    private float freezeTimer;
    private float stunTimer;
    private float burnTimer;
    private float burnTick;
    private int burnDamage = 1;
    private readonly List<DotEffect> dots = new List<DotEffect>();
    private float harmlessTimer;
    private bool colliderGhosted;
    private float telegraphAmount;
    private Color telegraphColor = Color.white;
    private Coroutine telegraphRoutine;
    private Vector3 telegraphBaseScale;
    private float tauntTimer;
    private float obstacleClimb;
    private Transform tauntTarget;
    private Color untintedColor;
    private bool tinted;

    protected int maxHealth;
    protected int currentHealth;
    protected int contactDamage;
    protected float moveSpeed;
    protected bool isDead;

    public static float GlobalSpeedMultiplier { get; set; } = 1f;

    public event System.Action<Enemy> Died;

    public bool IsBurning => burnTimer > 0f;
    public bool IsHarmless => harmlessTimer > 0f;
    public bool IsImmobilized => freezeTimer > 0f || stunTimer > 0f;
    public float ObstacleClimb01 => obstacleClimb;
    public virtual bool CanWalkOverObstacles => IsBoss;
    public virtual int GroupSize => 1;
    public virtual DamageElement AttackElement => DamageElement.None;

    public bool CostsDurability => true;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    protected Rigidbody2D Body => body;
    protected float SpeedMultiplier => slowFactor * GlobalSpeedMultiplier;

    public virtual bool IsBoss => false;

    public string IndexId => name.Replace("(Clone)", string.Empty).Trim();

    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrEmpty(displayName))
                return displayName;

            return Regex.Replace(GetType().Name, "(?<=[a-z])(?=[A-Z])", " ");
        }
    }

    public string Description =>
        !string.IsNullOrEmpty(description) ? description : DefaultDescription;

    protected virtual string DefaultDescription => "Nothing is known about this one yet.";

    public virtual string[] GetIndexStats()
    {
        return new string[0];
    }

    public virtual void GetIndexVisual(out Sprite sprite, out Color color)
    {
        SpriteRenderer found = GetComponentInChildren<SpriteRenderer>(true);

        bool hasSprite = found != null && found.sprite != null;

        sprite = hasSprite ? found.sprite : PlaceholderSprite;
        color = found != null ? found.color : Color.white;
    }
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

        if (CanWalkOverObstacles)
            PhysicsLayers.Exclude(gameObject, PhysicsLayers.ObstacleMask);
    }

    public void ApplySlow(float factor, float seconds)
    {
        if (isDead || seconds <= 0f)
            return;

        seconds *= StatusDurationScale;

        if (slowTimer <= 0f || factor < slowFactor)
            slowFactor = Mathf.Clamp(factor, 0.05f, 1f);

        slowTimer = Mathf.Max(slowTimer, seconds);
    }

    public void Freeze(float seconds)
    {
        if (isDead || seconds <= 0f)
            return;

        if (IsBoss)
        {
            ApplySlow(0.3f, seconds);
            return;
        }

        freezeTimer = Mathf.Max(freezeTimer, seconds);
    }

    public void Stun(float seconds)
    {
        if (isDead || seconds <= 0f)
            return;

        stunTimer = Mathf.Max(stunTimer, seconds * StatusDurationScale);
    }

    public void Burn(float seconds, int damagePerSecond = 1)
    {
        if (isDead || seconds <= 0f)
            return;

        if (burnTimer <= 0f)
        {
            burnTick = BurnTickSeconds;
            burnDamage = Mathf.Max(1, damagePerSecond);
        }
        else
        {
            burnDamage = Mathf.Max(burnDamage, damagePerSecond);
        }

        burnTimer = Mathf.Max(burnTimer, seconds);
    }

    public void ApplyDot(string id, float seconds, int damagePerSecond, Color tint)
    {
        if (isDead || seconds <= 0f || damagePerSecond <= 0)
            return;

        int ticks = Mathf.Max(1, Mathf.CeilToInt(seconds));

        foreach (DotEffect dot in dots)
        {
            if (dot.Id != id)
                continue;

            dot.TicksLeft = Mathf.Max(dot.TicksLeft, ticks);
            dot.Damage = Mathf.Max(dot.Damage, damagePerSecond);

            return;
        }

        dots.Add(new DotEffect
        {
            Id = id,
            TicksLeft = ticks,
            Timer = BurnTickSeconds,
            Damage = damagePerSecond,
            Tint = tint
        });
    }

    public void Taunt(Transform decoy, float seconds)
    {
        if (isDead || decoy == null || seconds <= 0f)
            return;

        tauntTarget = decoy;
        tauntTimer = seconds;
    }

    public void SetSpawnGrace(float seconds)
    {
        BeginGrace(seconds, false);
    }

    protected void BeginGrace(float seconds, bool ghostCollider)
    {
        if (isDead || seconds <= 0f)
            return;

        harmlessTimer = Mathf.Max(harmlessTimer, seconds);

        if (ghostCollider && bodyCollider != null)
        {
            bodyCollider.enabled = false;
            colliderGhosted = true;
        }
    }

    private void EndGrace()
    {
        harmlessTimer = 0f;

        if (colliderGhosted && bodyCollider != null)
            bodyCollider.enabled = true;

        colliderGhosted = false;
    }

    public void PlayTelegraph(float seconds, Color color, float swell = 0.2f)
    {
        if (isDead || seconds <= 0f || !gameObject.activeInHierarchy)
            return;

        if (telegraphRoutine != null)
        {
            StopCoroutine(telegraphRoutine);
            transform.localScale = telegraphBaseScale;
        }

        telegraphRoutine = StartCoroutine(TelegraphRoutine(seconds, color, swell));
    }

    private IEnumerator TelegraphRoutine(float seconds, Color color, float swell)
    {
        telegraphBaseScale = transform.localScale;
        telegraphColor = color;

        float elapsed = 0f;

        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / seconds);
            float wobble = Mathf.Sin(elapsed * 40f) * 0.04f * progress;
            float blink = 0.5f + 0.5f * Mathf.Sin(elapsed * 30f);

            transform.localScale = telegraphBaseScale * (1f + swell * progress + wobble);
            telegraphAmount = Mathf.Lerp(0.3f, 1f, progress) * blink;

            yield return null;
        }

        transform.localScale = telegraphBaseScale;
        telegraphAmount = 0f;
        telegraphRoutine = null;
    }

    private float StatusDurationScale => IsBoss ? BossStatusScale : 1f;

    protected bool UpdateStatus()
    {
        float step = Time.fixedDeltaTime;

        UpdateObstacleClimb(step);

        if (slowTimer > 0f)
        {
            slowTimer -= step;

            if (slowTimer <= 0f)
                slowFactor = 1f;
        }

        if (freezeTimer > 0f)
            freezeTimer -= step;

        if (stunTimer > 0f)
            stunTimer -= step;

        if (harmlessTimer > 0f)
        {
            harmlessTimer -= step;

            if (harmlessTimer <= 0f)
                EndGrace();
        }

        if (tauntTimer > 0f)
        {
            tauntTimer -= step;

            if (tauntTimer <= 0f)
                tauntTarget = null;
        }

        if (burnTimer > 0f)
        {
            burnTimer -= step;
            burnTick -= step;

            if (burnTick <= 0f)
            {
                burnTick += BurnTickSeconds;
                TakeDamage(burnDamage);

                if (isDead)
                    return true;
            }

            if (burnTimer <= 0f)
                burnDamage = 1;
        }

        if (UpdateDots(step))
            return true;

        UpdateTint();

        if (!IsImmobilized)
            return false;

        body.linearVelocity = Vector2.zero;

        return true;
    }

    private bool UpdateDots(float step)
    {
        for (int i = dots.Count - 1; i >= 0; i--)
        {
            DotEffect dot = dots[i];

            dot.Timer -= step;

            if (dot.Timer > 0f)
                continue;

            dot.Timer += BurnTickSeconds;
            dot.TicksLeft--;

            int damage = dot.Damage;

            if (dot.TicksLeft <= 0)
                dots.RemoveAt(i);

            TakeDamage(damage);

            if (isDead)
                return true;
        }

        return false;
    }

    private void UpdateObstacleClimb(float step)
    {
        if (!CanWalkOverObstacles || body == null)
            return;

        int mask = PhysicsLayers.ObstacleMask;
        bool over = mask != 0 && Physics2D.OverlapPoint(body.position, mask) != null;

        obstacleClimb = Mathf.MoveTowards(
            obstacleClimb, over ? 1f : 0f, step * ClimbSpeed
        );
    }

    private void UpdateTint()
    {
        if (flashRenderer == null)
            return;

        Color tint = Color.white;
        bool any = false;

        if (slowTimer > 0f)
        {
            tint *= SlowTint;
            any = true;
        }

        if (freezeTimer > 0f)
        {
            tint *= FreezeTint;
            any = true;
        }

        if (burnTimer > 0f)
        {
            tint *= BurnTint;
            any = true;
        }

        if (stunTimer > 0f)
        {
            tint *= StunTint;
            any = true;
        }

        foreach (DotEffect dot in dots)
        {
            tint *= dot.Tint;
            any = true;
        }

        if (harmlessTimer > 0f)
        {
            tint *= new Color(1f, 1f, 1f, 0.45f + 0.25f * Mathf.Sin(Time.time * 30f));
            any = true;
        }

        if (telegraphAmount > 0f)
        {
            Color warning = new Color(telegraphColor.r, telegraphColor.g, telegraphColor.b, tint.a);

            tint = Color.Lerp(tint, warning, telegraphAmount * 0.75f);
            any = true;
        }

        if (any)
        {
            if (!tinted)
            {
                untintedColor = flashRenderer.color;
                tinted = true;
            }

            flashRenderer.color = untintedColor * tint;
        }
        else if (tinted)
        {
            flashRenderer.color = untintedColor;
            tinted = false;
        }
    }

    protected virtual void FixedUpdate()
    {
        if (isDead)
            return;

        if (UpdateStatus())
            return;

        if (colliderGhosted)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

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
        if (tauntTarget != null && tauntTimer > 0f)
            return tauntTarget;

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

        body.linearVelocity = direction.normalized * moveSpeed * SpeedMultiplier;
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

    protected virtual void OnDisable()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;

            if (flashRenderer != null)
                flashRenderer.enabled = true;
        }

        if (telegraphRoutine != null)
        {
            StopCoroutine(telegraphRoutine);
            telegraphRoutine = null;

            transform.localScale = telegraphBaseScale;
            telegraphAmount = 0f;
        }
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
        bool wasBurning = IsBurning;

        isDead = true;

        GameStats.Add(StatType.EnemiesDefeated);
        RecordDefeat();

        Died?.Invoke(this);

        if (PlayerPower.Instance != null)
            PlayerPower.Instance.OnEnemyKilled(this, wasBurning, transform.position);

        Destroy(gameObject);
    }

    protected virtual void RecordDefeat()
    {
    }

    protected virtual void HandleHitPlayer(PlayerHealth player)
    {
    }

    protected virtual void HandleContact(Collision2D collision)
    {
        if (isDead || IsImmobilized || harmlessTimer > 0f)
            return;

        PlayerHealth player =
            collision.collider.GetComponentInParent<PlayerHealth>();

        if (player != null)
        {
            player.TakeDamage(contactDamage, AttackElement);
            HandleHitPlayer(player);
            return;
        }

        AllyMinion minion = collision.collider.GetComponentInParent<AllyMinion>();

        if (minion != null)
            minion.TakeHit(contactDamage);
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
