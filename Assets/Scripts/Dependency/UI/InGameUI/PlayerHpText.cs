using UnityEngine;
using BanpoFri;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using TMPro;
[FloatUIPath("UI/InGame/PlayerHpText")]
[FloatingDepth((int)Config.FloatingUIDepth.HpProgress)]
public class PlayerHpText : InGameFloatingUI
{
    [SerializeField]
    private TextMeshProUGUI HpText;

    public void SetHpText(int curhp)
    {
        HpText.text = ProjectUtility.AbbreviateNumber(curhp);
    }
}

