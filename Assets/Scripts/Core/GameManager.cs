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
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerInputReader inputReader;
    [SerializeField] private RoomManager roomManager;

    private GameObject overlay;

    public GameState State { get; private set; } = GameState.Playing;
    public int CurrentFloor { get; private set; } = 1;

    private bool CanOpenOverlay =>
        State == GameState.Playing &&
        (roomManager == null || !roomManager.IsTransitioning);

    private void Awake()
    {
        Time.timeScale = 1f;
    }

    private void Start()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (inputReader == null)
            inputReader = FindFirstObjectByType<PlayerInputReader>();

        if (roomManager == null)
            roomManager = FindFirstObjectByType<RoomManager>();

        if (playerHealth != null)
            playerHealth.Died += OnPlayerDied;
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.Died -= OnPlayerDied;

        Time.timeScale = 1f;
    }

    private void Update()
    {
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
        CurrentFloor++;
    }

    public void Resume()
    {
        if (State != GameState.Paused && State != GameState.Map)
            return;

        if (overlay != null)
            Destroy(overlay);

        overlay = null;
        State = GameState.Playing;

        SetGameplayActive(true);
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OpenPause()
    {
        if (!CanOpenOverlay)
            return;

        State = GameState.Paused;
        SetGameplayActive(false);

        overlay = PauseMenu.Create(Resume, Restart);
    }

    private void OpenMap()
    {
        if (!CanOpenOverlay)
            return;

        State = GameState.Map;
        SetGameplayActive(false);

        overlay = MapOverlay.Create(roomManager, Resume);
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

        GameOverScreen.Create(Restart);

        Time.timeScale = 0f;
    }
}
