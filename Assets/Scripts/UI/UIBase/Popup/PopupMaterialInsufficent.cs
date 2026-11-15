using UnityEngine;
using BanpoFri;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

[UIPath("UI/Popup/PopupMaterialInsufficent")]
public class PopupMaterialInsufficent : CommonUIBase
{
    [SerializeField]
    private Button CloseBtn;

    protected override void Awake()
    {
        base.Awake();
        CloseBtn.onClick.AddListener(Hide);
    }
}

