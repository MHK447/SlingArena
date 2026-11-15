using UnityEngine;
using BanpoFri;
using System.Collections.Generic;

public class BulletBase : MonoBehaviour
{
    [SerializeField]
    private ColliderAction ColliderAction = null;

    [SerializeField]
    protected SpriteRenderer BulletImg;

    [SerializeField]
    private Transform BulletRootTr;

    [SerializeField]
    private BoxCollider2D Col;

    [SerializeField]
    private bool IsRotation = false;

    [SerializeField]
    private float RotationSpeed = 360f;

    protected BulletStatus Status;

    [HideInInspector]
    public Transform TargetTr = null;

    [HideInInspector]
    public Transform ShooterTr = null;

    private int shooterLayer = -1;

    protected InGameBaseStage BaseStage = null;

    protected System.Action<BulletBase> OnHitCallback = null;

    protected System.Action<EnemyUnitBase, double> OnEnemyHitAction = null;

    protected bool IsCollision = false;

    protected int remainingPenetrations = 0;
    protected HashSet<Collider2D> hitTargets = new HashSet<Collider2D>();

    public TrailComponent TrailComponent;

    public virtual void Awake()
    {
        if (TrailComponent != null)
        {
            TrailComponent.InitTrail();
        }
    }

    public virtual void Set(BulletStatus status, Transform shootertr, Transform targettr, System.Action<BulletBase> onhitcallback, System.Action<EnemyUnitBase, double> onEnemyHitAction = null)
    {
        IsCollision = false;

        transform.SetParent(null);

        Vector3 direction = (targettr.position - shootertr.position).normalized;
        transform.position = shootertr.transform.position + direction * 0.15f;

        transform.rotation = Quaternion.identity;

        Status = status;
        OnEnemyHitAction = onEnemyHitAction;

        // Rigidbody2D가 없으면 OnTriggerEnter2D가 호출되지 않으므로 보장
        var rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (Col != null)
        {
            Col.enabled = true;
            Col.isTrigger = true;
        }

        if (TrailComponent != null)
        {
            TrailComponent.ClearTrail();
            TrailComponent.InitTrail(status.ProjectileSpeed / 1000f);
            TrailComponent.SetTrailActive(true);
        }

        BaseStage = GameRoot.Instance.InGameSystem.GetInGame<InGameBase>().Stage;

        ShooterTr = shootertr;
        shooterLayer = ResolveShooterLayer(ShooterTr);

        TargetTr = targettr;

        OnHitCallback = onhitcallback;

        remainingPenetrations = status.PenetrationCount;
        hitTargets.Clear();

        ColliderAction.TriggerEnterAction = OnTriggerEnter2D;
    }

    public void SetBulletImg(Sprite sprite)
    {
        if (BulletImg != null)
            BulletImg.sprite = sprite;

        SetColliderSize(1f);
    }

    private void Update()
    {
        Move();

        if (IsRotation)
        {
            BulletRootTr.transform.Rotate(0, 0, -RotationSpeed * Time.deltaTime);
        }
    }

    protected virtual void Move()
    {
        if (TargetTr == null) return;

        Vector3 dir = (TargetTr.position - transform.position).normalized;
        transform.position += dir * Status.ProjectileSpeed * Time.deltaTime;
    }

    // shootertr가 자식(BulletSpawnTr 등)일 때도 Player/Enemy layer를 정확히 잡도록 부모 체인을 거슬러 탐색.
    private static int ResolveShooterLayer(Transform tr)
    {
        if (tr == null || tr.gameObject == null) return -1;
        int playerLayer = LayerMask.NameToLayer("Player");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        var t = tr;
        while (t != null)
        {
            int l = t.gameObject.layer;
            if (l == playerLayer || l == enemyLayer) return l;
            t = t.parent;
        }
        return tr.gameObject.layer;
    }

