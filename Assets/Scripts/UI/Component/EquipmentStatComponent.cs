using UnityEngine;
using BanpoFri;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using TMPro;

public class EquipmentStatComponent : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI StatNameText;

    [SerializeField]
    private TextMeshProUGUI StatValueText;

    [SerializeField]
    private GameObject UpArrow;

    [SerializeField]
    private  GameObject DownArrow;
    

    public void Set(int stattypeidx , int statvalue , bool isup)
    {
       StatNameText.text =  Tables.Instance.GetTable<Localize>().GetString($"str_stat_type_{stattypeidx}");

       ProjectUtility.SetActiveCheck(UpArrow , isup);
       ProjectUtility.SetActiveCheck(DownArrow , !isup);

       StatValueText.text = $"+{statvalue}";

    }

}

