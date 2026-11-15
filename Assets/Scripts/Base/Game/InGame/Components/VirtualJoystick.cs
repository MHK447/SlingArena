using UnityEngine;
using UnityEngine.EventSystems;

// 모바일/PC 공용 플로팅 가상 조이스틱.
// - 화면(빈 영역)을 누르면 누른 위치에 조이스틱이 나타나고 배경을 Active On 한다.
// - 드래그 방향을 Direction(-1 ~ 1 정규화)으로 제공한다.
// - 손을 떼면 배경을 Active Off 하고 Direction 을 0 으로 되돌린다.
// PlayerUnitBase 가 Instance.IsActive / Instance.Direction 을 읽어 이동한다.
//
// 입력은 EventSystem 핸들러가 아니라 Update 폴링으로 처리한다.
//  - 예전에는 전체화면 Image(raycastTarget)를 최상위(Overlay 32767)로 두고 IPointerDownHandler 로 받았는데,
//    그 Image 가 팝업/버튼 위를 덮어 UI 클릭을 전부 가로챘다.
//  - 이제는 루트 Image 의 raycastTarget 을 끄고(=UI 를 막지 않음), 눌린 지점이 UI(버튼 등) 위이면
//    조이스틱을 시작하지 않는다(EventSystem.IsPointerOverGameObject). → 버튼 클릭도 되고, 버튼 위에선 조이스틱이 안 열린다.
[RequireComponent(typeof(RectTransform))]
public class VirtualJoystick : MonoBehaviour
{
    public static VirtualJoystick Instance { get; private set; }

    [Header("Refs")]
    [SerializeField] private RectTransform background; // 조이스틱 배경(터치 시 활성화)
    [SerializeField] private RectTransform handle;     // 배경 안에서 움직이는 손잡이

    [Header("Options")]
    [SerializeField] private float handleRange = 80f;             // 손잡이가 중심에서 벗어날 수 있는 최대 픽셀
    [SerializeField, Range(0f, 1f)] private float deadZone = 0.1f; // 이 값 이하 입력은 정지로 간주
    [SerializeField] private bool hideBackgroundOnStart = true;   // 시작 시 배경 숨김

    private RectTransform touchArea;
    private Canvas canvas;
    private Camera uiCam;
    private int activePointerId = NoPointer; // 현재 조이스틱을 잡고 있는 포인터(터치 fingerId, 마우스는 -1)

    private const int NoPointer = -999;
    private const int MousePointer = -1;

    // 현재 조이스틱을 누르고 있는지 여부. (터치 시 true, 떼면 false)
    public bool IsActive { get; private set; }

    // -1 ~ 1 로 정규화된 이동 방향. 누르지 않았거나 데드존 이내면 Vector2.zero.
    public Vector2 Direction { get; private set; }

    private void Awake()
    {
        Instance = this;
        touchArea = transform as RectTransform;
        canvas = GetComponentInParent<Canvas>();
        // ScreenSpaceOverlay 가 아니면 좌표 변환에 카메라가 필요하다.
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCam = canvas.worldCamera;

        if (hideBackgroundOnStart && background != null)
            background.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!IsActive)
            TryBegin();
        else
            TrackOrEnd();
    }

    // 눌리는 순간을 감지해 조이스틱을 시작한다. 단, UI(버튼/팝업) 위를 눌렀으면 무시한다.
    private void TryBegin()
    {
        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began && !IsPointerOverUI(t.fingerId))
                Begin(t.fingerId, t.position);
        }
        else if (Input.GetMouseButtonDown(0))
        {
            if (!IsPointerOverUI(MousePointer))
                Begin(MousePointer, Input.mousePosition);
        }
    }

    private void Begin(int pointerId, Vector2 screenPos)
    {
        activePointerId = pointerId;
        IsActive = true;

        if (background != null)
        {
            // 누른 위치로 배경을 이동시켜 그 자리에 조이스틱을 띄운다(플로팅).
            background.gameObject.SetActive(true);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(touchArea, screenPos, uiCam, out var localPoint))
                background.anchoredPosition = localPoint;
        }

        // 누르는 순간의 방향도 즉시 반영.
        UpdateDirection(screenPos);
    }

    // 잡고 있는 포인터를 계속 추적하고, 떼면 종료한다.
    private void TrackOrEnd()
    {
        Vector2 screenPos;
        bool released;

        if (activePointerId >= 0)
        {
            int idx = GetTouchIndex(activePointerId);
            if (idx < 0) { End(); return; } // 터치가 사라짐(취소)
            var t = Input.GetTouch(idx);
            screenPos = t.position;
            released = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
        }
        else
        {
            screenPos = Input.mousePosition;
            released = !Input.GetMouseButton(0);
        }

        UpdateDirection(screenPos);
        if (released) End();
    }

    private void UpdateDirection(Vector2 screenPos)
    {
        if (background == null) return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(background, screenPos, uiCam, out var localPoint))
            return;

        // 손잡이를 배경 반경 안으로 제한.
        float range = Mathf.Max(1f, handleRange);
        Vector2 clamped = Vector2.ClampMagnitude(localPoint, range);
        if (handle != null)
            handle.anchoredPosition = clamped;

        Vector2 dir = clamped / range;
        Direction = dir.magnitude < deadZone ? Vector2.zero : dir;
    }

    private void End()
    {
        // 손을 떼면 입력을 멈추고 조이스틱을 숨긴다.
        IsActive = false;
        activePointerId = NoPointer;
        Direction = Vector2.zero;

        if (handle != null)
            handle.anchoredPosition = Vector2.zero;
        if (background != null)
            background.gameObject.SetActive(false);
    }

    private static int GetTouchIndex(int fingerId)
    {
        for (int i = 0; i < Input.touchCount; i++)
            if (Input.GetTouch(i).fingerId == fingerId) return i;
        return -1;
    }

    // 포인터가 UI(버튼/팝업 등) 위에 있으면 true → 조이스틱을 시작하지 않는다.
    // (조이스틱 루트 Image 의 raycastTarget 은 꺼져 있어야 여기 자기 자신이 안 잡힌다.)
    private static bool IsPointerOverUI(int pointerId)
    {
        var es = EventSystem.current;
        if (es == null) return false;
        return pointerId >= 0 ? es.IsPointerOverGameObject(pointerId) : es.IsPointerOverGameObject();
    }
}
