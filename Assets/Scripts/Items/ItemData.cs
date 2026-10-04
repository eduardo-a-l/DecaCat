using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "DecaCat/Item")]
public class ItemData : ScriptableObject
{
    [Header("Basic Information")]
    [SerializeField] private string itemName;
    [SerializeField] private Sprite itemSprite;
    [SerializeField] private int maxDurability = 10;

    [Header("Combat")]
    [SerializeField] private int damage = 10;
    [SerializeField] private float cooldown = 0.5f;

    [Header("Melee Attack")]
    [SerializeField] private float attackRange = 0.8f;
    [SerializeField] private float attackRadius = 0.5f;
    [SerializeField] private float swingAngle = 100f;
    [SerializeField] private float swingDuration = 0.2f;

    public string ItemName => itemName;
    public Sprite ItemSprite => itemSprite;
    public int MaxDurability => maxDurability;

    public int Damage => damage;
    public float Cooldown => cooldown;
    public float AttackRange => attackRange;
    public float AttackRadius => attackRadius;
    public float SwingAngle => swingAngle;
    public float SwingDuration => swingDuration;
}