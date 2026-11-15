using System.Collections;
using System.Collections.Generic;
using TMPro;
using BanpoFri;
using UnityEngine;
using UnityEngine.UI;




[UIPath("UI/Page/HUDTotal", true)]
public class HUDTotal : UIBase
{
    [SerializeField]
    private Button ShopBtn;

    [SerializeField]
    private GameObject ShopReddotObj;

    [SerializeField]
    private Image ReddotImg;

    [SerializeField]
    private TextMeshProUGUI FreeTextObj;

    [SerializeField]
    private Image HeroIconImg;

    public Transform GetHeroBtnTr => HeroIconImg != null ? HeroIconImg.transform : null;

    public Queue<System.Action> ActionQueue = new Queue<System.Action>();
    private Coroutine waitQueue = null;

    private List<UIBase> UIList = new List<UIBase>();

 
    protected override void Awake()
    {
    }


}
