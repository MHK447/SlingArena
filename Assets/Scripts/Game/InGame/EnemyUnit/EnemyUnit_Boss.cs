using UnityEngine;
using BanpoFri;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class EnemyUnit_Boss : EnemyUnitBase
{
    private const float HealPercent = 0.1f;
    private const int DefensePerCast = 10;
    private const int DefenseArmorMaxStack = 200;

    // 액션 가중치 (회복 : 일반공격 : 방어버프 : 더블어택 = 2 : 5 : 1 : 1 = 합 9)
    // 입력 비율 20:50:10:10 을 정규화한 값. 더블어택은 base.AttackPlayerSequence 를 두 번 수행.
    private const int WeightHeal = 2;
    private const int WeightAttack = 5;
    private const int WeightDefense = 1;
    private const int WeightDoubleAttack = 1;
    private const int WeightTotal = WeightHeal + WeightAttack + WeightDefense + WeightDoubleAttack;

    private enum BossAction { Heal, Attack, Defense, DoubleAttack }

    // 회복/방어 시퀀스의 정지 대기. 매 호출마다 new 하지 않도록 캐싱.
    private static readonly WaitForSeconds BuffSequenceWait = new(0.6f);
    // 더블어택 두 번째 타격 직전 잠깐 텀.
    private static readonly WaitForSeconds DoubleAttackInterval = new(0.2f);

    private int bossArmor = 0;

    // 한 턴에 두 번 행동하지 않도록 재진입 가드. 코루틴/콜백/리스트 중복 등 어떤 원인으로
    // AttackPlayerSequence 가 중첩 호출되더라도 두 번째는 즉시 yield break.
    private bool isPerformingAction = false;


    // HP 프로그레스의 보스 전용 비주얼(BossRoot) 활성화.
    protected override bool IsBoss => true;

    public override void Set(int enemyidx, int unitHp, int unitDmg)
    {
        base.Set(enemyidx, unitHp, unitDmg);
        bossArmor = 0;
        isPerformingAction = false;
    }

    public override void Damage(int damage, Color textColor, bool isCritical = false)
    {
        if (damage > 0 && bossArmor > 0)
        {
            float reduced = damage * (100f / (100f + bossArmor));
            damage = Mathf.Max(1, Mathf.RoundToInt(reduced));
        }

        base.Damage(damage, textColor, isCritical);
    }

    // 한 턴 = 1 행동. 회복 / 일반공격 / 방어버프 / 더블어택 중 가중치에 따라 정확히 하나 선택.
    // isPerformingAction 재진입 가드로 중첩 호출 시 두 번째는 무조건 yield break.
    public override IEnumerator AttackPlayerSequence(PlayerUnitBase playerUnit)
    {
        if (isPerformingAction) yield break;
        if (playerUnit == null || IsDead) yield break;
        if (playerUnit.CurState == PlayerUnitBase.PlayerUnitState.Dead) yield break;
        if (IsFrozen) yield break;

        isPerformingAction = true;
        try
        {
            BossAction action = PickAction();

            // 풀피일 때 회복이 굴려졌으면 턴을 낭비하지 않도록 회복을 제외한 가중치로 재선택.
            if (action == BossAction.Heal && CurHp >= MaxHpValue)
            {
                action = PickActionExcludingHeal();
            }

            switch (action)
            {
                case BossAction.Attack:
                    yield return base.AttackPlayerSequence(playerUnit);
                    break;
                case BossAction.Heal:
                    yield return HealSelfSequence();
                    break;
                case BossAction.Defense:
                    yield return IncreaseDefenseSequence();
                    break;
                case BossAction.DoubleAttack:
                    yield return DoubleAttackSequence(playerUnit);
                    break;
            }
        }
        finally
        {
            isPerformingAction = false;
        }
    }

    // 가중치 기반 행동 선택. WeightTotal 범위에서 굴려서 누적 가중치 구간에 해당하는 액션 반환.
    private static BossAction PickAction()
    {
        int roll = Random.Range(0, WeightTotal);
        int cumulative = WeightHeal;
        if (roll < cumulative) return BossAction.Heal;
        cumulative += WeightAttack;
        if (roll < cumulative) return BossAction.Attack;
        cumulative += WeightDefense;
        if (roll < cumulative) return BossAction.Defense;
        return BossAction.DoubleAttack;
    }

    // Heal 만 제외한 가중치 (Attack : Defense : DoubleAttack = 5 : 1 : 1) 로 재선택.
    private static BossAction PickActionExcludingHeal()
    {
        int total = WeightAttack + WeightDefense + WeightDoubleAttack;
        int roll = Random.Range(0, total);
        int cumulative = WeightAttack;
        if (roll < cumulative) return BossAction.Attack;
        cumulative += WeightDefense;
        if (roll < cumulative) return BossAction.Defense;
        return BossAction.DoubleAttack;
    }

    // 더블어택 — base 공격 시퀀스를 연속 2회 수행. 한 턴 한 행동이라는 룰은 유지하되,
    // "더블어택" 이라는 단일 액션이 내부적으로 두 번 타격하도록 정의.
    private IEnumerator DoubleAttackSequence(PlayerUnitBase playerUnit)
    {
        if (playerUnit == null || playerUnit.CurState == PlayerUnitBase.PlayerUnitState.Dead)
            yield break;

        yield return base.AttackPlayerSequence(playerUnit);

        if (playerUnit == null || playerUnit.CurState == PlayerUnitBase.PlayerUnitState.Dead || IsDead)
            yield break;

        yield return DoubleAttackInterval;
        yield return base.AttackPlayerSequence(playerUnit);
    }

    private IEnumerator HealSelfSequence()
    {
        // 다른 행동이 절대 같이 안 나오도록 시퀀스 시작 시 즉시 Idle 로 고정.
        SetState(EnemyState.Idle);

        int healAmount = Mathf.Max(1, Mathf.RoundToInt(MaxHpValue * HealPercent));
        int before = CurHp;
        CurHp = Mathf.Min(CurHp + healAmount, MaxHpValue);
        int healed = CurHp - before;

        SetHp(CurHp);

        if (healed > 0)
        {
            GameRoot.Instance.DamageTextSystem.ShowDamage(healed, transform.position + Vector3.up * 2f, Color.green);
            GameRoot.Instance.EffectSystem.MultiPlay<HealEffect>(transform.position, x =>
            {
                if (x == null) return;
                x.transform.localScale = Vector3.one;
                x.Play(transform.position, transform);
                x.SetAutoRemove(true, 2f);
            }, transform);
        }

        yield return BuffSequenceWait;
    }

    private IEnumerator IncreaseDefenseSequence()
    {
        SetState(EnemyState.Idle);

        bossArmor = Mathf.Min(bossArmor + DefensePerCast, DefenseArmorMaxStack);

        SoundPlayer.Instance.PlaySound("effect_item_shield");

        var armorTextColor = new Color(0.3f, 0.6f, 1f);
        GameRoot.Instance.DamageTextSystem.ShowString($"+{DefensePerCast}", transform.position + Vector3.up * 2f, armorTextColor);
        GameRoot.Instance.EffectSystem.MultiPlay<ArmorUpEffect>(transform.position, x =>
        {
            if (x == null) return;
            x.transform.localScale = Vector3.one;
            x.Play(transform.position, transform);
            x.SetAutoRemove(true, 2f);
        }, transform);

        yield return BuffSequenceWait;
    }
}
