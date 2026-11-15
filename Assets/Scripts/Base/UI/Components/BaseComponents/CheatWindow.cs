using System.Collections;
using System.Numerics;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BanpoFri;
using System;
using System.Linq;

public class CheatWindow : MonoBehaviour
{
    [SerializeField]
    private InputField inputField;

    public void SetMoney()
    {
        if (string.IsNullOrEmpty(inputField.text))
        {
            BpLog.LogError("input field empty!");
            return;
        }
        BigInteger convert;
        if (!BigInteger.TryParse(inputField.text, out convert))
        {
            BpLog.LogError("input field string don't convert number!");
            return;
        }
        inputField.text = "";
        GameRoot.Instance.UserData.Money.Value += convert;
        GameRoot.Instance.UserData.HUDMoney.Value += convert;
    }


    public void SetInGameSilverCoin()
    {
        if (string.IsNullOrEmpty(inputField.text))
        {
            BpLog.LogError("input field empty!");
            return;
        }
        BigInteger convert;
        if (!BigInteger.TryParse(inputField.text, out convert))
        {
            BpLog.LogError("input field string don't convert number!");
            return;
        }
        inputField.text = "";
        GameRoot.Instance.UserData.Ingamesilvercoin.Value += (int)convert;
    }


    public void SetEnergeyMoney()
    {
        if (string.IsNullOrEmpty(inputField.text))
        {
            BpLog.LogError("input field empty!");
            return;
        }
        BigInteger convert;
        if (!BigInteger.TryParse(inputField.text, out convert))
        {
            BpLog.LogError("input field string don't convert number!");
            return;
        }
        inputField.text = "";
        GameRoot.Instance.UserData.CurMode.EnergyMoney.Value += convert;
    }

    public void SetHardStage()
    {
        if (string.IsNullOrEmpty(inputField.text))
        {
            BpLog.LogError("input field empty!");
            return;
        }
        BigInteger convert;
        if (!BigInteger.TryParse(inputField.text, out convert))
        {
            BpLog.LogError("input field string don't convert number!");
            return;
        }
        inputField.text = "";

        GameRoot.Instance.UserData.Hardmodestageidx.Value = (int)convert;
    }

    public void SetCash()
    {
        if (string.IsNullOrEmpty(inputField.text))
        {
            BpLog.LogError("input field empty!");
            return;
        }

        BigInteger convert;
        if (!BigInteger.TryParse(inputField.text, out convert))
        {
            BpLog.LogError("input field string don't convert number!");
            return;
        }

        inputField.text = "";

        if (convert > int.MaxValue || (convert + GameRoot.Instance.UserData.Cash.Value) > int.MaxValue)
        {
            GameRoot.Instance.UserData.Cash.Value = int.MaxValue;
            GameRoot.Instance.UserData.HUDCash.Value = int.MaxValue;
        }
        else
        {
            GameRoot.Instance.UserData.Cash.Value += (int)convert;
            GameRoot.Instance.UserData.HUDCash.Value += (int)convert;
        }
    }

    public void AddAllCard()
    {
        // var tdlist = Tables.Instance.GetTable<CardInfo>().DataList.ToList();


        // foreach (var td in tdlist)
        // {
        //     GameRoot.Instance.CardSystem.AddCard(td.card_idx);
        // }

        GameRoot.Instance.UserData.Save();
    }

    public void SetOneTimeTicket()
    {
        if (string.IsNullOrEmpty(inputField.text))
        {
            BpLog.LogError("input field empty!");
            return;
        }

        BigInteger convert;
        if (!BigInteger.TryParse(inputField.text, out convert))
        {
            BpLog.LogError("input field string don't convert number!");
            return;
        }

        inputField.text = "";

        // if (convert > int.MaxValue || (convert + GameRoot.Instance.UserData.Onetimedata.OneTimeExpandCountProperty.Value) > int.MaxValue)
        // {
        //     GameRoot.Instance.UserData.Onetimedata.OneTimeExpandCountProperty.Value = int.MaxValue;
        // }
        // else
        // {
        //     GameRoot.Instance.UserData.Onetimedata.OneTimeExpandCountProperty.Value =
        //     GameRoot.Instance.UserData.Onetimedata.CurRankCount += (int)convert;
        // }
    }



    public void SetPlayerUnit()
    {
        BigInteger convert;
        if (!BigInteger.TryParse(inputField.text, out convert))
        {
            BpLog.LogError("input field string don't convert number!");
            return;
        }
        inputField.text = "";
        GameRoot.Instance.UserData.Herogroudata.Equipplayeridx = (int)convert;
    }



