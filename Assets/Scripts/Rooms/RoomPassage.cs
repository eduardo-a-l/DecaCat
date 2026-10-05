using System.Collections.Generic;
using UnityEngine;

public class RoomPassage : MonoBehaviour
{
    private Room room;
    private RoomSide side;
    private Collider2D trigger;
    private List<GameObject> barriers;

    public void Setup(
        Room owner, RoomSide passageSide,
        Collider2D passageTrigger, List<GameObject> barrierObjects)
    {
        room = owner;
        side = passageSide;
        trigger = passageTrigger;
        barriers = barrierObjects;

        SetBlocked(false);
    }

    public void SetBlocked(bool blocked)
    {
        foreach (GameObject barrier in barriers)
            barrier.SetActive(blocked);

        trigger.enabled = !blocked;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerInputReader>() != null)
            room.NotifyPassageReached(side);
    }
}
