using UnityEngine;

// lasttowerdefense 와 동일한 2D 트리거 방식.
// (구 광산 게임의 3D OnTriggerEnter(Collider) 는 총알(BulletBase) 2D 충돌로 대체.
//  DeadAction/DeadEvent 는 DMK 애니메이션 이벤트 호환을 위해 유지.)
public class ColliderAction : MonoBehaviour
{
    [HideInInspector]
    public System.Action<Collider2D> TriggerEnterAction = null;

    public System.Action AttackAction = null;

    public System.Action DeadAction = null;

     public void OnTriggerEnter2D(Collider2D collision)
    {
        TriggerEnterAction?.Invoke(collision);
    }


    public void DeadEvent()
    {
        DeadAction?.Invoke();
    }

    public void AttackEvent()
    {
        AttackAction?.Invoke();
    }
}
