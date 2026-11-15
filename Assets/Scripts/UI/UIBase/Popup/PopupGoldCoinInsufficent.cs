using UnityEngine;
using BanpoFri;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

[UIPath("UI/Popup/PopupGoldCoinInsufficent")]
public class PopupGoldCoinInsufficent : CommonUIBase
{
    [SerializeField]
    private Button CloseBtn;


    protected override void Awake()
    {
        base.Awake();
        CloseBtn.onClick.AddListener(Hide);
    }
}

