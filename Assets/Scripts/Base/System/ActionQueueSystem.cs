using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;
using BanpoFri;


public class ActionQueueSystem
{


    #region System
    private readonly LinkedList<Action> actions = new();

    public bool IsTutorialonCheck = false;

    public Action MainLoobyAction = null;

    public void Append(Action action)
    {
        actions.AddLast(action);
    }

    public void InsertNext(Action action)
    {
        actions.AddFirst(action);
    }

    public void NextAction()
    {
        if (actions.Count == 0)
        {
            BpLog.Log($"ActionQueue finished");
            return;
        }

        var action = actions.First.Value;
        actions.RemoveFirst();

        try
        {
            action?.Invoke();
        }
        catch (System.Exception ex)
        {
            BpLog.LogError($"ActionQueue action failed: {ex}");
            NextAction();
        }

        BpLog.Log($"ActionQueue left : {actions.Count}");
    }

    public void ExecuteAfterQueueFinished(Action action)
    {
        if (action == null)
            return;

        if (actions.Count == 0)
        {
            action.Invoke();
            return;
        }

        // Ensure this work runs only after all currently queued actions are done.
        Append(() =>
        {
            try
            {
                action.Invoke();
            }
            catch (System.Exception ex)
            {
                BpLog.LogError($"ActionQueue ExecuteAfterQueueFinished action failed: {ex}");
            }
            NextAction();
        });
    }

    public void ClearActions()
    {
        actions.Clear();
    }
    #endregion

    #region Actions

    public struct GameFinishContext
    {
        public bool isStageClear;
        public bool shouldRestoreSkill;

        public int passKeyCount;
    }

    public void OnFirstInitCall()
    {
        ClearActions();

        //AttendancePopupCheck();

        // AttendancePopupCheck();
        // TowerRacePopupCheck();
        // StarterPackageRewardPopupCheck();
        // NoAdsPopupCheck();
        // GoldGrabPopupCheck();
        // FirstTreasureHunterPopupCheck();
        //OpenRewardPackage();
        OneTimeRankReward();
        OneTimeOpenCheck();
        //StarterPackageRewardOn();
        //LobbyRandomPackageCheck();
        //WelComeBackPopupCheck();
        NextAction();
    }

    public void OnGameFinishCall()
    {
        // 큐잉된 튜토리얼(예: HudTotal.CheckContentsOpen_Hero/Card/Training 이 직전에 푸시한 언락 튜토리얼)이 있으면
        // ClearActions 로 wipe 하지 않고 보존 — wipe 하면 튜토리얼이 영영 안 뜨고 IsTutorialonCheck 도 reset 안 됨.
        if (!IsTutorialonCheck)
        {
            ClearActions();
        }

        NewChapterCheck();
        MainRewardOpenTutorialCheck();
        ReviewPopupCheck();
        BoxOpenTutorialCheck();
        UnLocCardTutorialCheck();
        UnLocTrainingTutorialCheck();
        // 게임 종료(FinishCall) 후에는 패키지 팝업(노애즈 등)을 일절 띄우지 않는다.
        // LobbyRandomPackageCheck();
        //OpenRewardPackage();
        // OneTimeRankReward();
        // OneTimeOpenCheck();
        //NewAddKing();
        NextAction();
    }


    public void SpearManTutorialCheck()
    {

        // var finddata = GameRoot.Instance.UserData.Unitgroupdata.FindUnit((int)Config.WeaponUnit.Spear);
        // if (GameRoot.Instance.UserData.Stageidx.Value >= 2 && finddata == null)
        // {
        //     Append(() =>
        //     {
        //         GameRoot.Instance.UserData.Unitgroupdata.AddUnit((int)Config.WeaponUnit.Spear);
        //         //GameRoot.Instance.UISystem.OpenUI<PageGetCharacterInfo>(page => page.Set((int)Config.WeaponUnit.Spear), NextAction);
        //         SoundPlayer.Instance.PlaySound("unit_unlock");
        //     });
        // }
    }

    public void UnLocTrainingTutorialCheck()
    {
        if (!GameRoot.Instance.TutorialSystem.IsClearTuto(TutorialSystem.Tuto_5) &&
         GameRoot.Instance.ContentsOpenSystem.ContentsOpenCheck(ContentsOpenSystem.ContentsOpenType.TRAININGOPEN))
        {
            IsTutorialonCheck = true;
        }
    }


