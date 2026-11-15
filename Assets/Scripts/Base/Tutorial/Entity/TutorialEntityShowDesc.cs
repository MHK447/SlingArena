using UnityEngine;
using BanpoFri;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using TMPro;
public class TutorialEntityShowDesc : TutorialEntity
{
    [SerializeField]
    private TextMeshProUGUI Text;

    [SerializeField]
    private string TextStr;

    public override void StartEntity()
    {
        base.StartEntity();

        Text.text = Tables.Instance.GetTable<Localize>().GetString(TextStr);
        Done();
    }
}

