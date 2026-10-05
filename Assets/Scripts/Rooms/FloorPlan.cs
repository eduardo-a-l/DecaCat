using System.Collections.Generic;
using UnityEngine;

public class FloorPlan
{
    private readonly Dictionary<Vector2Int, HashSet<RoomSide>> links =
        new Dictionary<Vector2Int, HashSet<RoomSide>>();

    private readonly Dictionary<Vector2Int, RoomType> types =
        new Dictionary<Vector2Int, RoomType>();

    private readonly Dictionary<Vector2Int, RoomLayout> layouts =
        new Dictionary<Vector2Int, RoomLayout>();

    public FloorPlan(int seed)
    {
        Seed = seed;
    }

    public int Seed { get; }
    public Vector2Int StartCell => Vector2Int.zero;
    public int RoomCount => links.Count;
    public IEnumerable<Vector2Int> Cells => links.Keys;

    public bool HasCell(Vector2Int cell)
    {
        return links.ContainsKey(cell);
    }

    public void AddCell(Vector2Int cell)
    {
        if (links.ContainsKey(cell))
            return;

        links.Add(cell, new HashSet<RoomSide>());
        types[cell] = RoomType.Normal;
    }

    public void Link(Vector2Int cell, RoomSide side)
    {
        Vector2Int neighbor = cell + side.ToGridOffset();

        if (!links.ContainsKey(cell) || !links.ContainsKey(neighbor))
            return;

        links[cell].Add(side);
        links[neighbor].Add(side.Opposite());
    }

    public bool IsLinked(Vector2Int cell, RoomSide side)
    {
        return links.TryGetValue(cell, out HashSet<RoomSide> sides) &&
               sides.Contains(side);
    }

    public int GetLinkCount(Vector2Int cell)
    {
        return links.TryGetValue(cell, out HashSet<RoomSide> sides)
            ? sides.Count
            : 0;
    }

    public List<RoomSide> GetLinkedSides(Vector2Int cell)
    {
        List<RoomSide> linked = new List<RoomSide>();

        foreach (RoomSide side in RoomSideExtensions.All)
        {
            if (IsLinked(cell, side))
                linked.Add(side);
        }

        return linked;
    }

    public RoomType GetRoomType(Vector2Int cell)
    {
        return types[cell];
    }

    public void SetRoomType(Vector2Int cell, RoomType type)
    {
        types[cell] = type;
    }

    public RoomLayout GetLayout(Vector2Int cell)
    {
        return layouts.TryGetValue(cell, out RoomLayout layout) ? layout : null;
    }

    public void SetLayout(Vector2Int cell, RoomLayout layout)
    {
        layouts[cell] = layout;
    }
}
