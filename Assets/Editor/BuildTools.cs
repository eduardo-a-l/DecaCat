using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildTools
{
    private const string BuildFolder = "Builds";
    private const string IconFolder = "Assets/Art/Icon";

    private static readonly int[] IconSizes =
        { 16, 32, 36, 48, 64, 72, 96, 128, 144, 192, 256, 512, 1024 };

    [MenuItem("DecaCat/Build/Windows")]
    public static void BuildWindows()
    {
        Exit(BuildWindowsPlayer());
    }

    [MenuItem("DecaCat/Build/Android APK (install on a phone)")]
    public static void BuildAndroidApk()
    {
        Exit(BuildAndroidPlayer(false));
    }

    [MenuItem("DecaCat/Build/Android App Bundle (Google Play)")]
    public static void BuildAndroidBundle()
    {
        Exit(BuildAndroidPlayer(true));
    }

    [MenuItem("DecaCat/Build/Windows + Android APK")]
    public static void BuildAll()
    {
        bool windows = BuildWindowsPlayer();
        bool android = BuildAndroidPlayer(false);

        Exit(windows && android);
    }

    [MenuItem("DecaCat/Build/Open Builds Folder")]
    public static void OpenBuildsFolder()
    {
        string folder = GetBuildRoot();

        Directory.CreateDirectory(folder);
        EditorUtility.RevealInFinder(folder);
    }

    [MenuItem("DecaCat/Apply App Icon")]
    public static void ApplyAppIcon()
    {
        ApplyIcons(NamedBuildTarget.Standalone);
        ApplyIcons(NamedBuildTarget.Android);

        AssetDatabase.SaveAssets();
    }

    private static bool BuildWindowsPlayer()
    {
        string folder = Path.Combine(GetBuildRoot(), "Windows");

        if (Directory.Exists(folder))
            Directory.Delete(folder, true);

        Directory.CreateDirectory(folder);

        string exe = Path.Combine(folder, PlayerSettings.productName + ".exe");

        if (!Build(BuildTarget.StandaloneWindows64, exe))
            return false;

        foreach (string directory in Directory.GetDirectories(folder))
        {
            string name = Path.GetFileName(directory);

            if (name.EndsWith("_DoNotShip") ||
                name.EndsWith("_ButDontShipItWithYourGame"))
                Directory.Delete(directory, true);
        }

        string zip = Path.Combine(
            GetBuildRoot(),
            PlayerSettings.productName + "-Windows-v" +
            PlayerSettings.bundleVersion + ".zip"
        );

        if (File.Exists(zip))
            File.Delete(zip);

        ZipFile.CreateFromDirectory(folder, zip);

        Debug.Log("Windows zip ready: " + zip);

        if (!Application.isBatchMode)
            EditorUtility.RevealInFinder(zip);

        return true;
    }

    private static bool BuildAndroidPlayer(bool appBundle)
    {
        string folder = Path.Combine(GetBuildRoot(), "Android");

        Directory.CreateDirectory(folder);

        string file = Path.Combine(
            folder,
            PlayerSettings.productName + "-v" + PlayerSettings.bundleVersion +
            (appBundle ? ".aab" : ".apk")
        );

        bool previousBundle = EditorUserBuildSettings.buildAppBundle;
        bool previousCustom = PlayerSettings.Android.useCustomKeystore;
        string previousStore = PlayerSettings.Android.keystoreName;
        string previousStorePass = PlayerSettings.Android.keystorePass;
        string previousAlias = PlayerSettings.Android.keyaliasName;
        string previousAliasPass = PlayerSettings.Android.keyaliasPass;

        bool environmentKeystore = ConfigureAndroid();

        EditorUserBuildSettings.buildAppBundle = appBundle;

        if (appBundle && !PlayerSettings.Android.useCustomKeystore)
        {
            Debug.LogWarning(
                "No release keystore set, signing with the debug key. " +
                "Google Play will reject this. Set DECACAT_KEYSTORE, " +
                "DECACAT_KEYSTORE_PASS, DECACAT_KEY_ALIAS and DECACAT_KEY_PASS."
            );
        }

        try
        {
            bool success = Build(BuildTarget.Android, file);

            if (success && !Application.isBatchMode)
                EditorUtility.RevealInFinder(file);

            return success;
        }
        finally
        {
            EditorUserBuildSettings.buildAppBundle = previousBundle;

            if (environmentKeystore)
            {
                PlayerSettings.Android.useCustomKeystore = previousCustom;
                PlayerSettings.Android.keystoreName = previousStore;
                PlayerSettings.Android.keystorePass = previousStorePass;
                PlayerSettings.Android.keyaliasName = previousAlias;
                PlayerSettings.Android.keyaliasPass = previousAliasPass;
            }
        }
    }

    private static bool ConfigureAndroid()
    {
        PlayerSettings.SetScriptingBackend(
            NamedBuildTarget.Android, ScriptingImplementation.IL2CPP
        );

        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        string keystore = Environment.GetEnvironmentVariable("DECACAT_KEYSTORE");

        if (string.IsNullOrEmpty(keystore))
            return false;

        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = keystore;
        PlayerSettings.Android.keystorePass =
            Environment.GetEnvironmentVariable("DECACAT_KEYSTORE_PASS");
        PlayerSettings.Android.keyaliasName =
            Environment.GetEnvironmentVariable("DECACAT_KEY_ALIAS");
        PlayerSettings.Android.keyaliasPass =
            Environment.GetEnvironmentVariable("DECACAT_KEY_PASS");

        return true;
    }

    private static bool Build(BuildTarget target, string outputPath)
    {
        BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);

        if (!BuildPipeline.IsBuildTargetSupported(group, target))
        {
            Debug.LogError(
                target + " build support is not installed. " +
                "Add the module in Unity Hub (Installs > Add modules)."
            );

            return false;
        }

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("No enabled scenes in Build Settings.");
            return false;
        }

        ApplyAppIcon();

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = target,
            options = BuildOptions.None
        };

        BuildSummary summary = BuildPipeline.BuildPlayer(options).summary;

        if (summary.result != BuildResult.Succeeded)
        {
            Debug.LogError(
                "Build failed: " + summary.result +
                " (" + summary.totalErrors + " errors)"
            );

            return false;
        }

        Debug.Log(
            "Build succeeded: " + outputPath + " (" +
            (summary.totalSize / (1024f * 1024f)).ToString("0.0") + " MB)"
        );

        return true;
    }

    private static void ApplyIcons(NamedBuildTarget target)
    {
        int[] sizes = PlayerSettings.GetIconSizes(target, IconKind.Application);

        if (sizes == null || sizes.Length == 0)
            return;

        Texture2D[] icons = new Texture2D[sizes.Length];

        for (int i = 0; i < sizes.Length; i++)
            icons[i] = LoadIcon(sizes[i]);

        if (icons.Any(icon => icon == null))
        {
            Debug.LogWarning("App icon files are missing in " + IconFolder);
            return;
        }

        PlayerSettings.SetIcons(target, icons, IconKind.Application);
    }

    private static Texture2D LoadIcon(int size)
    {
        int best = IconSizes[IconSizes.Length - 1];

        foreach (int candidate in IconSizes)
        {
            if (candidate >= size)
            {
                best = candidate;
                break;
            }
        }

        string path = IconFolder + "/AppIcon_" + best + ".png";

        if (!File.Exists(path))
            return null;

        PrepareIconImport(path);

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static void PrepareIconImport(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
            return;

        bool correct =
            importer.textureType == TextureImporterType.Default &&
            !importer.mipmapEnabled &&
            importer.textureCompression == TextureImporterCompression.Uncompressed &&
            importer.npotScale == TextureImporterNPOTScale.None;

        if (correct)
            return;

        importer.textureType = TextureImporterType.Default;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }

    private static string GetBuildRoot()
    {
        string project = Directory.GetParent(Application.dataPath).FullName;

        return Path.Combine(project, BuildFolder);
    }

    private static void Exit(bool success)
    {
        if (Application.isBatchMode)
            EditorApplication.Exit(success ? 0 : 1);
    }
}
