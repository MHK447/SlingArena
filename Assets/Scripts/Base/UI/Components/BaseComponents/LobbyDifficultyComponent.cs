using UnityEngine;
using BanpoFri;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class LobbyDifficultyComponent : MonoBehaviour
{
    [SerializeField]
    private Button EasyBtn;

    [SerializeField]
    private Button HardBtn;


    void Awake()
    {
        EasyBtn.onClick.AddListener(OnClickEasyBtn);
        HardBtn.onClick.AddListener(OnClickHardBtn);

    }

    public void Init()
    {
        ProjectUtility.SetActiveCheck(this.gameObject, GameRoot.Instance.ContentsOpenSystem.ContentsOpenCheck(ContentsOpenSystem.ContentsOpenType.DifficultyOpen));

        GameRoot.Instance.WaitTimeAndCallback(0.2f, () =>
        {
            RefreshButtonStates();
        });
    }

    /// <summary>
    /// 현재 난이도에 맞춰 두 버튼의 Animator 상태를 항상 쌍으로 갱신 (한쪽만 Disabled로 두면 다른 쪽이 Normal로 안 바뀌어 둘 다 Normal이 되는 현상 방지)
    /// </summary>
    private void RefreshButtonStates()
    {
        var animEasy = EasyBtn.GetComponent<Animator>();
        var animHard = HardBtn.GetComponent<Animator>();
        if (GameRoot.Instance.UserData.DifficultyTypeProperty.Value == DifficultyType.Easy)
        {
            animEasy.Play("Normal", 0, 0f);
            animHard.Play("Disabled", 0, 0f);
        }
        else
        {
            animEasy.Play("Disabled", 0, 0f);
            animHard.Play("Normal", 0, 0f);
        }
    }

    public void OnClickEasyBtn()
    {
        GameRoot.Instance.UserData.DifficultyTypeProperty.Value = DifficultyType.Easy;
        RefreshButtonStates();

        GameRoot.Instance.UISystem.GetUI<PageLobbyBattle>()?.Init();
    }

    public void OnClickHardBtn()
    {
        GameRoot.Instance.UserData.DifficultyTypeProperty.Value = DifficultyType.Hard;
        RefreshButtonStates();

        GameRoot.Instance.UISystem.GetUI<PageLobbyBattle>()?.Init();
    }
}

