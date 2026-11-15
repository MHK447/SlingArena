using UnityEngine;
using BanpoFri;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class MoveMapController : MonoBehaviour
{
    [SerializeField]
    private List<MoveMapComponent> MoveMapComponentList = new List<MoveMapComponent>();

    [SerializeField]
    private GameObject MoveMapEffectObj;

    private bool isMoving = false;

    private PlayerUnitBase PlayerUnit;

    // PlayerUnit이 화면(카메라)에 대해 유지해야 할 월드 X 오프셋과 고정 Y.
    // 뷰포트 프로젝션(원근/zoom/aspect)에 의존하지 않고 카메라 트랜스폼과 1:1 트랜슬레이션으로 따라가도록 한다.
    private bool hasPlayerAnchor = false;
    private float playerCameraOffsetX = 0f;
    private float fixedWorldY = 0f;

    [SerializeField]
    private float attackViewportMinX = 0.05f;

    [SerializeField]
    private float attackViewportMaxX = 0.95f;

    // 타일(스프라이트의 부모 GameObject = Map_Middle 등) 단위 무한 스크롤 리사이클.
    // MoveMapComponent의 로컬 wrap은 그대로 두고, 부모 타일을 카메라 주위에 항상 배치되도록 재배치한다.
    [SerializeField]
    private bool wrapTilesAroundCamera = true;

    private List<Transform> tileParents;
    private float tileWidth = 0f;
    private bool tilesInitialized = false;

    // 직전 프레임의 카메라 X. 이동 중 카메라 X 변화량을 그대로 플레이어에 적용해 밀림 방지.
    private float lastTrackedCameraX = float.NaN;

    public bool IsMoving => isMoving;

    // 실제로 이동되는 카메라(또는 그 부모) 트랜스폼을 반환.
    // InGameBaseStage가 이동시키는 PanAndZoom 트랜스폼을 우선 사용하고, 없으면 Camera.main으로 폴백.
    private Transform GetTrackedCameraTransform()
    {
        var panZoom = GameRoot.Instance != null && GameRoot.Instance.InGameSystem != null
            ? GameRoot.Instance.InGameSystem.CurInGame?.GetMainCam
            : null;
        if (panZoom != null) return panZoom.transform;

        var cam = Camera.main;
        return cam != null ? cam.transform : null;
    }

    // 카메라/맵 이동 이후 프레임 끝에서 보정해야 한 프레임 늦지 않게 따라옴.
    private void LateUpdate()
    {
        // 매 프레임, 카메라가 어디에 있든 배경 타일이 항상 카메라 주위에 남아 있도록 보정.
        if (wrapTilesAroundCamera)
        {
            RecycleTilesAroundCamera();
        }

        // DungeonMagicKnight: 이동 시퀀스 중에만 플레이어를 카메라에 고정.
        // 평소(채굴/조이스틱)에는 플레이어가 자유롭게 움직여야 하므로 앵커를 강제하지 않는다.
        if (!isMoving)
        {
            var idleCamTr = GetTrackedCameraTransform();
            lastTrackedCameraX = idleCamTr != null ? idleCamTr.position.x : float.NaN;
            return;
        }

        if (PlayerUnit == null) return;

        if (!hasPlayerAnchor) return;

        var camTr = GetTrackedCameraTransform();
        if (camTr == null) return;

        float curCamX = camTr.position.x;
        float deltaCamX = float.IsNaN(lastTrackedCameraX) ? 0f : curCamX - lastTrackedCameraX;
        lastTrackedCameraX = curCamX;

        // 공격(채굴) 중에는 자유 이동을 허용하되, 카메라 X 변화량은 매 프레임 그대로 플레이어에 적용해
        // 카메라 이동 속도와 1:1로 따라가게 한다 (이동 중 카메라보다 밀리는 현상 방지).
        if (PlayerUnit.IsAttacking)
        {
            if (deltaCamX != 0f)
            {
                PlayerUnit.ApplyCameraDeltaX(deltaCamX);
            }

            var cam = Camera.main;
            if (cam == null) return;

            Vector3 vp = cam.WorldToViewportPoint(PlayerUnit.transform.position);
            float clampedX = Mathf.Clamp(vp.x, attackViewportMinX, attackViewportMaxX);
            if (!Mathf.Approximately(clampedX, vp.x))
            {
                Vector3 clampViewportPos = new Vector3(clampedX, vp.y, vp.z);
                Vector3 clampWorldPos = cam.ViewportToWorldPoint(clampViewportPos);
                PlayerUnit.transform.position = new Vector3(clampWorldPos.x, PlayerUnit.transform.position.y, PlayerUnit.transform.position.z);
            }
            return;
        }

        // 비공격: 카메라와 동일한 X 오프셋을 매 프레임 강제 → 카메라 이동 속도와 항상 1:1로 따라간다.
        float targetX = curCamX + playerCameraOffsetX;
        PlayerUnit.transform.position = new Vector3(targetX, fixedWorldY, PlayerUnit.transform.position.z);
    }

    public void Init()
    {
        Stop();
    }

    // 스테이지 복귀 등 전체 초기화 시 호출: 고정된 PlayerUnit 참조와 카메라 오프셋 캐시를 해제.
    public void ClearPlayerUnit()
    {
        PlayerUnit = null;
        hasPlayerAnchor = false;
        playerCameraOffsetX = 0f;
        fixedWorldY = 0f;
        lastTrackedCameraX = float.NaN;
    }

    public void SetPlayerUnit(PlayerUnitBase unit)
    {
        PlayerUnit = unit;

        // 현재 위치 기준으로 카메라(실제 이동되는 트랜스폼)와의 X 오프셋을 저장.
        // 이후 이동 시퀀스 동안 카메라가 움직일 때마다 이 오프셋을 유지해 화면상 같은 자리에 머물게 한다.
        if (unit != null)
        {
            var camTr = GetTrackedCameraTransform();
            if (camTr != null)
            {
                playerCameraOffsetX = unit.transform.position.x - camTr.position.x;
                fixedWorldY = unit.transform.position.y;
                hasPlayerAnchor = true;
                lastTrackedCameraX = camTr.position.x;
            }
        }
    }

    // 이동 시퀀스 시작 직전 호출: 플레이어의 현재 위치를 새 anchor로 갱신.
    public void RebaseAnchorToCurrentPosition()
    {
        if (PlayerUnit == null) return;
        var camTr = GetTrackedCameraTransform();
        if (camTr == null) return;
        playerCameraOffsetX = PlayerUnit.transform.position.x - camTr.position.x;
        fixedWorldY = PlayerUnit.transform.position.y;
        hasPlayerAnchor = true;
        lastTrackedCameraX = camTr.position.x;
    }

    public void Move(float speedRatio = 1f)
    {
        isMoving = true;

        // 이동 시작 시점의 플레이어 위치 기준으로 앵커 재캡처.
        RebaseAnchorToCurrentPosition();

        if (MoveMapEffectObj != null)
        {
            MoveMapEffectObj.SetActive(true);
        }

        if (PlayerUnit != null)
        {
            PlayerUnit.SetRun(true);
        }

        foreach (var comp in MoveMapComponentList)
        {
            comp.SetSpeedRatio(speedRatio);
            comp.StartInfiniteMove();
        }
    }

    public void Stop()
    {
        isMoving = false;

        if (MoveMapEffectObj != null)
        {
            MoveMapEffectObj.SetActive(false);
        }

        if (PlayerUnit != null)
        {
            PlayerUnit.SetRun(false);
        }

        foreach (var comp in MoveMapComponentList)
        {
            comp.StopMove();
        }
    }

    public void Pause()
    {
        foreach (var comp in MoveMapComponentList)
        {
            comp.PauseMove();
        }
    }

    public void Resume()
    {
        foreach (var comp in MoveMapComponentList)
        {
            comp.ResumeMove();
        }
    }

    // MoveMapComponentList에 등록된 스프라이트들의 부모(타일) 목록과 한 타일의 폭(=실제 월드 간격)을 1회만 캐시.
    // 동일 부모를 공유하는 다수의 레이어 스프라이트들은 부모를 옮기면 함께 이동하므로 레이어 정합도 유지된다.
    // 1) 같은 loopRange를 공유하는 부모들 중 가장 큰 그룹만 재활용 대상으로 삼는다(단일 패럴랙스 배경 등은 제외).
    // 2) tileWidth는 실제 월드 간격을 측정해서 사용한다 — TopBattleMap의 스케일이 1이 아니어도 정확히 일치한다.
    private void EnsureTilesInitialized()
    {
        if (tilesInitialized) return;

        tileParents = new List<Transform>();

        var parentToLoopRange = new Dictionary<Transform, float>();
        for (int i = 0; i < MoveMapComponentList.Count; i++)
        {
            var comp = MoveMapComponentList[i];
            if (comp == null) continue;
            var p = comp.transform.parent;
            if (p == null) continue;
            if (!parentToLoopRange.ContainsKey(p))
            {
                parentToLoopRange[p] = comp.customRepeatPosition.x - comp.EndPos.x;
            }
        }

        // loopRange 별로 부모를 그룹화 (소수점 오차 흡수를 위해 0.01 단위로 라운딩한 키 사용).
        var groups = new Dictionary<int, List<Transform>>();
        foreach (var kv in parentToLoopRange)
        {
            int key = Mathf.RoundToInt(kv.Value * 100f);
            if (!groups.TryGetValue(key, out var bucket))
            {
                bucket = new List<Transform>();
                groups[key] = bucket;
            }
            bucket.Add(kv.Key);
        }

        // 가장 큰(=재활용 대상이 되는) 그룹만 선택.
        List<Transform> mainGroup = null;
        foreach (var kv in groups)
        {
            if (kv.Value.Count >= 2 && (mainGroup == null || kv.Value.Count > mainGroup.Count))
                mainGroup = kv.Value;
        }

        if (mainGroup == null)
        {
            tilesInitialized = true;
            return;
        }

        tileParents = mainGroup;

        // 실제 월드 간격을 측정 → 부모 트랜스폼의 스케일이 어떻든 정확히 한 칸 옮길 수 있다.
        tileParents.Sort((a, b) => a.position.x.CompareTo(b.position.x));
        float minSpacing = float.MaxValue;
        for (int i = 1; i < tileParents.Count; i++)
        {
            float d = tileParents[i].position.x - tileParents[i - 1].position.x;
            if (d > 0.0001f && d < minSpacing) minSpacing = d;
        }

        tileWidth = (minSpacing == float.MaxValue) ? 0f : minSpacing;
        tilesInitialized = (tileParents.Count >= 2 && tileWidth > 0.0001f);
    }

    // 카메라가 이동하면 가장 왼쪽 타일을 가장 오른쪽 타일 뒤로(또는 그 반대로) 옮겨 무한 스크롤을 만든다.
    // 타일 묶음의 절반 이상 카메라에서 멀어진 타일을 반대편으로 보내는 정통 무한 타일 패턴.
    private void RecycleTilesAroundCamera()
    {
        EnsureTilesInitialized();
        if (!tilesInitialized || tileParents.Count < 2) return;

        var camTr = GetTrackedCameraTransform();
        if (camTr == null) return;

        float cameraX = camTr.position.x;
        int N = tileParents.Count;

        // 모듈러(Repeat) 방식 무한 스크롤 — 각 타일을 카메라 중심 밴드 [camX - band/2, camX + band/2) 안으로 감는다.
        // 예전의 "가장 먼 타일을 반대편으로 보내는" 방식은 타일 수가 적으면 밴드 폭이 점프 거리보다 좁아
        // 좌우로 무한 진동(프레임당 safety 소진)하며 타일이 튀거나 맵이 비어 보이는 원인이 됐다.
        float bandWidth = N * tileWidth;
        float bandStart = cameraX - bandWidth * 0.5f;
        for (int i = 0; i < N; i++)
        {
            var p = tileParents[i];
            if (p == null) continue;
            float wrappedX = bandStart + Mathf.Repeat(p.position.x - bandStart, bandWidth);
            if (!Mathf.Approximately(wrappedX, p.position.x))
                p.position = new Vector3(wrappedX, p.position.y, p.position.z);
        }
    }
}
