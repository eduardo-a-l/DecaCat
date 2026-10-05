using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    [Serializable]
    private class RoomEntry
    {
        [SerializeField] private RoomLayout layout;
        [SerializeField] private Vector2Int gridPosition;

        public RoomLayout Layout => layout;
        public Vector2Int GridPosition => gridPosition;
    }

    [SerializeField] private RoomTileSet tileSet;
    [SerializeField] private RoomEntry[] rooms;
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private float transitionDuration = 0.6f;

    private readonly Dictionary<Vector2Int, Room> roomsByCell =
        new Dictionary<Vector2Int, Room>();

    private Camera mainCamera;
    private Rigidbody2D playerBody;
    private Room currentRoom;
    private bool isTransitioning;

    public Room CurrentRoom => currentRoom;
    public bool IsTransitioning => isTransitioning;

    private void Start()
    {
        mainCamera = Camera.main;

        if (inputReader == null)
            inputReader = FindFirstObjectByType<PlayerInputReader>();

        if (inputReader != null)
            playerBody = inputReader.GetComponent<Rigidbody2D>();

        BuildRooms();

        if (currentRoom == null)
            return;

        currentRoom.gameObject.SetActive(true);
        MoveCamera(currentRoom.Center);
        currentRoom.Enter();
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

    private void BuildRooms()
    {
        if (tileSet == null || rooms == null || rooms.Length == 0)
        {
            Debug.LogWarning("RoomManager needs a tile set and at least one room");
            return;
        }

        Dictionary<Vector2Int, RoomLayout> layouts =
            new Dictionary<Vector2Int, RoomLayout>();

        bool hasStart = false;
        Vector2Int startCell = Vector2Int.zero;

        foreach (RoomEntry entry in rooms)
        {
            if (entry == null || entry.Layout == null)
                continue;

            if (layouts.ContainsKey(entry.GridPosition))
            {
                Debug.LogWarning(
                    "Two rooms share the grid position " + entry.GridPosition
                );

                continue;
            }

            layouts.Add(entry.GridPosition, entry.Layout);

            if (!hasStart)
            {
                hasStart = true;
                startCell = entry.GridPosition;
            }
        }

        if (!hasStart)
            return;

        Dictionary<Vector2Int, Vector2> centers =
            ComputeCenters(layouts, startCell);

        foreach (KeyValuePair<Vector2Int, RoomLayout> pair in layouts)
        {
            if (!centers.ContainsKey(pair.Key))
            {
                Debug.LogWarning(
                    "Room at " + pair.Key +
                    " is not connected to the start room and was skipped"
                );
            }
        }

        foreach (KeyValuePair<Vector2Int, Vector2> pair in centers)
        {
            Vector2Int cell = pair.Key;

            GameObject roomObject =
                new GameObject("Room " + cell.x + "," + cell.y);

            roomObject.SetActive(false);
            roomObject.transform.SetParent(transform, false);

            Room room = roomObject.AddComponent<Room>();

            room.Build(
                this, layouts[cell], tileSet, cell, pair.Value,
                GetConnectedSides(layouts, cell)
            );

            roomsByCell.Add(cell, room);
        }

        currentRoom = roomsByCell[startCell];
    }

    private static List<RoomSide> GetConnectedSides(
        Dictionary<Vector2Int, RoomLayout> layouts, Vector2Int cell)
    {
        List<RoomSide> connected = new List<RoomSide>();
        RoomLayout layout = layouts[cell];

        foreach (RoomSide side in RoomSideExtensions.All)
        {
            Vector2Int neighborCell = cell + side.ToGridOffset();

            if (!layouts.TryGetValue(neighborCell, out RoomLayout neighbor))
                continue;

            if (layout.HasPassage(side) && neighbor.HasPassage(side.Opposite()))
                connected.Add(side);
        }

        return connected;
    }

    private static Dictionary<Vector2Int, Vector2> ComputeCenters(
        Dictionary<Vector2Int, RoomLayout> layouts, Vector2Int startCell)
    {
        Dictionary<Vector2Int, Vector2> centers =
            new Dictionary<Vector2Int, Vector2>();

        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        centers[startCell] = Vector2.zero;
        queue.Enqueue(startCell);

        while (queue.Count > 0)
        {
            Vector2Int cell = queue.Dequeue();
            RoomLayout layout = layouts[cell];

            foreach (RoomSide side in GetConnectedSides(layouts, cell))
            {
                Vector2Int neighborCell = cell + side.ToGridOffset();

                if (centers.ContainsKey(neighborCell))
                    continue;

                centers[neighborCell] =
                    centers[cell] +
                    GetNeighborOffset(layout, layouts[neighborCell], side);

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
