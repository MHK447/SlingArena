using UnityEngine;
using BanpoFri;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using CartoonFX;


[EffectPath("Effect/DiceMergeStarEffect", false, false)]
public class DiceMergeStarEffect : Effect
{
    private static readonly Vector3 PositionOffset = new Vector3(0f, 0f, 0f);

    private Transform offsetFollowTrans;

    private bool initialized = false;

    private void Awake()
    {
        InitParticles();
    }

    // 프리팹의 particles 리스트가 비어 있어 base.Play 가 아무것도 재생하지 못하는 문제 보정:
    // 자식 ParticleSystem 들을 런타임에 캐싱하고, CFXR 파티클의 자동 제거(Destroy/Disable)를
    // 꺼서 풀(캐싱) 재사용 시에도 매번 다시 재생되게 한다.
    private void InitParticles()
    {
        if (initialized) return;
        initialized = true;

        if (particles == null || particles.Count == 0)
            particles = new List<ParticleSystem>(GetComponentsInChildren<ParticleSystem>(true));

        foreach (var cfxr in GetComponentsInChildren<CFXR_Effect>(true))
            cfxr.clearBehavior = CFXR_Effect.ClearBehavior.None;
    }

    public override void Play(Vector3 worldPos, Transform followTrans)
    {
        InitParticles();

        // 이전 재생 종료(CFXR Disable 등)로 꺼졌을 수 있는 자식 파티클 오브젝트를 다시 켠다.
        foreach (var pt in particles)
        {
            if (pt != null && !pt.gameObject.activeSelf)
                pt.gameObject.SetActive(true);
        }

        base.Play(worldPos + PositionOffset, null);
        offsetFollowTrans = followTrans;
    }

    private void LateUpdate()
    {
        if (offsetFollowTrans != null)
        {
            transform.position = offsetFollowTrans.position + PositionOffset;
        }
    }
}
