using UnityEngine;
using BanpoFri;
using UniRx;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
[UIPath("UI/Popup/PopupStageGiveup")]
public class PopupStageGiveup : UIBase
{
    [SerializeField]
    private Button GiveupBtn;

    [SerializeField]
    private Button QuiteBtn;


    protected override void Awake()
    {
        base.Awake();
        GiveupBtn.onClick.AddListener(OnClickGiveup);
        QuiteBtn.onClick.AddListener(Hide);
    }


    protected override void OnEnable()
    {
        base.OnEnable();
        GameRoot.Instance.GameSpeedSystem.StopGameSpeed(true);
    }


    public void OnClickGiveup()
    {
        Hide();
        // 자진 퇴장 시 웰컴백(재진입) 데이터 초기화
        GameRoot.Instance.UserData.ClearInGameResumeData(true);

        var getrecordcount = GameRoot.Instance.UserData.GetRecordCount(Config.RecordCountKeys.StageFailedCount,
          GameRoot.Instance.UserData.Stageidx.Value);

        var failtotal = GameRoot.Instance.UserData.GetRecordCount(Config.RecordCountKeys.StageLifeTime,
           GameRoot.Instance.UserData.Stageidx.Value);

        var starttime = GameRoot.Instance.InGameSystem.GetInGame<InGameBase>().Stage.StageStartTime;

        List<TpParameter> parameters = new List<TpParameter>();
        parameters.Add(new TpParameter("stage", GameRoot.Instance.UserData.Stageidx.Value));
        parameters.Add(new TpParameter("waveidx", GameRoot.Instance.UserData.Waveidx.Value));
        parameters.Add(new TpParameter("time_all", failtotal + starttime));
        parameters.Add(new TpParameter("try", getrecordcount));
        GameRoot.Instance.PluginSystem.AnalyticsProp.AllEvent(IngameEventType.None, "m_stage_fail", parameters);

        GameRoot.Instance.InGameSystem.GetInGame<InGameBase>().Stage.ReturnMainScreen();
    }

    public override void Hide()
    {
        base.Hide();
        GameRoot.Instance.GameSpeedSystem.StopGameSpeed(false);
    }
}
