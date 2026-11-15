using UnityEngine;
using UnityEngine.UI;
using BanpoFri;
using UniRx;
using TMPro;
using System.Linq;
using System.Collections.Generic;
public class InGamePlayerComponent : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> UpgradeObjList = new List<GameObject>();

    [SerializeField]
    private TextMeshProUGUI CurHpText;


    [SerializeField]
    private TextMeshProUGUI TotalDmgText;

    [SerializeField]
    private Image UnitImg;

    [SerializeField]
    private Slider HpSlider;

    [SerializeField]
    private AdsButton RevivalBtn;

    [SerializeField]
    private Transform RevivalRoot;

    private CompositeDisposable disposables = new CompositeDisposable();

    [HideInInspector]
    public int UnitIdx = 0;

    void Awake()
    {
        RevivalBtn.AddListener(TpAdxProp.AdRewardType.Revival, OnClickRevival);
    }

    public void Set(int unitidx)
    {
        UnitIdx = unitidx;

        // var td = Tables.Instance.GetTable<UnitInfo>().GetData(unitidx);

        // UnitImg.sprite = AtlasManager.Instance.GetSprite(Atlas.Atlas_UI_Upgrade, $"Upgrade_icon_{unitidx}");

        // var findunitdata = GameRoot.Instance.UserData.Unitgroupdata.FindUnit(unitidx);

        // var findunit = true;

        // HpSlider.value = 1f;

        // ProjectUtility.SetActiveCheck(RevivalRoot.gameObject, false);


        // UpgradeObjCheck();


    }


    public void SetHp(double hp, double starthp)
    {
        HpSlider.value = (float)hp / (float)starthp;

        CurHpText.text = Tables.Instance.GetTable<Localize>().GetFormat("str_hp_desc", ProjectUtility.AbbreviateNumber(hp));

        if (HpSlider.value <= 0)
        {
            ProjectUtility.SetActiveCheck(RevivalRoot.gameObject, true);
        }
    }


    public void OnClickRevival()
    {
        //GameRoot.Instance.InGameSystem.GetInGame<InGameBase>().Stage.PlayerUnitGroup.RevivalUnit(UnitIdx);
        Set(UnitIdx);
    }

    public void UpgradeObjCheck()
    {
        // [정리] 옛 InGameUpgradeSystem 제거 — 레벨 0 고정
        var level = 0;

        for(int i = 0; i < UpgradeObjList.Count; i++)
        {
            var parent = UpgradeObjList[i];
            bool isActive = level >= i + 1;
            
            foreach(Transform child in parent.transform)
            {
                ProjectUtility.SetActiveCheck(child.gameObject, isActive);
            }
        }
    }



    void OnDisable()
    {
        disposables.Clear();
    }

    void OnDestroy()
    {
        disposables.Clear();
    }

}
