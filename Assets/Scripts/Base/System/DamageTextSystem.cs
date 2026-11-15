using System;
using Cysharp.Threading.Tasks;
using DamageNumbersPro;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class DamageTextSystem
{
    private DmgNumberEffect dmg;
    private GameObject dmgAsset;
    // ShowDamageForDice가 풀에 남긴 회전/스케일이 다음 Spawn(ShowDamage/ShowString)에 그대로 따라오는 걸 복구하기 위한 기본값 캐시.
    private float defaultScale = 1f;

    public async void Create()
    {
        try
        {
            dmgAsset = await Addressables.LoadAssetAsync<GameObject>("DmgNumberEffect").ToUniTask();
            dmg = dmgAsset.GetComponent<DmgNumberEffect>();
            defaultScale = dmgAsset.transform.localScale.x;
            dmg.Number.PrewarmPool();
        }
        catch (Exception e)
        {
            Debug.LogError($"[DamageTextSystem] Create failed: {e.Message}");
        }
    }

    public void Dispose()
    {
        if (dmgAsset != null)
        {
            Addressables.Release(dmgAsset);
            dmgAsset = null;
            dmg = null;
        }
    }
    public void ShowDamage(double damage, Vector3 pos, Color color, bool iscritical = false)
    {
        if (dmg == null || dmg.Number == null) return;

        // 데미지/HP 는 소수점 없이 정수로 표시. 1000 이상은 약식(k/m/b) 표기.
        double rounded = Math.Ceiling(damage);

        string formattedName;
        if (rounded >= 1000)
        {
            formattedName = ProjectUtility.AbbreviateNumber(rounded);
        }
        else
        {
            formattedName = ((long)rounded).ToString();
        }
        Color finalColor = iscritical ? Color.yellow : color;
        DmgNumberEffect instance = dmg.Number.Spawn(pos, formattedName, finalColor).GetComponent<DmgNumberEffect>();
        // Dice 변종이 남긴 회전/스케일이 풀 재활용 시 그대로 따라와 메인 카메라에서 edge-on으로 안 보이는 케이스 방지.
        // PreparePooling은 localScale만 originalScale로 복구하고 회전은 건드리지 않으며, SetScale은 originalScale 자체를 덮어쓰기 때문에 매번 명시적으로 되돌려야 함.
        instance.transform.rotation = Quaternion.identity;
        instance.Number.SetScale(defaultScale);
        // 풀 재활용 시: Spawn 내부에서 SetActive(true) → OnEnable → Restart → UpdateText 가 일어나는데
        // 이 시점의 leftText 는 아직 prefab default ("1") 라 다음 프레임까지 우리가 넣은 텍스트가 반영 안 됨.
        // 그 1프레임 갭에 텍스트가 안 보이거나 잘못 보이는 케이스 방지 — Spawn 직후 강제로 다시 빌드.
        if (instance != null && instance.Number != null)
            instance.Number.UpdateText();

        instance.CriticalIcon.gameObject.SetActive(iscritical);
        instance.CriticalIcon.DORewind();
        instance.CriticalIcon.color = Color.white;
        instance.CriticalIcon.DOFade(0,0.3f)
            .SetEase(Ease.Linear)
            .SetDelay(0.1f);
    }

    // 주사위 SubCamera는 탑다운(X=90°) 뷰라서 기본 XY 평면 텍스트가 edge-on이 되어 안 보임 → XZ 평면으로 눕혀서 스폰.
    // SetColor가 tmp.color에만 적용돼서 prefab의 leftTextSettings 색에 묻히는 케이스를 피하려고 인라인 <color> 태그로 강제.
    public void ShowDamageForDice(double damage, Vector3 pos, Color color, float scale = 1f, bool iscritical = false)
    {
        if (dmg == null || dmg.Number == null) return;

        // 데미지/HP 는 소수점 없이 정수로 표시. 1000 이상은 약식(k/m/b) 표기.
        double rounded = Math.Ceiling(damage);

        string formattedName;
        if (rounded >= 1000)
        {
            formattedName = ProjectUtility.AbbreviateNumber(rounded);
        }
        else
        {
            formattedName = ((long)rounded).ToString();
        }
        Color finalColor = iscritical ? Color.yellow : color;
        // Spawn(..., color)는 tmp.color를 dice 색으로 세팅하고, TMP는 face color × vertex color로 곱해버려서
        // 인라인 <color>와 곱해지면 색이 어두워지거나 의도와 다르게 섞임 → vertex color는 흰색으로 두고 인라인 태그만 사용.
        string coloredText = $"<color=#{ColorUtility.ToHtmlStringRGBA(finalColor)}>{formattedName}</color>";
        DmgNumberEffect instance = dmg.Number.Spawn(pos, coloredText, Color.white).GetComponent<DmgNumberEffect>();
        instance.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        instance.Number.SetScale(scale);

        instance.CriticalIcon.gameObject.SetActive(iscritical);
        instance.CriticalIcon.DORewind();
        instance.CriticalIcon.color = Color.white;
        instance.CriticalIcon.DOFade(0,0.3f)
            .SetEase(Ease.Linear)
            .SetDelay(0.1f);
    }



    public void ShowString(string text, Vector3 pos, Color color, bool iscritical = false)
    {
        if (dmg == null || dmg.Number == null) return;

        string formattedName = text.ToString();
        Color finalColor = iscritical ? Color.yellow : color;
        DmgNumberEffect instance = dmg.Number.Spawn(pos, formattedName, finalColor).GetComponent<DmgNumberEffect>();
        // ShowDamage와 동일 이유 — 풀에서 Dice 변종 잔재(회전/스케일)를 매번 초기화.
        instance.transform.rotation = Quaternion.identity;
        instance.Number.SetScale(defaultScale);
        if (instance != null && instance.Number != null)
            instance.Number.UpdateText();

        instance.CriticalIcon.gameObject.SetActive(iscritical);
        instance.CriticalIcon.DORewind();
        instance.CriticalIcon.color = Color.white;
        instance.CriticalIcon.DOFade(0,0.3f)
            .SetEase(Ease.Linear)
            .SetDelay(0.1f);
    }
}


