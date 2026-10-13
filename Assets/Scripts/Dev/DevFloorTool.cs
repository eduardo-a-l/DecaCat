using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class DevFloorTool : MonoBehaviour
{
    public const bool Enabled = true;

    private const float WindowWidth = 320f;
    private const float WindowHeight = 452f;
    private const float Margin = 12f;
    private const float RowHeight = 28f;
    private const float RowStep = 33f;

    private static readonly float[] Speeds = { 0.25f, 0.5f, 1f, 2f, 4f };

    private GameManager gameManager;
    private PlayerInputReader inputReader;
    private bool open;
    private bool inputWasEnabled;
    private bool godMode;
    private float gameSpeed = 1f;
    private int targetFloor = 1;
    private string floorText = "1";
    private int itemIndex;
    private int powerIndex;
    private List<ItemData> items = new List<ItemData>();
    private PowerType[] powers;
    private string status = string.Empty;

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

        if (godMode)
        {
            PlayerHealth health = FindFirstObjectByType<PlayerHealth>();

            if (health != null)
                health.GrantInvulnerability(0.5f);
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

        items = ItemDatabase.All;
        powers = (PowerType[])Enum.GetValues(typeof(PowerType));
        itemIndex = Mathf.Clamp(itemIndex, 0, Mathf.Max(0, items.Count - 1));
        powerIndex = Mathf.Clamp(powerIndex, 0, Mathf.Max(0, powers.Length - 1));
        status = string.Empty;

        Time.timeScale = 0f;

        SetTarget(SaveSession.Data.floor);
    }

    private void Close()
    {
        open = false;

        Time.timeScale = gameSpeed;

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
        Time.timeScale = gameSpeed;

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void KillAllEnemies()
    {
        int killed = 0;

        foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            if (enemy == null || enemy.CurrentHealth <= 0)
                continue;

            enemy.TakeDamage(int.MaxValue);
            killed++;
        }

        status = "Killed " + killed + " enemies";
    }

    private void HealPlayer()
    {
        PlayerHealth health = FindFirstObjectByType<PlayerHealth>();

        if (health == null)
            return;

        health.RestoreFullHealth();
        status = "Healed";
    }

    private void GiveItem()
    {
        PlayerItem playerItem = FindFirstObjectByType<PlayerItem>();

        if (playerItem == null || items.Count == 0)
            return;

        ItemData item = items[itemIndex];

        playerItem.ApplyLoadout(item, item.MaxDurability);
        status = "Equipped " + item.ItemName;
    }

    private void GivePower()
    {
        if (PlayerPower.Instance == null || powers == null || powers.Length == 0)
            return;

        PlayerPower.Instance.Acquire(powers[powerIndex]);
        status = "Power " + powers[powerIndex];
    }

    private void ChangeHearts(int delta)
    {
        SaveData data = SaveSession.Data;

        data.bonusHearts = Mathf.Max(0, data.bonusHearts + delta);

        PlayerHealth health = FindFirstObjectByType<PlayerHealth>();

        if (health != null)
            health.SetBonusHealth(data.bonusHearts);

        SaveSession.Save();
    }

    private void ChangeDamage(int delta)
    {
        SaveData data = SaveSession.Data;

        data.damageUpgrades = Mathf.Max(0, data.damageUpgrades + delta);

        SaveSession.Save();
    }

    private void SetSpeed(float speed)
    {
        gameSpeed = speed;
    }

    private static int Cycle(int index, int delta, int count)
    {
        if (count <= 0)
            return 0;

        return (index + delta % count + count) % count;
    }

    private void OnGUI()
    {
        if (!InGame)
            return;

        float scale = Mathf.Clamp(Screen.height / 500f, 0.6f, 3f);

        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

        if (!open)
        {
            if (GUI.Button(new Rect(8f, 8f, 52f, 26f), "DEV"))
                Open();

            return;
        }

        GUI.Box(new Rect(8f, 8f, WindowWidth, WindowHeight), "DEV TOOLS (F9)");

        float x = 8f + Margin;
        float width = WindowWidth - Margin * 2f;
        float y = 36f;

        GUI.Label(
            new Rect(x, y, width, 22f),
            "Slot " + (SaveSession.ActiveSlot + 1) + ", saved floor " + SaveSession.Data.floor
        );

        y += 24f;

        if (GUI.Button(new Rect(x, y, 48f, RowHeight), "-10"))
            SetTarget(targetFloor - 10);

        if (GUI.Button(new Rect(x + 52f, y, 40f, RowHeight), "-1"))
            SetTarget(targetFloor - 1);

        string typed = GUI.TextField(new Rect(x + 98f, y, 70f, RowHeight), floorText, 4);

        if (typed != floorText)
        {
            floorText = typed;

            if (int.TryParse(typed, out int parsed))
                targetFloor = Mathf.Max(1, parsed);
        }

        if (GUI.Button(new Rect(x + 174f, y, 40f, RowHeight), "+1"))
            SetTarget(targetFloor + 1);

        if (GUI.Button(new Rect(x + 218f, y, 60f, RowHeight), "+10"))
            SetTarget(targetFloor + 10);

        y += RowStep;

        if (GUI.Button(new Rect(x, y, width, RowHeight), "Go to floor " + targetFloor))
            GoToFloor();

        y += RowStep + 4f;

        float half = (width - 6f) * 0.5f;

        if (GUI.Button(new Rect(x, y, half, RowHeight), "Heal full"))
            HealPlayer();

        if (GUI.Button(new Rect(x + half + 6f, y, half, RowHeight), "Kill all enemies"))
            KillAllEnemies();

        y += RowStep;

        godMode = GUI.Toggle(new Rect(x, y, width, RowHeight), godMode, " God mode (no damage)");

        y += RowStep;

        GUI.Label(new Rect(x, y + 4f, 60f, 22f), "Speed");

        for (int i = 0; i < Speeds.Length; i++)
        {
            string label = Speeds[i] + "x";

            if (Mathf.Approximately(gameSpeed, Speeds[i]))
                label = "[" + label + "]";

            if (GUI.Button(new Rect(x + 56f + i * 50f, y, 48f, RowHeight), label))
                SetSpeed(Speeds[i]);
        }

        y += RowStep + 4f;

        string itemName = items.Count > 0 ? items[itemIndex].ItemName : "none";

        if (GUI.Button(new Rect(x, y, 34f, RowHeight), "<"))
            itemIndex = Cycle(itemIndex, -1, items.Count);

        GUI.Label(new Rect(x + 40f, y + 4f, width - 160f, 22f), itemName);

        if (GUI.Button(new Rect(x + width - 118f, y, 34f, RowHeight), ">"))
            itemIndex = Cycle(itemIndex, 1, items.Count);

        if (GUI.Button(new Rect(x + width - 78f, y, 78f, RowHeight), "Equip"))
            GiveItem();

        y += RowStep;

        string powerName = powers != null && powers.Length > 0 ? powers[powerIndex].ToString() : "none";

        if (GUI.Button(new Rect(x, y, 34f, RowHeight), "<"))
            powerIndex = Cycle(powerIndex, -1, powers != null ? powers.Length : 0);

        GUI.Label(new Rect(x + 40f, y + 4f, width - 160f, 22f), powerName);

        if (GUI.Button(new Rect(x + width - 118f, y, 34f, RowHeight), ">"))
            powerIndex = Cycle(powerIndex, 1, powers != null ? powers.Length : 0);

        if (GUI.Button(new Rect(x + width - 78f, y, 78f, RowHeight), "Power"))
            GivePower();

        y += RowStep + 4f;

        GUI.Label(new Rect(x, y + 4f, 150f, 22f), "Bonus hearts: " + PermanentUpgrades.BonusHearts);

        if (GUI.Button(new Rect(x + width - 80f, y, 38f, RowHeight), "-"))
            ChangeHearts(-1);

        if (GUI.Button(new Rect(x + width - 38f, y, 38f, RowHeight), "+"))
            ChangeHearts(1);

        y += RowStep;

        GUI.Label(new Rect(x, y + 4f, 170f, 22f), "Damage stacks: " + PermanentUpgrades.DamageStacks);

        if (GUI.Button(new Rect(x + width - 80f, y, 38f, RowHeight), "-"))
            ChangeDamage(-1);

        if (GUI.Button(new Rect(x + width - 38f, y, 38f, RowHeight), "+"))
            ChangeDamage(1);

        y += RowStep;

        if (GUI.Button(new Rect(x, y, width, RowHeight), "Reset permanent upgrades"))
        {
            PermanentUpgrades.ResetAll();
            status = "Upgrades reset";
        }

        y += RowStep;

        GUI.Label(new Rect(x, y, width, 22f), status);

        y += 26f;

        if (GUI.Button(new Rect(x, y, width, RowHeight), "Close"))
            Close();
    }
}
