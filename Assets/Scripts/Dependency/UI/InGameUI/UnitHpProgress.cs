using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BanpoFri;
using TMPro;
using UniRx;

[FloatUIPath("UI/InGame/UnitHpProgress")]
[FloatingDepth((int)Config.FloatingUIDepth.HpProgress)]

public class UnitHpProgress : InGameFloatingUI
{  // 주의: 프리팹 배선상 HpSlider = 뒤쪽 흰색 '잔상' 바, DelayHealthBar = 앞쪽 빨간 '실제 HP' 바.
    [SerializeField]
    private Slider HpSlider;

    [SerializeField]
    private Slider DelayHealthBar;

    [SerializeField]
    private Image BossImg;

    // 피격 후 잔상 바가 머무는 시간(초) — 이 시간 동안 데미지 폭(흰색 구간)이 보인다.
    [SerializeField]
    private float DrainDelay = 0.25f;

    // 잔상 바가 목표값까지 드레인되는 시간(초).
    public float updatespeed = 0.4f;

    private double CurHp;

    private double MaxHp;

    private Coroutine Col;

    private CompositeDisposable disposables = new CompositeDisposable();

    private void OnEnable()
    {
        // 활성화될 때마다 초기화
        if (HpSlider != null) HpSlider.value = 1f;
        if (DelayHealthBar != null) DelayHealthBar.value = 1f;

    }

    // 보스 체력바 — 보스면 스케일 1.4 + 보스 아이콘(BossImg) 표시, 그 외엔 스케일 0.7 + 아이콘 숨김.
    // (체력바는 풀에서 재사용되므로 매 표시마다 호출해 보스/일반 상태를 확실히 되돌린다.)
    public void SetBossRoot(bool isBoss)
    {
        transform.localScale = Vector3.one * (isBoss ? 1.4f : 0.7f);
        if (BossImg != null)
            ProjectUtility.SetActiveCheck(BossImg.gameObject, isBoss);
    }

    public void SetHpText(double curhp, double maxhp, bool immediate = false)
    {
        CurHp = curhp;

        MaxHp = maxhp;

        // maxhp == 0 케이스에서 NaN/Infinity가 슬라이더에 들어가 시각적으로 비어 보이는 것 방지.
        var curhpvalue = (maxhp > 0) ? Mathf.Clamp01((float)(curhp / maxhp)) : 0f;

       
        // 앞쪽 빨간 바(실제 HP)는 항상 즉시 반영.
        if (DelayHealthBar != null) DelayHealthBar.value = curhpvalue;

        if (Col != null)
        {
            GameRoot.Instance.StopCoroutine(Col);
            Col = null;
        }

        // 뒤쪽 흰 잔상 바 — 즉시 모드/회복이면 바로 맞추고, 피격이면 잠시 머문 뒤 드레인.
        if (HpSlider != null)
        {
            if (immediate || HpSlider.value <= curhpvalue)
            {
                HpSlider.value = curhpvalue;
            }
            else
            {
                Col = GameRoot.Instance.StartCoroutine(UpdateDelayedHealthBar(curhpvalue));
            }
        }
    }

    private void OnDisable()
    {
        if (Col != null)
            GameRoot.Instance.StopCoroutine(Col);
    }

    public void SetArmor(int armor)
    {
    }


    public void SetShiled()
    {
        disposables.Clear();
    }

    void OnDestroy()
    {
        disposables.Clear();
    }




    private IEnumerator UpdateDelayedHealthBar(float targetvalue)
    {
        // 피격 직후 잔상이 잠시 머물러야 데미지 폭이 보인다. (연속 피격 시 코루틴 재시작 → 홀드도 리셋)
        if (DrainDelay > 0f)
            yield return new WaitForSeconds(DrainDelay);

        float preChangePct = HpSlider.value;
        float elapsed = 0f;

        while (elapsed < updatespeed)
        {
            elapsed += Time.deltaTime;
            HpSlider.value = Mathf.Lerp(preChangePct, targetvalue, elapsed / updatespeed);
            yield return null;
        }

        HpSlider.value = targetvalue;
        Col = null;
    }
}

