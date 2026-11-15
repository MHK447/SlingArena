using System;
using Google.FlatBuffers;
using UniRx;
using UnityEngine;

public partial class UserDataSystem
{
    public InGamePlayerData InGamePlayerData { get; private set; } = new InGamePlayerData();
    private void SaveData_PlayerData(FlatBufferBuilder builder)
    {
        // 선언된 변수들은 모두 저장되어야함



        Action cbAddDatas = () =>
        {
        };

        cb_SaveAddDatas += cbAddDatas;

    }
    private void LoadData_PlayerData()
    {
        // 로드 함수 내용
    }

}



public class InGamePlayerData
{
    public string Playername { get; set; } = "Player";

    public int Playerexp = 0;

    public IReactiveProperty<int> StartHpProperty { get; private set; } = new ReactiveProperty<int>(0);
    public IReactiveProperty<int> CurShiledProperty { get; private set; } = new ReactiveProperty<int>(0);
    public IReactiveProperty<int> ArmorProperty { get; private set; } = new ReactiveProperty<int>(0);

    public IReactiveProperty<int> CurHpProperty { get; private set; } = new ReactiveProperty<int>(0);

    public IReactiveProperty<int> RemainingEnemyCountProperty { get; private set; } = new ReactiveProperty<int>(0);


    public IReactiveProperty<int> PlayerLevelProperty { get; private set; } = new ReactiveProperty<int>(1);
    public IReactiveProperty<int> CurExpProperty { get; private set; } = new ReactiveProperty<int>(0);



    public IReactiveProperty<int> InGameUpgradeCountProperty { get; private set; } = new ReactiveProperty<int>(1);


    public IReactiveProperty<int> KillCountProperty = new ReactiveProperty<int>(0);

    public IReactiveProperty<bool> IsWaveRestProperty = new ReactiveProperty<bool>(false);

    // 맵 이동(카메라 스크롤) 진행 중일 때만 true. NextWaveSequence/BattleIntroSequence의 Move()~Stop() 구간.
    public IReactiveProperty<bool> IsMapMovingProperty = new ReactiveProperty<bool>(false);

    public IReactiveProperty<bool> IsGameStartProperty = new ReactiveProperty<bool>(false);

    



    public IReactiveProperty<int> NewDiceInTurnProperty { get; private set; } = new ReactiveProperty<int>(3);
    public int InGameReRollCount = 0;
    public int InGameBuySlotCount = 0;
    public int Damage = 1;

    public void UpdatePlayerLevelFromStageIndex()
    {
        GameRoot.Instance.UserData.InGamePlayerData.SetPlayerLevel(GameRoot.Instance.UserData.Stageidx.Value);
    }

    public void SetPlayerLevel(int level)
    {
        // level = Mathf.Max(level, Playerlevel);
        // Playerlevel = level;
    }

    public void StageClear()
    {
        Damage = 0;
        RemainingEnemyCountProperty.Value = 0;
        CurExpProperty.Value = 0;
        PlayerLevelProperty.Value = 1;
        InGameUpgradeCountProperty.Value = 1;
        KillCountProperty.Value = 0;
        NewDiceInTurnProperty.Value = 3;
        InGameReRollCount = 0;
        InGameBuySlotCount = 0;
        CurShiledProperty.Value = 0;
        IsWaveRestProperty.Value = false;
        IsMapMovingProperty.Value = false;
        IsGameStartProperty.Value = false;
    }


    public void SetPlayerHp(int hp)
    {
        StartHpProperty.Value = CurHpProperty.Value = hp;
    }

}
