using UnityEngine;
using BanpoFri;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using DG.Tweening;
using System;
using System.Collections.Generic;
using UniRx;

[UIPath("UI/Page/PageLobbyBattle")]
public class PageLobbyBattle : CommonUIBase
{
    [SerializeField]
    private Button StartBtn;

    [SerializeField]
    private Button SettingBtn;

    [SerializeField]
    private Button AdsEquipBtn;

    [SerializeField]
    private Button StarterPackageBtn;

    // [SerializeField]
    // private Button NoAdsBtn;

    // [SerializeField]
    // private LobbyBattleBoxComponent LobbyBattleBoxComponent;

    // public LobbyBattleBoxComponent GetLobbyBattleBoxComponent { get { return LobbyBattleBoxComponent; } }

    [SerializeField]
    private GameObject BoxRoot;

    [SerializeField]
    private TextMeshProUGUI StageNameText;

    [SerializeField]
    private TextMeshProUGUI StageText;

    // [SerializeField]
    // private LobbyStageRewardGroup RewardGroup;

    [SerializeField]
    private ChapterMapComponent MapComponent;

    [SerializeField]
    private GameObject BossStageImg;

    [SerializeField]
    private GameObject HardStageImg;

    [SerializeField]
    private TextMeshProUGUI BossDiffcultlyText;

    [SerializeField]
    private Button BookBtn;

    [SerializeField]
    private Transform BookBtnIconRoot;

    [SerializeField]
    private Button AdGoldBtn;

    [SerializeField]
    private List<Image> PlayerEquipUnitImgList = new List<Image>();

    private List<Tween> UnitIdleTweenList = new List<Tween>();

    // [SerializeField]
    // private Button NoAdsBtn;

    // [SerializeField]
    // private Button VIPBtn;

    // [SerializeField]
    // private Button FreeGemBtn;

    [SerializeField]
    private List<PageIconComponent> PageIconComponents = new List<PageIconComponent>();

    // [SerializeField]
    // private HudOneTimeComponent OneTimeComponent;

    [SerializeField]
    private Image LobbyMapImage;




    private CompositeDisposable disposables = new CompositeDisposable();


    [SerializeField]
    private Image BgImage;

    [SerializeField]
    private Image TileImg;

    [SerializeField]
    private LobbyDifficultyComponent LobbyDifficultyComponent;

    private CameraController CamController;

    override protected void Awake()
    {
        base.Awake();
        StartBtn.onClick.AddListener(OnStartBtnClick);
        SettingBtn.onClick.AddListener(OnClickSetting);
        BookBtn.onClick.AddListener(OnClickBook);
        AdGoldBtn.onClick.AddListener(OnClickAdGold);
        AdsEquipBtn.onClick.AddListener(OnClickAdsEquipBtn);
        StarterPackageBtn.onClick.AddListener(OnClickStarterPackageBtn);
        //FreeGemBtn.onClick.AddListener(OnClickFreeGemBtn);
        //NoAdsBtn.onClick.AddListener(OnClickNoAdsBtn);
        GameRoot.Instance.UserData.DifficultyTypeProperty.Subscribe(x =>
        {
            SetModeState(x);
        }).AddTo(disposables);
    }

    public void OnStartBtnClick()
    {

        OnStartGo();

    }

    // 전투 시작 진행 중 플래그 — 시작 버튼 연타로 OnStartGo 가 두 번 실행돼
    // Stage.Init/PopupLevelUpReward 가 중복 실행되는 것을 막는다. 로비 재진입 시 Init()에서 해제.
    private bool battleStarting = false;

    public void OnStartGo()
    {
        if (battleStarting) return;
        battleStarting = true;

        // 배틀 시작 버튼을 누르는 즉시 메인 BGM 끄기(전투 중에는 BGM off). 로비 복귀 시 다시 재생.
        SoundPlayer.Instance.StopBGM();

        Hide();

        GameRoot.Instance.UISystem.GetUI<HUDTotal>()?.Hide();

        StartBtn.interactable = false;

        List<TpParameter> parameters = new List<TpParameter>();
        parameters.Add(new TpParameter("Stage", GameRoot.Instance.UserData.Stageidx.Value));
        GameRoot.Instance.PluginSystem.AnalyticsProp.AllEvent(IngameEventType.None, "m_stage_start", parameters);

        // GameRoot.Instance.UISystem.OpenUI<PopupInGame>(popup => popup.Init());

        GameRoot.Instance.UserData.ClearInGameResumeData(false);

        // [정리] Stage.Init() 는 게임 최초 로드 시 GameRoot 에서 1회만 호출한다.
        // 전투 재진입 시 맵 재표시/플로팅 UI 재활성화는 StartWave() 가 담당.

        // 선택을 마치면 그때부터 전투(웨이브) 시작.
        // [정리] 옛 주사위 보급 팝업(PopupLevelUpReward) 제거 — 바로 웨이브 시작
        {
            var inGame = GameRoot.Instance.InGameSystem.GetInGame<InGameBase>();
            if (inGame != null && inGame.Stage != null)
                inGame.Stage.StartWave();
        }

        if (!GameRoot.Instance.ShopSystem.NoInterstitialAds.Value && GameRoot.Instance.ShopSystem.IsAdsRetentionPassed())
        {
            GameRoot.Instance.PluginSystem.ADProp.ShowBanner();
        }
    }

