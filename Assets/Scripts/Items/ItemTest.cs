using UnityEngine;

public class ItemTest : MonoBehaviour
{
    [SerializeField] private PlayerItem playerItem;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.U))
        {
            playerItem.UseItem();
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            playerItem.DropItem();
        }
    }
}