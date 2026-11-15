using UnityEngine;
using BanpoFri;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

/// <summary>
/// 해상도(화면 비율)에 따라 직교 카메라의 orthographicSize를 자동 보정한다.
/// 기준: ReferenceWidth x ReferenceHeight 해상도에서 ReferenceOrthoSize 가 되도록 디자인됨.
/// (예: 720x1334 → 16.28)
///
/// width-fit 방식 — 가로 폭(월드 단위)을 기준 비율 그대로 유지한다.
/// 따라서 어떤 해상도에서도 화면 가로에 보이는 월드 범위가 동일하며,
/// 비율이 다르면 세로 방향으로만 더(혹은 덜) 보이게 된다. (가로 크롭 없음)
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class CameraResolutionFitter : MonoBehaviour
{
    [Tooltip("보정할 직교 카메라. 비워두면 같은 오브젝트의 Camera 사용.")]
    [SerializeField] private Camera targetCamera;

    [Header("Reference (디자인 기준)")]
    [Tooltip("기준 해상도 가로")]
    [SerializeField] private float referenceWidth = 720f;
    [Tooltip("기준 해상도 세로")]
    [SerializeField] private float referenceHeight = 1334f;
    [Tooltip("기준 해상도에서의 orthographicSize")]
    [SerializeField] private float referenceOrthoSize = 16.28f;

    private int lastWidth = -1;
    private int lastHeight = -1;

    private void OnEnable() => Apply();
    private void Start() => Apply();

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 인스펙터 값 변경 / 해상도 프리뷰 변경 시 다음 프레임에 다시 적용되도록 캐시 무효화.
        lastWidth = lastHeight = -1;
    }
#endif

    [ContextMenu("Apply Now")]
    public void Apply()
    {
        var cam = targetCamera != null ? targetCamera : GetComponent<Camera>();
        if (cam == null || !cam.orthographic) return;
        if (referenceHeight <= 0f || Screen.width <= 0 || Screen.height <= 0) return;

        lastWidth = Screen.width;
        lastHeight = Screen.height;

        float referenceAspect = referenceWidth / referenceHeight;
        float currentAspect = (float)Screen.width / Screen.height;
        if (currentAspect <= 0f) return;

        // [정리] 카메라 사이즈 자동조절 제거 — 인스펙터에 설정한 orthographicSize 값을 그대로 사용.
        // width-fit: (orthoSize * aspect) = 가로 half-width 를 기준값으로 고정.
        // referenceOrthoSize * referenceAspect == currentSize * currentAspect 가 되도록 풀면 아래 식.
        // cam.orthographicSize = referenceOrthoSize * (referenceAspect / currentAspect);
    }
}

