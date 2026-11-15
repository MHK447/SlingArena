using System;
using BanpoFri;
using UnityEngine;
using UniRx;

// 광고 SDK 없는 스텁. 기존 퍼블리셔(ADX) SDK는 제거됨.
// 새 퍼블리셔 광고 SDK를 붙일 때 이 클래스 안의 TODO 지점만 채우면 호출측 코드는 그대로 동작한다.
public class TpAdxProp
{
    System.Action<bool> OnRewardResult;
    public Subject<bool> OnRewardResultBroadcast = new Subject<bool>();

    public System.Action OnInterDismissedAction = null;

    public enum AdRewardType
    {
        None = -1,
        Revival = 0,
        StageResult = 1,
        LevelUpReroll = 2,
        LuckyChoice = 3,
        InSufficientSilverCoint = 4,
        AdWeapon = 5,
        AdTile = 6,
        AdGold = 7,
        InGameSilverCoin = 8,
        AddKing = 9,
        StageClearAdReward = 10,
        DailyEquipmentAdReward = 11,

        InSufficientGoldCoin = 12,

        GameSpeedUp = 13,

        BlessAttack = 14,
        BlessHp = 15,
        BlessSilverCoin = 16,
        AddDice = 17,
        AdAddDice = 18,
        CastleRepair = 19,
        DailyDiceCard = 20,
        AllLevelUpReward = 21,

        Attendance = 9999,
    }

    public enum AdInterType
    {
        None = -1,
    }

    private AdRewardType adType = AdRewardType.None;

    public bool ShowingAd { get; private set; } = false;

    public bool isBanner = false;
    public bool isShowBanner = false;

    private bool isRewardAdsinit = false;

    // Button Dim
    public Action<bool> listenerRewardedAd;
    public void AddRewardedAdEvent(Action<bool> cb) { listenerRewardedAd += cb; }
    public void RemoveRewardedAdEvent(Action<bool> cb) { listenerRewardedAd -= cb; }

    // TODO(새 퍼블리셔 SDK): 리워드 광고 로드 여부 반환
    public bool IsRewardedAd()
    {
#if UNITY_EDITOR || TREEPLLA_AD_SKIP
        return true;
#else
        return false;
#endif
    }

    public bool IsRewardAdsInit() { return isRewardAdsinit; }

    public void Init()
    {
        // TODO(새 퍼블리셔 SDK): 광고 초기화/로드
        isRewardAdsinit = true;
    }

    #region Rewarded

    public void LoadRewardedAd()
    {
        // TODO(새 퍼블리셔 SDK)
    }

    public void ShowRewardAD(AdRewardType adType, System.Action<bool> OnSuccess)
    {
        GameRoot.Instance.Loading.Show(false);
        OnRewardResult = OnSuccess;
        this.adType = adType;

#if UNITY_EDITOR || TREEPLLA_AD_SKIP
        BpLog.Log("[TpAdxProp] 광고 스킵 (에디터/AD_SKIP)");
        onRewardResult(true);
#else
        // TODO(새 퍼블리셔 SDK): 리워드 광고 표시 후 결과를 onRewardResult(성공여부)로 전달
        BpLog.Log("[TpAdxProp] 광고 SDK 없음 - 리워드 실패 처리");
        onRewardResult(false);
#endif
    }

    private void onRewardResult(bool value)
    {
        System.Action ResultCb = () =>
        {
            if (OnRewardResult == null) return;

            if (value)
            {
                GameRoot.Instance.RemoveDelayAdTimeContents();
                GameRoot.Instance.UserData.AddRecordCount(Config.RecordCountKeys.AdWatchCount, 1);
            }
            else
            {
                GameRoot.Instance.UISystem.OpenUI<PopupToastmessage>(
                            popup => popup.Show(Tables.Instance.GetTable<Localize>().GetString("str_no_startads"))
                        );
            }
            OnRewardResult?.Invoke(value);
            OnRewardResult = null;
            OnRewardResultBroadcast.OnNext(value);
            GameRoot.Instance.Loading.Hide(true);
            adType = AdRewardType.None;
        };

        GameRoot.Instance.MainThreadDispatcher.Enqueue(ResultCb);
    }

    #endregion

    #region Interstitial

    // 전면 광고 없음: 대기 중인 dismiss 콜백만 즉시 실행 (ShopSystem 등 호출 흐름 유지)
    public void ShowInterstitialAD(AdInterType idx)
    {
        var dismissAction = OnInterDismissedAction;
        OnInterDismissedAction = null;
        dismissAction?.Invoke();
    }

    #endregion

    #region Banner

    // 배너 없음 (no-op)
    public void ShowBanner(int adPosition = -1, int adSize = -1)
    {
    }

    public void HideBanner(bool isDestroy = false)
    {
        isShowBanner = false;
        isBanner = false;
    }

    #endregion
}
