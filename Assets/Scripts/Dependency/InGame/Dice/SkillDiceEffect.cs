using UnityEngine;
using UnityEngine.Rendering;

public class SkillDiceEffect : MonoBehaviour
{
    [SerializeField] private MeshFilter SourceMeshFilter;
    [SerializeField] private MeshRenderer SourceMeshRenderer;

    [SerializeField, Range(1f, 1.3f)] private float OutlineScale = 1.08f;
    [SerializeField] private string ColorProperty = "_BaseColor";

    [SerializeField] private Color ColorA = new Color(1f, 0.85f, 0.3f, 1f);
    [SerializeField] private Color ColorB = new Color(1f, 0.35f, 0.85f, 1f);
    [SerializeField, Range(0f, 20f)] private float GlowStrength = 8f;
    [SerializeField] private float Speed = 3f;

    [SerializeField] private int MinActiveGrade = 4;

    [SerializeField] private Shader OutlineShader;
    [SerializeField] private Shader MaskShader;

    private const string OutlineShaderName = "Custom/DiceOutline";
    private const string MaskShaderName = "Custom/DiceOutlineMask";

    private GameObject outlineGO;
    private MeshFilter outlineFilter;
    private MeshRenderer outlineRenderer;
    private Material outlineMat;

    private GameObject maskGO;
    private MeshFilter maskFilter;
    private MeshRenderer maskRenderer;
    private Material maskMat;

    private int colorPropertyId;

    private void Awake()
    {
        colorPropertyId = Shader.PropertyToID(ColorProperty);
        if (SourceMeshFilter == null) SourceMeshFilter = GetComponent<MeshFilter>();
        if (SourceMeshRenderer == null) SourceMeshRenderer = GetComponent<MeshRenderer>();
        EnsureOutline();
        SetEffectActive(false);
    }

    private void OnEnable()
    {
        EnsureOutline();
        SetEffectActive(true);
    }

    private void OnDisable()
    {
        SetEffectActive(false);
    }

    private void OnDestroy()
    {
        if (outlineMat != null) DestroyMatSafe(outlineMat);
        if (maskMat != null) DestroyMatSafe(maskMat);
    }

    private static void DestroyMatSafe(Object o)
    {
        if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
    }

    public void SetActiveByGrade(int grade)
    {
        bool shouldRun = grade >= MinActiveGrade;
        enabled = shouldRun;
    }

    public void SetActiveByDiceIdx(int diceIdx)
    {
        enabled = diceIdx > 1;
        SetEffectActive(diceIdx > 1);
    }

    // 슈터가 슈팅 전(ready) 상태일 때 diceidx==1이라도 강조 이펙트를 켜두기 위한 오버라이드
    public void SetActiveOverride(bool active)
    {
        EnsureOutline();
        enabled = active;
        SetEffectActive(active);
    }

    // 외부에서 두 펄스 컬러를 동적으로 교체하고 즉시 머티리얼에 반영 (슈터 ready=주황 등)
    public void SetColors(Color colorA, Color colorB)
    {
        ColorA = colorA;
        ColorB = colorB;
        EnsureOutline();
        ApplyColorToMaterials(ColorA);
    }

    private void ApplyColorToMaterials(Color baseColor)
    {
        // 셰이더가 additive blend(Blend SrcAlpha One)이라 GlowStrength 곱셈은 G까지 포화시켜 노랑 만듦.
        // 색은 그대로 보내고, 강도(가산량)는 alpha로만 조절.
        var rgb = baseColor;
        rgb.a = Mathf.Clamp01(baseColor.a * GlowStrength);
        if (outlineMat != null && outlineMat.HasProperty(colorPropertyId))
            outlineMat.SetColor(colorPropertyId, rgb);
        if (maskMat != null && maskMat.HasProperty(colorPropertyId))
            maskMat.SetColor(colorPropertyId, rgb);
    }

