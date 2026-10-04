using System;
using UnityEngine;

[Serializable]
public class SlimeStage
{
    [SerializeField] private Sprite sprite;
    [SerializeField] private int health = 20;
    [SerializeField] private int damage = 1;
    [SerializeField] private float moveSpeed = 1.2f;
    [SerializeField] private float scale = 1f;
    [SerializeField] private Color placeholderColor = Color.white;

    public SlimeStage()
    {
    }

    public SlimeStage(
        int health, int damage, float moveSpeed,
        float scale, Color placeholderColor)
    {
        this.health = health;
        this.damage = damage;
        this.moveSpeed = moveSpeed;
        this.scale = scale;
        this.placeholderColor = placeholderColor;
    }

    public Sprite Sprite => sprite;
    public int Health => health;
    public int Damage => damage;
    public float MoveSpeed => moveSpeed;
    public float Scale => scale;
    public Color PlaceholderColor => placeholderColor;
}

[CreateAssetMenu(fileName = "New Slime", menuName = "DecaCat/Enemies/Slime")]
public class SlimeData : ScriptableObject
{
    [Header("Small slime (drag its sprite into Sprite)")]
    [SerializeField] private SlimeStage small =
        new SlimeStage(20, 1, 1.2f, 1f, new Color(0.55f, 0.9f, 0.45f, 1f));

    [Header("Big slime (two small slimes merged)")]
    [SerializeField] private SlimeStage big =
        new SlimeStage(40, 1, 0.7f, 1.4f, new Color(0.25f, 0.7f, 0.3f, 1f));

    public SlimeStage Small => small;
    public SlimeStage Big => big;
}
