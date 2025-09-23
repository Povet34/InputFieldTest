#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Reflection;

public class EnvBuildManager : EditorWindow
{
    public enum Env { PROD = 0, TEST = 1, DEV = 2 }

    // 각 환경별 키스토어 기본값
    private const string DEV_KEYSTORE_PATH = "Assets/Keystores/Dev.keystore";
    private const string DEV_KEYSTORE_PASSWORD = "chipy12";
    private const string DEV_KEY_ALIAS = "chipy12";
    private const string DEV_KEY_PASSWORD = "chipy12";

    private const string TEST_KEYSTORE_PATH = "Assets/Keystores/Test.keystore";
    private const string TEST_KEYSTORE_PASSWORD = "chipy12";
    private const string TEST_KEY_ALIAS = "chipy12";
    private const string TEST_KEY_PASSWORD = "chipy12";

    private const string PROD_KEYSTORE_PATH = "Assets/Keystores/Prod.keystore";
    private const string PROD_KEYSTORE_PASSWORD = "chipy12";
    private const string PROD_KEY_ALIAS = "chipy12";
    private const string PROD_KEY_PASSWORD = "chipy12";

    // 빌드 설정 (환경별 기본값을 제공하되, UI에서 수정 가능)
    private static Env selectedEnv = Env.DEV;
    private static string buildPath = "Builds/Dev/";
    private static string appName = "Chipy_Dev";

    // Pending 빌드 키
    private const string PendingBuildKey = "EnvBuildManager.PendingBuild"; // "APK" | "AAB"
    private const string PendingEnvKey = "EnvBuildManager.PendingEnv";     // "DEV" | "TEST" | "PROD"

    [MenuItem("Build/Env Build Manager")]
    public static void ShowWindow()
    {
        GetWindow<EnvBuildManager>("Env Build Manager");
    }

