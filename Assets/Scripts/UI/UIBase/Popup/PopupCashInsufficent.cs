using UnityEngine;
using BanpoFri;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

[UIPath("UI/Popup/PopupCashInsufficent")]
public class PopupCashInsufficent : CommonUIBase
{
    [SerializeField]
    private Button CloseBtn;

    [SerializeField]
    private ShopCurrencyComponent ShopcurrencyComp;

    protected override void Awake()
    {
        base.Awake();
        CloseBtn.onClick.AddListener(Hide);
    }

    public void SetInsufficent(int insufficentvalue)
    {
        var valuecash = insufficentvalue - GameRoot.Instance.UserData.Cash.Value;
        var td = Tables.Instance.GetTable<ShopCurrency>().DataList
            .Where(x => x.reward_type == (int)Config.RewardType.Currency
                && x.reward_idx == (int)Config.CurrencyID.Cash
                && x.reward_value >= valuecash)
            .OrderBy(x => x.reward_value)
            .FirstOrDefault();

        ShopcurrencyComp.SetCurrencyIdx(td.idx);

        ShopcurrencyComp.Init(()=> {
            Hide();
        });
    }
}

