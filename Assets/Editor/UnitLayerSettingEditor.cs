using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class UnitLayerSettingEditor : EditorWindow
{
    private GameObject targetPrefab;

    private static readonly string[] orderOneNames = { "Head", "Face 01", "Right Arm" };
    private static readonly string[] orderMinusOneNames = { "Right Leg", "Left Leg" , "Left Hand", "Sword" };

    [MenuItem("Tools/Unit Layer Setting")]
    public static void ShowWindow()
    {
        GetWindow<UnitLayerSettingEditor>("Unit Layer Setting");
    }

    private void OnGUI()
    {
        GUILayout.Label("Unit Layer Setting", EditorStyles.boldLabel);
        GUILayout.Space(10);

        targetPrefab = (GameObject)EditorGUILayout.ObjectField("Target Prefab", targetPrefab, typeof(GameObject), true);

        GUILayout.Space(10);

        if (GUILayout.Button("Apply Order In Layer"))
        {
            if (targetPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Prefab을 먼저 할당해주세요.", "OK");
                return;
            }

            ApplyOrderInLayer(targetPrefab);
        }
    }

    private void ApplyOrderInLayer(GameObject root)
    {
        var spriteRenderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        int changedCount = 0;

        foreach (var sr in spriteRenderers)
        {
            string goName = sr.gameObject.name;

            if (orderOneNames.Any(n => goName.Contains(n)))
            {
                sr.sortingOrder = 1;
                changedCount++;
                Debug.Log($"[UnitLayerSetting] '{goName}' → Order in Layer: 1");
            }
            else if (orderMinusOneNames.Any(n => goName.Contains(n)))
            {
                sr.sortingOrder = -1;
                changedCount++;
                Debug.Log($"[UnitLayerSetting] '{goName}' → Order in Layer: -1");
            }
        }

        if (changedCount > 0)
        {
            EditorUtility.SetDirty(root);
        }

        EditorUtility.DisplayDialog("Complete", $"{changedCount}개의 SpriteRenderer가 변경되었습니다.", "OK");
    }
}
