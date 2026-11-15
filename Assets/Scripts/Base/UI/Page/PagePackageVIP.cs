using UnityEngine;
using BanpoFri;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[UIPath("UI/Popup/PagePackageVIP")]

public class PagePackageVIP : UIBase
{
    [SerializeField]
    private Button PurchaseBtn;

    [SerializeField]
    private TextMeshProUGUI CostText;

    [SerializeField]
    private List<TextMeshProUGUI> RewardValueTextList = new List<TextMeshProUGUI>();

    private PackageType currentPackageType;
    private bool canInteract = true;

    protected override void Awake()
    {
        base.Awake();
        PurchaseBtn.onClick.AddListener(OnClickPurchase);
    }

    public override void OnShowBefore()
    {
        base.OnShowBefore();
        canInteract = true;
        SetPackage();
    }

    private void SetPackage()
    {
        bool hasInterstitialNoAds = GameRoot.Instance.ShopSystem.NoInterstitialAds.Value;

        // 전면광고 제거제 있으면 102, 없으면 103
        currentPackageType = hasInterstitialNoAds ? PackageType.NoAds_103 : PackageType.NoAds_102;

        // 이미 VIP 구매했는지 체크
        bool alreadyPurchased = GameRoot.Instance.UserData.BuyInappIds.Contains(InAppPurchaseManager.NoAdsVIP102)
            || GameRoot.Instance.UserData.BuyInappIds.Contains(InAppPurchaseManager.NoAdsVIP103);

        if (PurchaseBtn != null)
            PurchaseBtn.gameObject.SetActive(!alreadyPurchased);

        if (!alreadyPurchased && CostText != null)
        {
            var td = Tables.Instance.GetTable<ShopProduct>().GetData((int)currentPackageType);
            if (td != null)
            {
                var product = GameRoot.Instance.InAppPurchaseManager.GetProduct(td.product_id);
                if (product != null)
                {
                    CostText.text = product.metadata.localizedPriceString;
                }

                for (int i = 0; i < td.reward_value.Count; i++)
                {
                    RewardValueTextList[i].text = td.reward_value[i].ToString();
                }
            }
        }
    }

    public void OnClickPurchase()
    {
        if (!canInteract) return;
        canInteract = false;

        GameRoot.Instance.ShopSystem.PurchasePackage((int)currentPackageType, OnBuySuccess, InAppPurchaseLocation.popup, OnBuyCancel);
    }

    private void OnBuySuccess()
    {
        Hide();

        // 모든 광고 제거 적용
        GameRoot.Instance.ShopSystem.NoInterstitialAds.Value = true;
        GameRoot.Instance.ShopSystem.VipAdsProperty.Value = true;
        GameRoot.Instance.ShopSystem.CheckNoads();
        GameRoot.Instance.PluginSystem.ADProp.HideBanner(true);


        var battle = GameRoot.Instance.UISystem.GetUI<PageLobbyBattle>();
        if (battle != null)
            battle.InitIcons();

        SetPackage();
        canInteract = true;
    }

    private void OnBuyCancel()
    {
        canInteract = true;
    }
}
