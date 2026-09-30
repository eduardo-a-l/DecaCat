using UnityEngine;

public class HealthTest : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            playerHealth.TakeDamage(1);
        }
    }
}