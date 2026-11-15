using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniRx;
using BanpoFri;
using Unity.VisualScripting;
public class GameNotificationSystem
{
    public enum NotificationType
    {
        Once,
        Passive
    }

    public enum NotificationCategory
    {
        Training,
        CardUpgrade,
        CardBook,
        HeroUpgradeCheck,
        ShopAdCash,
        OneTimeReaward,
        OneTimeNextLevel,
        SeasonalRankChange,
        Freecash,
        DailyDiceCardCount,
        CardBuy,
    }

    public class NotificationData
    {
        public NotificationType type;
        public NotificationCategory category;
        public int targetIdx = -1;
        public int targetSubIdx = -1;
        public IReactiveProperty<bool> on = new ReactiveProperty<bool>(false);
        public IReactiveProperty<int> onCount = new ReactiveProperty<int>(0);
    }

    private ReactiveDictionary<NotificationCategory, List<NotificationData>> notifications = new ReactiveDictionary<NotificationCategory, List<NotificationData>>();
    private CompositeDisposable disposables = new CompositeDisposable();

    public ReactiveDictionary<NotificationCategory, List<NotificationData>> GetNotifications { get { return notifications; } }


    private bool IsPassive(NotificationCategory category)
    {
        return false;
    }

    public NotificationData GetData(NotificationCategory _key, int _targetIdx, int _targetSubIdx)
    {
        if (notifications.ContainsKey(_key))
        {
            if (notifications[_key].Count > 0)
            {
                return notifications[_key].Find(x => x.targetIdx == _targetIdx && x.targetSubIdx == _targetSubIdx);
            }
        }

        return null;
    }


    private int BuyOneCardPrice = 20;
    private int BuyTenCardPrice = 0;

    public void Create()
    {
        notifications.Clear();
        disposables.Clear();

        foreach (NotificationCategory e in System.Enum.GetValues(typeof(NotificationCategory)))
        {
            var managerNotiList = new List<NotificationData>();
            var managerNoti = new NotificationData()
            {
                type = IsPassive(e) ? NotificationType.Passive : NotificationType.Once,
                category = e,
                targetIdx = -1,
                targetSubIdx = -1
            };
            managerNotiList.Add(managerNoti);
            notifications.Add(e, managerNotiList);
        }

        GameRoot.Instance.UserData.Money.Subscribe(x =>
        {
            UpdateNotification(NotificationCategory.Training);
            UpdateNotification(NotificationCategory.HeroUpgradeCheck);
        }).AddTo(disposables);

        GameRoot.Instance.UserData.Material.Subscribe(x =>
       {
           UpdateNotification(NotificationCategory.CardUpgrade);
           UpdateNotification(NotificationCategory.CardBuy);
       }).AddTo(disposables);

        // 보유 카드의 Cardcount / Cardlevel 변화도 CardUpgrade noti 갱신을 트리거.
        SubscribeCardDataChanges();

        // foreach (var herogroupdata in GameRoot.Instance.UserData.Herogroudata.Equipheroitems)
        // {
        //     herogroupdata.Level.Subscribe(x =>
        //     {
        //         UpdateNotification(NotificationCategory.HeroUpgradeCheck);
        //     }).AddTo(disposables);
        // }


        UpdateNotification(NotificationCategory.ShopAdCash);
        UpdateNotification(NotificationCategory.CardUpgrade);
        UpdateNotification(NotificationCategory.CardBuy);
        UpdateNotification(NotificationCategory.DailyDiceCardCount);


    }

