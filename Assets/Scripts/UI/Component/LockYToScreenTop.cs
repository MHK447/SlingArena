using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
public class LockYToScreenTop : MonoBehaviour
{
    [Tooltip("화면 상단으로부터의 절대 스크린 픽셀 거리. 어떤 해상도에서도 이 값으로 잠김.")]
    public float screenPixelsFromTop;

    [Tooltip("에디터에서 사용자가 드래그하면 자동 캡처. 해상도 변경 프레임은 제외해서 잠금값이 안 흔들림.")]
    public bool autoCapture = true;

    RectTransform _rt;
    RectTransform _parentRT;
    Canvas _rootCanvas;
    bool _runtimeCaptured;
    int _lastScreenW, _lastScreenH;
    Vector3 _lastWorldPos;

    void OnEnable()
    {
        Cache();
        _lastScreenW = Screen.width;
        _lastScreenH = Screen.height;
        if (Application.isPlaying)
        {
            if (autoCapture && !_runtimeCaptured)
            {
                CaptureCurrent();
                _runtimeCaptured = true;
            }
        }
        else if (autoCapture && screenPixelsFromTop == 0f)
        {
            CaptureCurrent();
        }
        if (_rt != null) _lastWorldPos = _rt.position;
        Apply();
    }

#if UNITY_EDITOR
    void Update()
    {
        if (Application.isPlaying) return;
        Cache();
        if (_rt == null) return;

        bool resolutionChanged = Screen.width != _lastScreenW || Screen.height != _lastScreenH;
        _lastScreenW = Screen.width;
        _lastScreenH = Screen.height;

        // 사용자 드래그로 위치가 바뀐 경우에만 캡처 (해상도 변동 프레임 제외)
        if (autoCapture && !resolutionChanged)
        {
            float moved = Vector3.Distance(_rt.position, _lastWorldPos);
            if (moved > 0.0001f)
                CaptureCurrent();
        }

        Apply();
        _lastWorldPos = _rt.position;
    }
#endif

    void OnRectTransformDimensionsChange()
    {
        Apply();
    }

    void LateUpdate()
    {
        if (!Application.isPlaying) return;
        if (Screen.width != _lastScreenW || Screen.height != _lastScreenH)
        {
            _lastScreenW = Screen.width;
            _lastScreenH = Screen.height;
            Apply();
        }
        else
        {
            Apply();
        }
    }

    void Cache()
    {
        if (_rt == null) _rt = GetComponent<RectTransform>();
        if (_rt != null) _parentRT = _rt.parent as RectTransform;
        if (_rootCanvas == null)
        {
            var c = GetComponentInParent<Canvas>();
            _rootCanvas = c != null ? c.rootCanvas : null;
        }
    }

    public void CaptureCurrent()
    {
        Cache();
        if (_rt == null || _rootCanvas == null) return;

        Camera cam = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, _rt.position);
        screenPixelsFromTop = Screen.height - screen.y;
    }

    void Apply()
    {
        Cache();
        if (_rt == null || _parentRT == null || _rootCanvas == null) return;

        Camera cam = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera;

        // 현재 X 스크린 좌표는 보존, Y만 잠금
        Vector2 currentScreen = RectTransformUtility.WorldToScreenPoint(cam, _rt.position);
        Vector2 targetScreen = new Vector2(currentScreen.x, Screen.height - screenPixelsFromTop);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRT, targetScreen, cam, out var local))
        {
            Vector2 ap = _rt.anchoredPosition;
            ap.y = local.y;
            _rt.anchoredPosition = ap;
        }
    }
}
