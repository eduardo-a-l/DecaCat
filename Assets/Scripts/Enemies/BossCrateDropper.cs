using System.Collections.Generic;
using UnityEngine;

public class BossCrateDropper
{
    private const float FallHeight = 8f;
    private const float FallTime = 0.6f;
    private const float CornerClearRadius = 0.45f;

    private static readonly Color MarkerColor = new Color(1f, 0.9f, 0.3f, 0.5f);

    private readonly Dictionary<Crate, Vector3> dropped =
        new Dictionary<Crate, Vector3>();

    private float timer;

    public void Tick(float step, float interval, int count, Crate prefab, Room room)
    {
        timer += step;

        if (timer < interval)
            return;

        timer = 0f;

        Drop(count, prefab, room);
    }

    private void Drop(int count, Crate prefab, Room room)
    {
        if (room == null)
            return;

        if (prefab == null && FloorThemeCatalog.Instance != null)
            prefab = FloorThemeCatalog.Instance.DefaultCratePrefab;

        if (prefab == null)
            return;

        List<Vector3> corners = room.GetCornerPositions();

        for (int i = corners.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Vector3 temp = corners[i];
            corners[i] = corners[j];
            corners[j] = temp;
        }

        int placed = 0;

        foreach (Vector3 corner in corners)
        {
            if (placed >= count)
                break;

            if (IsCornerOccupied(corner))
                continue;

            Crate crate = Object.Instantiate(
                prefab, corner, Quaternion.identity, room.transform
            );

            crate.BeginFall(FallHeight, FallTime);
            dropped[crate] = corner;

            GroundMarker.Spawn(corner, 0.5f, FallTime, MarkerColor);

            placed++;
        }
    }

    private bool IsCornerOccupied(Vector3 corner)
    {
        foreach (KeyValuePair<Crate, Vector3> pair in dropped)
        {
            if (pair.Key != null &&
                Vector2.Distance(pair.Value, corner) < CornerClearRadius)
                return true;
        }

        foreach (Collider2D hit in
                 Physics2D.OverlapCircleAll(corner, CornerClearRadius))
        {
            if (hit.GetComponent<Crate>() != null)
                return true;
        }

        return false;
    }
}
