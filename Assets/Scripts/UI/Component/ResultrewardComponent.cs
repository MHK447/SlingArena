using TMPro;
using BanpoFri;
using UnityEngine;
using UnityEngine.UI;

public class ResultrewardComponent : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI AdRewardValueText;

    [SerializeField]
    private TextMeshProUGUI BaseRewardText;

    [SerializeField]
    private Image AdRewardIcon;

    [SerializeField]
    private Image BaseRewardIcon;

    [SerializeField]
    private GameObject AdRewardRoot;
    
    public GameObject GetAdRewardRoot {get {return AdRewardRoot;}}
    public GameObject GetBaseRewardRoot {get {return BaseRewardRoot;}}

    [SerializeField]
    private GameObject BaseRewardRoot;

    private int RewardType = 0;
    private int RewardIdx = 0;
    private double RewardValue = 0;

    private bool IsFreeCheck = false;

    public void Set(int rewardtype, int rewardidx, int rewardvalue, int adRewardValue)
    {
        // AdRewardRoot는 광고 보상값 기준으로 활성화 (Cash처럼 base=0, ad>0인 케이스도 표시되도록).
        // BaseRewardRoot는 기본 보상값 기준 그대로.
        ProjectUtility.SetActiveCheck(AdRewardRoot, adRewardValue > 0);
        ProjectUtility.SetActiveCheck(BaseRewardRoot, rewardvalue > 0);

        IsFreeCheck = true;
        //!GameRoot.Instance.ContentsOpenSystem.ContentsOpenCheck(ContentsOpenSystem.ContentsOpenType.InGameOpen);

        RewardType = rewardtype;
        RewardIdx = rewardidx;
        RewardValue = rewardvalue;

        AdRewardValueText.text = adRewardValue.ToString();
        BaseRewardText.text = System.Math.Round(RewardValue).ToString();

        //AdRewardIcon.sprite = BaseRewardIcon.sprite = AtlasManager.Instance.GetRewardImg(RewardType, RewardIdx);
    }
}
