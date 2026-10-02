using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class HeldItemVisual : MonoBehaviour
{
    [SerializeField] private PlayerItem playerItem;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        if (playerItem == null)
            return;

        ItemData item = playerItem.CurrentItem;

        if (item == null)
        {
            spriteRenderer.enabled = false;
            return;
        }

        spriteRenderer.enabled = true;
        spriteRenderer.sprite = item.ItemSprite;
    }
}