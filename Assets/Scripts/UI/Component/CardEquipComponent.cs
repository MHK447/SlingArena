using UnityEngine;
using BanpoFri;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using TMPro;

public class CardEquipComponent : MonoBehaviour
{

    [SerializeField]
    private Image CardBgImg;

    [SerializeField]
    private Image CardImg;

    [SerializeField]
    private Image CardGradeImg;

    [SerializeField]
    private TextMeshProUGUI CardGradeNameText;

    [SerializeField]
    private TextMeshProUGUI CardNameText;

    [SerializeField]
    private TextMeshProUGUI CardLevelText;


    public void Set(int equipidx , int equiptype , int cardgradeidx , int cardlevel)
    {
        CardBgImg.color = CardGradeImg.color = Config.Instance.GetImageColor($"InGameEquipment_Grade_Color_{cardgradeidx}");
        CardImg.sprite = AtlasManager.Instance.GetSprite(Atlas.Atlas_UI_Equipment , $"Equipment_Item_{equipidx}_{equiptype}");
        CardGradeNameText.text = Tables.Instance.GetTable<Localize>().GetString($"str_card_grade_{cardgradeidx}");
        CardNameText.text = Tables.Instance.GetTable<Localize>().GetString($"str_card_name_{equipidx}_{equiptype}");
        CardLevelText.text = $"Lv.{cardlevel}";
    }


}

