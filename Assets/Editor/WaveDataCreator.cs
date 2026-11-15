#if UNITY_EDITOR

using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public class WaveDataCreator : EditorWindow
{
    [MenuItem("Treeplla/Wave Data Creator")]
    private static void ShowWindow()
    {
        var window = GetWindow<WaveDataCreator>();
        window.titleContent = new GUIContent("Wave Data Creator");
        window.Show();
    }

    [System.Serializable]
    private class CurveWrapper
    {
        public AnimationCurve curve;
    }

    private string output = string.Empty;

    private int linesToCreate = 5;
    private string unitIdxCandidates = "1;2;3";
    private int dmgMin = 5;
    private int dmgMax = 10;
    private int hpMin = 50;
    private int hpMax = 100;
    private int unitCountMinRow = 1;
    private int unitCountMaxRow = 4;

    private AnimationCurve dmgCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    private AnimationCurve hpCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    private AnimationCurve countCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    private Vector2 scroll;

    private void OnEnable()
    {
        LoadPrefs();
    }

    private void OnGUI()
    {
        scroll = GUILayout.BeginScrollView(scroll);

        GUILayout.Label("Settings", EditorStyles.boldLabel);
        linesToCreate = EditorGUILayout.IntField("Count", linesToCreate);
        GUILayout.Space(10);

        GUILayout.Label("Unit Idx Candidates (semicolon list, e.g. 10;11;12;13)", EditorStyles.boldLabel);
        unitIdxCandidates = EditorGUILayout.TextField("unit_idx pool", unitIdxCandidates);
        GUILayout.Space(10);

        GUILayout.Label("Unit Dmg (min/max + curve, random per unit)", EditorStyles.boldLabel);
        dmgMin = EditorGUILayout.IntField("dmg min", dmgMin);
        dmgMax = EditorGUILayout.IntField("dmg max", dmgMax);
        GUILayout.Label("Dmg Curve (x=row 0~1, y=lerp min~max)");
        dmgCurve = EditorGUILayout.CurveField(dmgCurve, GUILayout.Height(80));
        GUILayout.Space(10);

        GUILayout.Label("Unit HP (min/max + curve, random per unit)", EditorStyles.boldLabel);
        hpMin = EditorGUILayout.IntField("hp min", hpMin);
        hpMax = EditorGUILayout.IntField("hp max", hpMax);
        GUILayout.Label("HP Curve (x=row 0~1, y=lerp min~max)");
        hpCurve = EditorGUILayout.CurveField(hpCurve, GUILayout.Height(80));
        GUILayout.Space(10);

        GUILayout.Label("Unit Count Per Row (수량 = 한 행의 unit_idx 개수)", EditorStyles.boldLabel);
        unitCountMinRow = EditorGUILayout.IntField("unit min count row", unitCountMinRow);
        unitCountMaxRow = EditorGUILayout.IntField("unit max count row", unitCountMaxRow);
        GUILayout.Label("Count Curve (x=row 0~1, y=lerp min~max)");
        countCurve = EditorGUILayout.CurveField(countCurve, GUILayout.Height(80));
        GUILayout.Space(20);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Create Wave Data", GUILayout.Height(EditorGUIUtility.singleLineHeight * 3), GUILayout.Width(200)))
            output = GenerateData();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(10);

        GUILayout.Label("Output (unit_idx \\t unit_dmg \\t unit_hp)", EditorStyles.boldLabel);
        output = EditorGUILayout.TextArea(output, GUILayout.Height(200));

        GUILayout.EndScrollView();
    }

    private string GenerateData()
    {
        int[] candidatePool;
        try
        {
            candidatePool = unitIdxCandidates.Trim().Trim(';').Split(';')
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => int.Parse(x.Trim())).ToArray();
        }
        catch
        {
            return "Invalid unit_idx input. Use semicolon-separated integers, e.g. 10;11;12";
        }

        if (candidatePool.Length == 0) return "No unit idx provided";

        // min > max 안전 처리
        int dLo = Mathf.Min(dmgMin, dmgMax);
        int dHi = Mathf.Max(dmgMin, dmgMax);
        int hLo = Mathf.Min(hpMin, hpMax);
        int hHi = Mathf.Max(hpMin, hpMax);
        // unit count 행 길이는 최소 1로만 클램프. 풀 길이는 상한이 아님 — 풀보다 많이 요청되면 풀을 순환해서 채움.
        int cLo = Mathf.Max(1, Mathf.Min(unitCountMinRow, unitCountMaxRow));
        int cHi = Mathf.Max(1, Mathf.Max(unitCountMinRow, unitCountMaxRow));

        var dmgEvalCurve = dmgCurve ?? AnimationCurve.Linear(0f, 0f, 1f, 1f);
        var hpEvalCurve = hpCurve ?? AnimationCurve.Linear(0f, 0f, 1f, 1f);
        var countEvalCurve = countCurve ?? AnimationCurve.Linear(0f, 0f, 1f, 1f);

        StringBuilder builder = new();
        for (int row = 0; row < linesToCreate; row++)
        {
            // 행 진행도 t (0~1). 행이 1개뿐이면 0으로 고정.
            float t = linesToCreate > 1 ? (float)row / (linesToCreate - 1) : 0f;

            // 1) 이번 행에 들어갈 unit 개수 N 결정 (count 커브 평가 + 약간의 랜덤 변동).
            float countT = Mathf.Clamp01(countEvalCurve.Evaluate(t)) * Random.Range(0.85f, 1.15f);
            int rowCount = Mathf.Clamp(
                Mathf.RoundToInt(Mathf.LerpUnclamped(cLo, cHi, countT)),
                cLo, cHi);

            // 2) 후보 풀을 오름차순 정렬한 뒤 앞에서부터 N개 순서대로 추출.
            //    예) 풀 [1,2,3,4,5], N=3 → 1;2;3 / N=5 → 1;2;3;4;5
            //    N이 풀보다 많으면 풀을 순환해서 채움. 예) 풀 [1,2,3], N=5 → 1;2;3;1;2
            var sortedPool = candidatePool.OrderBy(x => x).ToArray();
            var pickedIdx = new System.Collections.Generic.List<int>(rowCount);
            for (int k = 0; k < rowCount; k++)
                pickedIdx.Add(sortedPool[k % sortedPool.Length]);

            // unit_idx 컬럼
            for (int j = 0; j < pickedIdx.Count; j++)
            {
                builder.Append(pickedIdx[j]);
                if (j != pickedIdx.Count - 1) builder.Append(";");
            }
            builder.Append("\t");

            // unit_dmg 컬럼 — idx마다 독립 랜덤 (커브로 행 트렌드 + ±15% 변동)
            for (int j = 0; j < pickedIdx.Count; j++)
            {
                float dmgT = Mathf.Clamp01(dmgEvalCurve.Evaluate(t)) * Random.Range(0.85f, 1.15f);
                int dmg = Mathf.Clamp(
                    Mathf.RoundToInt(Mathf.LerpUnclamped(dLo, dHi, dmgT)),
                    dLo, dHi);
                builder.Append(dmg);
                if (j != pickedIdx.Count - 1) builder.Append(";");
            }
            builder.Append("\t");

            // unit_hp 컬럼 — idx마다 독립 랜덤 (커브로 행 트렌드 + ±15% 변동)
            for (int j = 0; j < pickedIdx.Count; j++)
            {
                float hpT = Mathf.Clamp01(hpEvalCurve.Evaluate(t)) * Random.Range(0.85f, 1.15f);
                int hp = Mathf.Clamp(
                    Mathf.RoundToInt(Mathf.LerpUnclamped(hLo, hHi, hpT)),
                    hLo, hHi);
                builder.Append(hp);
                if (j != pickedIdx.Count - 1) builder.Append(";");
            }
            builder.Append("\n");
        }

        SavePrefs();
        return builder.ToString();
    }

    private void SavePrefs()
    {
        EditorPrefs.SetInt("WaveDataCreator_linesToCreate", linesToCreate);
        EditorPrefs.SetString("WaveDataCreator_unitIdxCandidates", unitIdxCandidates);
        EditorPrefs.SetInt("WaveDataCreator_dmgMin", dmgMin);
        EditorPrefs.SetInt("WaveDataCreator_dmgMax", dmgMax);
        EditorPrefs.SetInt("WaveDataCreator_hpMin", hpMin);
        EditorPrefs.SetInt("WaveDataCreator_hpMax", hpMax);
        EditorPrefs.SetInt("WaveDataCreator_unitCountMinRow", unitCountMinRow);
        EditorPrefs.SetInt("WaveDataCreator_unitCountMaxRow", unitCountMaxRow);
        SaveCurve("WaveDataCreator_dmgCurve", dmgCurve);
        SaveCurve("WaveDataCreator_hpCurve", hpCurve);
        SaveCurve("WaveDataCreator_countCurve", countCurve);
    }

    private void LoadPrefs()
    {
        linesToCreate = EditorPrefs.GetInt("WaveDataCreator_linesToCreate", linesToCreate);
        unitIdxCandidates = EditorPrefs.GetString("WaveDataCreator_unitIdxCandidates", unitIdxCandidates);
        dmgMin = EditorPrefs.GetInt("WaveDataCreator_dmgMin", dmgMin);
        dmgMax = EditorPrefs.GetInt("WaveDataCreator_dmgMax", dmgMax);
        hpMin = EditorPrefs.GetInt("WaveDataCreator_hpMin", hpMin);
        hpMax = EditorPrefs.GetInt("WaveDataCreator_hpMax", hpMax);
        unitCountMinRow = EditorPrefs.GetInt("WaveDataCreator_unitCountMinRow", unitCountMinRow);
        unitCountMaxRow = EditorPrefs.GetInt("WaveDataCreator_unitCountMaxRow", unitCountMaxRow);
        dmgCurve = LoadCurve("WaveDataCreator_dmgCurve", AnimationCurve.Linear(0f, 0f, 1f, 1f));
        hpCurve = LoadCurve("WaveDataCreator_hpCurve", AnimationCurve.Linear(0f, 0f, 1f, 1f));
        countCurve = LoadCurve("WaveDataCreator_countCurve", AnimationCurve.Linear(0f, 0f, 1f, 1f));
    }

    private static void SaveCurve(string key, AnimationCurve curve)
    {
        if (curve == null) { EditorPrefs.DeleteKey(key); return; }
        var w = new CurveWrapper { curve = curve };
        EditorPrefs.SetString(key, JsonUtility.ToJson(w));
    }

    private static AnimationCurve LoadCurve(string key, AnimationCurve fallback)
    {
        var s = EditorPrefs.GetString(key, "");
        if (string.IsNullOrEmpty(s)) return fallback;
        try
        {
            var w = JsonUtility.FromJson<CurveWrapper>(s);
            return (w != null && w.curve != null) ? w.curve : fallback;
        }
        catch
        {
            return fallback;
        }
    }
}
#endif
