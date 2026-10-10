using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "Room Tile Set", menuName = "DecaCat/Rooms/Tile Set")]
public class RoomTileSet : ScriptableObject
{
    private static Sprite placeholderSquare;

    [Header("Drag your sprites here (one tile = 1 world unit)")]
    [SerializeField] private Sprite floorSprite;
    [SerializeField] private Sprite wallSprite;
    [SerializeField] private Sprite barrierSprite;
    [SerializeField] private Sprite exitSprite;

    [Header("Placeholder colors (used while a sprite is empty)")]
    [SerializeField] private Color floorColor = new Color(0.2f, 0.22f, 0.27f, 1f);
    [SerializeField] private Color wallColor = new Color(0.42f, 0.45f, 0.55f, 1f);
    [SerializeField] private Color barrierColor = new Color(0.9f, 0.15f, 0.15f, 0.85f);
    [SerializeField] private Color exitColor = new Color(0.6f, 0.4f, 1f, 1f);

    public Sprite BarrierSprite =>
        barrierSprite != null ? barrierSprite : PlaceholderSquare;

    public Color BarrierTint =>
        barrierSprite != null ? Color.white : barrierColor;

    public Sprite ExitSprite =>
        exitSprite != null ? exitSprite : PlaceholderSquare;

    public Color ExitTint =>
        exitSprite != null ? Color.white : exitColor;

    private static Sprite PlaceholderSquare
    {
        get
        {
            if (placeholderSquare == null)
            {
                Texture2D texture =
                    new Texture2D(1, 1, TextureFormat.RGBA32, false);

                texture.filterMode = FilterMode.Point;
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();

                placeholderSquare = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f),
                    1f
                );
            }

            return placeholderSquare;
        }
    }

    public Tile CreateFloorTile()
    {
        return CreateFloorTile(Color.white);
    }

    public Tile CreateWallTile()
    {
        return CreateWallTile(Color.white);
    }

    public Tile CreateFloorTile(Color tint)
    {
        return CreateTile(floorSprite, floorColor, tint, Tile.ColliderType.None);
    }

    public Tile CreateWallTile(Color tint)
    {
        return CreateTile(wallSprite, wallColor, tint, Tile.ColliderType.Grid);
    }

    private static Tile CreateTile(
        Sprite sprite, Color placeholderColor, Color tint,
        Tile.ColliderType colliderType)
    {
        Tile tile = CreateInstance<Tile>();
        bool hasSprite = sprite != null;

        tile.sprite = hasSprite ? sprite : PlaceholderSquare;
        tile.color = hasSprite ? tint : ThemedColor(placeholderColor, tint);
        tile.colliderType = colliderType;

        return tile;
    }

    private static Color ThemedColor(Color placeholder, Color tint)
    {
        Color.RGBToHSV(tint, out float tintHue, out float tintSaturation, out _);

        if (tintSaturation < 0.01f)
            return placeholder;

        Color.RGBToHSV(
            placeholder, out _, out float saturation, out float value
        );

        Color themed = Color.HSVToRGB(
            tintHue, Mathf.Max(saturation, tintSaturation * 0.8f), value
        );

        themed.a = placeholder.a;

        return themed;
    }
}