    public void SetOneTimeRankReward()
    {
        BigInteger convert;
        if (!BigInteger.TryParse(inputField.text, out convert))
        {
            BpLog.LogError("input field string don't convert number!");
            return;
        }
        inputField.text = "";
        // GameRoot.Instance.UserData.Onetimedata.RankRewardIdx = (int)convert;
    }

    
    // 입력한 idx 의 주사위를 인게임 주사위 트레이(PopupInGame.DiceControllerGroup)에 추가.
    public void SetDiceCount()
    {
        if (string.IsNullOrEmpty(inputField.text))
        {
            BpLog.LogError("input field empty!");
            return;
        }

        BigInteger convert;
        if (!BigInteger.TryParse(inputField.text, out convert))
        {
            BpLog.LogError("input field string don't convert number!");
            return;
        }

        inputField.text = "";

        // var popupInGame = GameRoot.Instance.UISystem.GetUI<PopupInGame>();
        // if (popupInGame == null)
        // {
        //     BpLog.LogError("PopupInGame not available!");
        //     return;
        // }
    }


    public void SetMaterial()
    {
        if (string.IsNullOrEmpty(inputField.text))
        {
            BpLog.LogError("input field empty!");
            return;
        }

        BigInteger convert;
        if (!BigInteger.TryParse(inputField.text, out convert))
        {
            BpLog.LogError("input field string don't convert number!");
            return;
        }

        inputField.text = "";

        if (convert > int.MaxValue || (convert + GameRoot.Instance.UserData.Material.Value) > int.MaxValue)
        {
            GameRoot.Instance.UserData.Material.Value = int.MaxValue;
            GameRoot.Instance.UserData.HUDMaterial.Value = int.MaxValue;
        }
        else
        {
            GameRoot.Instance.UserData.Material.Value += (int)convert;
            GameRoot.Instance.UserData.HUDMaterial.Value += (int)convert;
        }
    }

    public void SetStartTutorial()
    {
        if (string.IsNullOrEmpty(inputField.text))
        {
            BpLog.LogError("input field empty!");
            return;
        }

        BigInteger convert;
        if (!BigInteger.TryParse(inputField.text, out convert))
        {
            BpLog.LogError("input field string don't convert number!");
            return;
        }

        var id = convert.ToString();
        if (GameRoot.Instance.TutorialSystem.IsClearTuto(id.ToString()))
        {
            GameRoot.Instance.UserData.Tutorial.Remove(id.ToString());
        }


        inputField.text = "";

        GameRoot.Instance.TutorialSystem.StartTutorial(convert.ToString());
    }

    public void StartChoiceLevelUpReward()
    {
       // GameRoot.Instance.UISystem.OpenUI<PopupLevelUpReward>(popup => popup.Init(true));
    }



    public void AddEquipment()
    {


    }


    public void ALlAddEquipment()
    {


    }

    public void AddHeroUpgradeCard()
    {
        if (string.IsNullOrEmpty(inputField.text))
        {
            BpLog.LogError("input field empty!");
            return;
        }

        BigInteger convert;
        if (!BigInteger.TryParse(inputField.text, out convert))
        {
            BpLog.LogError("input field string don't convert number!");
            return;
        }

        inputField.text = "";

        GameRoot.Instance.UserData.Herocardupgradecard.Value += (int)convert;

    }

    public void AddTenItem()
    {
        var tdlist = Tables.Instance.GetTable<ItemInfo>().DataList.ToList();

        for (int i = 0; i < tdlist.Count; i++)
        {
            GameRoot.Instance.UserData.SetReward((int)Config.RewardType.Item, tdlist[i].idx, 10);
        }

    }

    public void CheatMedeationCheck()
    {
        // if (!AdxSDK.IsInitialized())
        // {
        //     BpLog.LogWarning("MAX SDK is not initialized. Initialize before opening debugger.");
        //     return;
        // }

        // // Open the AppLovin MAX mediation debugger to verify adapter connectivity.
        // MaxSdk.ShowMediationDebugger();
        // BpLog.Log("Opened MAX mediation debugger.");
    }




    public void OnClick_Hide()
    {
        GameRoot.Instance.SetCheatWindow(false);
    }

    public void SetStageMove()
    {
        if (string.IsNullOrEmpty(inputField.text))
        {
            BpLog.LogError("input field empty!");
            return;
        }

        BigInteger convert;
        if (!BigInteger.TryParse(inputField.text, out convert))
        {
            BpLog.LogError("input field string don't convert number!");
            return;
        }

        inputField.text = "";

        GameRoot.Instance.UserData.Stageidx.Value = (int)convert;
    }

