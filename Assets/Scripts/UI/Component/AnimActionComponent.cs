using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimActionComponent : MonoBehaviour
{
    public System.Action AnimAction_1;
    public System.Action<float> AnimAction_2;
    public System.Action<int> AnimAction_Start;
    public System.Action AnimAction_End;
    // 적 유닛 Dead 애니메이션 클립의 이벤트(StartAction_Dead)가 호출 — 사망 연출 종료 후 풀 반환용.
    public System.Action AnimAction_Dead;


    public void StartAction()
    {
        AnimAction_1?.Invoke();
    }

    public void StartAction_2()
    {
        AnimAction_2?.Invoke(1f);
    }

    public void StartAction_Start(int action)
    {
        AnimAction_2?.Invoke(action);
    }

    public void StartAction_End()
    {
        AnimAction_End?.Invoke();
    }
    
    public void StartAction_Dead()
    {
        AnimAction_Dead?.Invoke();
    }
}
