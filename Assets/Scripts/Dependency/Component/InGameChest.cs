using UnityEngine;
using BanpoFri;
using DG.Tweening;

public class InGameChest : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer Fx_Color;

    [SerializeField]
    private Animator Anim;

    // 등급 컬러 세팅 + 닫힌 상태(Idle)로 살짝 위에서 떨어지며 등장.
    public void Set(int grade)
    {
        Fx_Color.color = Config.Instance.GetImageColor($"InGameEquipment_Grade_Color_{grade}");

        Anim.Play("Idle", 0, 0f);

        transform.DOKill();

        var landPos = transform.position;
        transform.position = landPos + Vector3.up * 2.5f;
        transform.DOMove(landPos, 0.5f).SetEase(Ease.OutBounce);
    }

    public void Open()
    {
        Anim.SetTrigger("Open");
    }
}
