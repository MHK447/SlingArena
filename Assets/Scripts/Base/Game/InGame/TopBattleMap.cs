using UnityEngine;
using BanpoFri;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class TopBattleMap : MonoBehaviour
{
    [SerializeField]
    private MoveMapController MapController;

    [SerializeField, Tooltip("로드한 StageMap 이 부착될 부모. 비워두면 이 컴포넌트의 transform 을 사용.")]
    private Transform StageMapRoot;

    public MoveMapController GetMapController { get { return MapController; } }

    // 현재 로드되어 있는 StageMap 인스턴스와 그 deco_idx. 동일 idx 가 다시 요청되면 재로드하지 않는다.
    private GameObject _currentMapInstance;
    private int _currentMapDecoIdx = -1;
    private bool _isLoading = false;

    public void Init()
    {
        MapController.Init();

        SetMoveMap();
    }

    public void SetMoveMap()
    {
        var stageidx = GameRoot.Instance.UserData.Stageidx.Value;
        var td = Tables.Instance.GetTable<StageInfo>().GetData(stageidx);
        if (td == null) return;

        LoadStageMap(td.stage_deco_idx);
    }

    // StageMap_{decoIdx} 어드레서블을 instantiate 하고, decoIdx 가 바뀐 경우에만 이전 맵을 ReleaseInstance 한다.
    // 같은 decoIdx 가 들어오면 기존 인스턴스를 그대로 재사용해서 생성/파괴를 발생시키지 않는다.
    private void LoadStageMap(int decoIdx)
    {
        if (decoIdx <= 0) return;

        // 같은 decoIdx 가 이미 로드되어 있으면 그대로 재사용. 혹시 비활성 상태로 남아있으면 다시 켜준다.
        if (_currentMapDecoIdx == decoIdx && _currentMapInstance != null)
        {
            if (!_currentMapInstance.activeSelf)
                _currentMapInstance.SetActive(true);
            return;
        }

        // 같은 decoIdx 가 아직 로드 중이면 그 결과를 기다리기만 한다 — 중복 발사 금지.
        if (_isLoading && _currentMapDecoIdx == decoIdx) return;
        if (_isLoading) return;

        // 이전 맵은 새 맵 로드가 끝날 때까지 유지한다 — 즉시 파괴하면 로드 동안 배경이 통째로 비어 보인다.
        var prevInstance = _currentMapInstance;
        var prevIdx = _currentMapDecoIdx;
        _currentMapInstance = null;

        _isLoading = true;
        // 중복 호출 방지를 위해 idx 는 미리 갱신.
        _currentMapDecoIdx = decoIdx;

        string key = $"StageMap_{decoIdx}";
        var parent = StageMapRoot != null ? StageMapRoot : transform;

        Addressables.InstantiateAsync(key, parent, false).Completed += (handle) =>
        {
            _isLoading = false;

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                Debug.LogError($"[TopBattleMap] Failed to load addressable: {key}");
                // 실패한 핸들도 해제하여 누수 방지. 이전 맵을 계속 보여준다.
                if (handle.IsValid()) Addressables.Release(handle);
                _currentMapInstance = prevInstance;
                _currentMapDecoIdx = prevIdx;
                return;
            }

            // 도중에 다른 맵 요청이 들어와서 decoIdx 가 바뀌었다면 방금 받은 결과는 즉시 해제.
            if (_currentMapDecoIdx != decoIdx)
            {
                Addressables.ReleaseInstance(handle.Result);
                if (prevInstance != null)
                    Addressables.ReleaseInstance(prevInstance);
                return;
            }

            // 새 맵이 준비된 뒤에 이전 맵 해제 — 전환 중 빈 화면 없이 이어진다.
            _currentMapInstance = handle.Result;
            if (prevInstance != null)
                Addressables.ReleaseInstance(prevInstance);
        };
    }

    private void ReleaseCurrentMap()
    {
        if (_currentMapInstance != null)
        {
            Addressables.ReleaseInstance(_currentMapInstance);
            _currentMapInstance = null;
        }
        _currentMapDecoIdx = -1;
    }

    private void OnDestroy()
    {
        ReleaseCurrentMap();
    }
}
