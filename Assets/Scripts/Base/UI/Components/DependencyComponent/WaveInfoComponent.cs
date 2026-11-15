using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

// [임시 비활성화] 웨이브 진행 HUD — 웨이브 시스템 제거(스폰이 appear_time 순차 방식으로 변경)로
// 표시할 데이터(WaveInfo.wave)가 사라져 일단 꺼 둔다. 진행도 디자인 확정 후 재작성 예정.
// 직렬화 필드는 프리팹 참조가 깨지지 않도록 유지한다.
public class WaveInfoComponent : MonoBehaviour
{
    [SerializeField]
    private List<Image> WaveImgList = new List<Image>();

    [SerializeField]
    private GameObject NodeRoot;

    [SerializeField]
    private Slider Progress;

    [SerializeField]
    private GameObject ProgressRateArrow;

    [SerializeField]
    private float ProgressDuration = 1.5f;

    [SerializeField]
    private float ProgressDelay = 0.5f;

    [SerializeField]
    private float NodeRootHideDelay = 1f;

    private void Awake()
    {
        // 웨이브 데이터가 없어 표시할 내용이 없다 — HUD 자체를 끈다.
        gameObject.SetActive(false);
    }

    // ── 외부(프리팹/이전 코드) 호출 호환용 no-op 스텁 ──
    public void Init()
    {
        gameObject.SetActive(false);
    }

    public void PlayInitAnimation() { }

    public void SetNextWave(bool animate) { }

    public void UpdateProgress(bool animate, float delayOverride = -1f) { }
}
