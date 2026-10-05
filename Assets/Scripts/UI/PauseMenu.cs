using System;
using UnityEngine;

public static class PauseMenu
{
    private static readonly Color BackgroundColor = new Color(0f, 0f, 0f, 0.75f);
    private static readonly Color HintColor = new Color(1f, 1f, 1f, 0.6f);

    public static GameObject Create(Action onResume, Action onRestart)
    {
        GameObject root = UIFactory.CreateOverlayCanvas("PauseMenu", 100);

        UIFactory.CreatePanel(root.transform, "Background", BackgroundColor);

        UIFactory.CreateText(
            root.transform, "Title", "PAUSED", 110f, Color.white,
            new Vector2(0f, 190f), new Vector2(1000f, 150f)
        );

        UIFactory.CreateButton(
            root.transform, "ResumeButton", "Resume",
            new Vector2(0f, 20f), new Vector2(420f, 100f), onResume
        );

        UIFactory.CreateButton(
            root.transform, "RestartButton", "Restart",
            new Vector2(0f, -110f), new Vector2(420f, 100f), onRestart
        );

        UIFactory.CreateText(
            root.transform, "Hint", "Esc to resume", 32f, HintColor,
            new Vector2(0f, -240f), new Vector2(600f, 50f)
        );

        return root;
    }
}
