using UnityEngine;
using UnityEngine.UI;

public class ItemDisplay : MonoBehaviour
{
    [SerializeField] private PlayerItem playerItem;
    [SerializeField] private Image itemImage;

    private void Update()
    {
        if (playerItem == null || itemImage == null)
            return;

        ItemData item = playerItem.CurrentItem;

        if (item == null)
        {
            itemImage.enabled = false;
        }
        else
        {
            itemImage.enabled = true;
            itemImage.sprite = item.ItemSprite;
        }
    }
}