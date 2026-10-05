using UnityEngine;

public class TestTarget : MonoBehaviour, IDamageable
{
    [SerializeField] private int health = 3;

    public bool CostsDurability => true;

    public void TakeDamage(int damage)
    {
        health -= damage;

        Debug.Log("Target health: " + health);

        if (health <= 0)
        {
            Debug.Log("Target defeated");
            Destroy(gameObject);
        }
    }
}