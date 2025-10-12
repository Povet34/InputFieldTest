#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Reporting;
using System;
using System.IO;
using UnityEngine;

public class Builder
{
    // 하드코딩된 설정값들
    private const string DEFAULT_BUILD_PATH = "E:\\UnityContents\\Builds";
    private const string KEYSTORE_PATH = "E:\\UnityContents\\InputFieldTest\\user.keystore";
    private const string KEYSTORE_PASS = "test12";
    private const string KEYALIAS_NAME = "test12";
    private const string KEYALIAS_PASS = "test12";

    // AAB 빌드 (Android App Bundle)
    public static void BuildAndroidAAB()
    {
        BuildAndroid(true, "Test.aab");
    }

    // APK 빌드
    public static void BuildAndroidAPK()
    {
        BuildAndroid(false, "Test.apk");
    }

    // 공통 빌드 로직
    private static void BuildAndroid(bool isAppBundle, string fileName)
    {
        // === 빌드 설정 ===
        string[] scenes = { "Assets/Scenes/SampleScene.unity" };

        // 빌드 경로: 커맨드라인에서 받거나 기본값 사용
        string buildPath = GetCommandLineArgument("-buildPath");
        if (string.IsNullOrEmpty(buildPath))
        {
            buildPath = DEFAULT_BUILD_PATH;
            Debug.Log($"기본 빌드 경로 사용: {buildPath}");
        }

        // 빌드 폴더가 없으면 생성
        if (!Directory.Exists(buildPath))
        {
            Directory.CreateDirectory(buildPath);
            Debug.Log($"빌드 폴더 생성: {buildPath}");
        }

        // AAB 또는 APK 설정
        EditorUserBuildSettings.buildAppBundle = isAppBundle;
        string buildType = isAppBundle ? "AAB" : "APK";
        Debug.Log($"빌드 타입: {buildType}");

        // === 키스토어 설정 ===
        string keystorePath = GetCommandLineArgument("-keystorePath");
        if (string.IsNullOrEmpty(keystorePath))
        {
            keystorePath = KEYSTORE_PATH;
        }

        string keystorePass = GetCommandLineArgument("-keystorePass") ?? KEYSTORE_PASS;
        string keyaliasName = GetCommandLineArgument("-keyaliasName") ?? KEYALIAS_NAME;
        string keyaliasPass = GetCommandLineArgument("-keyaliasPass") ?? KEYALIAS_PASS;

        // 키스토어 파일 존재 확인
        if (!File.Exists(keystorePath))
        {
            Debug.LogError($"키스토어 파일을 찾을 수 없습니다: {keystorePath}");
            EditorApplication.Exit(1);
            return;
        }

        // 키스토어 적용
        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = keystorePath;
        PlayerSettings.Android.keystorePass = keystorePass;
        PlayerSettings.Android.keyaliasName = keyaliasName;
        PlayerSettings.Android.keyaliasPass = keyaliasPass;

        Debug.Log($"키스토어 설정 완료: {keystorePath}");

        // === 빌드 실행 ===
        Debug.Log($"===== 안드로이드 {buildType} 빌드를 시작합니다 =====");
        Debug.Log($"빌드 경로: {Path.Combine(buildPath, fileName)}");

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(buildPath, fileName),
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"{buildType} 빌드 성공! 용량: {summary.totalSize} bytes");
            Debug.Log($"빌드 파일 위치: {buildPlayerOptions.locationPathName}");
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError($"{buildType} 빌드 실패!");
            EditorApplication.Exit(1);
        }
    }

    private static string GetCommandLineArgument(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == name && args.Length > i + 1)
            {
                return args[i + 1];
            }
        }
        return null;
    }
}
#endif