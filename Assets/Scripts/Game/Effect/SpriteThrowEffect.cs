using DG.Tweening;
using UnityEngine.UI;
using BanpoFri;
using UnityEngine;

public struct SpriteThrowEffectParameters
{
    public Sprite sprite;
    public float scale;
    public float duration;
    public float delay;
    public bool fixRotation;
    public float rotation;
    public AnimationCurve scaleCurve;
    // true 면 Time.timeScale 의 영향을 받는다(게임 멈춤 중 비행도 같이 멈춤).
    // 기본 false = 기존 동작(unscaled — 팝업이 게임을 멈춰도 연출이 계속 돈다).
    public bool useScaledTime;
    // true 면 도착 콜백을 이펙트 수거(비활성화) '직후'에 호출 — 아이콘이 사라진 뒤 값이 반영돼야
    // 할 때 사용(경험치 별 등). 기본 false = 기존 동작(도착 시점, 수거 0.05초 전 호출).
    public bool callbackAfterCollect;
}

[EffectPath("Effect/SpriteThrowEffect", true)]
public class SpriteThrowEffect : Effect
{
    [SerializeField]
    private Image Img;

    private Vector3 startPos;
    private Transform endTarget;
    private Vector3 endPos;
    private Vector3 nudgePos;
    private float move;

    private SpriteThrowEffectParameters parameters;

    public void ShowWorldPos(Vector3 startWorldPos, Transform targetUI, System.Action callback, SpriteThrowEffectParameters _parameters)
    {
        ShowUIPos(ProjectUtility.worldToUISpace(GameRoot.Instance.UISystem.WorldCanvas, startWorldPos), targetUI, callback, _parameters);
    }

    public void ShowUIPos(Vector3 from, Transform targetUI, System.Action callback, SpriteThrowEffectParameters _parameters)
    {
        if (targetUI == null)
            return;

        parameters = _parameters;
        if (Img != null)
            Img.sprite = parameters.sprite;
        float duration = parameters.duration;
        Vector3 to = targetUI.position;

        move = 0;
        transform.SetPositionAndRotation(from, parameters.fixRotation ? Quaternion.Euler(0, 0, parameters.rotation) : Quaternion.Euler(0, 0, Random.Range(0, 360)));
        transform.localScale = Vector3.zero;

        startPos = from;
        endTarget = targetUI;
        nudgePos = Vector3.zero;

        // unscaled(독립 업데이트) 여부 — useScaledTime 이면 timeScale 영향을 받게 한다.
        bool unscaled = !parameters.useScaledTime;

        Sequence sequence = DOTween.Sequence();
        Vector3 targetNudgeOffset = (Vector3)Random.insideUnitCircle * 100;

        sequence.Append(
            DOVirtual.Vector3(Vector3.zero, targetNudgeOffset, 0.4f, x =>
            {
                nudgePos = x;
            }).SetUpdate(unscaled).SetEase(Ease.OutCubic).SetTarget(transform));

        if (parameters.scaleCurve == null) sequence.Join(transform.DOScale(parameters.scale, 0.1f).SetEase(Ease.InOutCubic).SetUpdate(unscaled));
        else sequence.Join(transform.DOScale(parameters.scale, duration).SetEase(parameters.scaleCurve).SetUpdate(unscaled));

        sequence.Join(DOVirtual.Float(0, 1, duration, x =>
        {
            move = x;
        }).SetEase(Ease.InCubic).SetUpdate(unscaled).SetTarget(transform).SetDelay(0.05f));

        sequence.InsertCallback(duration, () => { if (!parameters.callbackAfterCollect) callback?.Invoke(); });
        sequence.InsertCallback(duration + 0.05f, () =>
        {
            OnForceCollect();

            // callbackAfterCollect — 수거(비활성화)가 끝난 직후에 콜백.
            if (parameters.callbackAfterCollect) callback?.Invoke();
        });

        sequence.SetUpdate(unscaled);
        sequence.SetDelay(parameters.delay);
    }

    private void Update()
    {
        if (endTarget != null && move < 0.9f)
            endPos = endTarget.position;
        if (transform != null)
            transform.position = Vector3.Lerp(startPos, endPos - nudgePos, move) + nudgePos;
    }

    private void OnDisable()
    {
        if (transform != null)
        {
            transform.DOKill();
            transform.localScale = Vector3.zero;
        }
    }
}
