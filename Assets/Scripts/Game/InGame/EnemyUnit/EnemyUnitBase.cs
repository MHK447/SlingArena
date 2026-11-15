using UnityEngine;
using BanpoFri;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using DG.Tweening;

// lasttowerdefense 의 EnemyUnitBase 이식.
// 주사위 계열(InGameUpgradeSystem 의 ExecuteKill, InGameLevelUpBuffSystem 의 ElectrickShockDice)과
// 경험치 지급(ExpProgressComponent — DMK 미이식) 로직만 제거했고 나머지 전투 AI/연출은 동일하다.
public class EnemyUnitBase : MonoBehaviour
{
    public enum EnemyState
    {
        Idle,
        Run,
        Attack,
        Dead,
        Damage,
    }

    [SerializeField]
    protected Animator EnemyAnim;

    [SerializeField]
    private List<SpriteRenderer> EnemyImageList = new List<SpriteRenderer>();

    // Dead 애니메이션이 자식 GameObject 들의 m_IsActive 와 bone(Roll/Animal_bone/Body_bone 등) 의
    // localPosition/Rotation/Scale 을 바꿔놓는데 Idle/Walk 에는 그 path 키프레임이 없어서
    // 풀 재사용 시 원복되지 않는다. 첫 Set() 시점의 prefab 원본 상태를 캐싱해두고 그대로 되돌린다.
    // (root transform 은 spawn 위치라 제외)
    private Dictionary<GameObject, bool> _originalActiveStates;
    private Dictionary<Transform, Vector3> _originalLocalPositions;
    private Dictionary<Transform, Quaternion> _originalLocalRotations;
    private Dictionary<Transform, Vector3> _originalLocalScales;
    private bool _originalStatesCaptured = false;

    private bool isDamageFlashing = false;

    // 애니메이션 이벤트 콜백용
    private bool enemyAttackHitTriggered = false;
    private PlayerUnitBase currentAttackPlayerTarget;

    [SerializeField]
    private AnimActionComponent AnimActionComponent;

    protected InGameHpProgress InGameHpProgress;

    protected EnemyState CurState = EnemyState.Idle;

    public int EnemyIdx { get; private set; }

    public bool IsDead => CurState == EnemyState.Dead;

    public bool IsFrozen { get; private set; } = false;
    public bool IsPoisonedVisual { get; private set; } = false;

    // WaveInfo에서 전달된 이 적의 고유 스탯
    protected int CurHp;
    protected int MaxHp;
    public int MaxHpValue => MaxHp;
    public int CurHpValue => CurHp;
    public int AttackDamage { get; protected set; }

    // 처치 시 플레이어에게 지급되는 경험치. EnemyUnitGroup에서 wave_exp / 적 수로 계산해 주입.
    // (경험치 UI(ExpProgressComponent)는 아직 DMK 에 이식 전이라 값만 보관한다.)
    public int ExpReward { get; set; }

    private static readonly Color FrozenTint = new Color(0.5f, 0.75f, 1f);
    private static readonly Color PoisonTint = new Color(0.4f, 1f, 0.4f);

    // HP 프로그레스 바를 따라다니게 할 때 적용할 오프셋. 보스처럼 큰 유닛은 머리 위로 올리고 싶을 때 오버라이드.

    [SerializeField]
    protected Vector3 HpProgressOffset = new Vector3(0f, 2f, 0f);

    // 보스 전용 HP 바 표시 여부. true 면 HP 프로그레스의 BossRoot 가 켜져 보스용 비주얼이 적용됨.
    protected virtual bool IsBoss => false;

    public virtual void SetFrozen(bool frozen)
    {
        IsFrozen = frozen;

        if (isDamageFlashing) return;

        Color tint = frozen ? FrozenTint : Color.white;
        foreach (var sprite in EnemyImageList)
        {
            if (sprite != null)
                sprite.color = tint;
        }
    }

    public virtual void SetPoisoned(bool poisoned)
    {
        IsPoisonedVisual = poisoned;

        if (isDamageFlashing) return;
        if (IsFrozen) return;

        Color tint = poisoned ? PoisonTint : Color.white;
        foreach (var sprite in EnemyImageList)
        {
            if (sprite != null)
                sprite.color = tint;
        }
    }

