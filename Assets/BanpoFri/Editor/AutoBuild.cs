using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;
using UnityEditor.U2D;
using Debug = UnityEngine.Debug;

#if UNITY_IOS
using UnityEditor.iOS.Xcode;     // ✅ 여기
#endif

public class AutoBuild
{
	static void ApplyAndroidSigningCredentials()
	{
		if (!PlayerSettings.Android.useCustomKeystore ||
			string.IsNullOrEmpty(PlayerSettings.Android.keystoreName) ||
			string.IsNullOrEmpty(PlayerSettings.Android.keyaliasName))
		{
			throw new UnityEditor.Build.BuildFailedException(
				"Select the Android keystore and key alias in Player > Publishing Settings.");
		}

		var keystorePassword = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PASSWORD");
		var keyPassword = Environment.GetEnvironmentVariable("ANDROID_KEY_PASSWORD");
		if (!string.IsNullOrEmpty(keystorePassword))
			PlayerSettings.Android.keystorePass = keystorePassword;
		if (!string.IsNullOrEmpty(keyPassword))
			PlayerSettings.Android.keyaliasPass = keyPassword;

		if (string.IsNullOrEmpty(PlayerSettings.Android.keystorePass) ||
			string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass))
		{
			throw new UnityEditor.Build.BuildFailedException(
				"Enter both Android signing passwords in Player > Publishing Settings or supply ANDROID_KEYSTORE_PASSWORD and ANDROID_KEY_PASSWORD.");
		}
	}

	static string GetTargetPath()
	{
		return Environment.GetEnvironmentVariable("UNITY_TARGET_PATH");
	}

	static BuildOptions buildOption()
	{
		BuildOptions options = BuildOptions.None;
		var buildoption = Environment.GetEnvironmentVariable("BUILD_OPTIONS");
		if (string.IsNullOrEmpty(buildoption))
			return options;
		string[] array = buildoption.Split(',');
		foreach (var entity in array)
		{
			switch (entity)
			{
				case "debug":
					options = options | BuildOptions.Development | BuildOptions.AllowDebugging;
					break;
			}
		}
		return options;
	}

	static void checkBuildOption()
	{
		var buildoption = Environment.GetEnvironmentVariable("BUILD_OPTIONS");
		if (string.IsNullOrEmpty(buildoption))
			return;
		string[] array = buildoption.Split(',');
		foreach (var entity in array)
		{
			switch (entity)
			{
				case "GoogleStore":
					EditorUserBuildSettings.buildAppBundle = true;
					break;
			}
		}
	}

	static string GetVersionName()
	{
		string result = string.Empty;
		var version_name = Environment.GetEnvironmentVariable("VERSION_NAME");
		if (!string.IsNullOrEmpty(version_name))
		{
			result = version_name;
		}
		return result;
	}

	static string GetVersionCode()
	{
		string result = string.Empty;
		var version_code = Environment.GetEnvironmentVariable("VERSION_CODE");
		if (!string.IsNullOrEmpty(version_code))
		{
			result = version_code;
		}
		return result;
	}

	[MenuItem("Treeplla/Build/BakeFont")]
	public static void BakeFont()
	{
		var font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Arts/Fonts/Outfit-Black SDF.asset");
		if (font != null)
		{
			font.ClearFontAssetData();
			var str = LocalizeTextOverlapRemover.GetLocalizeText();
			font.TryAddCharacters(str);
			font.ReadFontAssetDefinition();
		}
		EditorUtility.SetDirty(font);
	}


    [MenuItem("Treeplla/Build/Build Android AAB")]
    public static void Build_Android_AAB()
    {
        ApplyAndroidSigningCredentials();

        EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Public;
        EditorUserBuildSettings.androidBuildType = AndroidBuildType.Release;
        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ETC2;
        EditorUserBuildSettings.androidETC2Fallback = AndroidETC2Fallback.Quality32BitDownscaled;
        EditorUserBuildSettings.buildScriptsOnly = false;
        EditorUserBuildSettings.allowDebugging = false;
        EditorUserBuildSettings.buildAppBundle = true;
        //EditorUserBuildSettings.overrideMaxTextureSize = 1024;
        checkBuildOption();

        var version_code = GetVersionCode();
        if (!string.IsNullOrEmpty(version_code))
            PlayerSettings.Android.bundleVersionCode = int.Parse(version_code);
        var version_name = GetVersionName();
        if (!string.IsNullOrEmpty(version_name))
        {
            PlayerSettings.bundleVersion = version_name;
        }

        UnityEngine.Debug.Log($"GetTargetPath() = {GetTargetPath()}");
        BuildPlayerOptions buildPlayerOptions = BuildPlayerWindow.DefaultBuildMethods.GetBuildPlayerOptions(new BuildPlayerOptions());
        buildPlayerOptions.options = buildOption() | BuildOptions.CompressWithLz4HC;
        //Google.Android.AppBundle.Editor.Internal.AppBundlePublisher.Build(buildPlayerOptions, null, false);

        // var result = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, GetTargetPath(), BuildTarget.Android, buildOption() | BuildOptions.CompressWithLz4HC);
        // if (result.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        // {
        //     const int kSuccessCode = 0;
        //     EditorApplication.Exit(kSuccessCode);
        // }
        // else
        // {
        //     const int kErrorCode = 1;
        //     EditorApplication.Exit(kErrorCode);
        // }
    }

	[MenuItem("BanpoFri/Build/PreBuild")]
	public static void ReadyForBuild()
	{
		BakeFont();
		PlayerSettings.SplashScreen.show = false;
		AddressableAssetSettings.CleanPlayerContent();
		AddressablesPlayerBuildResult result;
		AddressableAssetSettings.BuildPlayerContent(out result);
		if (!string.IsNullOrEmpty(result.Error))
			throw new System.Exception($"addressable build failed {result.Error}");
		CheatEnable();
	}

	public static void CheatEnable()
	{
		var cheatMode = Environment.GetEnvironmentVariable("CHEAT_MODE");
		if (!string.IsNullOrEmpty(cheatMode))
		{
			switch (cheatMode)
			{
				case "true":
					{
						string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(
														EditorUserBuildSettings.selectedBuildTargetGroup);
						var allDefines = definesString.Split(';').ToList();
						if (!allDefines.Contains("BanpoFri_LOG"))
							allDefines.Add("BanpoFri_LOG");
						PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, allDefines.ToArray());
					}
					break;
				case "false":
					{
						string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(
														EditorUserBuildSettings.selectedBuildTargetGroup);
						var allDefines = definesString.Split(';').ToList();
						allDefines.Remove("BanpoFri_LOG");
						PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, allDefines.ToArray());
					}
					break;
			}
		}
	}

	[MenuItem("BanpoFri/Build/Build Android")]
	public static void Build_Android()
	{
		EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
		ApplyAndroidSigningCredentials();

		EditorUserBuildSettings.androidBuildType = AndroidBuildType.Release;
		EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ETC2;
		EditorUserBuildSettings.androidETC2Fallback = AndroidETC2Fallback.Quality32Bit;
		EditorUserBuildSettings.buildScriptsOnly = false;
		EditorUserBuildSettings.allowDebugging = false;
		EditorUserBuildSettings.buildAppBundle = false;

		var version_code = GetVersionCode();
		if (!string.IsNullOrEmpty(version_code))
			PlayerSettings.Android.bundleVersionCode = int.Parse(version_code);
		var version_name = GetVersionName();
		if (!string.IsNullOrEmpty(version_name))
		{
			PlayerSettings.bundleVersion = version_name;
		}
		checkBuildOption();

		Debug.Log($"GetTargetPath() = {GetTargetPath()}");
		var result = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, GetTargetPath(), BuildTarget.Android, buildOption() | BuildOptions.CompressWithLz4HC);
		if (Application.isBatchMode)
			EditorApplication.Exit(result.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
	}

	[MenuItem("BanpoFri/Build/Build iOS")]
	public static void Build_iOS()
	{
		EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
		var version_code = GetVersionCode();
		if (!string.IsNullOrEmpty(version_code))
			PlayerSettings.iOS.buildNumber = version_code;

		var version_name = GetVersionName();
		if (!string.IsNullOrEmpty(version_name))
		{
			PlayerSettings.bundleVersion = version_name;
		}
		checkBuildOption();

		var result = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, GetTargetPath(), BuildTarget.iOS, buildOption() | BuildOptions.CompressWithLz4HC);
		if (result.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
		{
			const int kSuccessCode = 0;
			EditorApplication.Exit(kSuccessCode);
		}
		else
		{
			const int kErrorCode = 1;
			EditorApplication.Exit(kErrorCode);
		}
	}

