using UnityEngine;
using BanpoFri;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class EnemyUnit_ArmorTanker : EnemyUnitBase
{
    // ArmorTanker 종족 보정.
    // damage -20% / armor +20% (피격 데미지 20% 감소) / HP +20%
    private const float DamageMultiplier = 0.8f;
    private const float HpMultiplier = 1.2f;
    private const float IncomingDamageMultiplier = 0.8f; // armor +20% → 받는 피해 20% 감소

    public override void Set(int enemyidx, int unitHp, int unitDmg)
    {
        // WaveInfo가 넘긴 기본 hp/dmg에 ArmorTanker 보정을 적용한 뒤 상위 Set에 전달.
        int boostedHp = Mathf.Max(1, Mathf.RoundToInt(unitHp * HpMultiplier));
        int reducedDmg = Mathf.Max(0, Mathf.RoundToInt(unitDmg * DamageMultiplier));
        base.Set(enemyidx, boostedHp, reducedDmg);
    }

    public override void Damage(int damage, Color textColor, bool isCritical = false)
    {
        // armor +20% — 받는 데미지 20% 감소(최소 1 보장, 0 입력은 0 유지).
        int reduced = damage > 0
            ? Mathf.Max(1, Mathf.RoundToInt(damage * IncomingDamageMultiplier))
            : 0;
        base.Damage(reduced, textColor, isCritical);
    }
}