    public virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null) return;
        var collidedObj = collision.gameObject;
        if (collidedObj == null) return;

        if (IsCollision && remainingPenetrations <= 0) return;
        if (shooterLayer == -1) return;

        if (hitTargets.Contains(collision)) return;

        int collidedLayer = collidedObj.layer;

        // 플레이어가 쏜 총알 -> 적 유닛 히트
        if (collidedLayer == LayerMask.NameToLayer("Enemy") && shooterLayer == LayerMask.NameToLayer("Player"))
        {
            var enemy = collidedObj.GetComponent<EnemyUnitBase>();
            if (enemy != null && !enemy.IsDead)
            {
                hitTargets.Add(collision);
                enemy.Damage((int)Status.Damage, Status.IsCritical);

                OnEnemyHitAction?.Invoke(enemy, Status.Damage);
                // LightningEffect(enemy, Status.Damage);
                // FreezeEffectCheck(enemy);
                // PoisonEffectCheck(enemy);

                HandlePenetration();
            }
        }
        // 적이 쏜 총알 -> 플레이어 유닛 히트
        else if (collidedLayer == LayerMask.NameToLayer("Player") && shooterLayer == LayerMask.NameToLayer("Enemy"))
        {
            var player = collidedObj.GetComponent<PlayerUnitBase>();
            if (player != null && player.CurState != PlayerUnitBase.PlayerUnitState.Dead)
            {
                hitTargets.Add(collision);

                // 반사 데미지 대상 공격자 참조 전달 (ShooterTr이 EnemyUnitBase인 경우)
                EnemyUnitBase attacker = ShooterTr != null ? ShooterTr.GetComponent<EnemyUnitBase>() : null;
                player.Damage((int)Status.Damage, attacker);

                HandlePenetration();
            }
        }
        // 바닥에 닿으면 즉시 제거
        else if (collidedLayer == LayerMask.NameToLayer("Ground"))
        {
            FinishBullet();
        }
    }

    private void HandlePenetration()
    {
        if (remainingPenetrations > 0)
        {
            remainingPenetrations--;
            if (remainingPenetrations <= 0)
            {
                FinishBullet();
                return;
            }

            if (!HasOtherAliveEnemyToBounce())
            {
                FinishBullet();
            }
        }
        else
        {
            FinishBullet();
        }
    }

    private bool HasOtherAliveEnemyToBounce()
    {
        // 플레이어가 쏜 총알에서만 적 카운트 의미가 있음. 적이 쏜 총알은 기존 동작 유지.
        if (shooterLayer != LayerMask.NameToLayer("Player")) return true;
        if (BaseStage == null) return false;

        var group = BaseStage.GetEnemyUnitGroup;
        if (group == null) return false;

        var aliveList = group.GetAliveEnemyList();
        if (aliveList == null || aliveList.Count == 0) return false;

        for (int i = 0; i < aliveList.Count; i++)
        {
            var enemy = aliveList[i];
            if (enemy == null || enemy.IsDead) continue;

            var enemyCol = enemy.GetComponentInChildren<Collider2D>();
            if (enemyCol == null) continue;
            if (hitTargets.Contains(enemyCol)) continue;

            return true;
        }
        return false;
    }

    private void FinishBullet()
    {
        IsCollision = true;
        if (Col != null)
            Col.enabled = false;
        DisableTrail();
        OnHitCallback?.Invoke(this);
    }

    public virtual void SetColliderSize(float size)
    {
        if (BulletImg != null && BulletImg.sprite != null)
        {
            Vector2 spriteSize = BulletImg.bounds.size;
            Col.size = spriteSize * size;
        }
    }

    public void LightningEffect(EnemyUnitBase target, double damage)
    {
        if (shooterLayer != LayerMask.NameToLayer("Player"))
            return;

    

        var cachedTarget = target;
        var effectPos = new Vector3(target.transform.position.x, target.transform.position.y - 0.5f, transform.position.z);

        GameRoot.Instance.EffectSystem.Play<LightningEffect>(effectPos, x =>
        {
            if (SoundPlayer.Instance != null)
                SoundPlayer.Instance.PlaySound("effect_bomb");

            if (x == null)
                return;

            if (cachedTarget == null || cachedTarget.IsDead)
            {
                x.SetAutoRemove(true, 3f);
                return;
            }

          
            x.SetAutoRemove(true, 3f);
        });
    }

    public void FreezeEffectCheck(EnemyUnitBase target)
    {
        if (shooterLayer == LayerMask.NameToLayer("Player"))
        {
          
        }
    }

    public void PoisonEffectCheck(EnemyUnitBase target)
    {
      
    }

    protected virtual void DisableTrail()
    {
        if (TrailComponent != null)
        {
            TrailComponent.SetTrailActive(false);
        }
    }
}
