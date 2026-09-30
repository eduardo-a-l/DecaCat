using UnityEngine;
using UnityEngine.UI;

public class HealthDisplay : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Image[] hearts;

    [SerializeField] private Sprite fullHeart;
    [SerializeField] private Sprite emptyHeart;

    private void Update()
    {
        if (playerHealth == null || hearts == null)
            return;

        for (int i = 0; i < hearts.Length; i++)
        {
            bool isFull = i < playerHealth.CurrentHealth;

            hearts[i].sprite = isFull ? fullHeart : emptyHeart;
        }
    }
}