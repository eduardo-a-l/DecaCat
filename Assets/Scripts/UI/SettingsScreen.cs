using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SettingsScreen : MonoBehaviour
{
    private const string ControlsText =
        "Move: WASD / Arrow keys     Attack: Left mouse button\n" +
        "Interact: E     Drop: Q     Map: M     Pause: Esc";

    private static readonly Color BackgroundColor = new Color(0.04f, 0.04f, 0.06f, 0.97f);
    private static readonly Color ControlsColor = new Color(1f, 1f, 1f, 0.6f);

    public static GameObject Create(Action onClose)
    {
        GameObject root = UIFactory.CreateOverlayCanvas("Settings", 100);

        root.AddComponent<SettingsScreen>();

        UIFactory.CreatePanel(root.transform, "Background", BackgroundColor);

        UIFactory.CreateText(
            root.transform, "Title", "SETTINGS", 90f, Color.white,
            new Vector2(0f, 430f), new Vector2(1000f, 120f)
        );

        float y = 270f;

        AddSlider(
            root.transform, "Master Volume", y, GameSettings.MasterVolume,
            value =>
            {
                GameSettings.MasterVolume = value;
                GameSettings.Apply();
            }
        );

        y -= 90f;

        AddSlider(
            root.transform, "Music Volume", y, GameSettings.MusicVolume,
            value => GameSettings.MusicVolume = value
        );

        y -= 90f;

        AddSlider(
            root.transform, "SFX Volume", y, GameSettings.SfxVolume,
            value => GameSettings.SfxVolume = value
        );

        y -= 90f;

        if (!Application.isMobilePlatform)
        {
            AddToggle(
                root.transform, "Fullscreen", y,
                () => GameSettings.Fullscreen,
                value =>
                {
                    GameSettings.Fullscreen = value;
                    GameSettings.Apply();
                }
            );

            y -= 90f;
        }

        AddToggle(
            root.transform, "Damage Numbers", y,
            () => GameSettings.ShowDamageNumbers,
            value => GameSettings.ShowDamageNumbers = value
        );

        UIFactory.CreateText(
            root.transform, "Controls", ControlsText, 30f, ControlsColor,
            new Vector2(0f, -290f), new Vector2(1500f, 120f)
        );

        UIFactory.CreateButton(
            root.transform, "BackButton", "Back",
            new Vector2(0f, -470f), new Vector2(300f, 80f), onClose, 44f
        );

        return root;
    }

    private void OnDestroy()
    {
        GameSettings.Save();
    }

    private static void AddSlider(
        Transform parent, string label, float y, float value,
        UnityAction<float> onChanged)
    {
        UIFactory.CreateText(
            parent, label + "Label", label, 40f, Color.white,
            new Vector2(-450f, y), new Vector2(500f, 60f),
            null, TextAlignmentOptions.Left
        );

        TextMeshProUGUI percent = UIFactory.CreateText(
            parent, label + "Value", FormatPercent(value), 36f, Color.white,
            new Vector2(520f, y), new Vector2(140f, 50f)
        );

        UIFactory.CreateSlider(
            parent, label + "Slider", new Vector2(130f, y),
            new Vector2(600f, 40f), value,
            changed =>
            {
                percent.text = FormatPercent(changed);
                onChanged(changed);
            }
        );
    }

    private static void AddToggle(
        Transform parent, string label, float y,
        Func<bool> getValue, Action<bool> setValue)
    {
        UIFactory.CreateText(
            parent, label + "Label", label, 40f, Color.white,
            new Vector2(-450f, y), new Vector2(500f, 60f),
            null, TextAlignmentOptions.Left
        );

        Button button = null;

        button = UIFactory.CreateButton(
            parent, label + "Toggle", getValue() ? "On" : "Off",
            new Vector2(130f, y), new Vector2(260f, 64f),
            () =>
            {
                setValue(!getValue());
                RefreshToggle(button, getValue());
            },
            38f
        );
    }

    private static void RefreshToggle(Button button, bool value)
    {
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();

        if (label != null)
            label.text = value ? "On" : "Off";
    }

    private static string FormatPercent(float value)
    {
        return Mathf.RoundToInt(value * 100f) + "%";
    }
}
