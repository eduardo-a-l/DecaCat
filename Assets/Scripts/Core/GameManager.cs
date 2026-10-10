using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Playing,
    Paused,
    Map,
    GameOver
}

public class GameManager : MonoBehaviour
{
    private const string MainMenuScene = "MainMenu";

    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private RoomManager roomManager;

    private PlayerItem playerItem;
    private GameObject overlay;
    private bool tookDamageThisFloor;

    public GameState State { get; private set; } = GameState.Playing;
    public int CurrentFloor { get; private set; } = 1;

    private bool CanOpenOverlay =>
        State == GameState.Playing &&
        (roomManager == null || !roomManager.IsTransitioning);

    private void Awake()
    {
        Time.timeScale = 1f;

        SaveSession.EnsureActive();

        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (inputReader == null)
            inputReader = FindFirstObjectByType<PlayerInputReader>();

        if (roomManager == null)
            roomManager = FindFirstObjectByType<RoomManager>();

        playerItem = FindFirstObjectByType<PlayerItem>();

        CurrentFloor = Mathf.Max(1, SaveSession.Data.floor);

        ApplySavedLoadout();

        if (playerHealth != null)
        {
            playerHealth.Died += OnPlayerDied;
            playerHealth.Damaged += OnPlayerDamaged;
        }

        if (roomManager != null)
            roomManager.FloorGenerated += OnFloorGenerated;
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.Died -= OnPlayerDied;
            playerHealth.Damaged -= OnPlayerDamaged;
        }

        if (roomManager != null)
            roomManager.FloorGenerated -= OnFloorGenerated;

        Time.timeScale = 1f;
    }

    private void OnApplicationQuit()
    {
        SaveSession.Save();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            SaveSession.Save();
    }

    private void Update()
    {
        if (State == GameState.Playing && SaveSession.IsActive)
            SaveSession.Data.playTimeSeconds += Time.unscaledDeltaTime;

        if (inputReader == null)
            return;

        if (State == GameState.GameOver)
        {
            if (inputReader.RestartPressed)
                Restart();

            return;
        }

        if (inputReader.PausePressed)
            TogglePause();
        else if (inputReader.MapPressed)
            ToggleMap();
    }

    public void TogglePause()
    {
        if (State == GameState.Playing)
            OpenPause();
        else if (State == GameState.Paused || State == GameState.Map)
            Resume();
    }

    public void ToggleMap()
    {
        if (State == GameState.Playing)
            OpenMap();
        else if (State == GameState.Map)
            Resume();
    }

    public void NextFloor()
    {
        GameStats.Add(StatType.FloorsCleared);

        if (!tookDamageThisFloor)
            GameStats.Add(StatType.NoDamageFloors);

        CurrentFloor++;

        if (SaveSession.IsActive)
        {
            SaveSession.Data.floor = CurrentFloor;
            SaveSession.Data.floorSeed = 0;
        }
    }

    public void Resume()
    {
        if (State != GameState.Paused && State != GameState.Map)
            return;

        SetOverlay(null);
        State = GameState.Playing;

        SetGameplayActive(true);
    }

    public void Restart()
    {
        SaveSession.Save();

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitToMenu()
    {
        SaveSession.Save();

        Time.timeScale = 1f;
        SceneManager.LoadScene(MainMenuScene);
    }

    private void ApplySavedLoadout()
    {
        if (playerItem == null || string.IsNullOrEmpty(SaveSession.Data.loadoutItem))
            return;

        ItemData item = ItemDatabase.FindItem(SaveSession.Data.loadoutItem);

        if (item != null)
            playerItem.ApplyLoadout(item, SaveSession.Data.loadoutDurability);
    }

    private void OnFloorGenerated(int floor, int seed)
    {
        SaveData data = SaveSession.Data;

        data.floor = floor;
        data.floorSeed = seed;

        if (playerHealth != null)
            playerHealth.RestoreFullHealth();

        tookDamageThisFloor = false;

        if (playerItem != null)
        {
            ItemData item = playerItem.CurrentItem;

            data.loadoutItem = item != null ? item.ItemName : string.Empty;
            data.loadoutDurability = playerItem.CurrentDurability;
        }

        GameStats.SetMax(StatType.HighestFloor, floor);

        SaveSession.Save();
    }

    private void OnPlayerDamaged()
    {
        tookDamageThisFloor = true;
    }

    private void OpenPause()
    {
        if (!CanOpenOverlay)
            return;

        State = GameState.Paused;
        SetGameplayActive(false);

        SaveSession.Save();

        ShowPauseMenu();
    }

    private void OpenMap()
    {
        if (!CanOpenOverlay)
            return;

        State = GameState.Map;
        SetGameplayActive(false);

        SetOverlay(MapOverlay.Create(roomManager, Resume));
    }

    private void ShowPauseMenu()
    {
        SetOverlay(
            PauseMenu.Create(
                Resume, ShowAchievements, ShowIndex, ShowSettings, Restart,
                QuitToMenu
            )
        );
    }

    private void ShowSettings()
    {
        SetOverlay(SettingsScreen.Create(ShowPauseMenu));
    }

    private void ShowIndex()
    {
        SetOverlay(IndexScreen.Create(SaveSession.ActiveSlot, ShowPauseMenu));
    }

    private void ShowAchievements()
    {
        SetOverlay(
            AchievementsScreen.Create(SaveSession.ActiveSlot, ShowPauseMenu)
        );
    }

    private void SetOverlay(GameObject newOverlay)
    {
        if (overlay != null)
            Destroy(overlay);

        overlay = newOverlay;
    }

    private void SetGameplayActive(bool active)
    {
        Time.timeScale = active ? 1f : 0f;

        if (inputReader != null)
            inputReader.InputEnabled = active;
    }

    private void OnPlayerDied()
    {
        if (State == GameState.GameOver)
            return;

        State = GameState.GameOver;

        if (inputReader != null)
            inputReader.InputEnabled = false;

        GameStats.Add(StatType.Deaths);
        SaveSession.Save();

        GameOverScreen.Create(Restart, QuitToMenu);

        Time.timeScale = 0f;
    }
}
