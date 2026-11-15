using UnityEngine;
using BanpoFri;
using UniRx;

// 슬라임 강화(훈련) 시스템.
// - 3종 업그레이드: 공격력(Attack) / 공격속도(AttackSpeed) / 코인 증가(SilverCoinIncrease).
// - 레벨은 UserData(FlatBuffers, Master.dat)에 저장/로드된다.
//     UserData.Trainingatk / Trainingatkspeed / Trainingcoin (ReactiveProperty<int>)
// - 값 / 비용은 TrainingInfo 테이블(upgrade_idx = (int)TrainingType)에서 계산한다.
//     값  = base_value + value_increase * level
//     비용 = base_cost  + cost_increase  * level
public class TrainingSystem
{
    public enum TrainingType
    {
        Attack = 1,
        AttackSpeed = 2,
        SilverCoinIncrease = 3,
    }

    public void Create()
    {
        // 레벨은 UserData 로드 시점에 이미 채워지므로 별도 로드는 필요 없다.
    }

    // 현재(저장된) 업그레이드 레벨.
    public int GetLevel(TrainingType type)
    {
        var ud = GameRoot.Instance.UserData;
        switch (type)
        {
            case TrainingType.Attack: return ud.Trainingatk.Value;
            case TrainingType.AttackSpeed: return ud.Trainingatkspeed.Value;
            case TrainingType.SilverCoinIncrease: return ud.Trainingcoin.Value;
        }
        return 0;
    }

    // UI 등이 레벨 변화를 UniRx 로 구독할 수 있도록 레벨 ReactiveProperty 를 노출한다.
    public IReactiveProperty<int> GetLevelProperty(TrainingType type)
    {
        var ud = GameRoot.Instance.UserData;
        switch (type)
        {
            case TrainingType.Attack: return ud.Trainingatk;
            case TrainingType.AttackSpeed: return ud.Trainingatkspeed;
            case TrainingType.SilverCoinIncrease: return ud.Trainingcoin;
        }
        return ud.Trainingatk;
    }

    private void SetLevel(TrainingType type, int level)
    {
        var ud = GameRoot.Instance.UserData;
        switch (type)
        {
            case TrainingType.Attack: ud.Trainingatk.Value = level; break;
            case TrainingType.AttackSpeed: ud.Trainingatkspeed.Value = level; break;
            case TrainingType.SilverCoinIncrease: ud.Trainingcoin.Value = level; break;
        }
    }

    // 현재 레벨 기준 강화 값. base_value + value_increase * level.
    // (레벨 0 이면 base_value. 슬라임 데미지/공격속도/코인에 더해지는 값.)
    public double GetValue(TrainingType type)
    {
        var td = Tables.Instance.GetTable<TrainingInfo>().GetData((int)type);
        if (td == null) return 0;
        return (double)td.base_value + (double)td.value_increase * GetLevel(type);
    }

    // 다음 레벨로 올릴 때 드는 비용. base_cost + cost_increase * level.
    public int GetCost(TrainingType type)
    {
        var td = Tables.Instance.GetTable<TrainingInfo>().GetData((int)type);
        if (td == null) return 0;
        return td.base_cost + td.cost_increase * GetLevel(type);
    }

    // 업그레이드(레벨 +1)하고 저장한다.
    // (재화 차감/보유 체크는 UI/호출부에서 GetCost 로 처리한다.)
    public void Upgrade(TrainingType type)
    {
        SetLevel(type, GetLevel(type) + 1);
        GameRoot.Instance.UserData.Save();
    }
}
