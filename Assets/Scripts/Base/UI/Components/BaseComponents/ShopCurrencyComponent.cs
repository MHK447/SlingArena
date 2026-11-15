using UnityEngine;
using UnityEngine.UI;
using BanpoFri;
using TMPro;
using UniRx;

public class ShopCurrencyComponent : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI OriginalRewardValueText;

    [SerializeField]
    private TextMeshProUGUI RewardValueText;

    [SerializeField]
    private Image RewardIcon;


    [SerializeField]
    private Button PurchaseBtn;


    [SerializeField]
    private GameObject PromotionRoot;

    [SerializeField]
    private TextMeshProUGUI PriceText;

    [SerializeField]
    private TextMeshProUGUI CashText;

    [SerializeField]
    private ShopCurrencyIdx CurrencyIdx;


    [SerializeField]
    private GameObject PriceRoot;

    [SerializeField]
    private GameObject CashRoot;

    [SerializeField]
    private GameObject ReddotObj;

    private string ProductId;

    private CompositeDisposable disposables = new CompositeDisposable();

    private System.Action PurChaseCallback;

    void Awake()
    {
       //PurchaseBtn.onClick.AddListener(OnClickPurchase);
        //AdsBtn.AddListener(TpMaxProp.AdRewardType.InSufficientGoldCoin,OnClickAds,false);
    }

    public void SetCurrencyIdx(int currencyIdx)
    {
        CurrencyIdx = (ShopCurrencyIdx)currencyIdx;
    }

    public void Init(System.Action purchaseCallback = null)
    {

        var td = Tables.Instance.GetTable<ShopCurrency>().GetData((int)CurrencyIdx);

        if (td != null)
        {
            if (purchaseCallback != null)
                PurChaseCallback = purchaseCallback;

            ProductId = td.product_id;

            RewardIcon.sprite = AtlasManager.Instance.GetSprite(Atlas.Atlas_UI_Common, td.icon);


            OriginalRewardValueText.text = td.additional == -1 ? td.reward_value.ToString() : (td.reward_value / 2).ToString();
            RewardValueText.text = td.reward_value.ToString();

            ProjectUtility.SetActiveCheck(PromotionRoot, td.additional > 0);
            ProjectUtility.SetActiveCheck(OriginalRewardValueText.gameObject, td.additional > 0);

            ProjectUtility.SetActiveCheck(PriceRoot, td.purchase_type != 3);
            ProjectUtility.SetActiveCheck(CashRoot, td.purchase_type == 3);

            if (ReddotObj != null)
                ProjectUtility.SetActiveCheck(ReddotObj, false);


            disposables.Clear();

            PurchaseBtn.onClick.RemoveAllListeners();

            PurchaseBtn.interactable = true;


            switch (td.purchase_type)
            {
                case 1:
                    {
                        // ProductId 비어 있으면 IAP 미연결 항목 — 버튼 비활성화하고 IAP 호출 자체를 건너뜀.
                        if (string.IsNullOrEmpty(ProductId))
                        {
                            PurchaseBtn.interactable = false;
                            PriceText.text = string.Empty;
                            break;
                        }

                        PurchaseBtn.onClick.AddListener(OnClickPurchase);
                        var product = GameRoot.Instance.InAppPurchaseManager.GetProduct(ProductId);
                        if (product != null)
                        {
                            PriceText.text = product.metadata.localizedPriceString;
                        }
                    }
                    break;
                case 2:
                    {
                        PurchaseBtn.interactable = GameRoot.Instance.UserData.GetRecordCount(Config.RecordCountKeys.FreeCashCount) == 0;
                        PurchaseBtn.onClick.AddListener(OnClickFree);
                        var freecashcount = GameRoot.Instance.UserData.GetRecordCount(Config.RecordCountKeys.FreeCashCount);

                        ProjectUtility.SetActiveCheck(ReddotObj , freecashcount == 0);

                        if (freecashcount > 0)
                        {
                            // GameRoot.Instance.DailyResetSystem.FreeDailyResetRemindTime.Subscribe(x =>
                            // {
                            //     PriceText.text = ProjectUtility.GetTimeStringFormattingShort(x);
                            // }).AddTo(disposables);
                        }
                        else
                        {
                            PriceText.text = Tables.Instance.GetTable<Localize>().GetString("str_free");
                        }
                    }
                    break;
                case 3:
                    {
                        PurchaseBtn.onClick.AddListener(OnClickCash);
                        CashText.text = td.price.ToString();
                    }
                    break;
            }

        }

    }



    void OnEnable()
    {
        Init();
    }




    void OnDestroy()
    {
        disposables.Clear();
    }


    public void OnClickPurchase()
    {
        GameRoot.Instance.ShopSystem.InappPurchase(ProductId, (int)CurrencyIdx, InAppPurchaseLocation.shop, () =>
                {
                    var td = Tables.Instance.GetTable<ShopCurrency>().GetData((int)CurrencyIdx);

                    if (td != null)
                    {
                        PurChaseCallback?.Invoke();
                        ProjectUtility.PlayGoodsEffect(Vector3.zero, (int)td.reward_type, td.reward_idx, 0, td.reward_value);
                    }
                });
    }

    public void OnClickCash()
    {
        var td = Tables.Instance.GetTable<ShopCurrency>().GetData((int)CurrencyIdx);

        if (td != null)
        {
            if (GameRoot.Instance.UserData.Cash.Value >= td.price)
            {
                GameRoot.Instance.UserData.SetReward((int)Config.RewardType.Currency, (int)Config.CurrencyID.Cash, -td.price);

                var gemParams = new System.Collections.Generic.List<TpParameter>();
                gemParams.Add(new TpParameter("stage", GameRoot.Instance.UserData.Stageidx.Value));
                gemParams.Add(new TpParameter("waveidx", GameRoot.Instance.UserData.Waveidx.Value));
                gemParams.Add(new TpParameter("usetype", td.reward_idx == (int)Config.CurrencyID.Money ? 0 : 1));
                gemParams.Add(new TpParameter("usegem", td.price));
                gemParams.Add(new TpParameter("type", "Gem"));
                gemParams.Add(new TpParameter("gem", GameRoot.Instance.UserData.Cash.Value));
                GameRoot.Instance.PluginSystem.AnalyticsProp.AllEvent(IngameEventType.None, "m_use_gem", gemParams);

                PurChaseCallback?.Invoke();
                ProjectUtility.PlayGoodsEffect(Vector3.zero, (int)td.reward_type, td.reward_idx, 0, td.reward_value);
            }
            else
            {
                GameRoot.Instance.UISystem.OpenUI<PopupCashInsufficent>(popup => popup.SetInsufficent(td.price));
            }
        }
    }


    public void OnClickFree()
    {
        if(GameRoot.Instance.UserData.GetRecordCount(Config.RecordCountKeys.FreeCashCount) > 0) return;
        
        var td = Tables.Instance.GetTable<ShopCurrency>().GetData((int)CurrencyIdx);

        if (td != null)
        {
            PurChaseCallback?.Invoke();
            ProjectUtility.PlayGoodsEffect(Vector3.zero, (int)td.reward_type, td.reward_idx, 0, td.reward_value);


            GameRoot.Instance.UserData.AddRecordCount(Config.RecordCountKeys.FreeCashCount, 1);

            GameRoot.Instance.GameNotification.UpdateNotification(GameNotificationSystem.NotificationCategory.ShopAdCash);

            Init();
        }
    }


}
