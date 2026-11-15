using DG.Tweening;
using UnityEngine.Serialization;
using UnityEngine;
using BanpoFri;
using UnityEngine.UI;
using UniRx;
using TMPro;


[UIPath("HudCurrencyTop")]
public class HudCurrencyTop : UIBase
{
    [SerializeField]
    private bool IsBuffValueCheck;


    // [Header("Cash")]
    // [SerializeField]
    // private TextMeshProUGUI CashText;
    // [SerializeField]
    // private Transform CashIconTr;

    // public Transform GetCashIconTr { get { return CashIconTr; } }

    [Header("MoneyCoin")]
    [SerializeField]
    private TextMeshProUGUI MoneyText;

    [SerializeField]
    private Transform SilverCoinIconTr;

    [Header("Material")]
    [SerializeField]
    private TextMeshProUGUI MaterialText;

    [SerializeField]
    private Transform MaterialIconTr;



    public Transform GetSilverCoinIconTr { get { return SilverCoinIconTr; } }





    [Header("Reminder")]

    [SerializeField]
    private Button OptionBtn;
    [SerializeField]
    private Button ProfileBtn;


    [SerializeField]
    private GameObject MoneyRoot;

    public Transform GetMoneyRoot { get { return MoneyRoot.transform; } }

    [FormerlySerializedAs("StoneRoot")]
    [SerializeField]
    private GameObject CashRoot;

    [SerializeField]
    private GameObject MaterialRoot;

    public Transform GetMaterialRoot { get { return MaterialRoot.transform; } }


    private readonly bool[] RootStates = new bool[] { true, true, true };
    private readonly System.Numerics.BigInteger[] CurrencyValues = new System.Numerics.BigInteger[3];
    private Tweener[] Tweeners = new Tweener[3];


    private int MaxEnergy = 0;


    protected override void Awake()
    {
        base.Awake();
        SetDataHook();

        if (OptionBtn != null)
            OptionBtn.onClick.AddListener(OnClickOption);

        //if (BuyCoinBtn != null) BuyCoinBtn.onClick.AddListener(OnClickCoin);


    }

    private void Update()
    {
        SetTexts();
    }

    private void SetTexts()
    {
        if (MoneyText) MoneyText.text = ProjectUtility.CalculateMoneyToString((System.Numerics.BigInteger)CurrencyValues[0]);
        //if (CashText) CashText.text = ProjectUtility.CalculateMoneyToString((System.Numerics.BigInteger)CurrencyValues[1]);
        //if (MaterialText) MaterialText.text = ProjectUtility.CalculateMoneyToString((System.Numerics.BigInteger)CurrencyValues[2]);
    }

    public void RewardCheck()
    {

    }

    public void SyncReward()
    {
        CurrencyValues[0] = (int)GameRoot.Instance.UserData.Money.Value;
        CurrencyValues[1] = (int)GameRoot.Instance.UserData.Cash.Value;
        if (MoneyText) MoneyText.text = ProjectUtility.CalculateMoneyToString((System.Numerics.BigInteger)CurrencyValues[0]);
        //if (CashText) CashText.text = ProjectUtility.CalculateMoneyToString((System.Numerics.BigInteger)CurrencyValues[1]);
        //if (MaterialText) MaterialText.text = ProjectUtility.CalculateMoneyToString((System.Numerics.BigInteger)CurrencyValues[2]);
    }

    private void SetDataHook()
    {
        if (MoneyText != null)
        {
            MoneyText.text = ProjectUtility.CalculateMoneyToString(GameRoot.Instance.UserData.Money.Value);

            GameRoot.Instance.UserData.Money.Subscribe(x =>
            {
                if (!gameObject.activeInHierarchy)
                {
                    CurrencyValues[0] = (int)x;
                    return;
                }

                if (Tweeners[0] != null)
                {
                    Tweeners[0].Kill();
                    Tweeners[0] = null;
                }

                var startValue = CurrencyValues[0];
                var endValue = (System.Numerics.BigInteger)x;

                Tweeners[0] = DOVirtual.Float(0f, 1f, 0.5f, t =>
                {
                    CurrencyValues[0] = (System.Numerics.BigInteger)((double)startValue + (double)(endValue - startValue) * t);
                })
      .SetEase(Ease.Linear)
      .SetUpdate(true)
      .OnComplete(() =>
      {
          CurrencyValues[0] = endValue;
          Tweeners[0] = null;
      });

            }).AddTo(this);
        }

    //     if (CashText != null)
    //     {
    //         CashText.text = GameRoot.Instance.UserData.Cash.Value.ToString();

    //         GameRoot.Instance.UserData.Cash.Subscribe(x =>
    //         {
    //             if (!gameObject.activeInHierarchy)
    //             {
    //                 CurrencyValues[1] = (int)x;
    //                 return;
    //             }

    //             if (Tweeners[1] != null)
    //             {
    //                 Tweeners[1].Kill();
    //                 Tweeners[1] = null;
    //             }

    //             var startValue = CurrencyValues[1];
    //             var endValue = (System.Numerics.BigInteger)x;

    //             Tweeners[1] = DOVirtual.Float(0f, 1f, 0.2f, t =>
    //             {
    //                 CurrencyValues[1] = (System.Numerics.BigInteger)((double)startValue + (double)(endValue - startValue) * t);
    //             })
    //   .SetEase(Ease.Linear)
    //   .SetUpdate(true)
    //   .OnComplete(() =>
    //   {
    //       CurrencyValues[1] = endValue;
    //       Tweeners[1] = null;
    //   });
    //         }).AddTo(this);
    //     }


        if (MaterialText != null)
        {
            MaterialText.text = GameRoot.Instance.UserData.Material.Value.ToString();

            GameRoot.Instance.UserData.Material.Subscribe(x =>
            {
                if (!gameObject.activeInHierarchy)
                {
                    CurrencyValues[2] = (int)x;
                    return;
                }

                if (Tweeners[2] != null)
                {
                    Tweeners[2].Kill();
                    Tweeners[2] = null;
                }

                var startValue = CurrencyValues[1];
                var endValue = (System.Numerics.BigInteger)x;

                Tweeners[2] = DOVirtual.Float(0f, 1f, 0.2f, t =>
                {
                    CurrencyValues[2] = (System.Numerics.BigInteger)((double)startValue + (double)(endValue - startValue) * t);
                })
      .SetEase(Ease.Linear)
      .SetUpdate(true)
      .OnComplete(() =>
      {
          CurrencyValues[2] = endValue;
          Tweeners[2] = null;
      });
            }).AddTo(this);
        }
    }