    public void UnLocCardTutorialCheck()
    {
        if (!GameRoot.Instance.TutorialSystem.IsClearTuto(TutorialSystem.Tuto_6) &&
         GameRoot.Instance.ContentsOpenSystem.ContentsOpenCheck(ContentsOpenSystem.ContentsOpenType.CARDOPEN))
        {
            IsTutorialonCheck = true;
        }
    }



    public void UnLockStageUnitCheck()
    {
        // if (GameRoot.Instance.UserData.Stageidx.Value >= 3 &&
        //  GameRoot.Instance.ContentsOpenSystem.ContentsOpenCheck(ContentsOpenSystem.ContentsOpenType.UNITUPGRADEOPEN))
        // {
        //     Append(() =>
        //     {
        //         var tdlist = Tables.Instance.GetTable<UnitInfo>().DataList.ToList();

        //         var selectstageidx = 0;

        //         var finddata = tdlist.FirstOrDefault(x => GameRoot.Instance.UserData.Unitgroupdata.FindUnit(x.idx) == null);

        //         if (finddata != null)
        //         {
        //             selectstageidx = finddata.idx;

        //             GameRoot.Instance.UISystem.OpenUI<PageGetCharacterReward>(page => page.Set(selectstageidx), NextAction);
        //         }
        //         else
        //         {
        //             NextAction();
        //         }
        //     });
        // }
    }


    #endregion

    // private void ShowTouchLock()
    // {
    //     Append(() =>
    //     {
    //         GameRoot.Instance.UISystem.OpenUI<PopupTouchLock>(x => NextAction(), NextAction);
    //     });
    // }

    // private void HideTouchLock()
    // {
    //     Append(() =>
    //     {
    //         var touchlock = GameRoot.Instance.UISystem.GetUI<PopupTouchLock>();
    //         if (touchlock == null) { NextAction(); return; }
    //         touchlock.Hide();
    //     });
    // }

    private void MainRewardOpenTutorialCheck()
    {
        //메인 리워드 버튼 체크 
        if (!GameRoot.Instance.TutorialSystem.IsClearTuto(TutorialSystem.Tuto_3) && GameRoot.Instance.ContentsOpenSystem.ContentsOpenCheck(ContentsOpenSystem.ContentsOpenType.LobbyReward))
        {
            Append(() =>
             {
                 GameRoot.Instance.TutorialSystem.StartTutorial(TutorialSystem.Tuto_3);
                 GameRoot.Instance.TutorialSystem.OnActiveTutoEnd = NextAction;
             });
        }
    }
    private void BoxOpenTutorialCheck()
    {
        // //스테이지 보상 바운스볼 획득 튜토리얼
        // if (!GameRoot.Instance.TutorialSystem.IsClearTuto(TutorialSystem.Tuto_3)
        //     && GameRoot.Instance.ContentsOpenSystem.ContentsOpenCheck(ContentsOpenSystem.ContentsOpenType.BOXOPEN))
        // {
        //     Append(() =>
        //      {
        //          GameRoot.Instance.TutorialSystem.StartTutorial(TutorialSystem.Tuto_3);
        //          GameRoot.Instance.TutorialSystem.OnActiveTutoEnd = NextAction;
        //      });
        // }
    }

    public void OneTimeOpenCheck()
    {
        // if (GameRoot.Instance.OneTimeSystem.IsNewOpenSeasonal && GameRoot.Instance.ContentsOpenSystem.ContentsOpenCheck(ContentsOpenSystem.ContentsOpenType.OneTimeOpen))
        // {
        //     var td = Tables.Instance.GetTable<OneTimeEventInfo>().GetData(GameRoot.Instance.UserData.Onetimedata.OneTimeIdx);

        //     if (td != null)
        //     {
        //         Append(() =>
        //         {
        //             GameRoot.Instance.UISystem.OpenUI<PopupOneTimeOpen>(page => page.Set(td.idx, false), NextAction);
        //         });
        //     }
        // }
    }

