using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    [SerializeField] private RoomTileSet tileSet;
    [SerializeField] private FloorConfig[] floors;
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private float transitionDuration = 0.6f;

    private readonly Dictionary<Vector2Int, Room> roomsByCell =
        new Dictionary<Vector2Int, Room>();

    private Camera mainCamera;
    private Rigidbody2D playerBody;
    private Room currentRoom;
    private bool isTransitioning;
    private bool hasGenerated;
    private int floorNumber = 1;

    public Room CurrentRoom => currentRoom;
    public bool IsTransitioning => isTransitioning;
    public IEnumerable<Room> Rooms => roomsByCell.Values;

    private void Start()
    {
        mainCamera = Camera.main;

        if (inputReader == null)
            inputReader = FindFirstObjectByType<PlayerInputReader>();

        if (gameManager == null)
            gameManager = FindFirstObjectByType<GameManager>();

        if (inputReader != null)
            playerBody = inputReader.GetComponent<Rigidbody2D>();

        GenerateFloor(gameManager != null ? gameManager.CurrentFloor : 1);
    }

    public bool TryGetNeighbor(Room room, RoomSide side, out Room neighbor)
    {
        neighbor = null;

        if (room == null || !room.IsConnected(side))
            return false;

        return roomsByCell.TryGetValue(
            room.GridPosition + side.ToGridOffset(), out neighbor
        );
    }

    public void RequestTransition(Room from, RoomSide side)
    {
        if (isTransitioning || from != currentRoom)
            return;

        Vector2Int targetCell = from.GridPosition + side.ToGridOffset();

        if (!roomsByCell.TryGetValue(targetCell, out Room to))
            return;

        StartCoroutine(TransitionCoroutine(from, to, side));
    }

    public void AdvanceFloor()
    {
        if (isTransitioning)
            return;

        int next = floorNumber + 1;

        if (gameManager != null)
        {
            gameManager.NextFloor();
            next = gameManager.CurrentFloor;
        }

        GenerateFloor(next);
    }

    private void GenerateFloor(int number)
    {
        FloorConfig config = GetConfig(number);

        if (tileSet == null || config == null)
        {
            Debug.LogWarning("RoomManager needs a tile set and a floor config");
            return;
        }

        int seed = config.Seed != 0
            ? config.Seed + number
            : Random.Range(1, int.MaxValue);

        FloorPlan plan = FloorGenerator.Generate(config, seed);

        if (plan == null)
            return;

        ClearFloor();

        floorNumber = number;
        BuildRooms(plan);
        hasGenerated = true;

        Debug.Log(
            "Floor " + number + ": " + plan.RoomCount +
            " rooms, seed " + plan.Seed
        );

        currentRoom = roomsByCell[plan.StartCell];

        Vector3 start = currentRoom.Center;

        if (playerBody != null)
        {
            playerBody.linearVelocity = Vector2.zero;
            playerBody.position = start;
            playerBody.transform.position = new Vector3(
                start.x, start.y, playerBody.transform.position.z
            );
        }

        MoveCamera(start);

        currentRoom.gameObject.SetActive(true);
        currentRoom.Enter();
    }

    private FloorConfig GetConfig(int number)
    {
        if (floors == null || floors.Length == 0)
            return null;

        return floors[Mathf.Clamp(number - 1, 0, floors.Length - 1)];
    }

    private void ClearFloor()
    {
        foreach (Room room in roomsByCell.Values)
        {
            room.gameObject.SetActive(false);
            Destroy(room.gameObject);
        }

        roomsByCell.Clear();
        currentRoom = null;

        if (!hasGenerated)
            return;

        foreach (ItemPickup pickup in
                 FindObjectsByType<ItemPickup>(FindObjectsSortMode.None))
        {
            Destroy(pickup.gameObject);
        }
    }

    private void BuildRooms(FloorPlan plan)
    {
        WarnAboutMixedSizes(plan);

        Dictionary<Vector2Int, Vector2> centers = ComputeCenters(plan);

        foreach (KeyValuePair<Vector2Int, Vector2> pair in centers)
        {
            Vector2Int cell = pair.Key;

            GameObject roomObject =
                new GameObject("Room " + cell.x + "," + cell.y);

            roomObject.SetActive(false);
            roomObject.transform.SetParent(transform, false);

            Room room = roomObject.AddComponent<Room>();

            room.Build(
                this, plan.GetLayout(cell), tileSet, cell, pair.Value,
                plan.GetLinkedSides(cell), plan.GetRoomType(cell)
            );

            roomsByCell.Add(cell, room);
        }
    }

    private static void WarnAboutMixedSizes(FloorPlan plan)
    {
        RoomLayout first = plan.GetLayout(plan.StartCell);

        foreach (Vector2Int cell in plan.Cells)
        {
            RoomLayout layout = plan.GetLayout(cell);

            if (layout.Width != first.Width || layout.Height != first.Height)
            {
                Debug.LogWarning(
                    "Room layouts on one floor should share a size, but " +
                    layout.name + " differs from " + first.name
                );

                return;
            }
        }
    }

    private static Dictionary<Vector2Int, Vector2> ComputeCenters(FloorPlan plan)
    {
        Dictionary<Vector2Int, Vector2> centers =
            new Dictionary<Vector2Int, Vector2>();

        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        centers[plan.StartCell] = Vector2.zero;
        queue.Enqueue(plan.StartCell);

        while (queue.Count > 0)
        {
            Vector2Int cell = queue.Dequeue();
            RoomLayout layout = plan.GetLayout(cell);

            foreach (RoomSide side in plan.GetLinkedSides(cell))
            {
                Vector2Int neighborCell = cell + side.ToGridOffset();

                if (centers.ContainsKey(neighborCell))
                    continue;

                centers[neighborCell] =
                    centers[cell] +
                    GetNeighborOffset(layout, plan.GetLayout(neighborCell), side);

                queue.Enqueue(neighborCell);
            }
        }

        return centers;
    }

    private static Vector2 GetNeighborOffset(
        RoomLayout layout, RoomLayout neighbor, RoomSide side)
    {
        Vector2 exitLocal = layout.GetPassageLocalCenter(side);
        Vector2 entryLocal = neighbor.GetPassageLocalCenter(side.Opposite());

        if (side.IsHorizontal())
        {
            float distance = (layout.Width + neighbor.Width) / 2f;

            return new Vector2(
                side.ToVector().x * distance,
                exitLocal.y - entryLocal.y
            );
        }

        float verticalDistance = (layout.Height + neighbor.Height) / 2f;

        return new Vector2(
            exitLocal.x - entryLocal.x,
            side.ToVector().y * verticalDistance
        );
    }

    private IEnumerator TransitionCoroutine(Room from, Room to, RoomSide side)
    {
        isTransitioning = true;

        if (inputReader != null)
            inputReader.InputEnabled = false;

        from.BeginExit();
        to.Activate();

        Transform player = playerBody != null ? playerBody.transform : null;

        if (playerBody != null)
        {
            playerBody.linearVelocity = Vector2.zero;
            playerBody.simulated = false;
        }

        Vector3 playerStart = player != null ? player.position : Vector3.zero;
        Vector3 playerEnd = to.GetEntryPosition(side.Opposite());
        playerEnd.z = playerStart.z;

        Vector3 cameraStart =
            mainCamera != null ? mainCamera.transform.position : Vector3.zero;

        Vector3 cameraEnd =
            new Vector3(to.Center.x, to.Center.y, cameraStart.z);

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.SmoothStep(
                0f, 1f, Mathf.Clamp01(elapsed / transitionDuration)
            );

            if (mainCamera != null)
            {
                mainCamera.transform.position =
                    Vector3.Lerp(cameraStart, cameraEnd, t);
            }

            if (player != null)
                player.position = Vector3.Lerp(playerStart, playerEnd, t);

            yield return null;
        }

        if (mainCamera != null)
            mainCamera.transform.position = cameraEnd;

        from.Deactivate();
        currentRoom = to;

        if (playerBody != null)
        {
            playerBody.transform.position = playerEnd;
            playerBody.simulated = true;
            playerBody.position = playerEnd;
            playerBody.linearVelocity = Vector2.zero;
        }

        to.Enter();

        if (inputReader != null)
            inputReader.InputEnabled = true;

        isTransitioning = false;
    }

    private void MoveCamera(Vector3 center)
    {
        if (mainCamera == null)
            return;

        mainCamera.transform.position = new Vector3(
            center.x, center.y, mainCamera.transform.position.z
        );
    }
}