    private void OnClickOption()
    {
        GameRoot.Instance.UISystem.OpenUI<PopupOption>(popup => popup.Set(false));
    }

    private void OnClickCoin()
    {
        //BuyCoinBtn.interactable = false;
        // GameRoot.Instance.UISystem.OpenUI<PopupCoinInsufficent>(x =>
        // {
        //     BuyCoinBtn.interactable = true;
        // });
    }


    public void SetCurrencyState(ShowFlag currencyShow)
    {
        // 루트가 프리팹에 미할당이면 스킵 — 여기서 예외가 나면 호출자(UIBase.OnShowBefore)의
        // 자식 재활성화 루프까지 끊겨서 열리는 팝업이 빈 화면으로 남는다.
        if (MoneyRoot != null)
            RootStates[0] = ShowRoot(MoneyRoot.transform, RootStates[0], currencyShow.HasFlag(ShowFlag.Coin));
        if (CashRoot != null)
            RootStates[1] = ShowRoot(CashRoot.transform, RootStates[1], currencyShow.HasFlag(ShowFlag.Cash));
        if (MaterialRoot != null)
            RootStates[2] = ShowRoot(MaterialRoot.transform, RootStates[2], currencyShow.HasFlag(ShowFlag.Material));

        if (OptionBtn != null)
            OptionBtn.gameObject.SetActive(currencyShow.HasFlag(ShowFlag.Setting));
    }

    private bool ShowRoot(Transform root, bool currentState, bool value)
    {
        if (currentState == value)
        {
            // RootStates 캐시는 value 와 일치하지만, 실제 gameObject.activeSelf 가 어긋난 케이스 보강.
            // (이전 OnComplete 누락, 외부 SetActive, 빠른 페이지 전환 등으로 캐시-실상태 divergence 발생 시
            //  early-return 만 하면 화폐가 영영 안 보이게 됨. 여기서 강제 동기화.)
            if (root != null && root.gameObject != null && root.gameObject.activeSelf != value)
            {
                ProjectUtility.SetActiveCheck(root.gameObject, value);
                // 켜질 때는 scale 도 1 로 즉시 보정 (scale 이 0 으로 남아 있으면 활성화되어도 안 보임)
                if (value)
                    root.localScale = Vector3.one;
            }
            return currentState;
        }

        root.DOKill();
        if (value)
        {
            // 보이게 할 거면 트윈 시작 전 즉시 active=true. SetActive 를 OnComplete 에 두면
            // 트윈이 어떤 이유로 중단됐을 때 영영 inactive 로 남는 위험이 있음.
            ProjectUtility.SetActiveCheck(root.gameObject, true);
            root.DOScale(1, 0.2f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
            return true;
        }
        else
        {
            root.DOScale(0, 0.2f)
                .SetEase(Ease.InBack)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    if (root != null && root.gameObject != null)
                        ProjectUtility.SetActiveCheck(root.gameObject, false);
                });
            return false;
        }
    }

    public Transform GetRewardEndTr(int rewardType, int rewardIdx)
    {
        switch ((Config.RewardType)rewardType)
        {
            case Config.RewardType.Currency:
                {
                    switch ((Config.CurrencyID)rewardIdx)
                    {
                        case Config.CurrencyID.Money:
                            return SilverCoinIconTr;
                        // case Config.CurrencyID.Cash:
                        //     return CashIconTr;
                        case Config.CurrencyID.Material:
                            return MaterialIconTr;
                    }
                    break;
                }
        }


        return null;
    }


}