    public override void OnShowBefore()
    {
        base.OnShowBefore();
        Init();
    }


    public void Init()
    {
        // 로비로 돌아오면 다시 시작할 수 있게 해제.
        battleStarting = false;

        StartBtn.interactable = true;

        // LobbyBattleBoxComponent.Init();

        // RewardGroup.Init();

        //OneTimeComponent.Init();

        LobbyDifficultyComponent.Init();

        SetStage();


        MapComponent.transform.localScale = Vector3.zero;
        MapComponent.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);

        if (GameRoot.Instance.UserData.DifficultyTypeProperty.Value == DifficultyType.Easy)
        {
            MapComponent.EasySet(GameRoot.Instance.UserData.Stageidx.Value);
        }
        else
        {
            MapComponent.HardSet(GameRoot.Instance.UserData.Hardmodestageidx.Value);
        }

        // ProjectUtility.SetActiveCheck(RewardGroup.gameObject,
        //  GameRoot.Instance.ContentsOpenSystem.ContentsOpenCheck(ContentsOpenSystem.ContentsOpenType.LobbyReward));

        // ProjectUtility.SetActiveCheck(OneTimeComponent.gameObject,
        //     GameRoot.Instance.ContentsOpenSystem.ContentsOpenCheck(ContentsOpenSystem.ContentsOpenType.OneTimeOpen)
        //     && GameRoot.Instance.OneTimeSystem.CurState.Value != OneTimeSystem.OneTimeEventState.None);

        GameRoot.Instance.GameNotification.UpdateNotification(GameNotificationSystem.NotificationCategory.CardBook);

        //ProjectUtility.SetActiveCheck(LobbyDifficultyComponent.gameObject, GameRoot.Instance.ContentsOpenSystem.ContentsOpenCheck(ContentsOpenSystem.ContentsOpenType.DifficultyOpen));

        ProjectUtility.SetActiveCheck(LobbyDifficultyComponent.gameObject, false);

        // foreach (var starter in PageIconComponents)
        // {
        //     starter.Init();
        // }


        //FreeGemBtnCheck();

        CheckAdGoldBtn();

        InitIcons();

        SetUnitImg();


        if (GameRoot.Instance.ActionQueueSystem.MainLoobyAction != null && !GameRoot.Instance.ActionQueueSystem.IsTutorialonCheck && !GameRoot.Instance.TutorialSystem.IsActive())
        {
            GameRoot.Instance.ActionQueueSystem.MainLoobyAction?.Invoke();
            GameRoot.Instance.ActionQueueSystem.MainLoobyAction = null;
        }

        GameRoot.Instance.ShopSystem.GetWeaponPackageType();


