using UnityEngine;

[CreateAssetMenu(fileName = "New Floor Config", menuName = "DecaCat/Rooms/Floor Config")]
public class FloorConfig : ScriptableObject
{
    [Header("Size")]
    [SerializeField] private int minRooms = 8;
    [SerializeField] private int maxRooms = 12;

    [Header("Paths (chance that touching rooms get an extra connection)")]
    [SerializeField, Range(0f, 1f)] private float loopChance = 0.65f;

    [Header("Power rooms (optional dead-end rooms)")]
    [SerializeField] private int minPowerRooms = 0;
    [SerializeField] private int maxPowerRooms = 2;

    [Header("Room pools")]
    [SerializeField] private RoomLayout[] startLayouts;
    [SerializeField] private RoomLayout[] normalLayouts;
    [SerializeField] private RoomLayout[] powerLayouts;
    [SerializeField] private RoomLayout[] bossLayouts;

    [Header("Testing (0 = random every time)")]
    [SerializeField] private int seed;

    public int MinRooms => Mathf.Max(2, minRooms);
    public int MaxRooms => Mathf.Max(MinRooms, maxRooms);
    public float LoopChance => loopChance;
    public int MinPowerRooms => Mathf.Max(0, minPowerRooms);
    public int MaxPowerRooms => Mathf.Max(MinPowerRooms, maxPowerRooms);
    public RoomLayout[] StartLayouts => startLayouts;
    public RoomLayout[] NormalLayouts => normalLayouts;
    public RoomLayout[] PowerLayouts => powerLayouts;
    public RoomLayout[] BossLayouts => bossLayouts;
    public int Seed => seed;
}
