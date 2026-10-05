using System;
using UnityEngine;

public class GameOverScreen : MonoBehaviour
{
    private const float FadeDuration = 0.5f;

    private static readonly Color BackgroundColor = new Color(0f, 0f, 0f, 0.8f);
    private static readonly Color TitleColor = new Color(0.9f, 0.25f, 0.25f, 1f);
    private static readonly Color HintColor = new Color(1f, 1f, 1f, 0.6f);

    private CanvasGroup canvasGroup;
    private float elapsed;

    public static GameOverScreen Create(Action onRestart)
    {
        GameObject root = UIFactory.CreateOverlayCanvas("GameOverScreen", 100);

        CanvasGroup group = root.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        GameOverScreen screen = root.AddComponent<GameOverScreen>();
        screen.canvasGroup = group;

        UIFactory.CreatePanel(root.transform, "Background", BackgroundColor);

        UIFactory.CreateText(
            root.transform, "Title", "GAME OVER", 120f, TitleColor,
            new Vector2(0f, 140f), new Vector2(1200f, 160f)
        );

        UIFactory.CreateButton(
            root.transform, "RestartButton", "Restart",
            new Vector2(0f, -40f), new Vector2(420f, 100f), onRestart
        );

        UIFactory.CreateText(
            root.transform, "Hint", "or press R", 32f, HintColor,
            new Vector2(0f, -170f), new Vector2(600f, 50f)
        );

        return screen;
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        canvasGroup.alpha = Mathf.Clamp01(elapsed / FadeDuration);
    }
}