    public void UpdateNotification(NotificationCategory category, int subIdx = -1)
    {
        switch (category)
        {
            case NotificationCategory.Training:
                {
                    bool ison = false;

                    var data = GetData(category, -1, -1);
                    if (data != null)
                    {
                        // var NextBuyOrder = GameRoot.Instance.UserData.Newtrainingdatabuyorder.Value + 1;


                        // var td = Tables.Instance.GetTable<BlockTrainingInfo>().GetData(NextBuyOrder);

                        // var level = GameRoot.Instance.UserData.Stageidx.Value;

                        // if (td != null)
                        // {
                        //     ison = GameRoot.Instance.UserData.Money.Value >= td.cost && level >= td.level;
                        // }


                        data.on.Value = ison;
                    }
                }
                break;
            case NotificationCategory.CardUpgrade:
                {
                    var data = GetData(category, -1, -1);
                    if (data != null)
                    {
                        // 보유 카드 중 한 장이라도 업그레이드 가능한지 검사.
                        bool canUpgradeAnyCard = false;
                        // foreach (var carddata in GameRoot.Instance.UserData.Carddatas)
                        // {
                        //     var cardupgradleveltd = Tables.Instance.GetTable<CardUpgradeLevel>().GetData(carddata.Cardlevel.Value);
                        //     if (cardupgradleveltd == null) continue;

                        //     if (carddata.Cardcount.Value >= cardupgradleveltd.need_card)
                        //     {
                        //         canUpgradeAnyCard = true;
                        //         break;
                        //     }
                        // }


                        data.on.Value = canUpgradeAnyCard;
                    }
                }
                break;
            case NotificationCategory.CardBook:
                {
                    bool ison = false;

                    var data = GetData(category, -1, -1);
                    if (data != null)
                    {

                        // var tdlist = Tables.Instance.GetTable<EquipInfo>().DataList.FindAll(x => x.item_type != 3).ToList();

                        // foreach (var td in tdlist)
                        // {
                        //     for (int i = 0; i < 4; ++i)
                        //     {
                        //         var equipcheck = GameRoot.Instance.UserData.GetRecordCount(Config.RecordCountKeys.EQUIPWEAPONCOUNT, $"{td.idx}_{i + 1}");

                        //         var rewardbookweaponcount = GameRoot.Instance.UserData.GetRecordCount(Config.RecordCountKeys.REWARDBOOKWEAPONCOUNT, $"{td.idx}_{i + 1}");

                        //         if (equipcheck > 0 && rewardbookweaponcount == 0)
                        //         {
                        //             ison = true;
                        //             break;
                        //         }
                        //     }
                        // }


                        data.on.Value = ison;
                    }
                }
                break;
            case NotificationCategory.HeroUpgradeCheck:
                {
                    bool ison = false;

                    var data = GetData(category, -1, -1);
                    if (data != null)
                    {
                        // foreach (var equipheroitem in GameRoot.Instance.UserData.Herogroudata.Equipheroitems)
                        // {
                        //     if (equipheroitem.Heroitemdata == null) continue;

                        //     var td = Tables.Instance.GetTable<HeroItemInfo>().GetData(equipheroitem.Heroitemdata.Heroitemidx);

                        //     if (td == null) continue;

                        //     var finditemdata = GameRoot.Instance.ItemSystem.GetItemData((int)ItemSystem.ItemType.EquipUpgradeItem, td.item_equip_type);

                        //     var allfinditemdata = GameRoot.Instance.ItemSystem.GetItemData((int)ItemSystem.ItemType.EquipUpgradeItem, 0);

                        //     var UpgradeGoldCost = GameRoot.Instance.HeroEquipmentSystem.equip_item_level_gold_cost * equipheroitem.Level.Value;
                        //     var UpgradeMaterialCount = GameRoot.Instance.HeroEquipmentSystem.equip_item_level_count * equipheroitem.Level.Value;

                        //     var curupgradecountvalue = allfinditemdata.Itemcnt.Value + finditemdata.Itemcnt.Value;

                        //     if (UpgradeGoldCost <= GameRoot.Instance.UserData.Money.Value && UpgradeMaterialCount <= curupgradecountvalue)
                        //     {
                        //         ison = true;
                        //         break;
                        //     }
                        // }

                        // merge available with 3+ same idx/grade
                        // var heroItems = GameRoot.Instance.UserData.Herogroudata.Heroitemdatas;
                        // if (heroItems != null)
                        // {
                        //     var canMerge = heroItems
                        //         .GroupBy(item => new { item.Heroitemidx, item.Grade })
                        //         .Any(group => group.Count() >= 3);

                        //     if (canMerge)
                        //     {
                        //         ison = true;
                        //     }
                        // }

                        data.on.Value = ison;
                    }
                }
                break;
            case NotificationCategory.ShopAdCash:
                {

                    var data = GetData(category, -1, -1);
                    if (data != null)
                    {

                        var freecashcount = GameRoot.Instance.UserData.GetRecordCount(Config.RecordCountKeys.FreeCashCount);

                        data.on.Value = freecashcount == 0;
                    }
                }
                break;
            case NotificationCategory.OneTimeReaward:
                {
                    var noti = GetData(category, -1, -1);
                    if (noti == null) return;

                    // var curThemeIdx = GameRoot.Instance.OneTimeSystem.CurThemeIdx;
                    // var onetimeeventlist = Tables.Instance.GetTable<OneTimeEventReward>().DataList.FindAll(x => x.theme == curThemeIdx);

                    bool on = false;

                    // foreach (var onetime in onetimeeventlist)
                    // {
                    //     on = GameRoot.Instance.UserData.Onetimedata.OneTimeLevelProperty.Value >= onetime.level && GameRoot.Instance.UserData.Onetimedata.OneTimeRewardCount == onetime.level;

                    //     if (on) break;


                    // }

                    if (on != noti.on.Value)
                        noti.on.Value = on;
                }
                break;
            case NotificationCategory.OneTimeNextLevel:
                {
                    var noti = GetData(category, -1, -1);
                    if (noti == null) return;

                    // var onetimedata = GameRoot.Instance.UserData.Onetimedata;
                    // var themeidx = GameRoot.Instance.OneTimeSystem.CurThemeIdx;

                    // var td = Tables.Instance.GetTable<OneTimeEventReward>().GetData(new KeyValuePair<int, int>(themeidx, onetimedata.OneTimeLevelProperty.Value + 1));

                    bool on = false;

                    // if (td != null)
                    // {
                    //     var onetimevalue = onetimedata.OneTimeExpandCountProperty.Value + onetimedata.OneTimeCount;
                    //     on = onetimevalue >= td.count;
                    // }


                    if (on != noti.on.Value)
                        noti.on.Value = on;
                }
                break;
            case NotificationCategory.SeasonalRankChange:
                {
                    var noti = GetData(category, -1, -1);
                    if (noti == null) return;

                    // var OneTimeData = GameRoot.Instance.UserData.Onetimedata;

                    // var CurRankIdx = GameRoot.Instance.OneTimeSystem.GetPlayerRank(OneTimeData.CurRankCount) == -1 ? 10 : GameRoot.Instance.OneTimeSystem.GetPlayerRank(OneTimeData.CurRankCount);
                    // var BeforeRankIdx = GameRoot.Instance.OneTimeSystem.GetPlayerRank(OneTimeData.BeforeRankCount) == -1 ? 10 : GameRoot.Instance.OneTimeSystem.GetPlayerRank(OneTimeData.BeforeRankCount);

                    // bool on = CurRankIdx != BeforeRankIdx;

                    // if (on != noti.on.Value)
                    //     noti.on.Value = on;
                }
                break;
            case NotificationCategory.DailyDiceCardCount:
                {
                    var noti = GetData(category, -1, -1);
                    if (noti == null) return;

                    var dailyDiceCardCount = GameRoot.Instance.UserData.GetRecordCount(Config.RecordCountKeys.DailyDiceCardCount);

                    bool on = dailyDiceCardCount < 3;

                    if (on != noti.on.Value)
                        noti.on.Value = on;
                }
                break;
            case NotificationCategory.CardBuy:
                {
                    var noti = GetData(category, -1, -1);
                    if (noti == null) return;

                    bool on = GameRoot.Instance.UserData.Material.Value >= BuyOneCardPrice;

                    if (on != noti.on.Value)
                        noti.on.Value = on;
                }
                break;
        }
    }

