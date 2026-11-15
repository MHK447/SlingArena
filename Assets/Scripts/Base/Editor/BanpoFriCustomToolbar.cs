#if UNITY_EDITOR

using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEditor.SceneManagement;

public class BanpoFriCustomToolbar
{
    [MainToolbarElement("BanpoFri/BackgroundToggle", defaultDockPosition = MainToolbarDockPosition.Left)]
    public static MainToolbarElement BackgroundToggleButton()
    {
        var content = new MainToolbarContent("BG", "Toggle Run In Background");
        return new MainToolbarToggle(content, Application.runInBackground, (value) =>
        {
            Application.runInBackground = value;
        });
    }

    [MainToolbarElement("BanpoFri/TimeScale", defaultDockPosition = MainToolbarDockPosition.Left)]
    public static IEnumerable<MainToolbarElement> TimeScaleElements()
    {
        var sliderContent = new MainToolbarContent("TimeScale", "Adjust Time Scale");
        yield return new MainToolbarSlider(sliderContent, Time.timeScale, 0f, 10f, (value) =>
        {
            Time.timeScale = value;
        });

        var resetContent = new MainToolbarContent("INIT", "Reset TimeScale to 1");
        yield return new MainToolbarButton(resetContent, () =>
        {
            Time.timeScale = 1f;
            MainToolbar.Refresh("BanpoFri/TimeScale");
        });
    }

    [MainToolbarElement("BanpoFri/Language", defaultDockPosition = MainToolbarDockPosition.Left)]
    public static MainToolbarElement LanguageDropdown()
    {
        var currentLang = PlayerPrefs.GetString("UNITY_EDITOR_LANGS", "en");
        var content = new MainToolbarContent(currentLang, "Change Language");
        return new MainToolbarDropdown(content, (rect) =>
        {
            var menu = new GenericMenu();
            var langsList = new List<string>(Enum.GetNames(typeof(Language)));
            foreach (var lang in langsList)
            {
                var langName = lang;
                menu.AddItem(new GUIContent(langName), false, () =>
                {
                    PlayerPrefs.SetString("UNITY_EDITOR_LANGS", langName);
                    MainToolbar.Refresh("BanpoFri/Language");

                    if (Application.isPlaying)
                    {
                        Language curLang;
                        Enum.TryParse<Language>(langName, out curLang);
                        GameRoot.Instance.UserData.Language = curLang;
                        Config.Instance.UpdateFallbackOrder(curLang);

                        foreach (var ls in LocalizeString.Localizelist)
                        {
                            if (ls != null) ls.RefreshText();
                        }

                        var list = GameRoot.Instance.UISystem.RefreshComponentList;
                        foreach (var ls in list) ls.RefreshText();
                    }
                });
            }
            menu.DropDown(rect);
        });
    }

    [MainToolbarElement("BanpoFri/SceneSelector", defaultDockPosition = MainToolbarDockPosition.Left)]
    public static MainToolbarElement SceneSelector()
    {
        var currentScene = EditorSceneManager.GetActiveScene().name;
        var content = new MainToolbarContent(currentScene, "Change Select Scene");
        return new MainToolbarDropdown(content, (rect) =>
        {
            var menu = new GenericMenu();
            var findScenesGUID = AssetDatabase.FindAssets("t:Scene", new string[] { "Assets/Scenes" });
            for (int i = 0; i < findScenesGUID.Length; i++)
            {
                var convertPath = AssetDatabase.GUIDToAssetPath(findScenesGUID[i]);
                var sceneName = convertPath.Replace("Assets/Scenes/", "").Replace(".unity", "");
                menu.AddItem(new GUIContent(sceneName), false, () =>
                {
                    EditorSceneManager.OpenScene(convertPath);
                    MainToolbar.Refresh("BanpoFri/SceneSelector");
                });
            }
            menu.DropDown(rect);
        });
    }

    [MainToolbarElement("BanpoFri/ClearPlayerPrefs", defaultDockPosition = MainToolbarDockPosition.Left)]
    public static MainToolbarElement ClearPlayerPrefsButton()
    {
        var icon = EditorGUIUtility.IconContent("SaveFromPlay").image as Texture2D;
        var content = new MainToolbarContent(icon, "DeleteAll PlayerPrefs!");
        return new MainToolbarButton(content, () =>
        {
            Debug.Log("DeleteAll PlayerPrefs!");
            PlayerPrefs.DeleteAll();
        });
    }

    [MainToolbarElement("BanpoFri/DeleteMaster", defaultDockPosition = MainToolbarDockPosition.Left)]
    public static MainToolbarElement DeleteMasterButton()
    {
        var icon = EditorGUIUtility.IconContent("TreeEditor.Trash").image as Texture2D;
        var content = new MainToolbarContent(icon, "DeleteAll Master File!");
        return new MainToolbarButton(content, () =>
        {
            string filePath = "Assets/Master.dat";
            if (File.Exists(filePath))
            {
                Debug.Log("마스터 파일 삭제!");
                File.Delete(filePath);
                File.Delete(filePath + ".meta");
                File.Delete(filePath + ".savebackup");
                File.Delete(filePath + ".savebackup.meta");
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.Log("마스터 파일이 존재하지 않음!");
            }
        });
    }

    [MainToolbarElement("BanpoFri/SaveData", defaultDockPosition = MainToolbarDockPosition.Right)]
    public static IEnumerable<MainToolbarElement> SaveDataElements()
    {
        var refreshIcon = EditorGUIUtility.IconContent("TreeEditor.Refresh").image as Texture2D;
        var refreshContent = new MainToolbarContent(refreshIcon, "Refresh .dat List");
        yield return new MainToolbarButton(refreshContent, () =>
        {
            MainToolbar.Refresh("BanpoFri/SaveData");
        });

        var customSaveDataKey = "CustomSaveDataKey";
        var customSaveData = EditorPrefs.HasKey(customSaveDataKey)
            ? EditorPrefs.GetString(customSaveDataKey)
            : "Master";

        var dropContent = new MainToolbarContent(customSaveData, "Change Target .dat");
        yield return new MainToolbarDropdown(dropContent, (rect) =>
        {
            var menu = new GenericMenu();
            var findDats = Directory.GetFiles("Assets/", "*.dat");
            for (int i = 0; i < findDats.Length; i++)
            {
                var datName = findDats[i].Replace("Assets/", "").Replace(".dat", "");
                menu.AddItem(new GUIContent(datName), false, () =>
                {
                    EditorPrefs.SetString(customSaveDataKey, datName);
                    MainToolbar.Refresh("BanpoFri/SaveData");
                });
            }
            menu.DropDown(rect);
        });
    }
}

#endif
