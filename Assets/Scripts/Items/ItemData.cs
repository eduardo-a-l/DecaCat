using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "DecaCat/Item")]
public class ItemData : ScriptableObject
{
    [SerializeField] private string itemName;
    [SerializeField] private Sprite itemSprite;
    [SerializeField] private int maxDurability = 10;

    public string ItemName => itemName;
    public Sprite ItemSprite => itemSprite;
    public int MaxDurability => maxDurability;
}