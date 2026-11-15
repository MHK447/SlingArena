using System.Collections.Generic;
using UnityEngine;

// StageMap_Forest_1 의 Map_1/Map_2/Map_3 처럼 일렬로 늘어선 자식 타일들을
// 추적 대상(기본: PlayerUnitBase, 없으면 Main Camera) 의 X 좌표 기준으로
// 가장 멀어진 타일을 반대편으로 옮겨 무한 스크롤 효과를 만든다.
// 타일의 localPosition.y / Z / 회전 / 스케일 은 일절 건드리지 않는다 (x 만 이동).
public class InfiniteMapTileScroller : MonoBehaviour
{
    [SerializeField, Tooltip("순환시킬 타일 목록. 비워두면 자식 중 이름이 'Map_' 로 시작하는 것을 자동 사용.")]
    private List<Transform> tiles = new List<Transform>();

    [SerializeField, Tooltip("추적 대상. 비워두면 런타임에 PlayerUnitBase 또는 메인 카메라를 자동으로 찾는다.")]
    private Transform target;

    [SerializeField, Tooltip("한 타일의 폭. 0 이면 시작 시 타일 사이 최소 간격을 자동 측정한다.")]
    private float tileWidth = 0f;

    private bool initialized = false;

    private void Start()
    {
        InitializeTiles();
    }

    private void InitializeTiles()
    {
        if (tiles == null) tiles = new List<Transform>();

        // 비어 있다면 자식 중 "Map_" 프리픽스를 자동 수집.
        if (tiles.Count == 0)
        {
            foreach (Transform child in transform)
            {
                if (child != null && child.name.StartsWith("Map_"))
                    tiles.Add(child);
            }
        }

        if (tiles.Count < 2) return;

        if (tileWidth <= 0.0001f)
        {
            var sorted = new List<Transform>(tiles);
            sorted.Sort((a, b) => a.position.x.CompareTo(b.position.x));
            float minSpacing = float.MaxValue;
            for (int i = 1; i < sorted.Count; i++)
            {
                if (sorted[i] == null || sorted[i - 1] == null) continue;
                float d = sorted[i].position.x - sorted[i - 1].position.x;
                if (d > 0.0001f && d < minSpacing) minSpacing = d;
            }
            if (minSpacing < float.MaxValue) tileWidth = minSpacing;
        }

        initialized = tiles.Count >= 2 && tileWidth > 0.0001f;
    }

    private void LateUpdate()
    {
        if (!initialized)
        {
            InitializeTiles();
            if (!initialized) return;
        }

        if (target == null)
        {
            AutoFindTarget();
            if (target == null) return;
        }

        float pivotX = target.position.x;
        int N = tiles.Count;

        // 모듈러(Repeat) 방식 무한 스크롤 — 각 타일을 피벗 중심 밴드 [pivot - band/2, pivot + band/2) 안으로 감는다.
        // 예전의 "가장 먼 타일을 반대편으로 보내는" 방식은 밴드 폭이 점프 거리보다 좁으면
        // (타일 수가 적을 때) 좌우로 무한 진동해 타일이 튀거나 맵이 비어 보이는 원인이 됐다.
        // 이 방식은 어떤 피벗 위치에서도 해가 유일해서 진동이 원천적으로 불가능하다.
        float bandWidth = N * tileWidth;
        float bandStart = pivotX - bandWidth * 0.5f;
        for (int i = 0; i < N; i++)
        {
            var t = tiles[i];
            if (t == null) continue;
            float wrappedX = bandStart + Mathf.Repeat(t.position.x - bandStart, bandWidth);
            if (!Mathf.Approximately(wrappedX, t.position.x))
                t.position = new Vector3(wrappedX, t.position.y, t.position.z);
        }
    }

    private void AutoFindTarget()
    {
        // DungeonMagicKnight: 플레이어 유닛(PlayerUnitGroup 첫 유닛)을 우선 추적, 없으면 메인 카메라 폴백.
        if (GameRoot.IsInit())
        {
            var stage = GameRoot.Instance.InGameSystem.GetInGame<InGameBase>()?.Stage;
            var playerunit = stage != null ? stage.GetPlayerUnit : null;
            if (playerunit != null)
            {
                target = playerunit.transform;
                return;
            }
        }

        var cam = Camera.main;
        if (cam != null)
            target = cam.transform;
    }
}
