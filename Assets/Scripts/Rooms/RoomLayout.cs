using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Room Layout", menuName = "DecaCat/Rooms/Room Layout")]
public class RoomLayout : ScriptableObject
{
    [Header("Rules")]
    [SerializeField] private bool requiresClear = true;
    [SerializeField] private Enemy[] enemyPrefabs;
    [SerializeField] private Crate cratePrefab;

    [Header("Layout:   #  wall    .  floor    E  enemy    C  crate")]
    [SerializeField, TextArea(12, 24)] private string layout;

    [NonSerialized] private string[] rows;
    [NonSerialized] private int width;

    public bool RequiresClear => requiresClear;
    public Enemy[] EnemyPrefabs => enemyPrefabs;
    public Crate CratePrefab => cratePrefab;

    public int Width
    {
        get
        {
            Parse();
            return width;
        }
    }

    public int Height
    {
        get
        {
            Parse();
            return rows.Length;
        }
    }

    private void OnValidate()
    {
        rows = null;
    }

    public char GetCell(int x, int y)
    {
        Parse();

        int row = rows.Length - 1 - y;

        if (row < 0 || row >= rows.Length || x < 0)
            return '#';

        string line = rows[row];

        return x < line.Length ? line[x] : '#';
    }

    public bool IsFloor(int x, int y)
    {
        char cell = GetCell(x, y);

        return cell == '.' || cell == 'E' || cell == 'C';
    }

    public bool IsSpawn(int x, int y)
    {
        return GetCell(x, y) == 'E';
    }

    public bool IsCrate(int x, int y)
    {
        return GetCell(x, y) == 'C';
    }

    public RoomSide? GetPassageSide(int x, int y)
    {
        int w = Width;
        int h = Height;

        if (x > 0 && x < w - 1)
        {
            if (y == h - 1)
                return RoomSide.North;

            if (y == 0)
                return RoomSide.South;
        }

        if (y > 0 && y < h - 1)
        {
            if (x == 0)
                return RoomSide.West;

            if (x == w - 1)
                return RoomSide.East;
        }

        return null;
    }

    public List<Vector2Int> GetPassageCells(RoomSide side)
    {
        List<Vector2Int> cells = new List<Vector2Int>();
        int w = Width;
        int h = Height;

        if (side.IsHorizontal())
        {
            int x = side == RoomSide.West ? 0 : w - 1;

            for (int y = 1; y < h - 1; y++)
            {
                if (IsFloor(x, y))
                    cells.Add(new Vector2Int(x, y));
            }
        }
        else
        {
            int y = side == RoomSide.South ? 0 : h - 1;

            for (int x = 1; x < w - 1; x++)
            {
                if (IsFloor(x, y))
                    cells.Add(new Vector2Int(x, y));
            }
        }

        return cells;
    }

    public bool HasPassage(RoomSide side)
    {
        return GetPassageCells(side).Count > 0;
    }

    public Vector2 GetPassageLocalCenter(RoomSide side)
    {
        List<Vector2Int> cells = GetPassageCells(side);

        if (cells.Count == 0)
            return Vector2.zero;

        Vector2 sum = Vector2.zero;

        foreach (Vector2Int cell in cells)
            sum += new Vector2(cell.x + 0.5f, cell.y + 0.5f);

        return sum / cells.Count - new Vector2(Width / 2f, Height / 2f);
    }

    private void Parse()
    {
        if (rows != null)
            return;

        string[] lines =
            (layout ?? string.Empty).Replace("\r", string.Empty).Split('\n');

        int first = 0;
        int last = lines.Length - 1;

        while (first <= last && string.IsNullOrWhiteSpace(lines[first]))
            first++;

        while (last >= first && string.IsNullOrWhiteSpace(lines[last]))
            last--;

        int count = Mathf.Max(0, last - first + 1);

        rows = new string[count];
        width = 0;

        for (int i = 0; i < count; i++)
        {
            rows[i] = lines[first + i].TrimEnd();
            width = Mathf.Max(width, rows[i].Length);
        }
    }
}
