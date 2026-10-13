using UnityEngine;

public static class StuckDart
{
    private static readonly Vector2 DartSize = new Vector2(0.3f, 0.12f);

    public static void Attach(Enemy enemy, Color color, float seconds)
    {
        if (enemy == null)
            return;

        Vector2 offset = Random.insideUnitCircle * 0.25f;
        Vector3 position = enemy.transform.position + (Vector3)offset;

        SpriteRenderer renderer = PowerFx.CreateSprite(
            "StuckDart", PowerFx.Circle, color, position, DartSize, 6,
            enemy.transform
        );

        renderer.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        Object.Destroy(renderer.gameObject, seconds);
    }
}
