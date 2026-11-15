using UnityEngine;
using BanpoFri;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

[UIPath("UI/Popup/PopupWelComeBack", true)]
public class PopupWelComeBack : UIBase
{



    [SerializeField]
    private Button ReLoadBtn;

    [SerializeField]
    private TextMeshProUGUI WaveText;


    protected override void Awake()
    {
        base.Awake();

        ReLoadBtn.onClick.AddListener(OnClickReLoad);
    }

    public void Init()
    {
        bool isHardResume = GameRoot.Instance.UserData.IngameResumeData.Difficultytype == (int)DifficultyType.Hard;
        int stageIdx = isHardResume
            ? GameRoot.Instance.UserData.IngameResumeData.Hardmodestageidx
            : GameRoot.Instance.UserData.Stageidx.Value;

        // int waveCount = isHardResume
        //     ? Tables.Instance.GetTable<Hard_WaveInfo>().DataList.FindAll(x => x.stage == stageIdx).Count
        //     : Tables.Instance.GetTable<WaveInfo>().DataList.FindAll(x => x.stage == stageIdx).Count;

        // WaveText.text = Tables.Instance.GetTable<Localize>().GetString("str_desc_wave") +
        // $"{GameRoot.Instance.UserData.Waveidx.Value}/{waveCount}";
    }

    private void OnClickReLoad()
    {
        var lobbyPage = GameRoot.Instance.UISystem.GetUI<PageLobbyBattle>();
        lobbyPage?.Hide();

        bool isHardResume = GameRoot.Instance.UserData.IngameResumeData.Difficultytype == (int)DifficultyType.Hard;
        int stageIdx = isHardResume
            ? GameRoot.Instance.UserData.IngameResumeData.Hardmodestageidx
            : GameRoot.Instance.UserData.Stageidx.Value;

        List<TpParameter> parameters = new List<TpParameter>();
        parameters.Add(new TpParameter("Stage", stageIdx));
        GameRoot.Instance.PluginSystem.AnalyticsProp.AllEvent(IngameEventType.None, "m_stage_start", parameters);

        bool resumeStarted = GameRoot.Instance.UserData.StartBattleWithInGameResumeData();
        if (!resumeStarted)
        {
            // GameRoot.Instance.InGameSystem.GetInGame<InGameBase>().Stage.StartBattle();
        }


        if (!GameRoot.Instance.ShopSystem.NoInterstitialAds.Value && GameRoot.Instance.ShopSystem.IsAdsRetentionPassed())
        {
            GameRoot.Instance.PluginSystem.ADProp.ShowBanner();
        }

        Hide();
    }
}

