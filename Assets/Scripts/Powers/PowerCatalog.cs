using System;
using UnityEngine;

[Serializable]
public class PowerDefinition
{
    [SerializeField] private PowerType type;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;
    [SerializeField] private Sprite playerSprite;
    [SerializeField] private Color accent = Color.white;
    [SerializeField, TextArea(2, 6)] private string passive;
    [SerializeField, TextArea(2, 6)] private string special;
    [SerializeField, TextArea(2, 6)] private string negative;

    public PowerType Type => type;
    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public Sprite PlayerSprite => playerSprite;
    public Color Accent => accent;
    public string Passive => passive;
    public string Special => special;
    public string Negative => negative;

    public string FullDescription =>
        "Passive: " + passive + "\n\nSpecial: " + special +
        "\n\nNegative: " + negative;
}

[CreateAssetMenu(fileName = "Powers", menuName = "DecaCat/Powers/Power Catalog")]
public class PowerCatalog : ScriptableObject
{
    private const string ResourceName = "Powers";

    private static PowerCatalog instance;

    [SerializeField] private PowerDefinition[] powers;

    public static PowerCatalog Instance
    {
        get
        {
            if (instance == null)
                instance = Resources.Load<PowerCatalog>(ResourceName);

            return instance;
        }
    }

    public PowerDefinition[] Powers =>
        powers ?? new PowerDefinition[0];

    public PowerDefinition Get(PowerType type)
    {
        if (powers == null)
            return null;

        foreach (PowerDefinition definition in powers)
        {
            if (definition != null && definition.Type == type)
                return definition;
        }

        return null;
    }
}