    private void OnGUI()
    {
        GUILayout.Label("Environment Build Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        // 환경 선택
        var newEnv = (Env)EditorGUILayout.EnumPopup("Environment", selectedEnv);
        if (newEnv != selectedEnv)
        {
            selectedEnv = newEnv;
            // 환경 변경 시 기본 경로/앱명 제안
            switch (selectedEnv)
            {
                case Env.DEV:
                    if (buildPath == "" || buildPath.StartsWith("Builds/") == false) buildPath = "Builds/Dev/";
                    appName = appName.StartsWith("Chipy_") ? $"Chipy_Dev" : appName;
                    break;
                case Env.TEST:
                    if (buildPath == "" || buildPath.StartsWith("Builds/") == false) buildPath = "Builds/Test/";
                    appName = appName.StartsWith("Chipy_") ? $"Chipy_Test" : appName;
                    break;
                case Env.PROD:
                    if (buildPath == "" || buildPath.StartsWith("Builds/") == false) buildPath = "Builds/Prod/";
                    appName = appName.StartsWith("Chipy_") ? $"Chipy_Prod" : appName;
                    break;
            }
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Current Keystore", EditorStyles.boldLabel);
        var (ksPath, ksAlias) = GetKeystoreInfo(selectedEnv);
        EditorGUILayout.LabelField($"Keystore: {ksPath}");
        EditorGUILayout.LabelField($"Alias: {ksAlias}");

        EditorGUILayout.Space(10);
        GUILayout.Label("Build Settings", EditorStyles.boldLabel);
        buildPath = EditorGUILayout.TextField("Build Path:", buildPath);
        appName = EditorGUILayout.TextField("App Name:", appName);

        EditorGUILayout.Space(20);

        // 빌드 버튼들
        if (GUILayout.Button($"Build {selectedEnv} APK", GUILayout.Height(30)))
        {
            BuildAPK(selectedEnv);
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button($"Build {selectedEnv} AAB (Bundle)", GUILayout.Height(30)))
        {
            BuildAAB(selectedEnv);
        }

        EditorGUILayout.Space(20);

        // 유틸리티 버튼들
        GUILayout.Label("Utilities:", EditorStyles.boldLabel);

        if (GUILayout.Button($"Set {selectedEnv} Keystore Only"))
        {
            SetKeystoreForEnv(selectedEnv);
        }

        if (GUILayout.Button("Check Keystore Info"))
        {
            CheckKeystoreInfo(selectedEnv);
        }

        if (GUILayout.Button("Open Build Folder"))
        {
            OpenBuildFolder();
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox("환경 전환 → 의존성 리졸브 → 빌드 순서로 진행됩니다. 도중 리컴파일/리로드가 발생해도 자동으로 이어서 빌드됩니다.", MessageType.Info);
    }

    // 기존 단축키(DEV 전용) 호환
    [MenuItem("Build/Quick Dev APK Build")]
    public static void QuickDevBuild()
    {
        BuildAPK(Env.DEV);
    }

    // 진입: 환경 전환 + 리프레시 → (리로드 후) 자동 이어서 빌드
    public static void BuildAPK(Env env)
    {
        Debug.Log($"=== Prepare {env} APK Build (set env + resolve) ===");

        EditorPrefs.SetString(PendingBuildKey, "APK");
        EditorPrefs.SetString(PendingEnvKey, env.ToString());

        // 환경 전환
        TrySwitchEnv(env);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 리로드가 없는 경우 대비
        EditorApplication.delayCall += TryContinuePendingBuild;
    }

    public static void BuildAAB(Env env)
    {
        Debug.Log($"=== Prepare {env} AAB Build (set env + resolve) ===");

        EditorPrefs.SetString(PendingBuildKey, "AAB");
        EditorPrefs.SetString(PendingEnvKey, env.ToString());

        TrySwitchEnv(env);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 리로드가 없는 경우 대비
        EditorApplication.delayCall += TryContinuePendingBuild;
    }

    private static void TrySwitchEnv(Env env)
    {
        try
        {
            switch (env)
            {
                case Env.DEV: EnvSwitcher.SetDev(); break;
                case Env.TEST: EnvSwitcher.SetTest(); break;
                case Env.PROD: EnvSwitcher.SetProd(); break;
            }
            Debug.Log($"✅ EnvSwitcher.Set{env}() called");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"EnvSwitcher.Set{env} call failed or missing. Proceeding anyway. {e.Message}");
        }
    }

    // 실제 APK 빌드 로직
    private static void ExecuteAPKBuild(Env env)
    {
        Debug.Log($"=== Starting {env} APK Build ===");

        if (!SetKeystoreForEnv(env))
        {
            Debug.LogError("Failed to set keystore!");
            return;
        }

        string fullBuildPath = Path.Combine(buildPath, $"{appName}.apk");
        CreateBuildDirectory(buildPath);

        BuildPlayerOptions opts = new BuildPlayerOptions
        {
            scenes = GetEnabledScenePaths(),
            locationPathName = fullBuildPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(opts);

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"✅ {env} APK Build Successful! \nPath: {fullBuildPath}");
            EditorUtility.DisplayDialog("Build Complete",
                $"{env} APK build completed successfully!\n\nPath: {fullBuildPath}", "OK");
        }
        else
        {
            Debug.LogError($"❌ {env} APK Build Failed!");
            EditorUtility.DisplayDialog("Build Failed", $"{env} APK build failed! Check console for details.", "OK");
        }
    }

    // 실제 AAB 빌드 로직
    private static void ExecuteAABBuild(Env env)
    {
        Debug.Log($"=== Starting {env} AAB Build ===");

        if (!SetKeystoreForEnv(env))
        {
            Debug.LogError("Failed to set keystore!");
            return;
        }

        EditorUserBuildSettings.buildAppBundle = true;

        string fullBuildPath = Path.Combine(buildPath, $"{appName}.aab");
        CreateBuildDirectory(buildPath);

        BuildPlayerOptions opts = new BuildPlayerOptions
        {
            scenes = GetEnabledScenePaths(),
            locationPathName = fullBuildPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(opts);

        EditorUserBuildSettings.buildAppBundle = false;

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"✅ {env} AAB Build Successful! \nPath: {fullBuildPath}");
            EditorUtility.DisplayDialog("Build Complete",
                $"{env} AAB build completed successfully!\n\nPath: {fullBuildPath}", "OK");
        }
        else
        {
            Debug.LogError($"❌ {env} AAB Build Failed!");
            EditorUtility.DisplayDialog("Build Failed", $"{env} AAB build failed! Check console for details.", "OK");
        }
    }

    private static bool SetKeystoreForEnv(Env env)
    {
        var (path, pass, alias, aliasPass) = GetKeystoreConfig(env);

        if (!File.Exists(path))
        {
            Debug.LogError($"{env} keystore not found at: {path}");
            EditorUtility.DisplayDialog("Keystore Not Found",
                $"{env} keystore not found at:\n{path}\n\nPlease check the path.", "OK");
            return false;
        }

        PlayerSettings.Android.keystoreName = path;
        PlayerSettings.Android.keystorePass = pass;
        PlayerSettings.Android.keyaliasName = alias;
        PlayerSettings.Android.keyaliasPass = aliasPass;

        Debug.Log($"✅ {env} keystore settings applied successfully!");
        return true;
    }

    private static (string path, string alias) GetKeystoreInfo(Env env)
    {
        var (p, _, a, _) = GetKeystoreConfig(env);
        return (p, a);
    }

