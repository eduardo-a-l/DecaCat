using UnityEngine;

public class TestTarget : MonoBehaviour
{
    [SerializeField] private int health = 3;

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