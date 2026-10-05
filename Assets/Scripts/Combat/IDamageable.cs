public interface IDamageable
{
    bool CostsDurability { get; }

    void TakeDamage(int damage);
}