    private void EnsureOutline()
    {
        if (SourceMeshFilter == null || SourceMeshRenderer == null) return;
        if (SourceMeshFilter.sharedMesh == null) return;

        EnsureChild(ref maskGO, ref maskFilter, ref maskRenderer, ref maskMat,
                    "SkillDiceOutlineMask", MaskShader, MaskShaderName, 1f);

        EnsureChild(ref outlineGO, ref outlineFilter, ref outlineRenderer, ref outlineMat,
                    "SkillDiceOutline", OutlineShader, OutlineShaderName, OutlineScale);

        // Keep scales in sync with current OutlineScale (mask always 1, outline OutlineScale)
        if (maskGO != null) maskGO.transform.localScale = Vector3.one;
        if (outlineGO != null) outlineGO.transform.localScale = Vector3.one * OutlineScale;
    }

    private void EnsureChild(ref GameObject go, ref MeshFilter mf, ref MeshRenderer mr, ref Material mat,
                             string name, Shader shaderRef, string shaderName, float scale)
    {
        if (go == null)
        {
            go = new GameObject(name);
            go.hideFlags = HideFlags.DontSave;
            var t = go.transform;
            t.SetParent(transform, false);
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one * scale;
            mf = go.AddComponent<MeshFilter>();
            mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        if (mf.sharedMesh != SourceMeshFilter.sharedMesh)
            mf.sharedMesh = SourceMeshFilter.sharedMesh;

        if (mat == null)
        {
            var shader = shaderRef != null ? shaderRef : Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogWarning($"SkillDiceEffect: shader '{shaderName}' not found. Assign it directly in the inspector to avoid build stripping.");
                return;
            }
            mat = new Material(shader) { name = $"{name} (Instance)" };
            // 머티리얼 생성 즉시 ColorA 주입. additive blend라 GlowStrength로 RGB 곱하면 G가 포화돼 노랑이 됨.
            // RGB는 입력 그대로, alpha만 GlowStrength로 강조.
            if (mat.HasProperty(colorPropertyId))
            {
                var initRgb = ColorA;
                initRgb.a = Mathf.Clamp01(ColorA.a * GlowStrength);
                mat.SetColor(colorPropertyId, initRgb);
            }
        }

        int subCount = mf.sharedMesh.subMeshCount;
        var mats = mr.sharedMaterials;
        if (mats == null || mats.Length != subCount)
        {
            mats = new Material[subCount];
            for (int i = 0; i < subCount; i++) mats[i] = mat;
            mr.sharedMaterials = mats;
        }
        else
        {
            bool needSet = false;
            for (int i = 0; i < subCount; i++) if (mats[i] != mat) { needSet = true; break; }
            if (needSet)
            {
                for (int i = 0; i < subCount; i++) mats[i] = mat;
                mr.sharedMaterials = mats;
            }
        }
    }

    private void SetEffectActive(bool active)
    {
        if (outlineGO != null) outlineGO.SetActive(active);
        if (maskGO != null) maskGO.SetActive(active);
    }

    private void Update()
    {
        if (outlineMat == null) { EnsureOutline(); if (outlineMat == null) return; }

        // Track inspector tweaks while running
        if (outlineGO != null)
            outlineGO.transform.localScale = Vector3.one * OutlineScale;

        float t = Mathf.Sin(Time.unscaledTime * Speed) * 0.5f + 0.5f;
        // additive blend(SrcAlpha One)이라 RGB는 그대로, alpha만 GlowStrength로 강도 조절
        var rgb = Color.Lerp(ColorA, ColorB, t);
        rgb.a = Mathf.Clamp01(Mathf.Lerp(ColorA.a, ColorB.a, t) * GlowStrength);
        outlineMat.SetColor(colorPropertyId, rgb);
        if (maskMat != null && maskMat.HasProperty(colorPropertyId))
            maskMat.SetColor(colorPropertyId, rgb);
    }
}
