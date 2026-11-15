using UnityEngine;
using BanpoFri;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

// lasttowerdefense 의 PlayerUnitBase 이식(슬림 버전).
// 주사위(DiceSystem/InGameLevelUpBuffSystem/AlimentSystem)와 적 유닛 관련 로직은 전부 제거하고,
// 인게임 연출에 필요한 상태 전환(Idle/Run 등)과 맵 이동 시퀀스 연동(SetRun/ApplyCameraDeltaX)만 유지한다.
public class PlayerUnitBase : MonoBehaviour
{
    public enum PlayerUnitState
    {
        Idle,
        Run,
        Attack,
        Dead,
        Damage,
    }

    [SerializeField]
    protected Animator PlayerAnim;

    [SerializeField]
    private List<SpriteRenderer> PlayerImageList = new List<SpriteRenderer>();

    [SerializeField]
    private Vector3 HpOffset = new Vector3(0f, 2.2f, 0f);

    [SerializeField]
    private AnimActionComponent AnimActionComponent;

    public PlayerUnitState CurState = PlayerUnitState.Idle;

    private int UnitIdx = 0;

    private Vector3 originalPosition;
    private bool isAttacking = false;

    private bool isDamageFlashing = false;

    public bool IsAttacking => isAttacking;

    [Header("Attack Settings")]
    // 적과의 간격이 4~5.5 유닛 수준이라 160이면 사실상 순간이동으로 보임 — Run 애니메이션이 보이도록 느리게.
    [SerializeField] protected float moveToEnemySpeed = 23f;
    [SerializeField] protected float returnSpeed = 15f;

    // 애니메이션 이벤트(OnAttackHit) 대기용 상태.
    protected bool attackHitTriggered = false;
    private EnemyUnitBase currentAttackTarget;
    private System.Action currentAttackHitCallback;

    public virtual void Set(int playeridx)
    {
        if (AnimActionComponent != null)
            AnimActionComponent.AnimAction_1 = OnAttackHit;

        UnitIdx = playeridx;

        SetHp(GameRoot.Instance.UserData.InGamePlayerData.CurHpProperty.Value);

        SetState(PlayerUnitState.Idle);
    }

    protected InGameHpProgress InGameHpProgress;

    // 적 유닛(EnemyUnitBase.SetHp)과 동일하게 머리 위에 HP 프로그레스 바를 띄운다.
    public virtual void SetHp(int hp, bool forceRefresh = false)
    {
        GameRoot.Instance.UserData.InGamePlayerData.CurHpProperty.Value = hp;

        // 유닛이 이미 파괴된 경우 안전하게 무시
        if (this == null || gameObject == null) return;

        var targetTransform = transform;
        if (targetTransform == null) return;

        int maxHp = Mathf.Max(1, GameRoot.Instance.UserData.InGamePlayerData.StartHpProperty.Value);

        if (InGameHpProgress == null)
        {
            GameRoot.Instance.UISystem.LoadFloatingUI<InGameHpProgress>(hpprogress =>
            {
                // 로드 완료 시점에 유닛이 사라졌으면 바로 반환
                if (this == null || gameObject == null || !isActiveAndEnabled) return;
                if (targetTransform == null) return;
                if (hpprogress == null || hpprogress.gameObject == null) return;

                InGameHpProgress = hpprogress;
                ProjectUtility.SetActiveCheck(hpprogress.gameObject, true);
                hpprogress.Init(targetTransform);
                hpprogress.SetOffset(HpOffset);
                hpprogress.SetHpText(GameRoot.Instance.UserData.InGamePlayerData.CurHpProperty.Value, maxHp, true);
            });
        }
        else
        {
            if (InGameHpProgress.gameObject != null)
            {
                ProjectUtility.SetActiveCheck(InGameHpProgress.gameObject, true);
                InGameHpProgress.SetHpText(hp, maxHp, forceRefresh);
            }
        }
    }

    public virtual void DamageColorEffect()
    {
        if (isDamageFlashing) return;
        isDamageFlashing = true;

        // PlayerImageList 가 비어 있는 프리팹(PlayerUnit_1 등)도 플래시가 동작하도록 자식 SpriteRenderer 를 자동 수집.
        if (PlayerImageList.Count == 0)
            PlayerImageList.AddRange(GetComponentsInChildren<SpriteRenderer>(true));

        foreach (var sprite in PlayerImageList)
        {
            if (sprite != null)
                sprite.color = Color.red;
        }

        GameRoot.Instance.WaitTimeAndCallback(0.2f, () =>
        {
            if (this == null) return;
            foreach (var sprite in PlayerImageList)
            {
                if (sprite != null)
                    sprite.color = Color.white;
            }
            isDamageFlashing = false;
        });
    }

