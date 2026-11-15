using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BanpoFri;
using UniRx;
using System.Linq;
using System;

public enum InAppPurchaseLocation
{
    none = -1,
    shop = 0,
    popup = 1,
    hud,
    nomoney,
    other,
    banner,
}

public enum ShopState
{
    None,
    Purchasing,
}

public enum ShopCurrencyIdx
{
    Gold_1 = 1,
    Gold_2 = 2,
    Gold_3 = 3,
    Gold_4 = 4,
    Gold_5 = 5,
    Gold_6 = 6,
    Cash_Free = 100,
    Cash_1 = 101,
    Cash_2 = 102,
    Cash_3 = 103,
    Cash_4 = 104,
    Cash_5 = 105,
    Cash_6 = 106,
    Material_1 = 1001,
    Material_2 = 1002,
    Material_3 = 1003,
    Material_4 = 1004,
    Material_5 = 1005,
    Material_6 = 1006,
}

public enum PackageType
{
    None = -1,

    NoAds_100 = 100,
    NoAds_101 = 101,
    NoAds_102 = 102,
    NoAds_103 = 103,
    StarterPackage_1001 = 1001,

    LegendWeaponPackage_100004 = 100004,
    LegendWeaponPackage_100005 = 100005,
    
    BlessPackage_10002 = 10002,



}



public class ShopSystem
{
    public ShopState state { get; private set; } = ShopState.None;

    private System.IDisposable vipSubscription;

    public InAppPurchaseLocation curLocation = InAppPurchaseLocation.none;


    public System.DateTime ResetTime { get; private set; }
    public System.DateTime ResetStartTime { get; private set; }
    public enum ProductShopType
    {
        ShopCurrencyGem_01 = 101,
        ShopCurrencyGem_02 = 102,
        ShopCurrencyGem_03 = 103,
        ShopCurrencyGem_04 = 104,
        ShopCurrencyGem_05 = 105,
        ShopCurrencyGem_06 = 106,

        FreeGem = 1,
        AdGem = 2,

        GemRush_01 = 1001,
        GemRush_02 = 1002,
        GemRush_03 = 1003,
    }


    public IReactiveProperty<int> FreeAdRemindTime = new ReactiveProperty<int>(-1);

    private float curdeltatime = 0f;

    private float InterAdTime = 240f; // 기본값 4분
    private float currentInterAdTimer = 0f;
    private bool isInterAdReady = false;

    public int stage_energy_consume = 0;

    public int daily_reward_reset_time = 0;

    public ReactiveProperty<bool> NoInterstitialAds = new ReactiveProperty<bool>(false);

    public ReactiveProperty<bool> VipAdsProperty = new ReactiveProperty<bool>(false);

    // 이전엔 D1 리텐션(첫 실행 다음 날부터) 게이팅이었으나, 광고를 즉시 노출하도록 변경.
    public bool IsAdsRetentionPassed()
    {
        return true;
    }

    public ReactiveProperty<PackageType> GetCurWeaponPackageTypeproperty = new ReactiveProperty<PackageType>(PackageType.None);

    public void Create()
    {
        currentInterAdTimer = 0f;


        DayInitTime();
        CheckNoads();
        GetWeaponPackageType();
    }

    public void UpdateOneTimeSecond()
    {
        // VIP가 아닐 때만 광고 타이머 증가
        if (isInterAdReady)
        {
            currentInterAdTimer += 1f;


        }
    }

    public void CheckNoads()
    {
        string[] noAdPackageIds = Tables.Instance.GetTable<ShopProduct>().DataList.Where(x =>
        {
            return x.idx == (int)PackageType.NoAds_100 || x.idx == (int)PackageType.NoAds_101
                || x.idx == (int)PackageType.NoAds_102 || x.idx == (int)PackageType.NoAds_103;
        }).Select(x => x.product_id).ToArray();
        foreach (var id in noAdPackageIds)
        {
            NoInterstitialAds.Value |= GameRoot.Instance.UserData.BuyInappIds.Contains(id);
        }

        // VIP(102, 103)는 모든 광고 제거 (보상형 포함)
        bool isVipNoAds = GameRoot.Instance.UserData.BuyInappIds.Contains(InAppPurchaseManager.NoAdsVIP102)
            || GameRoot.Instance.UserData.BuyInappIds.Contains(InAppPurchaseManager.NoAdsVIP103);
        if (isVipNoAds)
        {
            VipAdsProperty.Value = true;
        }
    }


    // 광고 표시 시간 설정 (초 단위)
    public void SetInterAdTime(float seconds)
    {
        InterAdTime = seconds;
    }

    // 현재 타이머 리셋
    public void ResetInterAdTimer()
    {
        currentInterAdTimer = 0f;
    }

