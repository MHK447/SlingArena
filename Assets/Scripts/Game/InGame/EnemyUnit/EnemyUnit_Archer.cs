using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyUnit_Archer : EnemyUnitBase
{
    [SerializeField]
    private GameObject ArrowPrefab;

    [Header("Archer Settings")]
    [SerializeField] private float arrowSpeed = 30f;
    [SerializeField] private Transform ArrowSpawnTr;

    private List<BulletBase> activeBullets = new List<BulletBase>();
    private List<BulletBase> deadBullets = new List<BulletBase>();

    public override void Set(int enemyidx, int unitHp, int unitDmg)
    {
        base.Set(enemyidx, unitHp, unitDmg);
        ClearAllBullets();
    }

    public override IEnumerator AttackPlayerSequence(PlayerUnitBase playerUnit)
    {
        if (playerUnit == null || IsDead) yield break;
        if (playerUnit.CurState == PlayerUnitBase.PlayerUnitState.Dead) yield break;
        if (IsFrozen) yield break;

        // 제자리에서 바로 공격
        SetState(EnemyState.Attack);

        // 애니메이션 앞부분 대기 후 화살 발사
        yield return null;
        if (EnemyAnim != null)
        {
            var stateInfo = EnemyAnim.GetCurrentAnimatorStateInfo(0);
            yield return new WaitForSeconds(stateInfo.length * 0.3f);
        }

        // 화살 발사
        if (playerUnit != null && playerUnit.CurState != PlayerUnitBase.PlayerUnitState.Dead)
        {
            ShootArrow(playerUnit);
        }

        // 나머지 애니메이션 대기
        if (EnemyAnim != null)
        {
            var stateInfo = EnemyAnim.GetCurrentAnimatorStateInfo(0);
            float remaining = stateInfo.length * (1f - stateInfo.normalizedTime);
            if (remaining > 0f)
                yield return new WaitForSeconds(remaining);
        }

        SetState(EnemyState.Idle);
    }

    private void ShootArrow(PlayerUnitBase target)
    {
        if (ArrowPrefab == null) return;

        BulletBase bullet = GetBullet();
        if (bullet == null) return;

        bullet.gameObject.SetActive(true);
        SoundPlayer.Instance.PlaySound("effect_throw_straight");

        var status = new BulletStatus
        {
            Damage = AttackDamage,
            ProjectileSpeed = arrowSpeed,
            PenetrationCount = 0
        };

        Transform spawnTr = ArrowSpawnTr != null ? ArrowSpawnTr : transform;
        bullet.Set(status, spawnTr, target.transform, OnBulletHit);
    }

    private BulletBase GetBullet()
    {
        // deadBullets에서 재활용
        if (deadBullets.Count > 0)
        {
            int lastIdx = deadBullets.Count - 1;
            var bullet = deadBullets[lastIdx];
            deadBullets.RemoveAt(lastIdx);
            activeBullets.Add(bullet);
            return bullet;
        }

        // 새로 생성
        var go = Instantiate(ArrowPrefab);
        var newBullet = go.GetComponent<BulletBase>();
        if (newBullet == null)
        {
            Destroy(go);
            return null;
        }

        activeBullets.Add(newBullet);
        return newBullet;
    }

    private void OnBulletHit(BulletBase bullet)
    {
        bullet.gameObject.SetActive(false);
        activeBullets.Remove(bullet);
        deadBullets.Add(bullet);
    }

    public override void Dead()
    {
        base.Dead();
        ClearAllBullets();
    }

    private void ClearAllBullets()
    {
        for (int i = activeBullets.Count - 1; i >= 0; i--)
        {
            var b = activeBullets[i];
            if (b != null)
            {
                b.gameObject.SetActive(false);
                deadBullets.Add(b);
            }
        }
        activeBullets.Clear();
    }

    private void OnDestroy()
    {
        foreach (var b in activeBullets)
        {
            if (b != null) Destroy(b.gameObject);
        }
        foreach (var b in deadBullets)
        {
            if (b != null) Destroy(b.gameObject);
        }
        activeBullets.Clear();
        deadBullets.Clear();
    }
}