    // 적 유닛/적 총알이 공격자 참조와 함께 호출하는 오버로드.
    // lasttowerdefense 에서는 attacker 를 반사 데미지 등에 썼지만 해당 업그레이드는 미이식이라 참조는 무시한다.
    public virtual void Damage(int damage, EnemyUnitBase attacker)
    {
        Damage(damage);
    }

    public virtual void Damage(int damage)
    {
        if (damage <= 0) return;
        if (CurState == PlayerUnitState.Dead) return;

        // SetHp 를 경유해 값과 머리 위 HP 바를 함께 갱신한다.
        SetHp(GameRoot.Instance.UserData.InGamePlayerData.CurHpProperty.Value - damage);

        DamageColorEffect();
        GameRoot.Instance.DamageTextSystem.ShowDamage(damage, transform.position + Vector3.up * 2f, Color.red);

        if (GameRoot.Instance.UserData.InGamePlayerData.CurHpProperty.Value <= 0)
        {
            Dead();
        }
        else
        {
            SetState(PlayerUnitState.Damage);
            StartCoroutine(ReturnToIdleAfterHurt());
        }
    }

    private IEnumerator ReturnToIdleAfterHurt()
    {
        yield return new WaitForSeconds(0.3f);
        if (this != null && CurState == PlayerUnitState.Damage)
            SetState(PlayerUnitState.Idle);
    }

    // 프리팹마다 애니메이터 스테이트 이름이 다르다:
    //  - lasttowerdefense 영웅 리그: Idle / Walk / Attack / Dead1 / Defeat
    //  - DMK 고양이(CatUnit.controller): idle / Run / melee_attack (사망·피격 클립 없음)
    // 후보 이름 중 실제로 존재하는 첫 스테이트를 재생해 어느 프리팹이든 동작하게 한다.
    private void PlayFirstAvailableState(params string[] stateNames)
    {
        foreach (var stateName in stateNames)
        {
            int hash = Animator.StringToHash(stateName);
            if (PlayerAnim.HasState(0, hash))
            {
                PlayerAnim.Play(hash, 0, 0f);
                return;
            }
        }
    }

    public virtual void SetState(PlayerUnitState state)
    {
        CurState = state;
        if (PlayerAnim == null) return;

        switch (state)
        {
            case PlayerUnitState.Idle:
                PlayFirstAvailableState("Idle", "idle");
                break;
            case PlayerUnitState.Run:
                PlayFirstAvailableState("Walk", "Run", "run");
                break;
            case PlayerUnitState.Attack:
                PlayFirstAvailableState("Attack", "melee_attack");
                break;
            case PlayerUnitState.Dead:
                PlayFirstAvailableState("Dead1", "Dead", "idle", "Idle");
                break;
            case PlayerUnitState.Damage:
                PlayFirstAvailableState("Defeat", "Hurt", "idle", "Idle");
                break;
            default:
                break;
        }
    }

    // 맵 이동 시퀀스 시작/종료 시 MoveMapController 가 호출.
    public virtual void SetRun(bool isRun)
    {
        if (CurState == PlayerUnitState.Dead) return;

        if (isRun)
        {
            SetState(PlayerUnitState.Run);
        }
        else
        {
            SetState(PlayerUnitState.Idle);
        }
    }

    public virtual void Dead()
    {
        SetState(PlayerUnitState.Dead);

        if (InGameHpProgress != null && InGameHpProgress.gameObject != null)
            ProjectUtility.SetActiveCheck(InGameHpProgress.gameObject, false);

        // lasttowerdefense 는 여기서 부활(PopupRevival)/실패 결과(PopupStageResult) 팝업을 띄우지만
        // DMK 는 둘 다 미이식 — 소프트락 방지를 위해 잠시 후 로비로 복귀한다.
        GameRoot.Instance.WaitRealTimeAndCallback(1.5f, () =>
        {
            var inGame = GameRoot.Instance.InGameSystem.GetInGame<InGameBase>();
            if (inGame != null && inGame.Stage != null)
                inGame.Stage.ReturnMainScreen();
        });
    }

