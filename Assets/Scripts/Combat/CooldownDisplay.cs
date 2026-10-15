using UnityEngine;
using UnityEngine.UI;

public class CooldownDisplay : MonoBehaviour
{
    [SerializeField] private MeleeAttack meleeAttack;
    [SerializeField] private Image cooldownBar;

    private static readonly Color ExhaustedColor = new Color(0.9f, 0.15f, 0.15f, 1f);

    private Color normalColor = Color.white;
    private Texture2D generatedTexture;
    private Sprite generatedSprite;

    private void Awake()
    {
        if (cooldownBar != null)
        {
            if (cooldownBar.sprite == null)
            {
                generatedTexture = new Texture2D(1, 1);
                generatedTexture.SetPixel(0, 0, Color.white);
                generatedTexture.Apply();

                generatedSprite = Sprite.Create(
                    generatedTexture,
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f)
                );

                cooldownBar.sprite = generatedSprite;
            }

            normalColor = cooldownBar.color;
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

        bool exhausted = meleeAttack.CooldownExhausted;
        float total = exhausted ? Mathf.Max(0.01f, meleeAttack.ExhaustTotal) : cooldown;

        cooldownBar.enabled = true;
        cooldownBar.color = exhausted ? ExhaustedColor : normalColor;
        cooldownBar.fillAmount = Mathf.Clamp01(remaining / total);
    }

    private void OnDestroy()
    {
        if (generatedSprite != null)
            Destroy(generatedSprite);

        if (generatedTexture != null)
            Destroy(generatedTexture);
    }
}