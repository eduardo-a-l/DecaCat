using UnityEngine;

public class FloorExit : MonoBehaviour
{
    private const float ArmDelay = 1f;

    private RoomManager manager;
    private float armedTime;

    public void Setup(RoomManager owner)
    {
        manager = owner;
        armedTime = Time.time + ArmDelay;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (manager == null || Time.time < armedTime)
            return;

        if (other.GetComponentInParent<PlayerInputReader>() != null)
            manager.AdvanceFloor();
    }
}
