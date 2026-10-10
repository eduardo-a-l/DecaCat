using UnityEngine;

public class HealthTest : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            playerHealth.TakeDamage(1);
        }
    }
#endif
}