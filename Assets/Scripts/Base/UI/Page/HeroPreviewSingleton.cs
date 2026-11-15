using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class HeroPreviewSingleton
{
    private static GameObject Instance;
    private static AsyncOperationHandle<GameObject> Handle;
    private static bool HasHandle;
    private static string CurrentPrefabName;
    private static RenderTexture SharedRT;

    // DiceShow 전용 - diceidx별로 인스턴스를 풀링. 같은 idx 재호출 시 재활성화로 재활용.
    private static readonly Dictionary<int, GameObject> DicePool = new Dictionary<int, GameObject>();
    private static readonly Dictionary<int, AsyncOperationHandle<GameObject>> DicePoolHandles = new Dictionary<int, AsyncOperationHandle<GameObject>>();
    private static int CurrentDiceIdx = -1;

    public static RenderTexture SharedTexture => SharedRT;

    public static void Show(
        string prefabName,
        Camera previewCam,
        RawImage targetImage,
        Vector2Int textureSize,
        Vector3 localOffset)
    {
        if (previewCam == null || targetImage == null || string.IsNullOrEmpty(prefabName))
            return;

        EnsureRT(textureSize);
        previewCam.targetTexture = SharedRT;
        targetImage.texture = SharedRT;
        // 씬 기본값이 disabled(화면 덮임 방지) → RT가 연결된 동안만 카메라를 켠다.
        previewCam.enabled = true;

        var camTransform = previewCam.transform;

        if (Instance != null && CurrentPrefabName == prefabName)
        {
            Instance.transform.SetParent(camTransform, false);
            Instance.transform.SetLocalPositionAndRotation(localOffset, Quaternion.identity);
            return;
        }

        ReleaseInstance();
        Clear();
        CurrentPrefabName = prefabName;

        Handle = Addressables.InstantiateAsync(prefabName, camTransform);
        HasHandle = true;
        Handle.Completed += (h) =>
        {
            if (h.Status != AsyncOperationStatus.Succeeded || h.Result == null)
                return;

            Instance = h.Result;
            Instance.transform.SetLocalPositionAndRotation(localOffset, Quaternion.identity);

            if (Instance.TryGetComponent<Animator>(out var anim))
                anim.Play("Idle");
        };
    }

    public static void DetachFrom(Camera previewCam, RawImage targetImage)
    {
        if (targetImage != null && targetImage.texture == SharedRT)
            targetImage.texture = null;
        if (previewCam != null && previewCam.targetTexture == SharedRT)
        {
            previewCam.targetTexture = null;
            // targetTexture 없이 켜진 카메라는 화면 전체를 덮어버린다 → 분리 시 끔.
            previewCam.enabled = false;
        }
    }

    public static void ReleaseInstance()
    {
        if (Instance != null)
        {
            Addressables.ReleaseInstance(Instance);
            Instance = null;
        }

        if (HasHandle)
        {
            if (Handle.IsValid() && !Handle.IsDone)
                Addressables.Release(Handle);
            HasHandle = false;
        }

        CurrentPrefabName = null;
    }

    public static void ReleaseRT()
    {
        if (SharedRT == null)
            return;

        SharedRT.Release();
        Object.Destroy(SharedRT);
        SharedRT = null;
    }

    private static void EnsureRT(Vector2Int size)
    {
        if (SharedRT != null
            && SharedRT.width == size.x
            && SharedRT.height == size.y)
        {
            if (!SharedRT.IsCreated())
                SharedRT.Create();
            return;
        }

        ReleaseRT();

        SharedRT = new RenderTexture(size.x, size.y, 16, RenderTextureFormat.ARGB32);
        SharedRT.name = "HeroPreviewRT_Singleton";
        SharedRT.Create();
    }


    // 다이스 한 개를 카메라/RT에 렌더. 같은 diceidx 호출 시 캐시된 인스턴스 재활성화.
    // 다른 diceidx로 전환 시 이전 인스턴스는 비활성화만 (Destroy 안 함).
    public static void DiceShow(
          int diceidx,
          Camera previewCam,
          RawImage targetImage,
          Vector2Int textureSize,
          Vector3 localOffset)
    {
        if (previewCam == null || targetImage == null) return;

        EnsureRT(textureSize);
        previewCam.targetTexture = SharedRT;
        targetImage.texture = SharedRT;
        // 씬 기본값이 disabled(화면 덮임 방지) → RT가 연결된 동안만 카메라를 켠다.
        previewCam.enabled = true;

        var camTransform = previewCam.transform;

        // Show() 잔재 정리.
        ReleaseInstance();
        CurrentPrefabName = "Show_Dice";

        // 다른 다이스가 활성화 중이면 비활성화.
        if (CurrentDiceIdx >= 0 && CurrentDiceIdx != diceidx
            && DicePool.TryGetValue(CurrentDiceIdx, out var prev) && prev != null)
        {
            prev.SetActive(false);
        }

        CurrentDiceIdx = diceidx;

        // 캐시 히트: 위치만 재설정하고 활성화.
        if (DicePool.TryGetValue(diceidx, out var cached) && cached != null)
        {
            cached.transform.SetParent(camTransform, false);
            cached.transform.SetLocalPositionAndRotation(localOffset, Quaternion.identity);
            cached.SetActive(true);
            return;
        }

        // 캐시 미스: Addressables로 인스턴스화 후 풀에 등록.
        string prefabName = $"Show_Dice_{diceidx}";
        int requestedIdx = diceidx;
        var handle = Addressables.InstantiateAsync(prefabName, camTransform);
        DicePoolHandles[requestedIdx] = handle;
        handle.Completed += (h) =>
        {
            if (h.Status != AsyncOperationStatus.Succeeded || h.Result == null)
                return;

            var go = h.Result;
            DicePool[requestedIdx] = go;
            go.transform.SetLocalPositionAndRotation(localOffset, Quaternion.identity);

            var comp = go.GetComponent<DiceShowComponent>();
            if (comp != null) comp.Set(requestedIdx);

            // 콜백 도착 시점에 이미 다른 idx로 전환됐으면 비활성화 처리.
            if (CurrentDiceIdx != requestedIdx)
                go.SetActive(false);
        };
    }

    // 풀의 모든 다이스를 비활성화. 다음 DiceShow에서 재활용됨.
    public static void Clear()
    {
        foreach (var kv in DicePool)
        {
            if (kv.Value != null)
                kv.Value.SetActive(false);
        }
        CurrentDiceIdx = -1;
    }

    // 풀의 모든 다이스를 완전히 해제 (Addressables 회수). 씬 종료 등에서만 사용.
    public static void ReleaseDicePool()
    {
        foreach (var kv in DicePool)
        {
            if (kv.Value != null)
                Addressables.ReleaseInstance(kv.Value);
        }
        DicePool.Clear();

        foreach (var kv in DicePoolHandles)
        {
            var h = kv.Value;
            if (h.IsValid() && !h.IsDone)
                Addressables.Release(h);
        }
        DicePoolHandles.Clear();
        CurrentDiceIdx = -1;
    }
}
