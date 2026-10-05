using UnityEngine;

public enum RoomSide
{
    North,
    East,
    South,
    West
}

public enum RoomType
{
    Start,
    Normal,
    Power,
    Boss
}

public enum RoomState
{
    Unexplored,
    Active,
    Cleared
}

public static class RoomSideExtensions
{
    public static readonly RoomSide[] All =
    {
        RoomSide.North,
        RoomSide.East,
        RoomSide.South,
        RoomSide.West
    };

    public static RoomSide Opposite(this RoomSide side)
    {
        switch (side)
        {
            case RoomSide.North: return RoomSide.South;
            case RoomSide.East: return RoomSide.West;
            case RoomSide.South: return RoomSide.North;
            default: return RoomSide.East;
        }
    }

    public static Vector2Int ToGridOffset(this RoomSide side)
    {
        switch (side)
        {
            case RoomSide.North: return Vector2Int.up;
            case RoomSide.East: return Vector2Int.right;
            case RoomSide.South: return Vector2Int.down;
            default: return Vector2Int.left;
        }
    }

    public static Vector2 ToVector(this RoomSide side)
    {
        Vector2Int offset = side.ToGridOffset();
        return new Vector2(offset.x, offset.y);
    }

    public static bool IsHorizontal(this RoomSide side)
    {
        return side == RoomSide.East || side == RoomSide.West;
    }
}