#if UNITY_IOS
    [UnityEditor.Callbacks.PostProcessBuild]
    public static void PatchMetalDisplayLink(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS)
            return;

        string[] possiblePaths =
        {
            Path.Combine(pathToBuiltProject, "Classes/UnityAppController.h"),
            Path.Combine(pathToBuiltProject, "UnityFramework/UnityAppController.h")
        };

        string targetFile = null;
        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                targetFile = path;
                break;
            }
        }

        if (targetFile == null)
        {
            Debug.LogWarning("[MetalDisplayLink] UnityAppController.h 파일을 찾지 못했습니다.");
            return;
        }

        string content = File.ReadAllText(targetFile);
        string before = "#define UNITY_USES_METAL_DISPLAY_LINK (UNITY_HAS_IOSSDK_17_0 || UNITY_HAS_TVOSSDK_17_0)";
        string after = "#define UNITY_USES_METAL_DISPLAY_LINK 0";

        if (content.Contains(before))
        {
            content = content.Replace(before, after);
            File.WriteAllText(targetFile, content);
            Debug.Log("✅ [MetalDisplayLink] 자동 패치 완료");
        }
        else if (!content.Contains(after))
        {
            content = after + "\n" + content;
            File.WriteAllText(targetFile, content);
            Debug.Log("✅ [MetalDisplayLink] define 상단 추가");
        }
        else
        {
            Debug.Log("✅ [MetalDisplayLink] 이미 적용됨");
        }
    }
#endif
}
