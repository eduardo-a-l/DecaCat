using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class MapOverlay
{
    private const float MaxScale = 24f;
    private const float RoomInset = 1f;
    private const float ConnectorLength = 1.6f;
    private const float ConnectorThickness = 1f;
    private const float MarkerSize = 36f;

    private const string LegendText =
        "<color=#F2B340>Not cleared</color>      " +
        "<color=#6BBD8C>Cleared</color>      " +
        "<color=#8A8A94>Unexplored</color>      " +
        "White outline: you";

    private static readonly Vector2 MapArea = new Vector2(1500f, 700f);
    private static readonly Color BackgroundColor = new Color(0f, 0f, 0f, 0.88f);
    private static readonly Color UnexploredColor = new Color(0.28f, 0.28f, 0.33f, 1f);
    private static readonly Color ActiveColor = new Color(0.95f, 0.7f, 0.25f, 1f);
    private static readonly Color ClearedColor = new Color(0.42f, 0.74f, 0.55f, 1f);
    private static readonly Color ConnectorColor = new Color(0.55f, 0.55f, 0.62f, 1f);
    private static readonly Color UnknownColor = new Color(1f, 1f, 1f, 0.5f);

    public static GameObject Create(RoomManager manager, Action onClose)
    {
        GameObject root = UIFactory.CreateOverlayCanvas("MapOverlay", 100);

        UIFactory.CreatePanel(root.transform, "Background", BackgroundColor);

        UIFactory.CreateText(
            root.transform, "Title", "MAP", 90f, Color.white,
            new Vector2(0f, 470f), new Vector2(800f, 120f)
        );

        RectTransform area = UIFactory.CreateRect(
            root.transform, "Area", new Vector2(0f, 20f), MapArea
        );

        List<Room> visible = GetVisibleRooms(manager);

        if (visible.Count > 0)
            DrawMap(manager, visible, area);

        UIFactory.CreateText(
            root.transform, "Legend", LegendText, 34f, Color.white,
            new Vector2(0f, -390f), new Vector2(1500f, 60f)
        );

        UIFactory.CreateButton(
            root.transform, "CloseButton", "Close",
            new Vector2(0f, -465f), new Vector2(320f, 80f), onClose
        );

        return root;
    }

    private static List<Room> GetVisibleRooms(RoomManager manager)
    {
        List<Room> visible = new List<Room>();

        if (manager == null)
            return visible;

        foreach (Room room in manager.Rooms)
        {
            if (IsVisible(manager, room))
                visible.Add(room);
        }

        return visible;
    }

    private static bool IsVisible(RoomManager manager, Room room)
    {
        if (room.State != RoomState.Unexplored)
            return true;

        foreach (RoomSide side in RoomSideExtensions.All)
        {
            if (manager.TryGetNeighbor(room, side, out Room neighbor) &&
                neighbor.State != RoomState.Unexplored)
                return true;
        }

        return false;
    }

    private static void DrawMap(
        RoomManager manager, List<Room> visible, RectTransform area)
    {
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);

        foreach (Room room in visible)
        {
            Vector2 center = room.Center;
            Vector2 half = room.Size / 2f;

            min = Vector2.Min(min, center - half);
            max = Vector2.Max(max, center + half);
        }

        Vector2 span = max - min;

        float scale = Mathf.Min(
            MapArea.x / span.x, MapArea.y / span.y, MaxScale
        );

        Vector2 worldCenter = (min + max) / 2f;

        DrawConnectors(manager, visible, area, worldCenter, scale);

        foreach (Room room in visible)
            DrawRoom(manager, room, area, worldCenter, scale);
    }

    private static void DrawConnectors(
        RoomManager manager, List<Room> visible, RectTransform area,
        Vector2 worldCenter, float scale)
    {
        foreach (Room room in visible)
        {
            foreach (RoomSide side in room.ConnectedSides)
            {
                if (!manager.TryGetNeighbor(room, side, out Room neighbor))
                    continue;

                if (!visible.Contains(neighbor))
                    continue;

                if (room.State == RoomState.Unexplored &&
                    neighbor.State == RoomState.Unexplored)
                    continue;

                if (!IsBefore(room.GridPosition, neighbor.GridPosition))
                    continue;

                Vector2 seam =
                    ((Vector2)room.GetPassageWorldCenter(side) +
                     (Vector2)neighbor.GetPassageWorldCenter(side.Opposite())) / 2f;

                Vector2 size = side.IsHorizontal()
                    ? new Vector2(ConnectorLength, ConnectorThickness)
                    : new Vector2(ConnectorThickness, ConnectorLength);

                RectTransform rect = UIFactory.CreateRect(
                    area, "Connector",
                    (seam - worldCenter) * scale, size * scale
                );

                Image image = rect.gameObject.AddComponent<Image>();
                image.color = ConnectorColor;
                image.raycastTarget = false;
            }
        }
    }

    private static void DrawRoom(
        RoomManager manager, Room room, RectTransform area,
        Vector2 worldCenter, float scale)
    {
        Vector2 size = (room.Size - Vector2.one * RoomInset) * scale;
        Vector2 center = room.Center;

        RectTransform rect = UIFactory.CreateRect(
            area, "Room", (center - worldCenter) * scale, size
        );

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = GetColor(room.State);
        image.raycastTarget = false;

        if (room.State == RoomState.Unexplored)
        {
            UIFactory.CreateText(
                rect, "Unknown", "?", Mathf.Min(size.x, size.y) * 0.4f,
                UnknownColor, Vector2.zero, size
            );
        }

        if (room != manager.CurrentRoom)
            return;

        Outline outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(5f, 5f);

        RectTransform marker = UIFactory.CreateRect(
            rect, "You", Vector2.zero, Vector2.one * MarkerSize
        );

        Image markerImage = marker.gameObject.AddComponent<Image>();
        markerImage.sprite = UIFactory.CircleSprite;
        markerImage.color = Color.white;
        markerImage.raycastTarget = false;
    }

    private static Color GetColor(RoomState state)
    {
        switch (state)
        {
            case RoomState.Active: return ActiveColor;
            case RoomState.Cleared: return ClearedColor;
            default: return UnexploredColor;
        }
    }

    private static bool IsBefore(Vector2Int a, Vector2Int b)
    {
        return a.x != b.x ? a.x < b.x : a.y < b.y;
    }
}
