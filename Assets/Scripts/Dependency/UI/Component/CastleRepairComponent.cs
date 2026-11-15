using UnityEngine;
using BanpoFri;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using TMPro;


[FloatUIPath("UI/InGame/CastleRepairComponent")]
[FloatingDepth((int)Config.FloatingUIDepth.CastleRepairComponent)]
public class CastleRepairComponent : InGameFloatingUI
{
    [SerializeField]
    private Button CastleRepairBtn;

    [SerializeField]
    private GameObject AdObj;

    // 튜토리얼(동적 타겟)용 — 수리 버튼 GameObject. 클릭 시 튜토리얼이 이 버튼의 onClick 을 대신 호출한다.
    public GameObject GetCastleRepairBtnObj => CastleRepairBtn != null ? CastleRepairBtn.gameObject : gameObject;

    void Awake()
    {
        CastleRepairBtn.onClick.AddListener(OnClickCastleRepair);
    }

    // 최초 사용 전(무료 사용 기록 없음)이면 true — 이때는 광고 없이 즉시 적용.
    private bool IsFirstFreeUse => GameRoot.Instance.UserData.GetRecordCount(Config.RecordCountKeys.CastleRepairFreeUse) == 0;

    private void OnEnable()
    {
        // 이미 이번 스테이지에 사용했다면 — 튜토리얼 타겟 활성화/플로팅 UI 일괄 활성화 등으로
        // 외부에서 다시 켜져도 즉시 다시 숨긴다(스테이지당 1회 재사용 방지).
        // [정리] 옛 PlayerDiceGroup 제거
        // var inGame = GameRoot.Instance.InGameSystem.GetInGame<InGameBase>();
        // var stage = inGame != null ? inGame.Stage : null;
        // if (stage != null && stage.PlayerDiceGroup != null && stage.PlayerDiceGroup.CastleRepairUsed)
        // {
        //     ProjectUtility.SetActiveCheck(gameObject, false);
        //     return;
        // }

        // 최초 노출 — 광고 아이콘을 숨긴다(무료 사용).
        // 튜토리얼(Tuto_7) 시작은 PlayerDiceGroup 이 버튼 노출 시점에 1회만 처리한다(OnEnable 재호출로 꼬이지 않게).
        ProjectUtility.SetActiveCheck(AdObj, !IsFirstFreeUse);
    }

    // 성 수리 사용(체력 풀 회복 + 10초 무적 + 스테이지당 1회 제한).
    // 최초 1회는 광고 없이 무료 적용(튜토리얼 흐름), 이후엔 리워드 광고 시청 후 적용.
    // 실제 처리/제한 관리는 PlayerDiceGroup.UseCastleRepair 가 담당.
    public void OnClickCastleRepair()
    {
        if (IsFirstFreeUse)
        {
            GameRoot.Instance.UserData.AddRecordCount(Config.RecordCountKeys.CastleRepairFreeUse, 1);
            ApplyRepair();
            return;
        }

        GameRoot.Instance.PluginSystem.ADProp.ShowRewardAD(TpAdxProp.AdRewardType.CastleRepair, (result) =>
        {
            // 광고 없음(result == false, "광고없습니다")엔 수리 적용 안 함.
            if (result) ApplyRepair();
        });
    }

    private void ApplyRepair()
    {
        // [정리] 옛 PlayerDiceGroup 제거
        // var inGame = GameRoot.Instance.InGameSystem.GetInGame<InGameBase>();
        // var stage = inGame != null ? inGame.Stage : null;
        // if (stage != null && stage.PlayerDiceGroup != null)
        //     stage.PlayerDiceGroup.UseCastleRepair();

        // 사용(튜토리얼 무료 사용 포함) 즉시 버튼 끔 — 이번 스테이지에는 다시 뜨지 않는다.
        // (UseCastleRepair 의 castleRepairUsed 플래그가 재노출을 차단하고, 다음 스테이지 Init 에서 리셋.)
        ProjectUtility.SetActiveCheck(gameObject, false);
    }
}
