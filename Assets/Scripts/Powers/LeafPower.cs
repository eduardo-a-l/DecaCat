using System.Collections.Generic;
using UnityEngine;

public class LeafPower : PowerBehaviour
{
    private const int MaxLeaves = 4;
    private const float DistancePerLeaf = 3f;
    private const float OrbitRadius = 1.15f;
    private const float OrbitSpeed = 200f;
    private const int LeafDamage = 4;
    private const float LeafHitRadius = 0.3f;

    private static readonly Color LeafColor = new Color(0.35f, 0.85f, 0.3f, 1f);

    private readonly List<Transform> leaves = new List<Transform>();

    private float moved;
    private float angle;

    public override float DamageMultiplier => 0.5f;
    public override float SpecialCooldown => 3f;

    public override void OnLost()
    {
        foreach (Transform leaf in leaves)
        {
            if (leaf != null)
                Object.Destroy(leaf.gameObject);
        }

        leaves.Clear();
    }

    public override void Tick(float step)
    {
        if (leaves.Count < MaxLeaves)
        {
            moved += Owner.MovedThisFrame;

            while (moved >= DistancePerLeaf && leaves.Count < MaxLeaves)
            {
                moved -= DistancePerLeaf;
                AddLeaf();
            }
        }
        else
        {
            moved = 0f;
        }

        angle += OrbitSpeed * step;

        for (int i = leaves.Count - 1; i >= 0; i--)
        {
            Transform leaf = leaves[i];

            if (leaf == null)
            {
                leaves.RemoveAt(i);
                continue;
            }

            float leafAngle = angle + i * 360f / leaves.Count;
            float radians = leafAngle * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

            leaf.position = Owner.Position + offset * OrbitRadius;
            leaf.rotation = Quaternion.Euler(0f, 0f, leafAngle + 90f);

            if (TryHitEnemy(leaf.position))
            {
                Object.Destroy(leaf.gameObject);
                leaves.RemoveAt(i);
            }
        }
    }

    public override bool UseSpecial(Vector2 direction)
    {
        if (Owner.Health == null || !Owner.Health.Heal(1))
            return false;

        ZoneEffect.Spawn(Owner.Position, 1.2f, new Color(0.4f, 1f, 0.45f, 0.8f), 0.6f, true, 0.2f, 6);

        return true;
    }

    public override string GetStatusText()
    {
        return "Leaves " + leaves.Count + "/" + MaxLeaves;
    }

    private void AddLeaf()
    {
        SpriteRenderer renderer = PowerFx.CreateSprite(
            "Leaf", PowerFx.Circle, LeafColor, Owner.Position,
            new Vector2(0.2f, 0.46f), 4, null
        );

        leaves.Add(renderer.transform);
    }

    private static bool TryHitEnemy(Vector2 position)
    {
        foreach (Enemy enemy in PowerFx.GetEnemiesInRadius(position, LeafHitRadius))
        {
            enemy.TakeDamage(LeafDamage);
            ZoneEffect.Spawn(position, 0.4f, LeafColor, 0.25f, true, 0.3f, 6);

            return true;
        }

        return false;
    }
}
