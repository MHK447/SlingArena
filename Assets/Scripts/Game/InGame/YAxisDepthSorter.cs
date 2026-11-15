using UnityEngine;
using UnityEngine.Rendering;

// 유닛(플레이어/적)을 Y축 기준으로 정렬한다 — 아래(Y가 작은) 유닛이 위에 그려지도록
// 매 프레임 SortingGroup.sortingOrder 를 갱신. 유닛 프리팹 루트(SortingGroup 보유)에 붙인다.
// 적/아군이 모두 같은 BaseOrder/Precision 을 쓰므로 같은 정렬 기준으로 자연스럽게 Y 순으로 겹친다.
[RequireComponent(typeof(SortingGroup))]
public class YAxisDepthSorter : MonoBehaviour
{
    // 기준 정렬값. Precision·Y 범위(대략 -9~10)를 고려해, 배경/소품(<=222)과 이펙트/UI(>=2000) 사이의
    // 깨끗한 밴드[약 300~490]에 머물도록 400 으로 둔다.
    [SerializeField]
    private int BaseOrder = 400;

    // Y 1 당 정렬값 변화량. 클수록 가까운 Y 차이도 분리되지만 정렬 밴드가 넓어진다.
    [SerializeField]
    private int Precision = 10;

    private SortingGroup sortingGroup;

    private void Awake()
    {
        sortingGroup = GetComponent<SortingGroup>();
    }

    // 이동(Update)이 끝난 뒤 위치 기준으로 정렬해야 하므로 LateUpdate 에서 갱신.
    private void LateUpdate()
    {
        if (sortingGroup == null) return;

        sortingGroup.sortingOrder = BaseOrder - Mathf.RoundToInt(transform.position.y * Precision);
    }
}
