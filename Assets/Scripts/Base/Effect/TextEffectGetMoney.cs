using UnityEngine;
using BanpoFri;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using TMPro;

// 월드 좌표(광석 위치)에 뜨도록 WorldCanvas(InWorldUI)에 붙인다.
// (InUI=true 로 두면 스크린 스페이스 캔버스에 월드 좌표가 들어가 엉뚱한 위치에 뜬다.)
[EffectPath("Effect/TextEffectGetMoney", false, true)]
public class TextEffectGetMoney : Effect
{
    [SerializeField]
    private TextMeshProUGUI MoneyText;

    public void SetText(System.Numerics.BigInteger value)
    {
        MoneyText.text = $"+{ ProjectUtility.CalculateMoneyToString(value)}";
    }
}

