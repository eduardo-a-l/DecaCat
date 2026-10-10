using System;
using UnityEngine;

public static class PauseMenu
{
    private static readonly Color BackgroundColor = new Color(0f, 0f, 0f, 0.8f);
    private static readonly Color HintColor = new Color(1f, 1f, 1f, 0.6f);

    private static readonly Vector2 ButtonSize = new Vector2(500f, 90f);

    public static GameObject Create(
        Action onResume, Action onAchievements, Action onIndex,
        Action onSettings, Action onRestartFloor, Action onMainMenu)
    {
        GameObject root = UIFactory.CreateOverlayCanvas("PauseMenu", 100);

        UIFactory.CreatePanel(root.transform, "Background", BackgroundColor);

        UIFactory.CreateText(
            root.transform, "Title", "PAUSED", 110f, Color.white,
            new Vector2(0f, 340f), new Vector2(1000f, 150f)
        );

        AddButton(root, "ResumeButton", "Resume", 215f, onResume);
        AddButton(root, "AchievementsButton", "Achievements", 110f, onAchievements);
        AddButton(root, "IndexButton", "Index", 5f, onIndex);
        AddButton(root, "SettingsButton", "Settings", -100f, onSettings);
        AddButton(root, "RestartButton", "Restart Floor", -205f, onRestartFloor);
        AddButton(root, "MenuButton", "Main Menu", -310f, onMainMenu);

        UIFactory.CreateText(
            root.transform, "Hint", Application.isMobilePlatform ? "Back to resume" : "Esc to resume", 32f, HintColor,
            new Vector2(0f, -410f), new Vector2(600f, 50f)
        );

        return root;
    }

    private static void AddButton(
        GameObject root, string objectName, string label, float y, Action onClick)
    {
        UIFactory.CreateButton(
            root.transform, objectName, label,
            new Vector2(0f, y), ButtonSize, onClick, 48f
        );
    }
}
