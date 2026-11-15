using UnityEngine;
using BanpoFri;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class Bullet_LobShot : BulletBase
{
    private Vector3 startPosition;
    private Vector3 targetPosition; // 타겟의 초기 위치 저장
    private float elapsedTime = 0f;
    private float arcHeight = 2f; // 포물선의 높이
    private bool isInitialized = false;
    private Vector3 previousPosition;
    private float minDuration = 0.15f; // 최소 비행 시간 (초)
    private float flightDuration = 0.5f; // 실제 비행 시간 (거리/속도 기반)
    private bool hasReachedTarget = false; // 목표 도달 여부
    private Coroutine destroyCoroutine = null; // 삭제 코루틴

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 10f; // 회전 보간 속도 (높을수록 빠르게 방향 전환)

    public override void Set(BulletStatus status, Transform shootertr, Transform targettr, System.Action<BulletBase> onhitcallback, System.Action<EnemyUnitBase, double> onEnemyHitAction = null)
    {
        base.Set(status, shootertr, targettr, onhitcallback, onEnemyHitAction);

        // 재활용을 위한 초기화
        isInitialized = false;
        elapsedTime = 0f;
        flightDuration = 0.5f;
        startPosition = Vector3.zero;
        targetPosition = Vector3.zero;
        previousPosition = Vector3.zero;
        hasReachedTarget = false;

        // 이전 코루틴이 있다면 정지
        if (destroyCoroutine != null)
        {
            StopCoroutine(destroyCoroutine);
            destroyCoroutine = null;
        }
    }

    protected override void Move()
    {
        // 초기화
        if (!isInitialized)
        {
            startPosition = transform.position;
            previousPosition = transform.position;
            elapsedTime = 0f;

            if (TargetTr != null)
            {
                targetPosition = TargetTr.position;
            }
            else
            {
                targetPosition = transform.position + transform.right * 5f;
            }

            // 거리 기반 비행 시간 계산 (ProjectileSpeed 활용)
            float distance = Vector3.Distance(startPosition, targetPosition);
            float speed = Status.ProjectileSpeed > 0f ? Status.ProjectileSpeed : 30f;
            flightDuration = Mathf.Max(distance / speed, minDuration);

            // 거리에 비례한 포물선 높이
            arcHeight = Mathf.Clamp(distance * 0.4f, 3f, 12f);

            isInitialized = true;
        }

        // 시간 진행
        elapsedTime += Time.deltaTime;
        float t = Mathf.Clamp01(elapsedTime / flightDuration);

        // 포물선 궤적
        Vector3 linearPosition = Vector3.Lerp(startPosition, targetPosition, t);
        float heightOffset = arcHeight * Mathf.Sin(t * Mathf.PI);
        Vector3 arcPosition = linearPosition + Vector3.up * heightOffset;

        // 화살 방향 회전 (2D)
        Vector3 direction = arcPosition - previousPosition;
        if (direction.sqrMagnitude > 0.0001f)
        {
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion targetRot = Quaternion.Euler(0, 0, targetAngle);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        previousPosition = arcPosition;
        transform.position = arcPosition;

        // 목표 도달
        if (t >= 1.0f && !hasReachedTarget)
        {
            hasReachedTarget = true;
            if (destroyCoroutine == null)
            {
                destroyCoroutine = StartCoroutine(DestroyAfterDelay(0.05f));
            }
        }
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 삭제 처리
        if (!IsCollision)
        {
            IsCollision = true;
            DisableTrail();
            OnHitCallback?.Invoke(this);
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
