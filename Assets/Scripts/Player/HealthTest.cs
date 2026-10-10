using UnityEngine;
using UnityEngine.InputSystem;

public class HealthTest : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null && keyboard.hKey.wasPressedThisFrame)
            playerHealth.TakeDamage(1);
    }
#endif
}
