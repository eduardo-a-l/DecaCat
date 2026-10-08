using UnityEngine;

public static class SaveSession
{
    public static int ActiveSlot { get; private set; } = -1;
    public static SaveData Data { get; private set; }

    public static bool IsActive => Data != null && ActiveSlot >= 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnLoad()
    {
        ActiveSlot = -1;
        Data = null;
    }

    public static void StartNewGame(int slot)
    {
        ActiveSlot = slot;
        Data = new SaveData();

        Save();
    }

    public static bool Continue(int slot)
    {
        SaveData loaded = SaveSlots.Load(slot);

        if (loaded == null)
            return false;

        ActiveSlot = slot;
        Data = loaded;

        return true;
    }

    public static void EnsureActive()
    {
        if (IsActive)
            return;

        if (!Continue(0))
            StartNewGame(0);

        Debug.Log("No save slot was selected, using slot 1");
    }

    public static void Save()
    {
        if (IsActive)
            SaveSlots.Save(ActiveSlot, Data);
    }
}