    public void SetWaveStart()
    {
        if (string.IsNullOrEmpty(inputField.text))
        {
            BpLog.LogError("input field empty!");
            return;
        }

        BigInteger convert;
        if (!BigInteger.TryParse(inputField.text, out convert))
        {
            BpLog.LogError("input field string don't convert number!");
            return;
        }

        inputField.text = "";

        int targetWave = (int)convert;
        if (targetWave < 1)
        {
            BpLog.LogError("target wave must be >= 1");
            return;
        }

        var stage = GameRoot.Instance.InGameSystem.GetInGame<InGameBase>()?.Stage;
        if (stage == null)
        {
            BpLog.LogError("InGame stage not available!");
            return;
        }

        // 현재 wave 의 잔여 적/주사위 사용 큐 정리 — NextWaveSequence 의 정리 로직과 동일한 효과.
        // if (stage.GetEnemyUnitGroup != null)
        //     stage.GetEnemyUnitGroup.ClearData();
        // if (stage.DiceController != null)
        //     stage.DiceController.ClearAllDiceUse();

        // StartRest 가 내부에서 Waveidx 를 +1 하므로 target-1 로 세팅.
        GameRoot.Instance.UserData.Waveidx.Value = targetWave - 1;

        // 일반 웨이브 종료 흐름을 그대로 태움 — 맵/카메라 스크롤 + 다음 웨이브 적 등장 연출 재사용.
        // stage.StartRest();
    }


    public void SetHardStageMove()
    {
        if (string.IsNullOrEmpty(inputField.text))
        {
            BpLog.LogError("input field empty!");
            return;
        }

        BigInteger convert;
        if (!BigInteger.TryParse(inputField.text, out convert))
        {
            BpLog.LogError("input field string don't convert number!");
            return;
        }

        inputField.text = "";

        GameRoot.Instance.UserData.Hardmodestageidx.Value = (int)convert;
    }

    public void AddInGameDice()
    {

        if (string.IsNullOrEmpty(inputField.text))
        {
            BpLog.LogError("input field empty!");
            return;
        }

        BigInteger convert;
        if (!BigInteger.TryParse(inputField.text, out convert))
        {
            BpLog.LogError("input field string don't convert number!");
            return;
        }

        inputField.text = "";

        var stage = GameRoot.Instance.InGameSystem.GetInGame<InGameBase>()?.Stage;
        if (stage == null /* || stage.DiceController == null */)
        {
            BpLog.LogError("InGame stage or DiceController not available!");
            return;
        }

        // for (int i = 0; i < 10; ++i)
        //     stage.DiceController.AddDice((int)convert);
    }

    public void StageClearOpenCheat()
    {
        //GameRoot.Instance.UISystem.OpenUI<PopupStageResult>(popup => popup.Set(true));
    }

    public void ThreeInGameUpgradeOpen()
    {
        // [정리] 옛 InGameUpgrade/PopupLevelUpReward 시스템 제거 — 치트 비활성화
    }


    public void SetStageClear()
    {
        //GameRoot.Instance.UISystem.OpenUI<PopupStageResult>(popup => popup.Init(true));
    }

    public void ShowSelectChoice()
    {
        //GameRoot.Instance.UISystem.OpenUI<PopupLevelUpReward>(popup => popup.Init(true));
    }


    public void SetTicket()
    {
        if (string.IsNullOrEmpty(inputField.text))
        {
            BpLog.LogError("input field empty!");
            return;
        }
        BigInteger convert;
        if (!BigInteger.TryParse(inputField.text, out convert))
        {
            BpLog.LogError("input field string don't convert number!");
            return;
        }
        inputField.text = "";
        GameRoot.Instance.UserData.CurMode.GachaCoin.Value += (int)convert;
    }

    public void AddAllUnit()
    {
        // var tdlist = Tables.Instance.GetTable<UnitInfo>().DataList.ToList();

        // foreach (var td in tdlist)
        // {
        //     GameRoot.Instance.UserData.Unitgroupdata.AddUnit(td.idx);
        // }
    }

    public void SetStarAdd()
    {
        if (string.IsNullOrEmpty(inputField.text))
        {
            BpLog.LogError("input field empty!");
            return;
        }
        BigInteger convert;
        if (!BigInteger.TryParse(inputField.text, out convert))
        {
            BpLog.LogError("input field string don't convert number!");
            return;
        }
        inputField.text = "";
        GameRoot.Instance.UserData.Starcoinvalue.Value += (int)convert;
    }

    public void AddRewardBox()
    {
        if (string.IsNullOrEmpty(inputField.text))
        {
            BpLog.LogError("input field empty!");
            return;
        }
        BigInteger convert;
        if (!BigInteger.TryParse(inputField.text, out convert))
        {
            BpLog.LogError("input field string don't convert number!");
            return;
        }
        inputField.text = "";

        var stage = GameRoot.Instance.InGameSystem.GetInGame<InGameBase>()?.Stage;
        if (stage == null /* || stage.DiceController == null */)
        {
            BpLog.LogError("InGame stage or DiceController not available!");
            return;
        }

        // stage.DiceController.AddRandomDice((int)convert);
    }



#if UNITY_EDITOR
    [UnityEditor.MenuItem("BanpoFri/ShowCheat")]
    static void ShowCheat()
    {
        if (!UnityEditor.EditorApplication.isPlaying) return;
        var root = GameRoot.GetInstance();
        if (root != null) root.SetCheatWindow(true);
    }
#endif
}
