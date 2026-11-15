using UnityEngine;
using BanpoFri;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using TMPro;
using UniRx;

public class LobbyCardCastleComponent : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI HpText;

    [SerializeField]
    private TextMeshProUGUI CastleHpDescText;

    [SerializeField]
    private TextMeshProUGUI CastleLevelText;

    [SerializeField]
    private Button CastleBtn;

    [SerializeField]
    private TextMeshProUGUI CostText;

    [SerializeField]
    private GameObject EffectArrowObj;

    void Awake()
    {
        CastleBtn.onClick.AddListener(OnclickLevelUp);

        GameRoot.Instance.UserData.Castlelevel.Subscribe(x =>
        {
            Refresh();
        }).AddTo(this);

        GameRoot.Instance.UserData.Money.Subscribe(x =>
        {
            Refresh();
        }).AddTo(this);
    }

    public void Init()
    {
        Refresh();

        ProjectUtility.SetActiveCheck(EffectArrowObj, false);
    }

    private void Refresh()
    {
        var playersystem = GameRoot.Instance.PlayerSystem;

        SetHp();

        // 맥스 레벨이면 비용 대신 MAX 표시 + 버튼 비활성.
        bool ismax = playersystem.IsCastleMaxLevel();

        CostText.color = playersystem.GetCastleLevelUpCost() <= GameRoot.Instance.UserData.Money.Value ? Color.white : Color.red;

        CastleLevelText.text = Tables.Instance.GetTable<Localize>().GetFormat("str_caslte_level_Text", GameRoot.Instance.UserData.Castlelevel.Value);

        CastleHpDescText.text = Tables.Instance.GetTable<Localize>().GetFormat("str_caslte_level_hp", GameRoot.Instance.UserData.Castlelevel.Value
          , (int)PlayerSystem.CastleMaxLevel);

        CostText.text = ismax ? "MAX" : playersystem.GetCastleLevelUpCost().ToString();
        CastleBtn.interactable = !ismax;
    }

    public void SetHp()
    {
        HpText.text = GameRoot.Instance.PlayerSystem.GetCastleHp().ToString();
    }

    public void OnclickLevelUp()
    {
        // 레벨업 성공 시 Castlelevel 구독으로 UI 갱신. 맥스 레벨/재화 부족이면 아무것도 안 함.
        var issuccess = GameRoot.Instance.PlayerSystem.CastleLevelUp();

        if (issuccess)
        {
            ProjectUtility.SetActiveCheck(EffectArrowObj, true);

            Refresh();

            GameRoot.Instance.WaitTimeAndCallback(2f, () =>
            {
                ProjectUtility.SetActiveCheck(EffectArrowObj, false);
            });
        }
    }
}
