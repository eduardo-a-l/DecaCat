public readonly struct EnemyScaling
{
    public static readonly EnemyScaling None = new EnemyScaling(1f, 1f, 0);

    public EnemyScaling(float health, float speed, int damageBonus)
    {
        Health = health;
        Speed = speed;
        DamageBonus = damageBonus;
    }

    public float Health { get; }
    public float Speed { get; }
    public int DamageBonus { get; }
}
