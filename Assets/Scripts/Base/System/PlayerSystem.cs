using UnityEngine;
using UniRx;
using BanpoFri;
public class PlayerSystem
{
    // 성(캐슬) 레벨업 — 기본 체력 150, 레벨당 +50. 비용은 50원에서 시작해 레벨업마다 +50원(최대 300원).
    public const int CastleMaxLevel = 40;
    private const int CastleBaseHp = 800;
    private const int CastleHpPerLevel = 50;
private const int CastleCostPerLevel = 50;
    private const int CastleMaxCost = 300;

    public int dice_ad_ingame_silvercoin = 0;

    public bool IsStartGame = false;


    public void Create()
    {
        var values = Tables.Instance.GetTable<Define>();
        dice_ad_ingame_silvercoin = values?.GetData("ingame_ad_money")?.value
            ?? values?.GetData("dice_ad_ingame_silvercoin")?.value
            ?? 50;
    }

    // 레벨 1 = 기본 체력 150, 이후 레벨당 +50. (레벨 - 1 기준) + 훈련 103 성 체력 고정 가산.
    public int GetCastleHp()
    {
        return CastleBaseHp + CastleHpPerLevel * Mathf.Max(0, GameRoot.Instance.UserData.Castlelevel.Value - 1);
            // + (int)GameRoot.Instance.TrainingSystem.GetBuffValue(TrainingSystem.TrainingType.CastleHpIncrease);
    }

    // 첫 레벨업(1→2) 50원, 레벨업마다 +50원, 최대 300원.
    public int GetCastleLevelUpCost()
    {
        return Mathf.Min(CastleMaxCost, CastleCostPerLevel * Mathf.Max(1, GameRoot.Instance.UserData.Castlelevel.Value));
    }

    public bool IsCastleMaxLevel()
    {
        return GameRoot.Instance.UserData.Castlelevel.Value >= CastleMaxLevel;
    }

    // 성 레벨업 시도 — 맥스 레벨이거나 재화가 부족하면 false.
    public bool CastleLevelUp()
    {
        if (IsCastleMaxLevel()) return false;

        var cost = GetCastleLevelUpCost();

        if (GameRoot.Instance.UserData.Money.Value < cost) return false;

        GameRoot.Instance.UserData.Money.Value -= cost;
        GameRoot.Instance.UserData.HUDMoney.Value = GameRoot.Instance.UserData.Money.Value;

        GameRoot.Instance.UserData.Castlelevel.Value += 1;
        GameRoot.Instance.UserData.Save();

        return true;
    }


    public void GetPlayerUnitInfoData(int playeridx)
    {
        // var td = Tables.Instance.GetTable<HeroInfo>().GetData(playeridx);
        //
        // if (td != null)
        // {
        //
        //     GameRoot.Instance.UserData.InGamePlayerData.CurHpProperty.Value = GameRoot.Instance.UserData.InGamePlayerData.StartHpProperty.Value = td.base_hp;
        //
        //     GameRoot.Instance.UserData.InGamePlayerData.CurShiledProperty.Value = 0;
        //
        //     GameRoot.Instance.UserData.InGamePlayerData.Damage = td.base_atk;
        // }
    }


}
