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
    private const int ExitSortingOrder = -1;
    private const float ExitScale = 1.2f;

    private readonly List<Enemy> enemies = new List<Enemy>();
    private readonly List<RoomPassage> passages = new List<RoomPassage>();

    private RoomManager manager;

    public Collider2D WallCollider { get; private set; }
    public Collider2D ObstacleCollider { get; private set; }
    public IReadOnlyList<Enemy> Enemies => enemies;
    private readonly List<PowerAltar> altars = new List<PowerAltar>();
    private readonly List<Vector2Int> floorPoints = new List<Vector2Int>();
    private FloorEncounter encounter;
    private RoomLayout layout;
    private Transform gridTransform;
    private RoomTileSet tiles;
    private readonly List<RoomSide> linkedSides = new List<RoomSide>();

    public event Action<Room> Cleared;

    public RoomState State { get; private set; } = RoomState.Unexplored;
    public Vector2Int GridPosition { get; private set; }
    public RoomLayout Layout => layout;
    public Vector3 Center => transform.position;
    public Vector2 Size => new Vector2(layout.Width, layout.Height);
    public IReadOnlyList<RoomSide> ConnectedSides => linkedSides;
    public RoomType Type { get; private set; }

    private void Update()
    {
        if (State == RoomState.Active)
            CheckCleared();
    }

    public void Build(
        RoomManager owner, RoomLayout roomLayout, RoomTileSet tileSet,
        Vector2Int gridPosition, Vector2 center,
        IList<RoomSide> connectedSides, RoomType roomType,
        FloorEncounter floorEncounter)
    {
        manager = owner;
        encounter = floorEncounter;
        tiles = tileSet;
        Type = roomType;
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
        Tilemap obstacles = CreateTilemap("Obstacles", WallSortingOrder);

        WallCollider = SetupWallCollision(walls, PhysicsLayers.BorderLayer);
        ObstacleCollider = SetupWallCollision(obstacles, PhysicsLayers.ObstacleLayer);

        Tile floorTile = tileSet.CreateFloorTile(
            encounter != null ? encounter.FloorTint : Color.white
        );

        Tile wallTile = tileSet.CreateWallTile(
            encounter != null ? encounter.WallTint : Color.white
        );
        List<Vector2Int> spawnCells = new List<Vector2Int>();
        List<Vector2Int> crateCells = new List<Vector2Int>();
        List<Vector2Int> altarCells = new List<Vector2Int>();

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
                    Tilemap target = IsBorderCell(x, y) ? walls : obstacles;

                    target.SetTile(cell, wallTile);
                    continue;
                }

                floor.SetTile(cell, floorTile);

                if (layout.IsSpawn(x, y))
                    spawnCells.Add(new Vector2Int(x, y));

                if (layout.IsCrate(x, y))
                    crateCells.Add(new Vector2Int(x, y));

                if (layout.IsAltar(x, y))
                    altarCells.Add(new Vector2Int(x, y));
            }
        }

        foreach (RoomSide side in connectedSides)
            CreatePassage(side, tileSet);

        if (crateCells.Count == 0 && Type != RoomType.Boss)
            TryAddBonusCrate(crateCells);

        SpawnEnemies(spawnCells);
        SpawnCrates(crateCells);
        SpawnAltars(altarCells);
        SpawnUpgradeAltars();
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
        DiscoverEnemies();
        DiscoverAltars();
        CheckCleared();
        RefreshBarriers();
    }

    private void DiscoverEnemies()
    {
        foreach (Enemy enemy in enemies)
        {
            if (enemy != null)
                Discoveries.AddEnemy(enemy);
        }
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

    public void AddEnemy(Enemy enemy)
    {
        if (enemy == null)
            return;

        enemies.Add(enemy);

        if (State == RoomState.Active)
            Discoveries.AddEnemy(enemy);
    }

    public List<Vector3> GetCornerPositions()
    {
        int last = layout.Height - 2;
        int right = layout.Width - 2;

        Vector2Int[] cells =
        {
            new Vector2Int(1, 1),
            new Vector2Int(right, 1),
            new Vector2Int(1, last),
            new Vector2Int(right, last)
        };

        List<Vector3> corners = new List<Vector3>();

        foreach (Vector2Int cell in cells)
        {
            if (layout.IsFloor(cell.x, cell.y))
                corners.Add(transform.TransformPoint(CellToLocal(cell)));
        }

        return corners;
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

        if (Type == RoomType.Boss)
            CreateFloorExit();

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

    public Vector2 GetRandomFloorPoint()
    {
        if (floorPoints.Count == 0)
        {
            for (int y = 1; y < layout.Height - 1; y++)
            {
                for (int x = 1; x < layout.Width - 1; x++)
                {
                    if (layout.GetCell(x, y) == '.')
                        floorPoints.Add(new Vector2Int(x, y));
                }
            }
        }

        if (floorPoints.Count == 0)
            return transform.position;

        Vector2Int cell = floorPoints[UnityEngine.Random.Range(0, floorPoints.Count)];

        return transform.TransformPoint(CellToLocal(cell));
    }

    public bool IsObstacleAt(Vector2 worldPosition)
    {
        int mask = PhysicsLayers.ObstacleMask;

        return mask != 0 && Physics2D.OverlapPoint(worldPosition, mask) != null;
    }

    private bool IsBorderCell(int x, int y)
    {
        return x == 0 || y == 0 || x == layout.Width - 1 || y == layout.Height - 1;
    }

    private static CompositeCollider2D SetupWallCollision(Tilemap walls, int layer)
    {
        PhysicsLayers.SetLayer(walls.gameObject, layer);

        Rigidbody2D body = walls.gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Static;

        CompositeCollider2D composite =
            walls.gameObject.AddComponent<CompositeCollider2D>();

        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;

        TilemapCollider2D tilemapCollider =
            walls.gameObject.AddComponent<TilemapCollider2D>();

        tilemapCollider.compositeOperation =
            Collider2D.CompositeOperation.Merge;

        return composite;
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

    private void CreateFloorExit()
    {
        GameObject exit = new GameObject("FloorExit");
        exit.transform.SetParent(transform, false);
        exit.transform.localScale = Vector3.one * ExitScale;

        SpriteRenderer spriteRenderer = exit.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = tiles.ExitSprite;
        spriteRenderer.color = tiles.ExitTint;
        spriteRenderer.sortingOrder = ExitSortingOrder;

        BoxCollider2D trigger = exit.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = spriteRenderer.sprite.bounds.size;

        exit.AddComponent<FloorExit>().Setup(manager);
    }

    private void SpawnAltars(List<Vector2Int> altarCells)
    {
        PowerCatalog catalog = PowerCatalog.Instance;

        if (altarCells.Count == 0 || encounter == null || catalog == null)
            return;

        bool hasSave = SaveSession.IsActive;

        if (hasSave && SaveSession.Data.takenAltarRooms.Contains(GetAltarKey()))
            return;

        PowerType held = default;
        bool holding = hasSave &&
            Enum.TryParse(SaveSession.Data.loadoutPower, out held);

        List<PowerDefinition> options = new List<PowerDefinition>();

        foreach (PowerDefinition definition in catalog.Powers)
        {
            if (definition != null && !(holding && definition.Type == held))
                options.Add(definition);
        }

        StableRandom random = new StableRandom(
            encounter.RunSeed,
            encounter.Floor * 1009 + GridPosition.x * 97 + GridPosition.y * 13 + 7
        );

        Transform altarRoot = new GameObject("Altars").transform;
        altarRoot.SetParent(transform, false);

        foreach (Vector2Int cell in altarCells)
        {
            if (options.Count == 0)
                break;

            int index = random.Next(options.Count);
            PowerDefinition chosen = options[index];

            options.RemoveAt(index);

            Vector3 position = transform.TransformPoint(CellToLocal(cell));

            altars.Add(PowerAltar.Spawn(position, altarRoot, chosen, OnAltarTaken));
        }
    }

    private void SpawnUpgradeAltars()
    {
        if (Type != RoomType.Start || encounter == null || !SaveSession.IsActive)
            return;

        int floor = encounter.Floor;

        if (!PermanentUpgrades.IsUpgradeFloor(floor) ||
            PermanentUpgrades.IsChosenFor(floor))
            return;

        Transform altarRoot = new GameObject("UpgradeAltars").transform;
        altarRoot.SetParent(transform, false);

        Vector2Int[] targets =
        {
            new Vector2Int(layout.Width / 2 - 4, layout.Height / 2),
            new Vector2Int(layout.Width / 2 + 3, layout.Height / 2)
        };

        PermanentUpgradeType[] types =
        {
            PermanentUpgradeType.MaxHeart,
            PermanentUpgradeType.Damage
        };

        for (int i = 0; i < types.Length; i++)
        {
            Vector2Int cell = FindOpenCell(targets[i]);
            Vector3 position = transform.TransformPoint(CellToLocal(cell));

            altars.Add(
                PowerAltar.SpawnUpgrade(position, altarRoot, types[i], OnAltarTaken)
            );
        }
    }

    private Vector2Int FindOpenCell(Vector2Int target)
    {
        for (int distance = 0; distance <= 6; distance++)
        {
            for (int dy = -distance; dy <= distance; dy++)
            {
                for (int dx = -distance; dx <= distance; dx++)
                {
                    if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != distance)
                        continue;

                    int x = target.x + dx;
                    int y = target.y + dy;

                    if (x < 1 || y < 1 || x > layout.Width - 2 || y > layout.Height - 2)
                        continue;

                    if (layout.GetCell(x, y) == '.')
                        return new Vector2Int(x, y);
                }
            }
        }

        return target;
    }

    private void OnAltarTaken(PowerAltar taken)
    {
        foreach (PowerAltar other in altars)
        {
            if (other != null && other != taken)
                other.Vanish();
        }

        if (!SaveSession.IsActive)
            return;

        if (taken.IsUpgrade)
        {
            List<int> chosen = SaveSession.Data.upgradeFloorsChosen;

            if (!chosen.Contains(encounter.Floor))
                chosen.Add(encounter.Floor);

            return;
        }

        List<string> list = SaveSession.Data.takenAltarRooms;
        string key = GetAltarKey();

        if (!list.Contains(key))
            list.Add(key);
    }

    private string GetAltarKey()
    {
        return encounter.Floor + ":" + GridPosition.x + ":" + GridPosition.y;
    }

    private void DiscoverAltars()
    {
        foreach (PowerAltar altar in altars)
        {
            if (altar != null && !altar.Taken && !altar.IsUpgrade)
            {
                Discoveries.Add(
                    Discoveries.PowerKind, altar.PowerType.ToString(),
                    altar.Definition.DisplayName
                );
            }
        }
    }

    private void TryAddBonusCrate(List<Vector2Int> crateCells)
    {
        StableRandom random = encounter != null
            ? new StableRandom(
                encounter.RunSeed,
                encounter.Floor * 2003 + GridPosition.x * 131 + GridPosition.y * 17 + 3)
            : new StableRandom(UnityEngine.Random.Range(1, int.MaxValue), 3);

        if (random.Next(2) == 0)
            return;

        int width = layout.Width;
        int height = layout.Height;

        List<Vector2Int> options = new List<Vector2Int>();

        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                if (layout.GetCell(x, y) != '.')
                    continue;

                bool sideDoorLane = (y == 4 || y == 5) && (x <= 3 || x >= width - 4);
                bool endDoorLane = (x == 8 || x == 9) && (y <= 2 || y >= height - 3);

                if (!sideDoorLane && !endDoorLane)
                    options.Add(new Vector2Int(x, y));
            }
        }

        if (options.Count > 0)
            crateCells.Add(options[random.Next(options.Count)]);
    }

    private void SpawnCrates(List<Vector2Int> crateCells)
    {
        Crate prefab = layout.CratePrefab;

        if (prefab == null)
        {
            FloorThemeCatalog catalog = FloorThemeCatalog.Instance;

            if (catalog != null)
                prefab = catalog.DefaultCratePrefab;
        }

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
        if (spawnCells.Count == 0)
            return;

        EnemyScaling scaling =
            encounter != null ? encounter.Scaling : EnemyScaling.None;

        Transform enemyRoot = null;
        Enemy groupPrefab = null;
        int groupLeft = 0;

        foreach (Vector2Int cell in spawnCells)
        {
            Enemy prefab;

            if (groupLeft > 0 && groupPrefab != null)
            {
                prefab = groupPrefab;
                groupLeft--;
            }
            else
            {
                prefab = ChooseEnemy();
                groupPrefab = prefab;
                groupLeft = 0;

                if (prefab != null && prefab.GroupSize > 1 &&
                    UnityEngine.Random.value < 0.75f)
                    groupLeft = prefab.GroupSize - 1;
            }

            if (prefab == null)
                continue;

            if (enemyRoot == null)
            {
                enemyRoot = new GameObject("Enemies").transform;
                enemyRoot.SetParent(transform, false);
            }

            Vector3 position = transform.TransformPoint(CellToLocal(cell));

            enemies.Add(Enemy.Spawn(prefab, position, enemyRoot, scaling));
        }
    }

    private Enemy ChooseEnemy()
    {
        if (encounter != null)
        {
            if (Type == RoomType.Boss && encounter.BossPrefab != null)
                return encounter.BossPrefab;

            if (Type != RoomType.Boss && layout.UseFloorEnemies)
            {
                Enemy picked = encounter.PickEnemy();

                if (picked != null)
                    return picked;
            }
        }

        Enemy[] prefabs = layout.EnemyPrefabs;

        if (prefabs == null || prefabs.Length == 0)
            return null;

        return prefabs[UnityEngine.Random.Range(0, prefabs.Length)];
    }
}
