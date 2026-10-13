using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class DevFloorTool : MonoBehaviour
{
    public const bool Enabled = true;

    private const float WindowWidth = 300f;
    private const float WindowHeight = 250f;

    private GameManager gameManager;
    private PlayerInputReader inputReader;
    private bool open;
    private bool inputWasEnabled;
    private float previousTimeScale = 1f;
    private int targetFloor = 1;
    private string floorText = "1";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (!Enabled || FindFirstObjectByType<DevFloorTool>() != null)
            return;

        GameObject host = new GameObject("DevFloorTool");
        host.AddComponent<DevFloorTool>();

        DontDestroyOnLoad(host);
    }

    private bool InGame
    {
        get
        {
            if (gameManager == null)
                gameManager = FindFirstObjectByType<GameManager>();

            return gameManager != null && SaveSession.IsActive;
        }
    }

    private void Update()
    {
        if (!InGame)
        {
            open = false;
            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null && keyboard.f9Key.wasPressedThisFrame)
        {
            if (open)
                Close();
            else
                Open();
        }
    }

    private void Open()
    {
        open = true;

        inputReader = FindFirstObjectByType<PlayerInputReader>();

        if (inputReader != null)
        {
            inputWasEnabled = inputReader.InputEnabled;
            inputReader.InputEnabled = false;
        }

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        SetTarget(SaveSession.Data.floor);
    }

    private void Close()
    {
        open = false;

        Time.timeScale = previousTimeScale;

        if (inputReader != null)
            inputReader.InputEnabled = inputWasEnabled;
    }

    private void SetTarget(int floor)
    {
        targetFloor = Mathf.Max(1, floor);
        floorText = targetFloor.ToString();
    }

    private void GoToFloor()
    {
        SaveData data = SaveSession.Data;

        data.floor = targetFloor;
        data.floorSeed = 0;
        data.takenAltarRooms.Clear();

        SaveSession.Save();

        open = false;
        Time.timeScale = 1f;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnGUI()
    {
        if (!InGame)
            return;

        float scale = Mathf.Clamp(Screen.height / 540f, 1f, 3f);

        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

        if (!open)
        {
            if (GUI.Button(new Rect(8f, 8f, 52f, 26f), "DEV"))
                Open();

            return;
        }

        GUI.Box(new Rect(8f, 8f, WindowWidth, WindowHeight), "DEV: save slot floor");

        GUI.Label(
            new Rect(20f, 36f, WindowWidth - 24f, 22f),
            "Slot " + (SaveSession.ActiveSlot + 1) + ", saved floor " + SaveSession.Data.floor
        );

        if (GUI.Button(new Rect(20f, 64f, 48f, 28f), "-10"))
            SetTarget(targetFloor - 10);

        if (GUI.Button(new Rect(72f, 64f, 40f, 28f), "-1"))
            SetTarget(targetFloor - 1);

        string typed = GUI.TextField(new Rect(118f, 64f, 70f, 28f), floorText, 4);

        if (typed != floorText)
        {
            floorText = typed;

            if (int.TryParse(typed, out int parsed))
                targetFloor = Mathf.Max(1, parsed);
        }

        if (GUI.Button(new Rect(194f, 64f, 40f, 28f), "+1"))
            SetTarget(targetFloor + 1);

        if (GUI.Button(new Rect(238f, 64f, 60f, 28f), "+10"))
            SetTarget(targetFloor + 10);

        if (GUI.Button(new Rect(20f, 102f, WindowWidth - 24f, 32f), "Go to floor " + targetFloor))
            GoToFloor();

        if (GUI.Button(new Rect(20f, 142f, WindowWidth - 24f, 28f), "Reset permanent upgrades"))
            PermanentUpgrades.ResetAll();

        string upgradeInfo =
            "Hearts +" + PermanentUpgrades.BonusHearts +
            "  Damage stacks " + PermanentUpgrades.DamageStacks;

        GUI.Label(new Rect(20f, 176f, WindowWidth - 24f, 22f), upgradeInfo);

        if (GUI.Button(new Rect(20f, 206f, WindowWidth - 24f, 28f), "Close (F9)"))
            Close();
    }
}
