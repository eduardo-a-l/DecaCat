using System;
using System.IO;
using System.Text;
using UnityEngine;

public static class SaveSlots
{
    public const int SlotCount = 3;

    private const string CodePrefix = "DECACAT1";

    public static string ExportFolder
    {
        get { return Path.Combine(Application.persistentDataPath, "Exports"); }
    }

    public static string GetPath(int slot)
    {
        return Path.Combine(
            Application.persistentDataPath,
            "save_slot_" + (slot + 1) + ".json"
        );
    }

    public static bool Exists(int slot)
    {
        return File.Exists(GetPath(slot));
    }

    public static SaveData Load(int slot)
    {
        string path = GetPath(slot);

        if (!File.Exists(path))
            return null;

        try
        {
            return Sanitize(JsonUtility.FromJson<SaveData>(File.ReadAllText(path)));
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "Could not read save slot " + (slot + 1) + ": " + exception.Message
            );

            return null;
        }
    }

    public static void Save(int slot, SaveData data)
    {
        if (data == null)
            return;

        data.lastSaved = DateTime.UtcNow.ToString("o");

        string path = GetPath(slot);
        string temporary = path + ".tmp";

        try
        {
            File.WriteAllText(temporary, JsonUtility.ToJson(data));
            File.Copy(temporary, path, true);
            File.Delete(temporary);
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Could not write save slot " + (slot + 1) + ": " + exception.Message
            );
        }
    }

    public static void Delete(int slot)
    {
        try
        {
            if (File.Exists(GetPath(slot)))
                File.Delete(GetPath(slot));
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Could not delete save slot " + (slot + 1) + ": " + exception.Message
            );
        }
    }

    public static bool Export(int slot, out string message)
    {
        SaveData data = Load(slot);

        if (data == null)
        {
            message = "Slot " + (slot + 1) + " is empty, nothing to export";
            return false;
        }

        string code = CreateExportCode(data);

        GUIUtility.systemCopyBuffer = code;

        string path = WriteExportFile(slot, code);

        message = "Slot " + (slot + 1) + " exported: copied to the clipboard";

        if (path.Length > 0)
            message += " and saved to " + path;

        return true;
    }

    public static string CreateExportCode(SaveData data)
    {
        string json = JsonUtility.ToJson(data);
        string payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

        return CodePrefix + "-" + payload + "-" + Checksum(json);
    }

    public static bool TryParseExportCode(
        string text, out SaveData data, out string error)
    {
        data = null;
        error = string.Empty;

        string code = RemoveWhitespace(text);

        if (code.Length == 0)
        {
            error = "Nothing to import: the clipboard is empty";
            return false;
        }

        string[] parts = code.Split('-');

        if (parts.Length != 3 || parts[0] != CodePrefix)
        {
            error = "The clipboard doesn't contain a DecaCat save code";
            return false;
        }

        string json;

        try
        {
            json = Encoding.UTF8.GetString(Convert.FromBase64String(parts[1]));
        }
        catch (Exception)
        {
            error = "The save code is damaged";
            return false;
        }

        if (Checksum(json) != parts[2])
        {
            error = "The save code is damaged or was edited";
            return false;
        }

        SaveData parsed;

        try
        {
            parsed = Sanitize(JsonUtility.FromJson<SaveData>(json));
        }
        catch (Exception)
        {
            error = "The save code could not be read";
            return false;
        }

        if (parsed == null)
        {
            error = "The save code could not be read";
            return false;
        }

        if (parsed.version > SaveData.CurrentVersion)
        {
            error = "This save comes from a newer version of the game";
            return false;
        }

        data = parsed;
        return true;
    }

    private static string WriteExportFile(int slot, string code)
    {
        try
        {
            Directory.CreateDirectory(ExportFolder);

            string path = Path.Combine(
                ExportFolder, "DecaCat_Slot" + (slot + 1) + "_Export.txt"
            );

            File.WriteAllText(path, code);

            return path;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Could not write the export file: " + exception.Message);
            return string.Empty;
        }
    }

    private static SaveData Sanitize(SaveData data)
    {
        if (data == null)
            return null;

        data.floor = Mathf.Max(1, data.floor);
        data.floorSeed = Mathf.Max(0, data.floorSeed);
        data.loadoutDurability = Mathf.Max(0, data.loadoutDurability);
        data.playTimeSeconds = Mathf.Max(0f, data.playTimeSeconds);

        if (data.loadoutItem == null)
            data.loadoutItem = string.Empty;

        if (data.stats == null)
            data.stats = new System.Collections.Generic.List<StatEntry>();

        if (data.usedWeapons == null)
            data.usedWeapons = new System.Collections.Generic.List<string>();

        if (data.unlockedAchievements == null)
            data.unlockedAchievements = new System.Collections.Generic.List<string>();

        return data;
    }

    private static string RemoveWhitespace(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        StringBuilder builder = new StringBuilder(text.Length);

        foreach (char character in text)
        {
            if (!char.IsWhiteSpace(character))
                builder.Append(character);
        }

        return builder.ToString();
    }

    private static string Checksum(string text)
    {
        uint hash = 2166136261;

        unchecked
        {
            foreach (byte value in Encoding.UTF8.GetBytes(text))
            {
                hash ^= value;
                hash *= 16777619;
            }
        }

        return hash.ToString("x8");
    }
}