    // 광고 표시 강제 활성화/비활성화
    public void SetInterAdEnabled(bool enabled)
    {
        isInterAdReady = enabled;

        // 비활성화 시 타이머도 리셋
        if (!enabled)
        {
            currentInterAdTimer = 0f;
        }
    }

    public void UpdateOneSecond()
    {
        if (GameRoot.Instance.UserData.Dayinitialtime != default(System.DateTime))
        {
            var CurTime = TimeSystem.GetCurTime();

            var diff = GameRoot.Instance.UserData.Dayinitialtime.Subtract(CurTime);
            FreeAdRemindTime.Value = (int)diff.TotalSeconds;
            if (diff.TotalSeconds < 0)
            {
                DayInitTime();
                //TestMinuteInitTime();
            }
        }

        if (GameRoot.Instance.UserData.Starterpackdata.Isbuy.Value && GameRoot.Instance.UserData.Starterpackdata.Starterpackbuytime != default(DateTime))
        {
            GameRoot.Instance.UserData.Starterpackdata.PackageTimeProperty.Value = (int)(GameRoot.Instance.UserData.Starterpackdata.Starterpackbuytime - TimeSystem.GetCurTime()).TotalSeconds;
        }
    }


    public string OriginalPackageCost(int packageidx, bool isdouble = false)
    {
        var td = Tables.Instance.GetTable<ShopProduct>().GetData(packageidx);

        if (td != null)
        {
            var product = GameRoot.Instance.InAppPurchaseManager.GetProduct(td.product_id);
            if (product?.metadata == null)
                return string.Empty;

            var price = (float)product.metadata.localizedPrice;

            float returnvalue = 0f;

            if (!isdouble)
                returnvalue = price / ((100 - (float)td.sale) / 100f);
            else
                returnvalue = price / ((100 - (float)td.double_sale) / 100f);

            return $"{returnvalue:F1}";

        }

        return string.Empty;
    }


    public void GetWeaponPackageType()
    {
        PackageType curtype = PackageType.None;

        if (GameRoot.Instance.UserData.BuyInappIds.Contains("tinycastle_starter_package_1001"))
        {

            var ownedCards = GameRoot.Instance.UserData.Carddatas;
            bool has114 = ownedCards.Exists(x => x.Cardidx == 114);
            bool has115 = ownedCards.Exists(x => x.Cardidx == 115);

            if (has114 && has115)
            {
                curtype = PackageType.None;
            }
            else if (has114)
            {
                curtype = PackageType.LegendWeaponPackage_100005;
            }
            else if (has115)
            {
                curtype = PackageType.LegendWeaponPackage_100004;
            }
            else
            {
                curtype = PackageType.LegendWeaponPackage_100004;
            }
        }

        GetCurWeaponPackageTypeproperty.Value = curtype;
    }


    public void RewardPay(int rewardtype, int rewardidx, int rewardvalue)
    {
        switch (rewardtype)
        {
            case (int)Config.RewardType.Currency:
                {
                    switch (rewardidx)
                    {
                        case (int)Config.CurrencyID.Cash:
                            {
                                GameRoot.Instance.UserData.SetReward(rewardtype, rewardidx, rewardvalue);
                            }
                            break;
                            // case (int)Config.CurrencyID.EnergyMoney:
                            //     {
                            //         GameRoot.Instance.UserData.Energycoin.Value += rewardvalue;
                            //     }
                            //     break;
                    }

                }
                break;
        }
    }


    public void InappPurchase(string product_id, int itemIdx, InAppPurchaseLocation location, System.Action OnSuccess, System.Action OnFailed = null)
    {
        if (state == ShopState.Purchasing)
            return;

#if UNITY_EDITOR
        state = ShopState.Purchasing;
        if (!GameRoot.Instance.UserData.BuyInappIds.Contains(product_id))
            GameRoot.Instance.UserData.BuyInappIds.Add(product_id);

        OnSuccess?.Invoke();
        state = ShopState.None;
        return;
#endif
        GameRoot.Instance.Loading.Show(false);
        state = ShopState.Purchasing;

        curLocation = location;
        GameRoot.Instance.InAppPurchaseManager.BuyProductID(product_id, itemIdx, result =>
        {
            GameRoot.Instance.Loading.Hide(true);
            state = ShopState.None;

            if (result == InAppPurchaseManager.Result.Success)
            {
                if (!GameRoot.Instance.UserData.BuyInappIds.Contains(product_id))
                {
                    GameRoot.Instance.UserData.BuyInappIds.Add(product_id);
                }




                OnSuccess?.Invoke();
                //GameRoot.Instance.UserData.Save(true);
            }
            else
                OnFailed?.Invoke();
        });


        //logs
        List<TpParameter> parameters = new List<TpParameter>();
        parameters.Add(new TpParameter("idx", itemIdx));
        parameters.Add(new TpParameter("af_product_id", product_id));
        parameters.Add(new TpParameter("stage", GameRoot.Instance.UserData.Stageidx.Value));
        parameters.Add(new TpParameter("place", GameRoot.Instance.ShopSystem.curLocation.ToString()));
        GameRoot.Instance.PluginSystem.AnalyticsProp.AllEvent(IngameEventType.None,
            "m_purchase_popup", parameters);
    }