    public void NewChapterCheck()
    {
        var stageidx = GameRoot.Instance.UserData.Stageidx.Value;

        var recordcount = GameRoot.Instance.UserData.GetRecordCount(Config.RecordCountKeys.NewChapter, stageidx);

        // 6의 배수 스테이지(6, 12, 18 ...) 클리어 직후에 새 챕터 팝업 노출.
        // PopupStageResult.Hide 에서 Stageidx 가 +1 증가한 뒤에 이 체크가 호출되므로
        // "6 스테이지 클리어 직후" = "현재 stageidx == 7" → (stageidx - 1) % 6 == 0.
        // stageidx == 1 (게임 시작 직후) 가산 방지를 위해 stageidx > 1 가드 추가.
        if (stageidx > 1 && (stageidx - 1) % 6 == 0 && recordcount == 0)
        {
            var stagetd = Tables.Instance.GetTable<StageInfo>().GetData(stageidx);
            GameRoot.Instance.UserData.AddRecordCount(Config.RecordCountKeys.NewChapter, 1, stageidx);
            Append(() =>
             {
                 GameRoot.Instance.UISystem.OpenUI<PopupNewChapter>(x => x.Set(stageidx), NextAction);
             });

        }
    }

    // public void NewAddKing()
    // {
    //     if (GameRoot.Instance.UserData.Herogroudata.Equipplayeridx == 0 && GameRoot.Instance.UserData.Stageidx.Value > 4)
    //     {

    //         if (IsTutorialonCheck)
    //         {
    //             MainLoobyAction = () =>
    //             {
    //                 GameRoot.Instance.UISystem.OpenUI<PageGetCharacterReward>(null, NextAction);
    //             };
    //         }
    //         else
    //         {
    //             Append(() =>
    //             {
    //                 GameRoot.Instance.UISystem.OpenUI<PageGetCharacterReward>(null, NextAction);
    //             });
    //         }
    //     }
    // }

    public void OpenRewardPackage()
    {
        if (HasPendingLobbyUnlockTutorial())
            return;

        // if (IsOneTimeOpenPending())
        //     return;

        // 2개 조건: ShopOpen + 스타터 패키지 노출 횟수 3회 미만
        bool condition1 = GameRoot.Instance.ContentsOpenSystem.ContentsOpenCheck(ContentsOpenSystem.ContentsOpenType.ShopOpen);
        if (!condition1)
            return;

        // 50% 확률
        // if (UnityEngine.Random.Range(0, 2) != 0)
        //     return;

        // 스타터 패키지 미구매 시 큐에 추가 (순서대로 실행)
        if (!GameRoot.Instance.UserData.Starterpackdata.Isbuy.Value)
        {
            // 스타터 패키지 자동 오픈 비활성화.
            // Append(() => GameRoot.Instance.UISystem.OpenUI<PopupPackageStarter>(popup => popup.Init(), NextAction));
        }
        else
        {
            // 스타터 패키지 구매 완료 시 무기 카드 패키지 노출
            var weaponPackageType = GameRoot.Instance.ShopSystem.GetCurWeaponPackageTypeproperty.Value;
            if (weaponPackageType != PackageType.None)
            {
                // Append(() => GameRoot.Instance.UISystem.OpenUI<PopupPackageCard>(
                //     popup => popup.Set(GameRoot.Instance.ShopSystem.GetCurWeaponPackageTypeproperty.Value), NextAction));
            }
        }

        // 노광고 / VIP 패키지 노출
        bool canShowNoAds = !GameRoot.Instance.ShopSystem.NoInterstitialAds.Value &&
            GameRoot.Instance.ContentsOpenSystem.ContentsOpenCheck(ContentsOpenSystem.ContentsOpenType.InterAdOpen) &&
            GameRoot.Instance.ShopSystem.IsAdsRetentionPassed();


        int daysSinceStart = (TimeSystem.GetCurTime() - GameRoot.Instance.UserData.FirstStartTime).Days; ;

        bool canShowVip = GameRoot.Instance.UserData.FirstStartTime == (default(System.DateTime)) ? false : !GameRoot.Instance.ShopSystem.VipAdsProperty.Value && daysSinceStart >= 8;
        if (canShowNoAds && canShowVip)
        {
            // 둘 다 조건 충족 시 50% 확률로 하나만 노출
            if (UnityEngine.Random.Range(0, 2) == 0)
                // Append(() => GameRoot.Instance.UISystem.OpenUI<PopupNoAdsPackages>(null, NextAction));
            // else
                Append(() => GameRoot.Instance.UISystem.OpenUI<PagePackageVIP>(null, NextAction));
        }
        else if (canShowNoAds)
        {
            // Append(() => GameRoot.Instance.UISystem.OpenUI<PopupNoAdsPackages>(null, NextAction));
        }
        else if (canShowVip)
        {
            Append(() => GameRoot.Instance.UISystem.OpenUI<PagePackageVIP>(null, NextAction));
        }
    }

