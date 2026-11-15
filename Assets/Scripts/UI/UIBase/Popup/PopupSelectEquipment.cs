using UnityEngine;
using BanpoFri;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine.UI;
[UIPath("UI/Popup/PopupSelectEquipment")]
public class PopupSelectEquipment : CommonUIBase, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField]
    private RectTransform CardRoot;   // CardImg — 좌우 스와이프로 움직일 카드

    [SerializeField]
    private List<EquipmentStatComponent> StatComponentList;

    [SerializeField]
    private CardEquipComponent CardEquipComponent;

    [SerializeField]
    private GameObject GreenGlow;   // 오른쪽(장비 장착) 방향 반원 글로우 — 카드가 오른쪽으로 넘어가면 표시

    [SerializeField]
    private Image HeaderImg;


    // 놓았을 때 원위치 복귀 시간.
    private const float ReturnDuration = 0.25f;

    // 드래그 방향으로 카드가 기울어지는 정도 — 이동량(px)당 각도와 최대 각도.
    private const float TiltPerPixel = 0.05f;
    private const float MaxTiltAngle = 12f;

    // 오른쪽으로 이 거리(px) 이상 이동하면 GreenGlow 표시.
    private const float GlowActivateOffset = 80f;

    private Vector2 cardOriginPos;
    private bool cardDragging;
    private float dragStartLocalX;
    private float cardStartX;

    protected override void Awake()
    {
        base.Awake();
        if (CardRoot != null)
            cardOriginPos = CardRoot.anchoredPosition;
    }


    public void Set(int equipidx , int equiptype , int cardgradeidx , int cardlevel)
    {
        // 카드 루트 스케일 0 → 1 등장 연출. 팝업이 재활용되므로 Set 될 때마다
        // 남은 트윈을 정리하고 0부터 다시 재생한다.
        if (CardRoot != null)
        {
            var cardTr = CardRoot.transform;
            cardTr.DOKill();
            cardTr.localScale = Vector3.zero;
            cardTr.localRotation = Quaternion.identity;      // 이전 드래그의 기울기 잔여 제거
            CardRoot.anchoredPosition = cardOriginPos;       // 이전 드래그의 위치 잔여 제거
            HeaderImg.color = Config.Instance.GetImageColor($"InGameEquipment_Grade_Color_{cardgradeidx}");

            if (GreenGlow != null)
                ProjectUtility.SetActiveCheck(GreenGlow, false);
            cardTr.DOScale(1.6f, 0.35f).SetEase(Ease.OutBack).SetUpdate(true);

            CardEquipComponent.Set(equipidx , equiptype , cardgradeidx , cardlevel);


            var td = Tables.Instance.GetTable<EquipmentInfo>().GetData(new KeyValuePair<int, int>(equipidx, equiptype));

            if(td != null)
            {
                foreach(var stat in StatComponentList)
                {
                    ProjectUtility.SetActiveCheck(stat.gameObject , false);
                }

                for(int i = 0; i < td.status_type.Count; i++)
                {
                    var stat = StatComponentList[i];
                    ProjectUtility.SetActiveCheck(stat.gameObject , true);
                    stat.Set(td.status_type[i] , td.status_value[i] , td.status_value[i] > 0);
                }
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (CardRoot == null) return;

        // 카드 위에서 시작한 드래그만 처리 (Dim 등 다른 곳 스와이프는 무시).
        var pressed = eventData.pointerPressRaycast.gameObject;
        if (pressed == null || !pressed.transform.IsChildOf(CardRoot)) return;

        if (!TryGetLocalX(eventData, out var localX)) return;

        cardDragging = true;
        dragStartLocalX = localX;
        cardStartX = CardRoot.anchoredPosition.x;
        CardRoot.DOKill();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!cardDragging || CardRoot == null) return;
        if (!TryGetLocalX(eventData, out var localX)) return;

        // 손가락을 따라 좌우로만 이동.
        CardRoot.anchoredPosition = new Vector2(cardStartX + (localX - dragStartLocalX), cardOriginPos.y);

        // 원위치 기준 이동량만큼 진행 방향으로 살짝 기울임 (오른쪽 이동 = 시계 방향).
        var offsetX = CardRoot.anchoredPosition.x - cardOriginPos.x;
        var tilt = Mathf.Clamp(-offsetX * TiltPerPixel, -MaxTiltAngle, MaxTiltAngle);
        CardRoot.localRotation = Quaternion.Euler(0f, 0f, tilt);

        // 카드가 오른쪽(장비 장착) 방향으로 충분히 넘어가면 반원 글로우 표시.
        if (GreenGlow != null)
            ProjectUtility.SetActiveCheck(GreenGlow, offsetX > GlowActivateOffset);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!cardDragging || CardRoot == null) return;
        cardDragging = false;

        // 글로우가 켜진 상태(오른쪽으로 충분히 민 상태)에서 놓으면 선택 확정 — 팝업 닫기.
        var offsetX = CardRoot.anchoredPosition.x - cardOriginPos.x;
        if (offsetX > GlowActivateOffset)
        {
            if (GreenGlow != null)
                ProjectUtility.SetActiveCheck(GreenGlow, false);
            Hide();
            return;
        }

        // 원위치로 스냅백 (기울기도 함께 복귀).
        CardRoot.DOAnchorPosX(cardOriginPos.x, ReturnDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
        CardRoot.DOLocalRotate(Vector3.zero, ReturnDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);

        if (GreenGlow != null)
            ProjectUtility.SetActiveCheck(GreenGlow, false);
    }

    // 스크린 좌표 → 카드 부모 기준 로컬 X.
    private bool TryGetLocalX(PointerEventData eventData, out float localX)
    {
        localX = 0f;
        var parent = CardRoot.parent as RectTransform;
        if (parent == null) return false;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out var local))
            return false;
        localX = local.x;
        return true;
    }
}
