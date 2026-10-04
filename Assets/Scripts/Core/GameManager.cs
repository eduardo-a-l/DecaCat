using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Playing,
    GameOver
}

public class GameManager : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerInputReader inputReader;

    public GameState State { get; private set; } = GameState.Playing;

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
        if (State == GameState.GameOver &&
            inputReader != null &&
            inputReader.RestartPressed)
        {
            Restart();
        }
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