    public void WelComeBackPopupCheck()
    {
        if (!GameRoot.Instance.UserData.HasInGameResumeData())
            return;

        bool isHardResume = GameRoot.Instance.UserData.IngameResumeData.Difficultytype == (int)DifficultyType.Hard;
        int stageIdx = isHardResume
            ? GameRoot.Instance.UserData.IngameResumeData.Hardmodestageidx
            : GameRoot.Instance.UserData.Stageidx.Value;

        if (stageIdx >= 2)
        {
            Append(() =>
            {
                GameRoot.Instance.UISystem.OpenUI<PopupWelComeBack>(x => x.Init(), NextAction);
            });
        }
    }

    public void StarterPackageRewardOn()
    {
        if (HasPendingLobbyUnlockTutorial())
            return;

        // 히어로 튜토리얼 등 큐잉 대기 중인 튜토리얼이 있으면 패키지 보류.
        if (IsTutorialonCheck)
            return;

        // if (!GameRoot.Instance.DailyResetSystem.StarterPackageRewardCheck())
        //     return;

        // 스타터 패키지 자동 오픈 비활성화.
        // Append(() => GameRoot.Instance.UISystem.OpenUI<PopupPackageStarter>(popup => popup.Init(), NextAction));
    }

    // 메인 로비 진입 시 스타터 패키지 / 노애즈 패키지 둘 중 가능한 것을 노출.
    // 둘 다 가능하면 50% 확률로 하나만 노출.
    public void LobbyRandomPackageCheck()
    {
        // 튜토리얼이 진행 중이거나 곧 발동될 예정이면 패키지 노출 스킵.
        if (HasAnyPendingTutorial())
            return;

        // 스타터 패키지 자동 오픈 비활성화 — canShowStarter 미사용.
        // bool canShowStarter = !GameRoot.Instance.UserData.Starterpackdata.Isbuy.Value
        //     && GameRoot.Instance.ContentsOpenSystem.ContentsOpenCheck(ContentsOpenSystem.ContentsOpenType.ShopOpen);

        bool canShowNoAds = !GameRoot.Instance.ShopSystem.NoInterstitialAds.Value
            && GameRoot.Instance.ContentsOpenSystem.ContentsOpenCheck(ContentsOpenSystem.ContentsOpenType.InterAdOpen)
            && GameRoot.Instance.ShopSystem.IsAdsRetentionPassed();

        // 스타터 패키지 자동 오픈 비활성화 — 노애즈 패키지만 노출.
        // if (canShowStarter && canShowNoAds)
        // {
        //     if (UnityEngine.Random.Range(0, 2) == 0)
        //         Append(() => GameRoot.Instance.UISystem.OpenUI<PopupPackageStarter>(popup => popup.Init(), NextAction));
        //     else
        //         Append(() => GameRoot.Instance.UISystem.OpenUI<PopupNoAdsPackages>(null, NextAction));
        // }
        // else if (canShowStarter)
        // {
        //     Append(() => GameRoot.Instance.UISystem.OpenUI<PopupPackageStarter>(popup => popup.Init(), NextAction));
        // }
        // else
        if (canShowNoAds)
        {
            // Append(() => GameRoot.Instance.UISystem.OpenUI<PopupNoAdsPackages>(null, NextAction));
        }
    }

    // private bool IsOneTimeOpenPending()
    // {
    //     // if (!GameRoot.Instance.OneTimeSystem.IsNewOpenSeasonal ||
    //     //     !GameRoot.Instance.ContentsOpenSystem.ContentsOpenCheck(ContentsOpenSystem.ContentsOpenType.OneTimeOpen))
    //     //     return false;

    //     // var td = Tables.Instance.GetTable<OneTimeEventInfo>().GetData(GameRoot.Instance.UserData.Onetimedata.OneTimeIdx);
    //     // return td != null;
    // }

    public void OneTimeRankReward()
    {
        // if (GameRoot.Instance.UserData.Onetimedata.RankRewardIdx > 0)
        // {
        //     GameRoot.Instance.OneTimeSystem.GetOneTimeRewardCheck();

        //     var td = Tables.Instance.GetTable<RankInfo>().GetData(GameRoot.Instance.UserData.Onetimedata.RankRewardIdx);

        //     if (td != null)
        //     {
        //         Append(() =>
        //         {
        //             GameRoot.Instance.UISystem.OpenUI<PageOneTimeEventDone>(page => page.Set(td.rank_idx, NextAction));
        //         });
        //     }
        // }
    }

