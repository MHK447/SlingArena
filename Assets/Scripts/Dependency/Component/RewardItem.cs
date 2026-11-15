    using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BanpoFri;

public class RewardItem : MonoBehaviour
{
    [SerializeField]
    private Image icon;
    [SerializeField]
    private TextMeshProUGUI cnt;

    [SerializeField]
    private Button InfoBtn;

    [SerializeField]
    private GameObject ToolTipObj;

    [SerializeField]
    private Image iconfx;

    private bool IsToolTip = false;

    [SerializeField]
    private TextMeshProUGUI InfoDesc;

    void Awake()
    {
        if (InfoBtn != null)
        {
            InfoBtn.onClick.AddListener(OnClickInfo);
        }

    }

    public void Set(int rewardType, int rewardIdx, int rewardValue)
    {
        if (ToolTipObj != null)
            ProjectUtility.SetActiveCheck(ToolTipObj.gameObject, false);

        IsToolTip = false;

            if (icon != null)
        {
            icon.sprite = ProjectUtility.GetRewardItemIconImg(rewardType, rewardIdx);
        }

        if (iconfx != null)
        {
            iconfx.sprite = ProjectUtility.GetRewardItemIconImg(rewardType, rewardIdx);
            iconfx.SetNativeSize();
        }

        if (iconfx != null)
            ProjectUtility.SetActiveCheck(iconfx.gameObject, rewardType == (int)Config.RewardType.Card);

        switch (rewardType)
        {
            case (int)Config.RewardType.Item:
                {
                    switch (rewardIdx)
                    {
                        case (int)Config.ItemType.OneTimeBuff:
                        case (int)Config.ItemType.LuckyVerseCoinBuff:
                        case (int)Config.ItemType.LuckyVerseTicketMaxBuff:
                        case (int)Config.ItemType.LuckyVerseRankBuff:
                        case (int)Config.ItemType.LuckyVerseLabTimeBuff:
                            {
                                if (cnt != null)
                                    cnt.text = "";
                            }
                            break;
                        default:
                            {
                                if (cnt != null)
                                    cnt.text = rewardValue.ToString();
                            }
                            break;
                    }
                }
                break;
            default:
                if (cnt != null)
                    cnt.text = rewardValue.ToString();
                break;
        }

        if (InfoBtn != null)
        {
            var td = Tables.Instance.GetTable<RewardInfo>().GetData(new KeyValuePair<int, int>(rewardType, rewardIdx));
            if (td != null)
            {
                InfoDesc.text = Tables.Instance.GetTable<Localize>().GetString(td.desc);
            }

            ProjectUtility.SetActiveCheck(InfoBtn.gameObject, rewardType == (int)Config.RewardType.Item && rewardIdx == 2); // 재화 버프일떄만 
        }
    }


    public void OnClickInfo()
    {
        IsToolTip = !IsToolTip;

        if (ToolTipObj != null)
        {
            ToolTipObj.SetActive(IsToolTip);
        }
    }

    private void Update()
    {
        if (InfoBtn == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            IsToolTip = false;
            ProjectUtility.SetActiveCheck(ToolTipObj, IsToolTip);
        }
    }
}
