using UnityEngine;

public static class PhysicsLayers
{
    private const string ObstacleName = "Obstacle";
    private const string BorderName = "RoomWall";

    private static int obstacleLayer = -2;
    private static int borderLayer = -2;

    public static int ObstacleLayer
    {
        get
        {
            if (obstacleLayer == -2)
                obstacleLayer = LayerMask.NameToLayer(ObstacleName);

            return obstacleLayer;
        }
    }

    public static int BorderLayer
    {
        get
        {
            if (borderLayer == -2)
                borderLayer = LayerMask.NameToLayer(BorderName);

            return borderLayer;
        }
    }

    public static int ObstacleMask => ToMask(ObstacleLayer);
    public static int BorderMask => ToMask(BorderLayer);
    public static int WallMask => ObstacleMask | BorderMask;

    public static void SetLayer(GameObject target, int layer)
    {
        if (layer >= 0)
            target.layer = layer;
    }

    public static void Exclude(GameObject target, int mask)
    {
        if (target == null || mask == 0)
            return;

        foreach (Collider2D collider in target.GetComponentsInChildren<Collider2D>(true))
            collider.excludeLayers = collider.excludeLayers.value | mask;
    }

    public static void Restore(GameObject target, int mask)
    {
        if (target == null || mask == 0)
            return;

        foreach (Collider2D collider in target.GetComponentsInChildren<Collider2D>(true))
            collider.excludeLayers = collider.excludeLayers.value & ~mask;
    }

    private static int ToMask(int layer)
    {
        return layer < 0 ? 0 : 1 << layer;
    }
}
