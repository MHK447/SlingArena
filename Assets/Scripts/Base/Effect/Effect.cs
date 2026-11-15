using System.Linq;
using System.Collections.Generic;
using UnityEngine;


public class Effect : MonoBehaviour
{
    [SerializeField]
    protected List<ParticleSystem> particles = new List<ParticleSystem>();

    private Transform FollowTrans = null;

    private bool autoRemove = false;
    private float deltaTime = 0f;
    private float targetTime = 1f;

    // 팔로우 시(또는 단일 위치 적용 시) 더해줄 오프셋. 인스펙터에서 기본값 설정 가능, 런타임은 SetPositionOffset로 덮어쓰기.
    [SerializeField]
    private Vector3 PositionOffset = Vector3.zero;

    public virtual void Play(Vector3 worldPos, Transform followTrans)
    {
        this.transform.position = worldPos + PositionOffset;
        foreach(var pt in particles)
        {
            pt.Clear();
            pt.Play();
            //pt.Simulate(0f, false, true);
        }
        //var tweens = this.transform.GetComponentsInChildren<DoTweenScriptComponent>();
        //foreach (var tw in tweens)
        //    tw.Restart();

        //if(tweens.Length > 0)
        //{
        //    tweens.Last().AddCallback(()=> {
        //        OnForceCollect();
        //        deltaTime = 0f;
        //    });
        //}

        FollowTrans = followTrans;
    }

    public virtual void Play()
    {
        foreach(var pt in particles)
        {
            pt.Play();
        }
    }

    public virtual void Stop()
    {
        foreach(var pt in particles)
        {            
            pt.Stop();
        }
    }

    public void SetAutoRemove(bool value, float time)
    {
        autoRemove = value;
        targetTime = time;
    }

    // 팔로우/단일 위치에 더해질 오프셋 설정 (예: 캐릭터 머리 위로 띄우기).
    public void SetPositionOffset(Vector3 offset)
    {
        PositionOffset = offset;
        if (FollowTrans != null)
            this.transform.position = FollowTrans.position + PositionOffset;
    }

    private void Update()
    {
        if(FollowTrans != null)
        {
            this.transform.position = FollowTrans.position + PositionOffset;
        }
        if(autoRemove)
        {
            if(deltaTime >= targetTime)
            {
                OnForceCollect();
                deltaTime = 0f;
                return;
            }
            deltaTime += Time.deltaTime;
        }
    }
    
    public void OnForceCollect()
    {
        GameRoot.Instance.EffectSystem.ReturnMultiEffect(this);
    }
}
