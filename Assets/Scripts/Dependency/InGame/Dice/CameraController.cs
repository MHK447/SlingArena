using UnityEngine;

[ExecuteAlways]
public class CameraController : MonoBehaviour
{

    [SerializeField]
    private Camera MainCamera;

    public Camera GetMainCam { get { return MainCamera; } }

    // 카메라가 따라갈 대상(플레이어 유닛 등). SetFollowTarget 으로 지정.
    [SerializeField]
    private Vector3 FollowOffset = Vector3.zero;

    // 0 이면 즉시 스냅, 값이 클수록 부드럽게 따라감.
    [SerializeField]
    private float FollowSmoothTime = 0.15f;

    private Transform followTarget;
    private Vector3 followVelocity;

    // 맵 이동 시퀀스(카메라 팬) 동안 팔로우를 잠시 끈다.
    // 시퀀스가 카메라를 직접 이동시키는 동안 SmoothDamp 팔로우와 서로 밀고 당기는 것을 방지.
    private bool followEnabled = true;

    public void SetFollowTarget(Transform target)
    {
        followTarget = target;
    }

    public void SetFollowEnabled(bool enabled)
    {
        followEnabled = enabled;
        // 재개 시 SmoothDamp 관성으로 튀지 않도록 속도 리셋.
        if (enabled) followVelocity = Vector3.zero;
    }

    private void LateUpdate()
    {
        if (MainCamera == null) return;

        if (!followEnabled) return;

        if (followTarget == null) return;

        var camTr = MainCamera.transform;

        // X/Y 만 대상에 맞추고 Z(카메라 깊이)는 유지.
        Vector3 desired = new Vector3(
            followTarget.position.x + FollowOffset.x,
            followTarget.position.y + FollowOffset.y,
            camTr.position.z);

        if (FollowSmoothTime > 0f && Application.isPlaying)
            camTr.position = Vector3.SmoothDamp(camTr.position, desired, ref followVelocity, FollowSmoothTime);
        else
            camTr.position = desired;
    }

    // 750x1334(w/h ≈ 0.5622)에서 ortho=10.5 가 되는 구도를 기준.
    // 어떤 해상도에서도 가로 시야(월드 폭)를 기준과 동일하게 유지한다 (width-fit).
    private const float ReferenceWidth = 750f;
    private const float ReferenceHeight = 1334f;
    private const float ReferenceOrthoSize = 10.5f;

    // 기준 카메라 포지션 — X/Y는 0, Z만 -3.3.
    private static readonly Vector3 ReferencePosition = new Vector3(0f, 0f, -3.3f);

    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;

#if UNITY_EDITOR
    // 에디터 전용 배너 미리보기 — 실제 광고 SDK는 에디터에서 표시되지 않으므로 BannerBottom 프리팹을 띄워서 구도 확인.
    private const string BannerBottomPrefabPath = "Assets/MaxSdk/Prefabs/BannerBottom.prefab";
    private GameObject editorBannerStub;
#endif

    private void OnEnable()
    {
        // 진입 시 강제 갱신 — 캐시 초기화로 다음 Update에서 ApplySubCameraSize가 반드시 호출되도록.
        lastScreenWidth = -1;
        lastScreenHeight = -1;
        ApplySubCameraSize();

#if UNITY_EDITOR
        EnsureEditorBannerStub();
#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        RemoveEditorBannerStub();
#endif
    }

    private void Start()
    {
        ApplySubCameraSize();
    }

    private void Update()
    {
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            ApplySubCameraSize();
    }

#if UNITY_EDITOR
    // 인스펙터 값 변경 시에도 Edit 모드에서 즉시 반영.
    private void OnValidate()
    {
        if (!isActiveAndEnabled) return;
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            lastScreenWidth = -1;
            lastScreenHeight = -1;
            ApplySubCameraSize();
        };
    }

    private bool ShouldShowEditorBanner()
    {
        // Play 모드에서는 NoInterstitialAds 구매 여부를 존중. Edit 모드에서는 GameRoot가 없으니 항상 표시.
        if (!Application.isPlaying) return true;
        if (GameRoot.Instance == null || GameRoot.Instance.ShopSystem == null) return true;
        return !GameRoot.Instance.ShopSystem.NoInterstitialAds.Value;
    }

    private void EnsureEditorBannerStub()
    {
        if (!ShouldShowEditorBanner())
        {
            RemoveEditorBannerStub();
            return;
        }

        if (editorBannerStub != null)
        {
            if (!editorBannerStub.activeSelf) editorBannerStub.SetActive(true);
            return;
        }

        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(BannerBottomPrefabPath);
        if (prefab == null) return;

        editorBannerStub = Instantiate(prefab);
        editorBannerStub.name = "[EditorOnly] BannerBottom Preview";
        editorBannerStub.hideFlags = HideFlags.DontSave;
    }

    private void RemoveEditorBannerStub()
    {
        if (editorBannerStub == null) return;

        if (Application.isPlaying) Destroy(editorBannerStub);
        else DestroyImmediate(editorBannerStub);
        editorBannerStub = null;
    }
#endif

    private void ApplySubCameraSize()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        if (MainCamera == null || Screen.height <= 0) return;

        float referenceAspect = ReferenceWidth / ReferenceHeight;
        float currentAspect = (float)Screen.width / Screen.height;
        if (currentAspect <= 0f) return;

        // 기준 해상도에서 보이는 월드 절반 폭 (10.5 * 750/1334).
        float referenceHalfWidth = ReferenceOrthoSize * referenceAspect;

        // 가로 시야를 기준과 동일하게 맞춤 — 세로가 길수록 size가 커지고, 가로가 길수록 작아진다.
        MainCamera.orthographicSize = referenceHalfWidth / currentAspect;
        MainCamera.transform.position = ReferencePosition;
    }
}

