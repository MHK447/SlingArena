using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class ScreenPositionPinner : MonoBehaviour
{
    public enum HorizontalAnchor { Left, Center, Right }
    public enum VerticalAnchor { Bottom, Center, Top }

    [SerializeField] private HorizontalAnchor horizontalAnchor = HorizontalAnchor.Center;
    [SerializeField] private VerticalAnchor verticalAnchor = VerticalAnchor.Top;

    [Tooltip("앵커 모서리/중심에서 떨어진 절대 픽셀 오프셋 (해상도 무관 고정)")]
    [SerializeField] private Vector2 pixelOffset;

    [SerializeField] private bool captureOnStart = true;

    private RectTransform rt;
    private Canvas rootCanvas;
    private RectTransform parentRT;
    private int lastW, lastH;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null) rootCanvas = rootCanvas.rootCanvas;
        parentRT = rt.parent as RectTransform;
    }

    private void Start()
    {
        if (captureOnStart) CaptureCurrentAsReference();
        Apply();
    }

    private void Update()
    {
        if (Screen.width != lastW || Screen.height != lastH) Apply();
    }

    public void CaptureCurrentAsReference()
    {
        Camera cam = (rootCanvas != null && rootCanvas.renderMode   != RenderMode.ScreenSpaceOverlay)
            ? rootCanvas.worldCamera : null;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, rt.position);

        float anchorX = GetAnchorX(Screen.width);
        float anchorY = GetAnchorY(Screen.height);
        pixelOffset = new Vector2(screen.x - anchorX, screen.y - anchorY);
    }

    private float GetAnchorX(float w)
    {
        switch (horizontalAnchor)
        {
            case HorizontalAnchor.Left: return 0f;
            case HorizontalAnchor.Right: return w;
            default: return w * 0.5f;
        }
    }

    private float GetAnchorY(float h)
    {
        switch (verticalAnchor)
        {
            case VerticalAnchor.Bottom: return 0f;
            case VerticalAnchor.Top: return h;
            default: return h * 0.5f;
        }
    }

    private void Apply()
    {
        if (parentRT == null) return;
        lastW = Screen.width;
        lastH = Screen.height;

        Vector2 targetScreen = new Vector2(
            GetAnchorX(Screen.width) + pixelOffset.x,
            GetAnchorY(Screen.height) + pixelOffset.y);

        Camera cam = (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? rootCanvas.worldCamera : null;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRT, targetScreen, cam, out var local))
            rt.anchoredPosition = local;
    }
}
