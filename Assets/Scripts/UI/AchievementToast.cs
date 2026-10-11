using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AchievementToast : MonoBehaviour
{
    private const float SlideTime = 0.35f;
    private const float HoldTime = 3f;
    private const float HiddenY = 640f;
    private const float ShownY = 440f;

    private static readonly Vector2 PanelSize = new Vector2(820f, 150f);
    private static readonly Color GoldColor = new Color(0.95f, 0.78f, 0.3f, 1f);
    private static readonly Color PanelColor = new Color(0.06f, 0.06f, 0.08f, 0.95f);
    private static readonly Color DescriptionColor = new Color(1f, 1f, 1f, 0.7f);

    private class ToastInfo
    {
        public string Header;
        public string Title;
        public string Description;
    }

    private static readonly Queue<ToastInfo> Pending = new Queue<ToastInfo>();

    private static AchievementToast active;

    private RectTransform panel;
    private float elapsed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnLoad()
    {
        Pending.Clear();
        active = null;
    }

    public static void Show(AchievementDefinition achievement)
    {
        Show("ACHIEVEMENT UNLOCKED", achievement.Title, achievement.Description);
    }

    public static void Show(string header, string title, string description)
    {
        Pending.Enqueue(new ToastInfo
        {
            Header = header,
            Title = title,
            Description = description
        });

        if (active == null)
            ShowNext();
    }

    private static void ShowNext()
    {
        if (Pending.Count == 0)
            return;

        ToastInfo info = Pending.Dequeue();

        GameObject root = UIFactory.CreateOverlayCanvas("AchievementToast", 150);

        AchievementToast toast = root.AddComponent<AchievementToast>();
        toast.Build(info);

        active = toast;
    }

    private void Build(ToastInfo info)
    {
        panel = UIFactory.CreateRect(
            transform, "Panel", new Vector2(0f, HiddenY), PanelSize
        );

        Image fill = panel.gameObject.AddComponent<Image>();
        fill.color = PanelColor;
        fill.raycastTarget = false;

        UIFactory.AddOutline(panel, PanelSize, 4f, GoldColor);

        UIFactory.CreateText(
            panel, "Header", info.Header, 26f, GoldColor,
            new Vector2(0f, 48f), new Vector2(780f, 36f)
        );

        UIFactory.CreateText(
            panel, "Title", info.Title, 46f, Color.white,
            new Vector2(0f, 6f), new Vector2(780f, 60f)
        );

        UIFactory.CreateText(
            panel, "Description", info.Description, 28f,
            DescriptionColor, new Vector2(0f, -44f), new Vector2(780f, 40f)
        );
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;

        float y;

        if (elapsed < SlideTime)
        {
            y = Mathf.Lerp(HiddenY, ShownY, elapsed / SlideTime);
        }
        else if (elapsed < SlideTime + HoldTime)
        {
            y = ShownY;
        }
        else
        {
            y = Mathf.Lerp(
                ShownY, HiddenY, (elapsed - SlideTime - HoldTime) / SlideTime
            );
        }

        panel.anchoredPosition = new Vector2(0f, y);

        if (elapsed < SlideTime * 2f + HoldTime)
            return;

        active = null;
        Destroy(gameObject);

        ShowNext();
    }

    private void OnDestroy()
    {
        if (active == this)
            active = null;
    }
}
