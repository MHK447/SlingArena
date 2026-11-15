using UnityEngine;
using BanpoFri;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

// 쿨타임이 차오르는 만큼 0~1 로 채워지는 다이스 게이지.
// 0%(빈 상태)=빨강에서 시작해 채워질수록 초록에 가까워지고, 100%(가득)일 때 초록.

[FloatUIPath("UI/InGame/DiceProgressGuage")]
[FloatingDepth((int)Config.FloatingUIDepth.HpProgress)]
public class DiceProgressGuage : InGameFloatingUI
{
    // 쿨타임 진행도를 표시할 슬라이더(0~1). 우선 사용.
    [SerializeField]
    private Slider ProgressSlider;

    // 슬라이더가 없을 때 사용하는 채워지는 이미지. (Image Type = Filled 권장)
    [SerializeField]
    private Image FillImage;

    // 0%(빈 상태) 색.
    [SerializeField]
    private Color EmptyColor = Color.red;

    // 100%(가득 찬 상태) 색.
    [SerializeField]
    private Color FullColor = Color.green;

    // 인스펙터에서 컴포넌트 추가 시 같은 오브젝트의 Slider/Image 를 기본 연결.
    private void Reset()
    {
        ProgressSlider = GetComponentInChildren<Slider>(true);
        FillImage = GetComponent<Image>();
    }

    private void Awake()
    {
        if (ProgressSlider == null)
            ProgressSlider = GetComponentInChildren<Slider>(true);
        if (FillImage == null)
            FillImage = GetComponentInChildren<Image>();
    }

    // t: 0~1 진행도. 슬라이더가 있으면 value 를, 없으면 fillAmount+색(빨강→초록)을 갱신.
    public void SetProgress(float t)
    {
        t = Mathf.Clamp01(t);

        if (ProgressSlider != null)
        {
            ProgressSlider.value = t;
            return;
        }

        if (FillImage == null) return;
        FillImage.fillAmount = t;
        FillImage.color = Color.Lerp(EmptyColor, FullColor, t);
    }

    // 게이지를 보이게 한다(쿨타임 시작 시).
    public void Show()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
    }
}
