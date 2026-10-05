using System.Collections.Generic;
using UnityEngine;

public static class FloorGenerator
{
    private const double ClusterChance = 0.6;
    private const int AttemptsPerRoom = 60;

    public static FloorPlan Generate(FloorConfig config, int seed)
    {
        System.Random random = new System.Random(seed);
        FloorPlan plan = new FloorPlan(seed);

        GrowRooms(plan, config, random);

        Vector2Int bossCell = ChooseBossCell(plan, random);

        AddLoops(plan, config, random, bossCell);
        AssignTypes(plan, config, random, bossCell);

        if (!AssignLayouts(plan, config, random))
            return null;

        return plan;
    }

    private static void GrowRooms(
        FloorPlan plan, FloorConfig config, System.Random random)
    {
        int target = random.Next(config.MinRooms, config.MaxRooms + 1);

        List<Vector2Int> cells = new List<Vector2Int>();

        plan.AddCell(plan.StartCell);
        cells.Add(plan.StartCell);

        int attempts = 0;
        int maxAttempts = target * AttemptsPerRoom;

        while (cells.Count < target && attempts < maxAttempts)
        {
            attempts++;

            Vector2Int origin = cells[random.Next(cells.Count)];
            RoomSide side = RoomSideExtensions.All[
                random.Next(RoomSideExtensions.All.Length)
            ];

            Vector2Int cell = origin + side.ToGridOffset();

            if (plan.HasCell(cell))
                continue;

            int neighbors = CountNeighbors(plan, cell);

            if (neighbors > 2)
                continue;

            if (neighbors == 2 && random.NextDouble() > ClusterChance)
                continue;

            plan.AddCell(cell);
            plan.Link(origin, side);
            cells.Add(cell);
        }
    }

    private static int CountNeighbors(FloorPlan plan, Vector2Int cell)
    {
        int count = 0;

        foreach (RoomSide side in RoomSideExtensions.All)
        {
            if (plan.HasCell(cell + side.ToGridOffset()))
                count++;
        }

        return count;
    }

    private static Vector2Int ChooseBossCell(FloorPlan plan, System.Random random)
    {
        Dictionary<Vector2Int, int> distances = GetDistances(plan);

        List<Vector2Int> candidates = new List<Vector2Int>();
        int bestScore = -1;

        foreach (Vector2Int cell in plan.Cells)
        {
            if (cell == plan.StartCell)
                continue;

            int score = distances[cell];

            if (plan.GetLinkCount(cell) == 1)
                score += 1000;

            if (score > bestScore)
            {
                bestScore = score;
                candidates.Clear();
                candidates.Add(cell);
            }
            else if (score == bestScore)
            {
                candidates.Add(cell);
            }
        }

        if (candidates.Count == 0)
            return plan.StartCell;

        return candidates[random.Next(candidates.Count)];
    }

    private static Dictionary<Vector2Int, int> GetDistances(FloorPlan plan)
    {
        Dictionary<Vector2Int, int> distances = new Dictionary<Vector2Int, int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        distances[plan.StartCell] = 0;
        queue.Enqueue(plan.StartCell);

        while (queue.Count > 0)
        {
            Vector2Int cell = queue.Dequeue();

            foreach (RoomSide side in plan.GetLinkedSides(cell))
            {
                Vector2Int neighbor = cell + side.ToGridOffset();

                if (distances.ContainsKey(neighbor))
                    continue;

                distances[neighbor] = distances[cell] + 1;
                queue.Enqueue(neighbor);
            }
        }

        return distances;
    }

    private static void AddLoops(
        FloorPlan plan, FloorConfig config, System.Random random, Vector2Int bossCell)
    {
        List<Vector2Int> cells = new List<Vector2Int>(plan.Cells);

        foreach (Vector2Int cell in cells)
        {
            foreach (RoomSide side in RoomSideExtensions.All)
            {
                Vector2Int neighbor = cell + side.ToGridOffset();

                if (!plan.HasCell(neighbor) || plan.IsLinked(cell, side))
                    continue;

                if (cell == bossCell || neighbor == bossCell)
                    continue;

                if (!IsBefore(cell, neighbor))
                    continue;

                if (random.NextDouble() < config.LoopChance)
                    plan.Link(cell, side);
            }
        }
    }

    private static void AssignTypes(
        FloorPlan plan, FloorConfig config, System.Random random, Vector2Int bossCell)
    {
        plan.SetRoomType(plan.StartCell, RoomType.Start);

        if (bossCell != plan.StartCell)
            plan.SetRoomType(bossCell, RoomType.Boss);

        if (config.PowerLayouts == null || config.PowerLayouts.Length == 0)
            return;

        List<Vector2Int> deadEnds = new List<Vector2Int>();

        foreach (Vector2Int cell in plan.Cells)
        {
            if (cell == plan.StartCell || cell == bossCell)
                continue;

            if (plan.GetLinkCount(cell) == 1)
                deadEnds.Add(cell);
        }

        Shuffle(deadEnds, random);

        int count = random.Next(config.MinPowerRooms, config.MaxPowerRooms + 1);
        count = Mathf.Min(count, deadEnds.Count);

        for (int i = 0; i < count; i++)
            plan.SetRoomType(deadEnds[i], RoomType.Power);
    }

    private static bool AssignLayouts(
        FloorPlan plan, FloorConfig config, System.Random random)
    {
        List<Vector2Int> cells = new List<Vector2Int>(plan.Cells);

        foreach (Vector2Int cell in cells)
        {
            RoomType type = plan.GetRoomType(cell);
            List<RoomSide> sides = plan.GetLinkedSides(cell);

            RoomLayout layout = PickLayout(GetPool(config, type), sides, random);

            if (layout == null)
                layout = PickLayout(config.NormalLayouts, sides, random);

            if (layout == null)
            {
                Debug.LogError(
                    config.name + " has no layout with openings on " +
                    string.Join(", ", sides) + " for a " + type + " room"
                );

                return false;
            }

            plan.SetLayout(cell, layout);
        }

        return true;
    }

    private static RoomLayout[] GetPool(FloorConfig config, RoomType type)
    {
        switch (type)
        {
            case RoomType.Start: return config.StartLayouts;
            case RoomType.Power: return config.PowerLayouts;
            case RoomType.Boss: return config.BossLayouts;
            default: return config.NormalLayouts;
        }
    }

    private static RoomLayout PickLayout(
        RoomLayout[] pool, List<RoomSide> sides, System.Random random)
    {
        if (pool == null)
            return null;

        List<RoomLayout> fitting = new List<RoomLayout>();

        foreach (RoomLayout layout in pool)
        {
            if (layout == null)
                continue;

            bool fits = true;

            foreach (RoomSide side in sides)
            {
                if (!layout.HasPassage(side))
                {
                    fits = false;
                    break;
                }
            }

            if (fits)
                fitting.Add(layout);
        }

        return fitting.Count > 0 ? fitting[random.Next(fitting.Count)] : null;
    }

    private static void Shuffle(List<Vector2Int> list, System.Random random)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            Vector2Int temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    private static bool IsBefore(Vector2Int a, Vector2Int b)
    {
        return a.x != b.x ? a.x < b.x : a.y < b.y;
    }
}
