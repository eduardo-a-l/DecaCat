using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Room : MonoBehaviour
{
    private const float EntryDepth = 1f;
    private const int FloorSortingOrder = -10;
    private const int WallSortingOrder = -5;
    private const int BarrierSortingOrder = -2;

    private readonly List<Enemy> enemies = new List<Enemy>();
    private readonly List<RoomPassage> passages = new List<RoomPassage>();

    private RoomManager manager;
    private RoomLayout layout;
    private Transform gridTransform;
    private readonly List<RoomSide> linkedSides = new List<RoomSide>();

    public event Action<Room> Cleared;

    public RoomState State { get; private set; } = RoomState.Unexplored;
    public Vector2Int GridPosition { get; private set; }
    public RoomLayout Layout => layout;
    public Vector3 Center => transform.position;
    public Vector2 Size => new Vector2(layout.Width, layout.Height);
    public IReadOnlyList<RoomSide> ConnectedSides => linkedSides;

    private void Update()
    {
        if (State == RoomState.Active)
            CheckCleared();
    }

    public void Build(
        RoomManager owner, RoomLayout roomLayout, RoomTileSet tileSet,
        Vector2Int gridPosition, Vector2 center,
        IList<RoomSide> connectedSides)
    {
        manager = owner;
        layout = roomLayout;
        GridPosition = gridPosition;
        linkedSides.Clear();
        linkedSides.AddRange(connectedSides);
        transform.position = new Vector3(center.x, center.y, 0f);

        gridTransform = new GameObject("Grid", typeof(Grid)).transform;
        gridTransform.SetParent(transform, false);
        gridTransform.localPosition =
            new Vector3(-layout.Width / 2f, -layout.Height / 2f, 0f);

        Tilemap floor = CreateTilemap("Floor", FloorSortingOrder);
        Tilemap walls = CreateTilemap("Walls", WallSortingOrder);
        SetupWallCollision(walls);

        Tile floorTile = tileSet.CreateFloorTile();
        Tile wallTile = tileSet.CreateWallTile();
        List<Vector2Int> spawnCells = new List<Vector2Int>();
        List<Vector2Int> crateCells = new List<Vector2Int>();

        for (int y = 0; y < layout.Height; y++)
        {
            for (int x = 0; x < layout.Width; x++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);

                bool open =
                    layout.IsFloor(x, y) &&
                    !IsSealedPassage(x, y, connectedSides);

                if (!open)
                {
                    walls.SetTile(cell, wallTile);
                    continue;
                }

                floor.SetTile(cell, floorTile);

                if (layout.IsSpawn(x, y))
                    spawnCells.Add(new Vector2Int(x, y));

                if (layout.IsCrate(x, y))
                    crateCells.Add(new Vector2Int(x, y));
            }
        }

        foreach (RoomSide side in connectedSides)
            CreatePassage(side, tileSet);

        SpawnEnemies(spawnCells);
        SpawnCrates(crateCells);
    }

    public void Activate()
    {
        gameObject.SetActive(true);
        SetEnemiesFrozen(true);
    }

    public void Deactivate()
    {
        gameObject.SetActive(false);
    }

    public void BeginExit()
    {
        SetEnemiesFrozen(true);
    }

    public void Enter()
    {
        if (State == RoomState.Unexplored)
            State = RoomState.Active;

        SetEnemiesFrozen(false);
        CheckCleared();
        RefreshBarriers();
    }

    public void NotifyPassageReached(RoomSide side)
    {
        manager.RequestTransition(this, side);
    }

    public bool IsConnected(RoomSide side)
    {
        return linkedSides.Contains(side);
    }

    public Vector3 GetPassageWorldCenter(RoomSide side)
    {
        return transform.TransformPoint(layout.GetPassageLocalCenter(side));
    }

    public Vector3 GetEntryPosition(RoomSide side)
    {
        Vector2 inward = -side.ToVector();

        return GetPassageWorldCenter(side) + (Vector3)(inward * EntryDepth);
    }

    private void CheckCleared()
    {
        if (State != RoomState.Active)
            return;

        enemies.RemoveAll(enemy => enemy == null);

        if (enemies.Count > 0)
            return;

        State = RoomState.Cleared;
        Debug.Log(name + " cleared");

        RefreshBarriers();

        if (Cleared != null)
            Cleared(this);
    }

    private void RefreshBarriers()
    {
        bool blocked = layout.RequiresClear && State == RoomState.Active;

        foreach (RoomPassage passage in passages)
            passage.SetBlocked(blocked);
    }

    private void SetEnemiesFrozen(bool frozen)
    {
        foreach (Enemy enemy in enemies)
        {
            if (enemy != null)
                enemy.SetFrozen(frozen);
        }
    }

    private bool IsSealedPassage(int x, int y, IList<RoomSide> connectedSides)
    {
        RoomSide? side = layout.GetPassageSide(x, y);

        return side.HasValue && !connectedSides.Contains(side.Value);
    }

    private Vector3 CellToLocal(Vector2Int cell)
    {
        return new Vector3(
            cell.x + 0.5f - layout.Width / 2f,
            cell.y + 0.5f - layout.Height / 2f,
            0f
        );
    }

    private Tilemap CreateTilemap(string objectName, int sortingOrder)
    {
        GameObject tilemapObject = new GameObject(
            objectName, typeof(Tilemap), typeof(TilemapRenderer)
        );

        tilemapObject.transform.SetParent(gridTransform, false);
        tilemapObject.GetComponent<TilemapRenderer>().sortingOrder =
            sortingOrder;

        return tilemapObject.GetComponent<Tilemap>();
    }

    private static void SetupWallCollision(Tilemap walls)
    {
        Rigidbody2D body = walls.gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;

        CompositeCollider2D composite =
            walls.gameObject.AddComponent<CompositeCollider2D>();

        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;

        TilemapCollider2D tilemapCollider =
            walls.gameObject.AddComponent<TilemapCollider2D>();

        tilemapCollider.compositeOperation =
            Collider2D.CompositeOperation.Merge;
    }

    private void CreatePassage(RoomSide side, RoomTileSet tileSet)
    {
        List<Vector2Int> cells = layout.GetPassageCells(side);

        if (cells.Count == 0)
            return;

        GameObject passageObject = new GameObject("Passage " + side);
        passageObject.transform.SetParent(transform, false);
        passageObject.transform.localPosition =
            layout.GetPassageLocalCenter(side);

        BoxCollider2D trigger = passageObject.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = side.IsHorizontal()
            ? new Vector2(1f, cells.Count)
            : new Vector2(cells.Count, 1f);

        List<GameObject> barriers = new List<GameObject>();

        foreach (Vector2Int cell in cells)
        {
            GameObject barrier = new GameObject("Barrier");
            barrier.transform.SetParent(passageObject.transform, false);
            barrier.transform.localPosition =
                CellToLocal(cell) - passageObject.transform.localPosition;

            SpriteRenderer spriteRenderer =
                barrier.AddComponent<SpriteRenderer>();

            spriteRenderer.sprite = tileSet.BarrierSprite;
            spriteRenderer.color = tileSet.BarrierTint;
            spriteRenderer.sortingOrder = BarrierSortingOrder;

            BoxCollider2D collider = barrier.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;

            barrier.SetActive(false);
            barriers.Add(barrier);
        }

        RoomPassage passage = passageObject.AddComponent<RoomPassage>();
        passage.Setup(this, side, trigger, barriers);
        passages.Add(passage);
    }

    private void SpawnCrates(List<Vector2Int> crateCells)
    {
        Crate prefab = layout.CratePrefab;

        if (prefab == null || crateCells.Count == 0)
            return;

        Transform crateRoot = new GameObject("Crates").transform;
        crateRoot.SetParent(transform, false);

        foreach (Vector2Int cell in crateCells)
        {
            Vector3 position = transform.TransformPoint(CellToLocal(cell));

            Instantiate(prefab, position, Quaternion.identity, crateRoot);
        }
    }

    private void SpawnEnemies(List<Vector2Int> spawnCells)
    {
        Enemy[] prefabs = layout.EnemyPrefabs;

        if (prefabs == null || prefabs.Length == 0 || spawnCells.Count == 0)
            return;

        Transform enemyRoot = new GameObject("Enemies").transform;
        enemyRoot.SetParent(transform, false);

        foreach (Vector2Int cell in spawnCells)
        {
            Enemy prefab = prefabs[UnityEngine.Random.Range(0, prefabs.Length)];

            if (prefab == null)
                continue;

            Vector3 position = transform.TransformPoint(CellToLocal(cell));

            enemies.Add(
                Instantiate(prefab, position, Quaternion.identity, enemyRoot)
            );
        }
    }
}
