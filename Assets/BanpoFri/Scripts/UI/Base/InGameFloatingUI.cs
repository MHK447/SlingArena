using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BanpoFri;


// LateUpdate 팔로우가 이동 주체(MoveMapController.LateUpdate 등)보다 항상 뒤에 돌도록 실행 순서를 늦춘다.
// Update 에서 따라가면 코루틴/LateUpdate 로 움직이는 대상보다 1프레임 늦어 이동 중 떨림이 생긴다.
[DefaultExecutionOrder(1000)]
public class InGameFloatingUI : MonoBehaviour, IFloatingUI
{
    [SerializeField]
    private bool TrackingScale = false;
    [SerializeField]
    private bool TrackingPos = false;
    protected Transform FollowTrans = null;
    [SerializeField]
    private Vector3 OffsetVec;

    public virtual void Init(Transform parent)
    {
        FollowTrans = parent;   
        this.transform.position = FollowTrans.position + OffsetVec;
    }

    public void UpdatePos()
    {
        if (FollowTrans != null)
            this.transform.position = FollowTrans.position + OffsetVec;
    }

    public void SetUpdatePos(Vector3 position)
    {
        this.transform.position = position + OffsetVec;
    }

    public void SetOffset(Vector3 pos)
    {
        OffsetVec = pos;
    }


    public void SetLocalUpdatePos(Vector3 position)
    {
        this.transform.localPosition = position + OffsetVec;
    }

    protected virtual void LateUpdate()
    {
        if (TrackingPos)
        {
            if (FollowTrans != null)
                this.transform.position = FollowTrans.position + OffsetVec;
        }

        if (TrackingScale)
        {
            if (FollowTrans != null)
                this.transform.localScale = FollowTrans.localScale;
        }


        if(FollowTrans == null || !FollowTrans.gameObject.activeSelf)
        {
            this.gameObject.SetActive(false);
        }
    }


    public virtual void Hide()
    {
        this.gameObject.SetActive(false);
    }
    
}