    private static (string path, string pass, string alias, string aliasPass) GetKeystoreConfig(Env env)
    {
        switch (env)
        {
            case Env.DEV:
                return (DEV_KEYSTORE_PATH, DEV_KEYSTORE_PASSWORD, DEV_KEY_ALIAS, DEV_KEY_PASSWORD);
            case Env.TEST:
                return (TEST_KEYSTORE_PATH, TEST_KEYSTORE_PASSWORD, TEST_KEY_ALIAS, TEST_KEY_PASSWORD);
            case Env.PROD:
            default:
                return (PROD_KEYSTORE_PATH, PROD_KEYSTORE_PASSWORD, PROD_KEY_ALIAS, PROD_KEY_PASSWORD);
        }
    }

    private static void CheckKeystoreInfo(Env env)
    {
        var (path, alias) = GetKeystoreInfo(env);
        if (File.Exists(path))
        {
            Debug.Log($"✅ {env} keystore found at: {path}");
            Debug.Log($"Current Unity keystore settings:");
            Debug.Log($"- Keystore: {PlayerSettings.Android.keystoreName}");
            Debug.Log($"- Alias: {PlayerSettings.Android.keyaliasName}");

            EditorUtility.DisplayDialog("Keystore Info",
                $"{env} keystore found!\n\nPath: {path}\nAlias: {alias}", "OK");
        }
        else
        {
            Debug.LogError($"❌ {env} keystore not found at: {path}");
            EditorUtility.DisplayDialog("Keystore Not Found",
                $"{env} keystore not found!\n\nPath: {path}", "OK");
        }
    }

    private static string[] GetEnabledScenePaths()
    {
        var scenes = new string[EditorBuildSettings.scenes.Length];
        for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
        {
            scenes[i] = EditorBuildSettings.scenes[i].path;
        }
        return scenes;
    }

    private static void CreateBuildDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            Debug.Log($"Created build directory: {path}");
        }
    }

    private static void OpenBuildFolder()
    {
        if (Directory.Exists(buildPath))
        {
            EditorUtility.RevealInFinder(buildPath);
        }
        else
        {
            Debug.LogWarning($"Build folder does not exist: {buildPath}");
            EditorUtility.DisplayDialog("Folder Not Found",
                $"Build folder does not exist:\n{buildPath}", "OK");
        }
    }

    // =========================
    // 이어서 빌드 실행 관리 (InitializeOnLoad)
    // =========================

    [InitializeOnLoadMethod]
    private static void TryContinuePendingBuild()
    {
        var pending = EditorPrefs.GetString(PendingBuildKey, string.Empty);
        var pendingEnv = EditorPrefs.GetString(PendingEnvKey, string.Empty);
        if (string.IsNullOrEmpty(pending) || string.IsNullOrEmpty(pendingEnv)) return;

        // 한 번만 처리되도록 즉시 제거
        EditorPrefs.DeleteKey(PendingBuildKey);
        EditorPrefs.DeleteKey(PendingEnvKey);

        if (!Enum.TryParse<Env>(pendingEnv, out var env))
        {
            Debug.LogWarning($"Unknown pending env: {pendingEnv}");
            return;
        }

        // 에디터가 준비될 때까지 대기 후 실행
        WaitUntilEditorReady(() =>
        {
            // 의존성 리졸브(EDM4U 있으면 실행)
            RunAndroidExternalDependencyResolverIfPresent();

            // 리프레시 후 빌드
            AssetDatabase.Refresh();

            if (pending == "APK")
            {
                ExecuteAPKBuild(env);
            }
            else if (pending == "AAB")
            {
                ExecuteAABBuild(env);
            }
            else
            {
                Debug.LogWarning($"Unknown pending build type: {pending}");
            }
        });
    }

    private static void WaitUntilEditorReady(Action onReady)
    {
        void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Poll;
                return;
            }
            onReady?.Invoke();
        }

        EditorApplication.delayCall += Poll;
    }

    private static void RunAndroidExternalDependencyResolverIfPresent()
    {
        try
        {
            var type =
                Type.GetType("GooglePlayServices.PlayServicesResolver, Google.JarResolver") ??
                Type.GetType("GooglePlayServices.PlayServicesResolver, ExternalDependencyManager");

            if (type == null)
            {
                Debug.Log("EDM4U PlayServicesResolver not found. Skipping resolve.");
                return;
            }

            var menuResolve = type.GetMethod("MenuResolve", BindingFlags.Public | BindingFlags.Static);
            if (menuResolve != null)
            {
                Debug.Log("Running PlayServicesResolver.MenuResolve() ...");
                menuResolve.Invoke(null, null);
                return;
            }

            var resolve = type.GetMethod("Resolve", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
            if (resolve != null)
            {
                Debug.Log("Running PlayServicesResolver.Resolve() ...");
                resolve.Invoke(null, null);
                return;
            }

            Debug.Log("PlayServicesResolver found but no known resolve method. Skipping.");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to run Android External Dependency Resolver: {e.Message}");
        }
    }
}

#endif