    public void PurchasePackage(int packageidx, System.Action successCallback = null, InAppPurchaseLocation location = InAppPurchaseLocation.popup, System.Action failCallback = null)
    {
        var td = Tables.Instance.GetTable<ShopProduct>().GetData(packageidx);

        if (td != null)
        {

            GameRoot.Instance.ShopSystem.InappPurchase(td.product_id, packageidx, location, () =>
            {
                var td = Tables.Instance.GetTable<ShopProduct>().GetData(packageidx);

                if (td != null)
                {
                    List<RewardData> rewarddatas = new List<RewardData>();

                    for (int i = 0; i < td.reward_type.Count; i++)
                    {
                        if (td.reward_type[i] > 0)
                        {
                            var item = new RewardData(td.reward_type[i], td.reward_idx[i], td.reward_value[i]);

                            rewarddatas.Add(item);
                        }
                    }

                    if (rewarddatas.Count > 0)
                    {
                        // GameRoot.Instance.UISystem.OpenUI<PagePurchaseConfirm>(popup => popup.Set(rewarddatas));
                    }

                    if (packageidx == (int)PackageType.StarterPackage_1001)
                    {
                        GameRoot.Instance.UserData.Starterpackdata.Isbuy.Value = true;
                    }

                    successCallback?.Invoke();
                }
            }, failCallback);

        }
    }



    public void DayInitTime()
    {
        var CurTime = TimeSystem.GetCurTime();


        ResetStartTime = ResetTime = new System.DateTime(CurTime.Year, CurTime.Month, CurTime.Day, daily_reward_reset_time, 0, 0);

        if (CurTime.Hour >= daily_reward_reset_time)
        {
            ResetTime = ResetTime.AddDays(1);
        }
        else
        {
            ResetStartTime = ResetTime.AddDays(-1);
        }


        if (GameRoot.Instance.UserData.Dayinitialtime == default(System.DateTime))
        {
            Reset();
        }
        else
        {
            var diff = ResetStartTime.Subtract(GameRoot.Instance.UserData.Dayinitialtime);
            if (diff.TotalSeconds >= 0)
            {
                Reset();
            }
        }
    }


    public void Reset()
    {

        GameRoot.Instance.UserData.Dayinitialtime = ResetTime;
        GameRoot.Instance.UserData.ResetRecordCount(Config.RecordCountKeys.AdGemCount, 0);
        GameRoot.Instance.UserData.ResetRecordCount(Config.RecordCountKeys.FreeGemCount, 0);
    }

    public void ShowInterAd()
    {
        if (NoInterstitialAds.Value)
            return;

        if (!IsAdsRetentionPassed())
            return;

        var interopen = GameRoot.Instance.ContentsOpenSystem.ContentsOpenCheck(ContentsOpenSystem.ContentsOpenType.InterAdOpen);

        var showadcout = GameRoot.Instance.UserData.GetRecordCount(Config.RecordCountKeys.ShowInterAdCount);

        // ShowInterAdCount 는 PopupStageResult.Hide 에서 스테이지 결과창 닫힐 때마다 +1.
        // 임계값을 2 로 두면 "스테이지 2회 클리어당 광고 1회" 빈도가 됨. (이전엔 1 — 매 스테이지)
        const int interAdShowThreshold = 2;

        if (interopen && showadcout >= interAdShowThreshold && GameRoot.Instance.UserData.Waveidx.Value == 4)
        {
            GameRoot.Instance.UserData.ResetRecordCount(Config.RecordCountKeys.ShowInterAdCount, 0);

            GameRoot.Instance.PluginSystem.ADProp.OnInterDismissedAction = () =>
            {
                // if (GameRoot.Instance != null && GameRoot.Instance.UISystem != null)
                //     GameRoot.Instance.UISystem.OpenUI<PopupNoAdsPackages>();
            };

            GameRoot.Instance.PluginSystem.ADProp.ShowInterstitialAD(TpAdxProp.AdInterType.None);
        }
    }


    public enum PackageRewardType
    {
        CurrencyAndHero = 0,
        Currency = 1,
        Card = 2,
        CardAndCurrency = 3,
        Hero = 4,
    }


    private const float CurrencyRewardSequenceDelay = 0.25f;


