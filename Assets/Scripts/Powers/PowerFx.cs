using System.Collections.Generic;
using UnityEngine;

public static class PowerFx
{
    private const int RingPixels = 128;

    private static Sprite ringSprite;
    private static Sprite squareSprite;
    private static RoomManager roomManager;

    public static Sprite Circle => UIFactory.CircleSprite;

    public static Sprite Ring
    {
        get
        {
            if (ringSprite == null)
                ringSprite = CreateRing();

            return ringSprite;
        }
    }

    public static Sprite Square
    {
        get
        {
            if (squareSprite == null)
                squareSprite = CreateSquare();

            return squareSprite;
        }
    }

    public static Room CurrentRoom
    {
        get
        {
            if (roomManager == null)
                roomManager = Object.FindFirstObjectByType<RoomManager>();

            return roomManager != null ? roomManager.CurrentRoom : null;
        }
    }

    public static Transform EffectParent
    {
        get
        {
            Room room = CurrentRoom;

            return room != null ? room.transform : null;
        }
    }

    public static List<Enemy> GetRoomEnemies()
    {
        List<Enemy> result = new List<Enemy>();
        Room room = CurrentRoom;

        if (room == null)
            return result;

        foreach (Enemy enemy in room.Enemies)
        {
            if (enemy != null && enemy.CurrentHealth > 0)
                result.Add(enemy);
        }

        return result;
    }

    public static List<Enemy> GetEnemiesInRadius(Vector2 center, float radius)
    {
        List<Enemy> result = new List<Enemy>();

        foreach (Collider2D hit in Physics2D.OverlapCircleAll(center, radius))
        {
            Enemy enemy = hit.GetComponentInParent<Enemy>();

            if (enemy != null && enemy.CurrentHealth > 0 && !result.Contains(enemy))
                result.Add(enemy);
        }

        return result;
    }

    public static Enemy GetClosestEnemy(Vector2 from, float maxDistance, Enemy exclude = null)
    {
        Enemy closest = null;
        float best = maxDistance * maxDistance;

        foreach (Enemy enemy in GetRoomEnemies())
        {
            if (enemy == exclude)
                continue;

            float distance = ((Vector2)enemy.transform.position - from).sqrMagnitude;

            if (distance < best)
            {
                best = distance;
                closest = enemy;
            }
        }

        return closest;
    }

    public static Vector2 Rotate(Vector2 direction, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        return new Vector2(
            direction.x * cos - direction.y * sin,
            direction.x * sin + direction.y * cos
        );
    }

    public static SpriteRenderer CreateSprite(
        string objectName, Sprite sprite, Color color, Vector3 position,
        Vector2 worldSize, int sortingOrder, Transform parent)
    {
        GameObject spriteObject = new GameObject(objectName);

        if (parent != null)
            spriteObject.transform.SetParent(parent, true);

        spriteObject.transform.position = position;

        SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;

        Vector2 native = sprite.bounds.size;

        spriteObject.transform.localScale = new Vector3(
            worldSize.x / Mathf.Max(0.0001f, native.x),
            worldSize.y / Mathf.Max(0.0001f, native.y),
            1f
        );

        return renderer;
    }

    private static Sprite CreateRing()
    {
        Texture2D texture = new Texture2D(
            RingPixels, RingPixels, TextureFormat.RGBA32, false
        );

        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        float radius = RingPixels * 0.5f;
        float inner = radius * 0.86f;

        for (int y = 0; y < RingPixels; y++)
        {
            for (int x = 0; x < RingPixels; x++)
            {
                float dx = x + 0.5f - radius;
                float dy = y + 0.5f - radius;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                float alpha =
                    Mathf.Clamp01(radius - distance) *
                    Mathf.Clamp01(distance - inner);

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();

        return Sprite.Create(
            texture, new Rect(0f, 0f, RingPixels, RingPixels),
            new Vector2(0.5f, 0.5f), RingPixels
        );
    }

    private static Sprite CreateSquare()
    {
        Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
                texture.SetPixel(x, y, Color.white);
        }

        texture.Apply();

        return Sprite.Create(
            texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f
        );
    }
}
