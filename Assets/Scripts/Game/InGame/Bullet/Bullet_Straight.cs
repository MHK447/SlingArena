using UnityEngine;
using BanpoFri;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class Bullet_Straight : BulletBase
{
    private Vector3 flightDirection;
    private float flightSpeed = 30f;
    private bool isInitialized = false;
    private Vector3 targetOffset = Vector3.zero;

    public override void Set(BulletStatus status, Transform shootertr, Transform targettr, System.Action<BulletBase> onhitcallback, System.Action<EnemyUnitBase, double> onEnemyHitAction = null)
    {
        base.Set(status, shootertr, targettr, onhitcallback, onEnemyHitAction);

        isInitialized = false;
        flightDirection = Vector3.zero;
        targetOffset = Vector3.zero;
    }

    // 조준점 보정용 오프셋 (예: 적의 머리 높이로 조준)
    public void SetTargetOffset(Vector3 offset)
    {
        targetOffset = offset;
    }

    protected override void Move()
    {
        // 초기화: 발사 시점의 방향/속도/회전을 고정
        if (!isInitialized)
        {
            Vector3 startPosition = transform.position;
            Vector3 targetPosition = TargetTr != null
                ? TargetTr.position + targetOffset
                : startPosition + transform.right * 20f;

            Vector3 direction = targetPosition - startPosition;
            if (direction.sqrMagnitude > 0.0001f)
            {
                flightDirection = direction.normalized;
                float angle = Mathf.Atan2(flightDirection.y, flightDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
            else
            {
                flightDirection = transform.right;
            }

            flightSpeed = Status.ProjectileSpeed > 0f ? Status.ProjectileSpeed : 30f;

            isInitialized = true;
        }

        // 충돌할 때까지 같은 방향으로 직진
        transform.position += flightDirection * flightSpeed * Time.deltaTime;
    }

    // 비행 중 새 타겟으로 방향 재설정 (기존 타겟이 죽었을 때 호출)
    public void Retarget(Transform newTarget)
    {
        if (newTarget == null) return;

        TargetTr = newTarget;

        Vector3 direction = (newTarget.position + targetOffset) - transform.position;
        if (direction.sqrMagnitude > 0.0001f)
        {
            flightDirection = direction.normalized;
            float angle = Mathf.Atan2(flightDirection.y, flightDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    protected override void DisableTrail()
    {
        if (TrailComponent != null)
        {
            TrailComponent.SetTrailActive(false);
        }
    }
}