    public void GetPackageRewards(PackageRewardType rewardtype, List<RewardData> rewarddatas)
    {
        if (rewarddatas == null || rewarddatas.Count == 0)
            return;

        switch (rewardtype)
        {
            case PackageRewardType.Currency:
                PlayCurrencyEffects(rewarddatas, Vector3.zero, true);
                ApplyDirectRewards(rewarddatas);
                break;

            case PackageRewardType.Hero:
                ShowHeroDirection(rewarddatas, (_, __) => ApplyDirectRewards(rewarddatas));
                break;

            case PackageRewardType.Card:
                ShowCardDirection(rewarddatas, (_, __) => ApplyDirectRewards(rewarddatas));
                break;

            case PackageRewardType.CurrencyAndHero:
                ShowHeroDirection(rewarddatas, (popupPos, hasPos) =>
                {
                    PlayCurrencyEffects(rewarddatas, popupPos, !hasPos);
                    ApplyDirectRewards(rewarddatas);
                });
                break;

            case PackageRewardType.CardAndCurrency:
                ShowCardDirection(rewarddatas, (popupPos, hasPos) =>
                {
                    PlayCurrencyEffects(rewarddatas, popupPos, !hasPos);
                    ApplyDirectRewards(rewarddatas);
                });
                break;
        }
    }

    private void ShowHeroDirection(List<RewardData> rewarddatas, System.Action<Vector3, bool> onClosed)
    {
        var heroReward = rewarddatas.Find(x => x.RewardType != (int)Config.RewardType.Currency);
        if (heroReward == null)
        {
            onClosed?.Invoke(Vector3.zero, false);
            return;
        }

        Vector3 captured = Vector3.zero;
        bool captureSuccess = false;
        // GameRoot.Instance.UISystem.OpenUI<PageGetCharacter>(
        //     page =>
        //     {
        //         page.Set(heroReward.RewardIdx);
        //         captured = page.transform.position;
        //         captureSuccess = true;
        //     },
        //     () => onClosed?.Invoke(captured, captureSuccess));
    }

    private void ShowCardDirection(List<RewardData> rewarddatas, System.Action<Vector3, bool> onClosed)
    {
        var cardReward = rewarddatas.Find(x => x.RewardType == (int)Config.RewardType.Card);
        if (cardReward == null)
        {
            onClosed?.Invoke(Vector3.zero, false);
            return;
        }

        int cardCount = (int)cardReward.RewardValue;
        if (cardCount <= 0) cardCount = 1;

        Vector3 captured = Vector3.zero;
        bool captureSuccess = false;
        // GameRoot.Instance.UISystem.OpenUI<PageGachaSkillCard>(
        //     page =>
        //     {
        //         page.ChoiceCard(cardReward.RewardIdx, cardCount);
        //         captured = page.transform.position;
        //         captureSuccess = true;
        //     },
        //     () => onClosed?.Invoke(captured, captureSuccess));
    }

    private void PlayCurrencyEffects(List<RewardData> rewarddatas, Vector3 startPos, bool centerStart)
    {
        var currencyRewards = rewarddatas.FindAll(x => x.RewardType == (int)Config.RewardType.Currency);
        if (currencyRewards.Count == 0)
            return;

        var hud = GameRoot.Instance.UISystem.GetUI<HudCurrencyTop>();
        if (hud == null)
            return;

        Vector3 effectStart = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

        float delay = 0f;
        foreach (var c in currencyRewards)
        {
            var captured = c;
            var endTr = hud.GetRewardEndTr(captured.RewardType, captured.RewardIdx) ?? hud.transform;

            GameRoot.Instance.WaitTimeAndCallback(delay, () =>
            {
                GameRoot.Instance.EffectSystem.MultiPlay<RewardEffect>(effectStart, x =>
                {
                    x.Set(captured.RewardType, captured.RewardIdx, endTr, () =>
                    {
                        GameRoot.Instance.UserData.SetReward(
                            captured.RewardType,
                            captured.RewardIdx,
                            captured.RewardValue);
                    });
                    x.SetAutoRemove(true, 4f);
                });
            });
            delay += CurrencyRewardSequenceDelay;
        }
    }

    private void ApplyDirectRewards(List<RewardData> rewarddatas)
    {
        foreach (var reward in rewarddatas)
        {
            if (reward.RewardType == (int)Config.RewardType.Currency)
                continue;
            if (reward.RewardType == (int)Config.RewardType.Card)
                continue;
            if (IsHeroReward(reward))
                continue;

            GameRoot.Instance.UserData.SetReward(reward.RewardType, reward.RewardIdx, reward.RewardValue);
        }
    }

    private bool IsHeroReward(RewardData reward)
    {
        return reward.RewardType == (int)Config.RewardType.HeroUpgrade
            || reward.RewardType == (int)Config.RewardType.RandHeroItem;
    }



}
