using System;
using System.IO;
using UnityEngine;

[Serializable]
public class SettingsData
{
    public float masterVolume = 1f;
    public float musicVolume = 1f;
    public float sfxVolume = 1f;
    public bool fullscreen = true;
    public bool showDamageNumbers = true;
}

public static class GameSettings
{
    private static SettingsData data;

    public static float MasterVolume
    {
        get { return Data.masterVolume; }
        set { Data.masterVolume = Mathf.Clamp01(value); }
    }

    public static float MusicVolume
    {
        get { return Data.musicVolume; }
        set { Data.musicVolume = Mathf.Clamp01(value); }
    }

    public static float SfxVolume
    {
        get { return Data.sfxVolume; }
        set { Data.sfxVolume = Mathf.Clamp01(value); }
    }

    public static bool Fullscreen
    {
        get { return Data.fullscreen; }
        set { Data.fullscreen = value; }
    }

    public static bool ShowDamageNumbers
    {
        get { return Data.showDamageNumbers; }
        set { Data.showDamageNumbers = value; }
    }

    private static SettingsData Data
    {
        get
        {
            if (data == null)
                Load();

            return data;
        }
    }

    private static string FilePath
    {
        get { return Path.Combine(Application.persistentDataPath, "settings.json"); }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        Load();
        Apply();

        Application.quitting -= Save;
        Application.quitting += Save;
    }

    public static void Apply()
    {
        AudioListener.volume = Data.masterVolume;

        if (Application.isMobilePlatform)
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            return;
        }

        FullScreenMode mode = Data.fullscreen
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;

        if (Screen.fullScreenMode != mode)
            Screen.fullScreenMode = mode;
    }

    public static void Save()
    {
        if (data == null)
            return;

        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(data));
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Could not save settings: " + exception.Message);
        }
    }

    private static void Load()
    {
        data = null;

        try
        {
            if (File.Exists(FilePath))
                data = JsonUtility.FromJson<SettingsData>(File.ReadAllText(FilePath));
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Could not read settings: " + exception.Message);
        }

        if (data != null)
            return;

        data = new SettingsData();
        data.fullscreen = Screen.fullScreenMode != FullScreenMode.Windowed;
    }
}
