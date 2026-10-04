using UnityEngine;
using UnityEngine.UI;

public class CooldownDisplay : MonoBehaviour
{
    [SerializeField] private MeleeAttack meleeAttack;
    [SerializeField] private Image cooldownBar;

    private void Awake()
    {
        if (cooldownBar != null)
        {
            cooldownBar.type = Image.Type.Filled;
            cooldownBar.fillMethod = Image.FillMethod.Horizontal;
            cooldownBar.fillOrigin = 0;
            cooldownBar.fillAmount = 1f;
        }
    }

    private void Update()
    {
        if (meleeAttack == null || cooldownBar == null)
            return;

        float cooldown = meleeAttack.CurrentCooldown;
        float remaining = meleeAttack.CooldownRemaining;

        if (cooldown <= 0f || remaining <= 0f)
        {
            cooldownBar.enabled = false;
            return;
        }

        cooldownBar.enabled = true;
        cooldownBar.fillAmount = remaining / cooldown;
    }
}