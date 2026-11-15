using UnityEngine;
using BanpoFri;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using TMPro;

public class ChapterMapComponent : MonoBehaviour
{
    [SerializeField]
    private Image MapImg;

    [SerializeField]
    private RectTransform MapGroupRect;

    [SerializeField]
    private TextMeshProUGUI StageNameText;



    int StageIdx = 0;

    public void EasySet(int stageidx)
    {
        StageIdx = stageidx;

        var td = Tables.Instance.GetTable<StageInfo>().GetData(stageidx);

        if (td != null)
        {
            StageNameText.text =  stageidx + "." + Tables.Instance.GetTable<Localize>().GetString($"str_stage_name_{stageidx}");
            MapImg.sprite = AtlasManager.Instance.GetSprite(Atlas.Atlas_Stage, td.map_img);
        }
    }
    public void HardSet(int stageidx)
    {
        StageIdx = stageidx;

        // var td = Tables.Instance.GetTable<Hard_StageInfo>().GetData(stageidx);

        // if (td != null)
        // {
        //     MapGroupRect.anchoredPosition = new Vector2(MapGroupRect.anchoredPosition.x, td.map_unit_ypos);


        //     MapImg.sprite = AtlasManager.Instance.GetSprite(Atlas.Atlas_Stage, td.map_img);

        // }
    }
}
