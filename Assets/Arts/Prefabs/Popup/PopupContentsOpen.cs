using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using BanpoFri;
using UnityEngine;
using UnityEngine.UI;

[UIPath("UI/Popup/PopupContentsOpen")]
public class PopupContentsOpen : CommonUIBase
{
    [SerializeField]
    private Image contentsIcon;
    [SerializeField]
    private Animator ani;
    private Action endCallback;
    private Vector3 targetWorldPos;
    private RectTransform targetRect;

    public void Set(ContentsOpenSystem.ContentsOpenType contentsOpenIdx, RectTransform _targetRect, Action onCallback)
    {
        SoundPlayer.Instance.PlaySound("effect_contents_open");
        targetRect = _targetRect;
        endCallback = onCallback;
        var td = Tables.Instance.GetTable<ContentsOpenCheck>().GetData((int)contentsOpenIdx);
        if(td != null)
        {
            contentsIcon.sprite  = AtlasManager.Instance.GetSprite(Atlas.Atlas_UI_Common, td.image);
        }

        // 이전에 MoveTarget으로 옮겨진 위치가 남아있을 수 있으므로 트윈 정리 후
        // 아이콘을 화면(부모) 정가운데로 강제 리셋. 아이콘 RectTransform의 앵커가
        // (0.5, 0.5)로 설정돼 있다는 전제(=프리팹 기본값) — 그래야 anchoredPosition=0이
        // 곧 화면 중심이 됨.
        var iconRect = contentsIcon.rectTransform;
        iconRect.DOKill();
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.localScale = Vector3.one;

        // Show 애니메이션을 처음부터 다시 재생해서 이전 상태 잔재 제거
        if (ani != null)
        {
            ani.Play("Show", 0, 0f);
            ani.Update(0f);
        }

     
    }



    public void MoveTarget()
    {
        ani.Play("Hide", 0, 0f);
        // var center = new UnityEngine.Vector3(
        // GameRoot.Instance.InGameSystem.CurInGame.CamPixelWidth / 2,
        // GameRoot.Instance.InGameSystem.CurInGame.CamPixelHeight / 2, 0);
        contentsIcon.transform.DOMove(targetRect.position, 1f ).SetEase(Ease.InExpo).onComplete = () => {
            endCallback?.Invoke();
            endCallback = null;
            Hide();
        };
    }

    // IEnumerator WaitFrameforAni()
    // {
    //     yield return new WaitForEndOfFrame();
    //     yield return new WaitForEndOfFrame();
    //     if(ani != null)
    //     {
    //         ani.Play("Show");
    //     }
    // }
}
