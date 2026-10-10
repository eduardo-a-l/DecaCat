using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    private const string GameScene = "Game";

    private static readonly Color BackgroundColor = new Color(0.05f, 0.05f, 0.07f, 1f);
    private static readonly Vector2 ButtonSize = new Vector2(560f, 100f);

    private GameObject current;

    private void Start()
    {
        Time.timeScale = 1f;

        ShowMain();
    }

    private void Show(GameObject screen, bool allowBack = true)
    {
        if (current != null)
            Destroy(current);

        current = screen;

        if (allowBack)
            BackAction.Attach(screen, ShowMain);
    }

    private void ShowMain()
    {
        GameObject root = UIFactory.CreateOverlayCanvas("MainMenu", 50);

        UIFactory.CreatePanel(root.transform, "Background", BackgroundColor);

        UIFactory.CreateText(
            root.transform, "Title", "DECACAT", 170f, Color.white,
            new Vector2(0f, 290f), new Vector2(1400f, 220f)
        );

        AddButton(root, "PlayButton", "Play", 70f, ShowSlots);
        AddButton(root, "AchievementsButton", "Achievements", -40f, ShowAchievements);
        AddButton(root, "IndexButton", "Index", -150f, ShowIndex);
        AddButton(root, "SettingsButton", "Settings", -260f, ShowSettings);

        if (!Application.isMobilePlatform)
            AddButton(root, "QuitButton", "Quit", -370f, Quit);

        MenuCat.Create(root.transform);

        Show(root, false);
    }

    private void ShowSlots()
    {
        Show(SlotSelectScreen.Create(StartSlot, ShowMain));
    }

    private void ShowSettings()
    {
        Show(SettingsScreen.Create(ShowMain));
    }

    private void ShowIndex()
    {
        Show(IndexScreen.Create(GetDefaultAchievementSlot(), ShowMain));
    }

    private void ShowAchievements()
    {
        Show(AchievementsScreen.Create(GetDefaultAchievementSlot(), ShowMain));
    }

    private void StartSlot(int slot, bool isNewGame)
    {
        if (isNewGame)
            SaveSession.StartNewGame(slot);
        else if (!SaveSession.Continue(slot))
            return;

        SceneManager.LoadScene(GameScene);
    }

    private static int GetDefaultAchievementSlot()
    {
        for (int slot = 0; slot < SaveSlots.SlotCount; slot++)
        {
            if (SaveSlots.Exists(slot))
                return slot;
        }

        return 0;
    }

    private static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void AddButton(
        GameObject root, string objectName, string label, float y,
        System.Action onClick)
    {
        UIFactory.CreateButton(
            root.transform, objectName, label,
            new Vector2(0f, y), ButtonSize, onClick, 54f
        );
    }
}