    private bool HasPendingLobbyUnlockTutorial()
    {
        // HudTotal.CheckContentsOpen_Training/Card/Hero 는 큐잉 진입 즉시 TutoClear 로
        // Tuto_5/6/7 을 저장하므로, 아래 IsClearTuto 검사는 "컨텐츠는 열렸지만 아직
        // 큐잉 전" 인 구간만 잡는다. 큐잉 후 ~ 튜토리얼 종료까지는 IsTutorialonCheck 가 커버.
        if (IsTutorialonCheck)
            return true;

        bool isTrainingTutorialPending =
            !GameRoot.Instance.TutorialSystem.IsClearTuto(TutorialSystem.Tuto_5) &&
            GameRoot.Instance.ContentsOpenSystem.ContentsOpenCheck(ContentsOpenSystem.ContentsOpenType.TRAININGOPEN);

        bool isCardTutorialPending =
            !GameRoot.Instance.TutorialSystem.IsClearTuto(TutorialSystem.Tuto_6) &&
            GameRoot.Instance.ContentsOpenSystem.ContentsOpenCheck(ContentsOpenSystem.ContentsOpenType.CARDOPEN);

        // Tuto_7 — 영웅 강화 잠금 해제 튜토리얼. HudTotal.CheckContentsOpen_Hero 가
        // HeroUpgradeOpen 컨텐츠 오픈 시점에 큐에 푸시함. 튜토리얼이 끝날 때까지 패키지 팝업은 보류.
        // bool isHeroTutorialPending =
        //     !GameRoot.Instance.TutorialSystem.IsClearTuto(TutorialSystem.Tuto_7) &&
        //     GameRoot.Instance.ContentsOpenSystem.ContentsOpenCheck(ContentsOpenSystem.ContentsOpenType.HeroUpgradeOpen);

        return isTrainingTutorialPending || isCardTutorialPending ;
    }

    // 메인로비/인게임 진입 시점에 자동 발동될 수 있는 모든 튜토리얼 검사.
    // 패키지 팝업처럼 "튜토리얼 도중에는 안 떠야 하는" 컨텐츠 가드 용도.
    private bool HasAnyPendingTutorial()
    {
        // 현재 활성 중인 튜토리얼.
        if (GameRoot.Instance.TutorialSystem.IsActive())
            return true;

        // 큐잉만 되고 아직 시작 전인 튜토리얼(예: Tuto_7 히어로 잠금 해제) 도 진행 중으로 간주.
        // HudTotal.CheckContentsOpen_Hero 가 액션 큐에 푸시하는 시점에 true 로 켜고,
        // 튜토리얼 종료(OnActiveTutoEnd) 콜백에서 false 로 복귀시킴.
        if (IsTutorialonCheck)
            return true;

        // 메인 리워드 튜토리얼(Tuto_3) — OnGameFinishCall에서 큐에 들어감.
        if (!GameRoot.Instance.TutorialSystem.IsClearTuto(TutorialSystem.Tuto_3) &&
            GameRoot.Instance.ContentsOpenSystem.ContentsOpenCheck(ContentsOpenSystem.ContentsOpenType.LobbyReward))
            return true;

        // 트레이닝/카드 언락 튜토리얼(Tuto_5/6).
        if (HasPendingLobbyUnlockTutorial())
            return true;

        return false;
    }


    private void AttendancePopupCheck()
    {
        // if (!GameRoot.Instance.ContentsOpenSystem.ContentsOpenCheck(ContentsOpenSystem.ContentsOpenType.AttendanceReward)) return;
        // if (!GameRoot.Instance.AttendanceSystem.IsAttendanceRewardCheck()) return;

        // Append(() =>
        // {
        //     GameRoot.Instance.UISystem.OpenUI<PopupAttendance>(popup => popup.Init(), NextAction);
        // });
    }


    private void ReviewPopupCheck()
    {
        if (GameRoot.Instance.ContentsOpenSystem.ContentsOpenCheck(ContentsOpenSystem.ContentsOpenType.ReviewPopup)
        && GameRoot.Instance.UserData.GetRecordCount(Config.RecordCountKeys.ReviewPopup) == 0)
        {
            Append(() =>
             {
                 GameRoot.Instance.UserData.AddRecordCount(Config.RecordCountKeys.ReviewPopup, 1);
                 GameRoot.Instance.UISystem.OpenUI<PopupReview>(null, NextAction);
             });
        }
    }


}