    // Attack 애니메이션 클립의 이벤트(StartAction → AnimAction_1)에서 호출.
    public virtual void OnAttackHit()
    {
        // 애니메이션 이벤트가 한 사이클 내 중복 호출되어도 데미지는 1회만 적용되도록 가드.
        if (attackHitTriggered) return;

        if (currentAttackTarget != null && !currentAttackTarget.IsDead)
        {
            int damage = Mathf.Max(1, GameRoot.Instance.UserData.InGamePlayerData.Damage);
            currentAttackTarget.Damage(damage);
            SoundPlayer.Instance.PlaySound("effect_swing");
            currentAttackHitCallback?.Invoke();
        }
        attackHitTriggered = true;
        currentAttackTarget = null;
        currentAttackHitCallback = null;
    }

    // lasttowerdefense 의 AttackSequence 이식 — 주사위 등급(diceGrade) 인자만 제거.
    // 적에게 달려가 Attack 애니메이션을 재생하고, 타격 이벤트 후 애니메이션이 끝날 때까지 대기한다.
    public virtual IEnumerator AttackSequence(EnemyUnitBase target, System.Action onAttackHit = null)
    {
        if (target == null || target.IsDead) yield break;

        // 적에게 빠르게 이동 (타겟이 움직여도 매 프레임 현재 위치로 추적)
        SetState(PlayerUnitState.Run);
        const float attackRange = 3f;

        while (target != null && !target.IsDead)
        {
            Vector3 toTarget = target.transform.position - transform.position;
            float dist = toTarget.magnitude;
            if (dist <= attackRange) break;

            float step = moveToEnemySpeed * Time.deltaTime;
            bool arrived = step >= dist - attackRange;
            if (arrived) step = dist - attackRange;
            transform.position += (toTarget / dist) * step;

            // 도착한 프레임에 프레임 대기 없이 즉시 공격으로 전환 (Run 애니 끊김이 한 프레임도 늦지 않도록)
            if (arrived) break;
            yield return null;
        }

        // 공격 실행 (애니메이션 이벤트 기반)
        if (target != null && !target.IsDead)
        {
            attackHitTriggered = false;
            currentAttackTarget = target;
            currentAttackHitCallback = onAttackHit;

            SetState(PlayerUnitState.Attack);

            // Play 예약만 걸리면 다음 Animator 갱신까지 Run 포즈가 남으므로 즉시 평가해서 이번 프레임에 공격 포즈로 스냅.
            if (PlayerAnim != null)
                PlayerAnim.Update(0f);

            // 애니메이션 이벤트(OnAttackHit) 대기.
            // 클립에 이벤트가 누락된 프리팹이 있어도 소프트락되지 않도록 2초 후 강제 타격 처리.
            float waited = 0f;
            while (!attackHitTriggered)
            {
                waited += Time.deltaTime;
                if (waited >= 2f)
                {
                    OnAttackHit();
                    break;
                }
                yield return null;
            }

            // 나머지 애니메이션 재생 대기
            yield return null;
            if (PlayerAnim != null)
            {
                var stateInfo = PlayerAnim.GetCurrentAnimatorStateInfo(0);
                float remaining = stateInfo.length * (1f - stateInfo.normalizedTime);
                if (remaining > 0f)
                    yield return new WaitForSeconds(remaining);
            }
        }
    }

    public virtual IEnumerator ReturnToOriginalPosition()
    {
        if (this == null || gameObject == null) yield break;

        // 왼쪽 바라보기 (복귀 방향)
        Vector3 scale = transform.localScale;
        transform.localScale = new Vector3(-Mathf.Abs(scale.x), scale.y, scale.z);

        SetState(PlayerUnitState.Run);

        while (this != null && gameObject != null
            && Vector3.Distance(transform.position, originalPosition) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, originalPosition, returnSpeed * Time.deltaTime);
            yield return null;
        }

        if (this == null || gameObject == null) yield break;

        transform.position = originalPosition;

        // 오른쪽 바라보기 (원래 방향 복원)
        transform.localScale = new Vector3(Mathf.Abs(scale.x), scale.y, scale.z);

        SetState(PlayerUnitState.Idle);

        isAttacking = false;
    }

    public void SaveOriginalPosition()
    {
        originalPosition = transform.position;
    }

    // MoveMapController가 매 프레임 카메라의 X 이동량을 플레이어와 originalPosition에 동시 적용하기 위해 호출.
    public void ApplyCameraDeltaX(float deltaX)
    {
        if (deltaX == 0f) return;
        var pos = transform.position;
        pos.x += deltaX;
        transform.position = pos;
        originalPosition.x += deltaX;
    }
}