    public virtual void Set(int enemyidx, int unitHp, int unitDmg)
    {
        if (AnimActionComponent != null)
        {
            AnimActionComponent.AnimAction_1 = OnEnemyAttackHit;
            AnimActionComponent.AnimAction_Dead = DeadAfter;
        }

        // 재활용 시 이전 사이클의 잔존 상태(피격 플래시, 동결, 스프라이트 알파, 좌우 반전 등)를 초기화한다.
        ResetVisualState();

        EnemyIdx = enemyidx;

        // WaveInfo의 unit_hp / unit_dmg를 따라간다.
        MaxHp = Mathf.Max(1, unitHp);
        CurHp = MaxHp;
        AttackDamage = Mathf.Max(0, unitDmg);

        SetHp(CurHp);
        SetState(EnemyState.Idle);

        var td = Tables.Instance.GetTable<EnemyInfo>().GetData(enemyidx);

        if (td != null)
        {

        }
    }

    protected virtual void ResetVisualState()
    {
        isDamageFlashing = false;
        IsFrozen = false;
        enemyAttackHitTriggered = false;
        currentAttackPlayerTarget = null;

        CaptureOriginalStatesOnce();
        RestoreOriginalStates();

        foreach (var sprite in EnemyImageList)
        {
            if (sprite != null)
                sprite.color = Color.white;
        }

        var scale = transform.localScale;
        transform.localScale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), scale.z);
    }

    private void CaptureOriginalStatesOnce()
    {
        if (_originalStatesCaptured) return;
        _originalStatesCaptured = true;

        _originalActiveStates = new Dictionary<GameObject, bool>();
        _originalLocalPositions = new Dictionary<Transform, Vector3>();
        _originalLocalRotations = new Dictionary<Transform, Quaternion>();
        _originalLocalScales = new Dictionary<Transform, Vector3>();

        var transforms = GetComponentsInChildren<Transform>(true);
        foreach (var t in transforms)
        {
            if (t == null || t == transform) continue;
            _originalActiveStates[t.gameObject] = t.gameObject.activeSelf;
            _originalLocalPositions[t] = t.localPosition;
            _originalLocalRotations[t] = t.localRotation;
            _originalLocalScales[t] = t.localScale;
        }
    }

    private void RestoreOriginalStates()
    {
        if (_originalActiveStates != null)
        {
            foreach (var kv in _originalActiveStates)
            {
                if (kv.Key == null) continue;
                if (kv.Key.activeSelf != kv.Value)
                    kv.Key.SetActive(kv.Value);
            }
        }

        if (_originalLocalPositions != null)
        {
            foreach (var kv in _originalLocalPositions)
            {
                if (kv.Key == null) continue;
                kv.Key.localPosition = kv.Value;
            }
        }

        if (_originalLocalRotations != null)
        {
            foreach (var kv in _originalLocalRotations)
            {
                if (kv.Key == null) continue;
                kv.Key.localRotation = kv.Value;
            }
        }

        if (_originalLocalScales != null)
        {
            foreach (var kv in _originalLocalScales)
            {
                if (kv.Key == null) continue;
                kv.Key.localScale = kv.Value;
            }
        }
    }


    public virtual void SetState(EnemyState state)
    {
        CurState = state;
        if (EnemyAnim == null) return;

        switch (state)
        {
            case EnemyState.Idle:
                EnemyAnim.Play("Idle", 0, 0f);
                break;
            case EnemyState.Run:
                EnemyAnim.Play("Walk", 0, 0f);
                break;
            case EnemyState.Attack:
                EnemyAnim.Play("Attack", 0, 0f);
                break;
            case EnemyState.Dead:
                EnemyAnim.Play("Dead", 0, 0f);
                break;
            case EnemyState.Damage:
                // Layer Lab 몬스터 컨트롤러는 "Hit", 다른 에셋은 "Hurt" 로 스테이트 이름이 다르다.
                if (EnemyAnim.HasState(0, Animator.StringToHash("Hit")))
                    EnemyAnim.Play("Hit", 0, 0f);
                else if (EnemyAnim.HasState(0, Animator.StringToHash("Hurt")))
                    EnemyAnim.Play("Hurt", 0, 0f);
                break;
        }
    }

    public virtual void DamageColorEffect()
    {
        if (isDamageFlashing) return;
        isDamageFlashing = true;

        foreach (var sprite in EnemyImageList)
        {
            if (sprite != null)
                sprite.color = Color.red;
        }

        GameRoot.Instance.WaitTimeAndCallback(0.2f, () =>
        {
            if (this == null) return;
            Color restore = IsFrozen ? FrozenTint : IsPoisonedVisual ? PoisonTint : Color.white;
            foreach (var sprite in EnemyImageList)
            {
                if (sprite != null)
                    sprite.color = restore;
            }
            isDamageFlashing = false;
        });
    }

    public virtual void Damage(int damage)
    {
        Damage(damage, false);
    }

    public virtual void Damage(int damage, bool isCritical)
    {
        Color textColor = isCritical ? Color.yellow : Color.red;
        Damage(damage, textColor, isCritical);
    }

    public virtual void Damage(int damage, Color textColor, bool isCritical = false)
    {
        // 이 적의 자체 HP를 깎는다 (WaveInfo의 unit_hp 기반).
        CurHp -= damage;
        if (CurHp < 0) CurHp = 0;

        SetHp(CurHp);

        DamageColorEffect();
        if (damage > 0)
            SoundPlayer.Instance.PlaySound("effect_damage");
        GameRoot.Instance.DamageTextSystem.ShowDamage(damage, transform.position + Vector3.up * 2f, Color.red, isCritical);

        if (CurHp <= 0)
        {
            Dead();
        }
        else if (damage > 0)
        {
            SetState(EnemyState.Damage);

            // 컨트롤러에 Hit -> Idle 전환이 없으므로 코드로 복귀시킨다.
            GameRoot.Instance.WaitTimeAndCallback(0.35f, () =>
            {
                if (this == null || gameObject == null) return;
                if (CurState == EnemyState.Damage && !IsDead)
                    SetState(EnemyState.Idle);
            });
        }
    }

    public virtual void Dead()
    {
        SetState(EnemyState.Dead);

        // 죽은 자리를 그룹에 기록 — 웨이브 클리어 보상 상자가 이 위치에 등장한다.
        var group = GetComponentInParent<EnemyUnitGroup>();
        if (group != null)
            group.LastDeadPosition = transform.position;

        if (InGameHpProgress != null)
        {
            ProjectUtility.SetActiveCheck(InGameHpProgress.gameObject, false);
        }

        // 킬 카운트 집계 (DMK 는 KillCountProperty 를 웨이브/스테이지 진행 판정에 사용).
        GameRoot.Instance.UserData.InGamePlayerData.KillCountProperty.Value += 1;
    }

    public virtual void DeadAfter()
    {
        if (this == null || gameObject == null) return;
        ProjectUtility.SetActiveCheck(gameObject, false);
    }

    [Header("Attack Settings")]
    [SerializeField] protected float moveToTargetSpeed = 60f;
    [SerializeField] protected float returnSpeed = 50f;
    [SerializeField] protected float attackStopDistance = 3f;

    private Vector3 enemyOriginalPosition;

    // Slashing 애니메이션 이벤트에서 호출
    public void OnEnemyAttackHit()
    {
        // 애니메이션 이벤트 중복 호출 방어.
        if (enemyAttackHitTriggered) return;

        if (currentAttackPlayerTarget != null && currentAttackPlayerTarget.CurState != PlayerUnitBase.PlayerUnitState.Dead)
        {
            // 적 본인의 공격력(WaveInfo unit_dmg)을 사용.
            currentAttackPlayerTarget.Damage(AttackDamage, this);
        }
        enemyAttackHitTriggered = true;
        currentAttackPlayerTarget = null;
    }

    // 플레이어 앞까지 달려가서 공격하고 원래 자리로 복귀한다 (lasttowerdefense 원본 방식).
    public virtual IEnumerator AttackPlayerSequence(PlayerUnitBase playerUnit)
    {
        if (playerUnit == null || IsDead) yield break;
        if (playerUnit.CurState == PlayerUnitBase.PlayerUnitState.Dead) yield break;
        if (IsFrozen) yield break;

        enemyOriginalPosition = transform.position;

        // 플레이어에게 이동
        SetState(EnemyState.Run);

        while (true)
        {
            if (playerUnit == null) yield break;
            Vector3 targetPos = playerUnit.transform.position + (transform.position - playerUnit.transform.position).normalized * attackStopDistance;
            if (Vector3.Distance(transform.position, targetPos) <= 0.1f) break;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveToTargetSpeed * Time.deltaTime);
            yield return null;
        }

        // 공격 (애니메이션 이벤트 기반)
        if (playerUnit != null && playerUnit.CurState != PlayerUnitBase.PlayerUnitState.Dead)
        {
            enemyAttackHitTriggered = false;
            currentAttackPlayerTarget = playerUnit;

            SetState(EnemyState.Attack);

            // 애니메이션 이벤트(OnEnemyAttackHit)가 호출될 때까지 대기.
            // 클립에 이벤트가 누락된 경우 소프트락 방지를 위해 2초 후 강제 타격 처리.
            float waited = 0f;
            while (!enemyAttackHitTriggered)
            {
                waited += Time.deltaTime;
                if (waited >= 2f)
                {
                    OnEnemyAttackHit();
                    break;
                }
                yield return null;
            }

            // 나머지 애니메이션 재생 대기
            yield return null;
            if (EnemyAnim != null)
            {
                var stateInfo = EnemyAnim.GetCurrentAnimatorStateInfo(0);
                float remaining = stateInfo.length * (1f - stateInfo.normalizedTime);
                if (remaining > 0f)
                    yield return new WaitForSeconds(remaining);
            }
        }

        // 원래 위치로 복귀
        SetState(EnemyState.Run);
        // 왼쪽 바라보기 (복귀 방향)
        Vector3 scale = transform.localScale;
        transform.localScale = new Vector3(-Mathf.Abs(scale.x), scale.y, scale.z);

        while (Vector3.Distance(transform.position, enemyOriginalPosition) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, enemyOriginalPosition, returnSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = enemyOriginalPosition;
        // 원래 방향 복원
        transform.localScale = new Vector3(Mathf.Abs(scale.x), scale.y, scale.z);
        SetState(EnemyState.Idle);
    }


    public virtual void SetHp(double hp)
    {
        // 유닛이 이미 파괴된 경우 안전하게 무시
        if (this == null || gameObject == null) return;

        // transform 사용 전 로컬 참조 확보 (파괴된 오브젝트 접근 방지)
        var targetTransform = transform;
        if (targetTransform == null) return;

        if (InGameHpProgress == null)
        {
            GameRoot.Instance.UISystem.LoadFloatingUI<InGameHpProgress>(hpprogress =>
                    {
                        // 로드 완료 시점에 유닛이 사라졌으면 바로 반환
                        if (this == null || gameObject == null || !isActiveAndEnabled)
                        {
                            return;
                        }

                        // transform가 유효한지 다시 확인
                        if (targetTransform == null) return;
                        if (hpprogress == null || hpprogress.gameObject == null) return;

                        InGameHpProgress = hpprogress;
                        // 먼저 비활성화하여 잘못된 위치에서 보이지 않도록 함
                        ProjectUtility.SetActiveCheck(hpprogress.gameObject, true);
                        hpprogress.Init(targetTransform);
                        hpprogress.SetOffset(HpProgressOffset);
                        hpprogress.SetBossRoot(IsBoss);
                        hpprogress.SetHpText(hp, MaxHp);
                    });
        }
        else
        {
            if (InGameHpProgress != null && InGameHpProgress.gameObject != null)
            {
                InGameHpProgress.SetHpText(hp, MaxHp);

                // 먼저 비활성화하여 잘못된 위치에서 보이지 않도록 함
                ProjectUtility.SetActiveCheck(InGameHpProgress.gameObject, true);
                InGameHpProgress.Init(targetTransform);
                InGameHpProgress.SetOffset(HpProgressOffset);
                InGameHpProgress.SetBossRoot(IsBoss);
                InGameHpProgress.SetHpText(hp, MaxHp);
            }
        }

    }
}