    public void ChangeOnceNotification(NotificationCategory category, bool value, int targetIdx = -1)
    {
        if (notifications.ContainsKey(category))
        {
            var find = notifications[category].Find(x => x.targetIdx == targetIdx);
            if (find != null)
            {
                find.on.Value = value;
            }
        }
    }

    public void UpdateOneSeconds()
    {
    }

    // 카드 데이터의 Cardcount / Cardlevel 변화에 구독을 걸어 CardUpgrade noti를 갱신.
    // 가챠 등으로 카드가 새로 추가될 수 있으므로, 새 카드는 RefreshCardDataSubscriptions로
    // 다시 호출해 재구독해야 한다.
    private CompositeDisposable cardDataDisposables = new CompositeDisposable();
    private HashSet<int> subscribedCardIdx = new HashSet<int>();

    public void SubscribeCardDataChanges()
    {
        cardDataDisposables.Clear();
        subscribedCardIdx.Clear();

        var carddatas = GameRoot.Instance.UserData.Carddatas;
        if (carddatas == null) return;

        foreach (var carddata in carddatas)
        {
            if (carddata == null || subscribedCardIdx.Contains(carddata.Cardidx)) continue;
            subscribedCardIdx.Add(carddata.Cardidx);

            carddata.Cardcount.Subscribe(_ => UpdateNotification(NotificationCategory.CardUpgrade)).AddTo(cardDataDisposables);
            carddata.Cardlevel.Subscribe(_ => UpdateNotification(NotificationCategory.CardUpgrade)).AddTo(cardDataDisposables);
        }
    }

    // 가챠 등으로 새 카드가 추가된 직후 호출. 이미 구독된 idx는 건너뛰고 신규만 추가 구독.
    public void RefreshCardDataSubscriptions()
    {
        var carddatas = GameRoot.Instance.UserData.Carddatas;
        if (carddatas == null) return;

        foreach (var carddata in carddatas)
        {
            if (carddata == null || subscribedCardIdx.Contains(carddata.Cardidx)) continue;
            subscribedCardIdx.Add(carddata.Cardidx);

            carddata.Cardcount.Subscribe(_ => UpdateNotification(NotificationCategory.CardUpgrade)).AddTo(cardDataDisposables);
            carddata.Cardlevel.Subscribe(_ => UpdateNotification(NotificationCategory.CardUpgrade)).AddTo(cardDataDisposables);
        }

        UpdateNotification(NotificationCategory.CardUpgrade);
    }

}