        SetPlayerPreview(GameRoot.Instance.UserData.Herogroudata.Equipplayeridx);

    }


    public void InitIcons()
    {
        //ProjectUtility.SetActiveCheck(BookBtnIconRoot.gameObject, GameRoot.Instance.ContentsOpenSystem.ContentsOpenCheck(ContentsOpenSystem.ContentsOpenType.EquipBook));
       // ProjectUtility.SetActiveCheck(NoAdsBtn.gameObject, !GameRoot.Instance.ShopSystem.NoInterstitialAds.Value &&
       // GameRoot.Instance.ContentsOpenSystem.ContentsOpenCheck(ContentsOpenSystem.ContentsOpenType.InterAdOpen) &&
        //GameRoot.Instance.ShopSystem.IsAdsRetentionPassed());

        bool alreadyPurchasedVIP = GameRoot.Instance.UserData.BuyInappIds.Contains(InAppPurchaseManager.NoAdsVIP102)
            || GameRoot.Instance.UserData.BuyInappIds.Contains(InAppPurchaseManager.NoAdsVIP103);
        int daysSinceStart = GameRoot.Instance.UserData.FirstStartTime == (default(System.DateTime)) ? -1 : (TimeSystem.GetCurTime() - GameRoot.Instance.UserData.FirstStartTime).Days;
        //ProjectUtility.SetActiveCheck(VIPBtn.gameObject, !alreadyPurchasedVIP && daysSinceStart >= 8);
    }




    public void FreeGemBtnCheck()
    {
        var count = GameRoot.Instance.UserData.GetRecordCount(Config.RecordCountKeys.AdsEquipCount);

        ProjectUtility.SetActiveCheck(AdsEquipBtn.gameObject, GameRoot.Instance.ContentsOpenSystem.ContentsOpenCheck(ContentsOpenSystem.ContentsOpenType.FreeGemBtnOpen) && count < 4);
    }

    public void OnClickSetting()
    {
        GameRoot.Instance.UISystem.OpenUI<PopupOption>(popup => popup.Set(false));
    }


    public void SetStage()
    {
        if (GameRoot.Instance.UserData.DifficultyTypeProperty.Value == DifficultyType.Easy)
        {
            var stageidx = GameRoot.Instance.UserData.Stageidx.Value;


            var td = Tables.Instance.GetTable<StageInfo>().DataList.ToList().FindAll(x => x.stage_idx == stageidx).LastOrDefault();

            var displayIdx = stageidx > 60 ? ((stageidx - 1) % 60) + 1 : stageidx;
            StageNameText.text = $"{stageidx}.{Tables.Instance.GetTable<Localize>().GetString($"str_stage_name_{displayIdx}")}";
            StageText.text = Tables.Instance.GetTable<Localize>().GetFormat("str_main_stage_desc", stageidx);
            StageNameText.color = Config.Instance.GetTextColor("White");
            ProjectUtility.SetActiveCheck(BossStageImg, td.diffiyculty == 2);
            ProjectUtility.SetActiveCheck(HardStageImg, td.diffiyculty == 1);

            LobbyMapImage.sprite = AtlasManager.Instance.GetSprite(Atlas.Atlas_Stage, $"Lobby_Map_Stage_{td.stage_deco_idx}");

            BossDiffcultlyText.text = Tables.Instance.GetTable<Localize>().GetString($"str_difficultly_stage_{td.diffiyculty}");
        }
        else
        {
            var stageidx = GameRoot.Instance.UserData.Hardmodestageidx.Value;

            // var td = Tables.Instance.GetTable<Hard_StageInfo>().DataList.ToList().FindAll(x => x.stage_idx == stageidx).LastOrDefault();

            var displayIdx = stageidx > 60 ? ((stageidx - 1) % 60) + 1 : stageidx;
            StageNameText.text = $"{stageidx}.{Tables.Instance.GetTable<Localize>().GetString($"str_hard_stage_name_{displayIdx}")}";
            StageText.text = Tables.Instance.GetTable<Localize>().GetFormat("str_main_stage_desc", stageidx);
            StageNameText.color = Config.Instance.GetTextColor("Red");
            // ProjectUtility.SetActiveCheck(BossStageImg, td.diffiyculty == 2);
            // ProjectUtility.SetActiveCheck(HardStageImg, td.diffiyculty == 1);

            // BossDiffcultlyText.text = Tables.Instance.GetTable<Localize>().GetString($"str_difficultly_stage_{td.diffiyculty}");
        }
    }


    public void SetUnitImg()
    {
        KillUnitIdleAnim();

        foreach(var image in PlayerEquipUnitImgList)
        {
            ProjectUtility.SetActiveCheck(image.gameObject , false);
        }


        for(int i = 0; i < GameRoot.Instance.UserData.Carddatas.Count; i++)
        {
            var carddata = GameRoot.Instance.UserData.Carddatas[i];

            // PlayerEquipUnitImgList[i].sprite = GameRoot.Instance.CardSystem.GetCharImg(carddata.Cardidx);

            ProjectUtility.SetActiveCheck(PlayerEquipUnitImgList[i].gameObject , true);

            // 숨쉬듯이 천천히 부드럽게 커졌다 작아졌다 반복 (유닛마다 시작 타이밍을 살짝 어긋나게)
            UnitIdleTweenList.Add(PlayerEquipUnitImgList[i].transform
                .DOScale(1.1f, 1.2f)
                .SetEase(Ease.InOutSine)
                .SetDelay(i * 0.15f)
                .SetLoops(-1, LoopType.Yoyo));
        }
    }

    private void KillUnitIdleAnim()
    {
        foreach (var tween in UnitIdleTweenList)
            tween?.Kill();

        UnitIdleTweenList.Clear();

        foreach (var image in PlayerEquipUnitImgList)
        {
            if (image != null)
                image.transform.localScale = Vector3.one;
        }
    }

    void OnDisable()
    {
        KillUnitIdleAnim();
    }

    public void OnClickOption()
    {
        GameRoot.Instance.UISystem.OpenUI<PopupOption>(popup => popup.Set(false));
    }


    public void OnClickBook()
    {
        // GameRoot.Instance.UISystem.OpenUI<PopupCardBook>(popup => popup.Init());
    }

    public void OnClickAdGold()
    {
        GameRoot.Instance.PluginSystem.ADProp.ShowRewardAD(TpAdxProp.AdRewardType.AdGold, (result) =>
        {
            // 광고 없음(result == false, "광고없습니다")엔 보상 없음.
            if (!result) return;

            ProjectUtility.SetActiveCheck(AdGoldBtn.gameObject, false);

            GameRoot.Instance.EffectSystem.MultiPlay<RewardEffect>(transform.position, x =>
                                {
                                    //적에 전체체력에 퍼센트

                                    var endtr = GameRoot.Instance.UISystem.GetUI<HudCurrencyTop>().GetRewardEndTr((int)Config.RewardType.Currency, (int)Config.CurrencyID.Money);
                                    SoundPlayer.Instance.PlaySound("get_coin");

                                    x.Set((int)Config.RewardType.Currency, (int)Config.CurrencyID.Money, endtr, () =>
                                    {
                                        GameRoot.Instance.UserData.SetReward((int)Config.RewardType.Currency, (int)Config.CurrencyID.Money, 300);
                                    });
                                    x.SetAutoRemove(true, 4f);
                                });
        });
    }


    public void PageIconInit(PageIconType type)
    {
        //PageIconComponents[(int)type - 1].Init();
    }

    public void OnClickAdsEquipBtn()
    {
        // GameRoot.Instance.UISystem.OpenUI<PopupAdsEquip>(popup => popup.Init());
    }

    public void OnClickNoAdsBtn()
    {
        // GameRoot.Instance.UISystem.OpenUI<PopupNoAdsPackages>();
    }

    public void OnClickVIPBtn()
    {
        GameRoot.Instance.UISystem.OpenUI<PagePackageVIP>();
    }

    public void OnClickFreeGemBtn()
    {
        // GameRoot.Instance.UISystem.OpenUI<PopupAdsEquip>(popup => popup.Init());
    }


    public void CheckAdGoldBtn()
    {
        var stageidx = GameRoot.Instance.UserData.Stageidx.Value;

        var failedcount = GameRoot.Instance.UserData.GetRecordCount(Config.RecordCountKeys.StageFailedCount, stageidx);

        ProjectUtility.SetActiveCheck(AdGoldBtn.gameObject,
        GameRoot.Instance.ContentsOpenSystem.ContentsOpenCheck(ContentsOpenSystem.ContentsOpenType.TRAININGOPEN) && failedcount > 1);
    }


    public void OnClickStarterPackageBtn()
    {
       // GameRoot.Instance.UISystem.OpenUI<PopupPackageStarter>(popup => popup.Init());
    }

    public void SetModeState(DifficultyType type)
    {
        // BgImage.color = Config.Instance.GetImageColor($"lobby_bg_{(int)type}");
        // TileImg.color = Config.Instance.GetImageColor($"lobby_tile_{(int)type}");
        // GlowImg.color = Config.Instance.GetImageColor($"lobby_glow_{(int)type}");
    }

    public void SetPlayerPreview(int heroidx)
    {
        // var td = Tables.Instance.GetTable<HeroInfo>().GetData(heroidx);
        // if (td == null) return;

        //if (HeroPreviewImage == null) return;

        //HeroPreviewSingleton.Show(td.prefab, previewCam, HeroPreviewImage, HeroPreviewTextureSize, HeroPreviewLocalOffset);
    }




    void OnDestroy()
    {
        disposables.Clear();
        KillUnitIdleAnim();

        // if (CamController != null)
        //     HeroPreviewSingleton.DetachFrom(CamController.GetHeroPreviewCam, HeroPreviewImage);
        // else if (HeroPreviewImage != null)
        //     HeroPreviewSingleton.DetachFrom(null, HeroPreviewImage);
    }

}
