using UnityEngine;

public enum WeaponBehaviour
{
    Standard,
    Potion,
    Fan,
    Lollipop,
    Magnet,
    MagicWand,
    Spatula
}

public enum PotionEffect
{
    Damage,
    Slow,
    Poison,
    Fire
}

[CreateAssetMenu(fileName = "New Item", menuName = "DecaCat/Item")]
public class ItemData : ScriptableObject
{
    [Header("Basic Information")]
    [SerializeField] private string itemName;
    [SerializeField] private Sprite itemSprite;
    [SerializeField] private int maxDurability = 10;
    [SerializeField, TextArea(2, 6)] private string description;

    [Header("Combat")]
    [SerializeField] private int damage = 10;
    [SerializeField] private float cooldown = 0.5f;

    [Header("Melee Attack")]
    [SerializeField] private float attackRange = 0.8f;
    [SerializeField] private float attackRadius = 0.5f;
    [SerializeField] private float swingAngle = 100f;
    [SerializeField] private float swingDuration = 0.2f;
    [SerializeField] private bool hitsMultipleTargets;
    [SerializeField] private float knockback;

    [Header("Swing Effect (optional sprite, drawn facing right)")]
    [SerializeField] private Sprite swingEffectSprite;
    [SerializeField] private Color swingEffectColor = new Color(1f, 1f, 1f, 0.9f);

    [Header("Ranged weapons")]
    [SerializeField] private bool isRanged;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private float projectileLifetime = 1.5f;
    [SerializeField] private Vector2 projectileSize = new Vector2(0.3f, 0.3f);
    [SerializeField] private Color projectileColor = Color.white;
    [SerializeField] private int shotsPerDurability = 1;
    [SerializeField] private int bounces;
    [SerializeField] private bool aimLock;

    [Header("Special weapons")]
    [SerializeField] private WeaponBehaviour behaviour;
    [SerializeField] private PotionEffect potionEffect;
    [SerializeField] private float areaRadius = 2f;
    [SerializeField] private float effectSeconds;
    [SerializeField] private int effectDamage;
    [SerializeField] private float slowFactor = 0.5f;
    [SerializeField] private int projectileCount = 5;
    [SerializeField] private float coneAngle = 70f;
    [SerializeField] private float coneRange = 5f;
    [SerializeField] private float forceDistance = 3f;
    [SerializeField] private float hitDotSeconds;
    [SerializeField] private int hitDotDamage;

    public string ItemName => itemName;
    public Sprite ItemSprite => itemSprite;
    public int MaxDurability => maxDurability;
    public string Description => description;

    public int Damage => damage;
    public float Cooldown => cooldown;
    public float AttackRange => attackRange;
    public float AttackRadius => attackRadius;
    public float SwingAngle => swingAngle;
    public float SwingDuration => swingDuration;
    public bool HitsMultipleTargets => hitsMultipleTargets;
    public float Knockback => knockback;
    public Sprite SwingEffectSprite => swingEffectSprite;
    public Color SwingEffectColor => swingEffectColor;
    public bool IsRanged => isRanged;
    public float ProjectileSpeed => projectileSpeed;
    public float ProjectileLifetime => projectileLifetime;
    public Vector2 ProjectileSize => projectileSize;
    public Color ProjectileColor => projectileColor;
    public int ShotsPerDurability => Mathf.Max(1, shotsPerDurability);
    public int Bounces => bounces;
    public bool AimLock => aimLock;
    public WeaponBehaviour Behaviour => behaviour;
    public PotionEffect PotionEffect => potionEffect;
    public float AreaRadius => areaRadius;
    public float EffectSeconds => effectSeconds;
    public int EffectDamage => effectDamage;
    public float SlowFactor => slowFactor;
    public int ProjectileCount => Mathf.Max(1, projectileCount);
    public float ConeAngle => coneAngle;
    public float ConeRange => coneRange;
    public float ForceDistance => forceDistance;
    public float HitDotSeconds => hitDotSeconds;
    public int HitDotDamage => hitDotDamage;
}