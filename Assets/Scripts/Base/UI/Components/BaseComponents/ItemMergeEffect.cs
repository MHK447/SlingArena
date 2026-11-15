using UnityEngine;
using BanpoFri;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using Coffee.UIExtensions;

// UI(트레이) 머지 이펙트 — Overlay 캔버스에서는 월드 파티클이 UI 에 가려지므로,
// UIParticle 로 자식 파티클을 UI 메시로 렌더링하고, 자체 Canvas(overrideSorting)로
// 팝업들보다 위에 그린다.
// Play 로 들어온 좌표는 "화면(스크린) 좌표"로 해석해서 부모 캔버스의 로컬(UI) 좌표로
// 변환 후 anchoredPosition 으로 배치한다.
[EffectPath("Effect/ItemMergeEffect", true, false)]
public class ItemMergeEffect : Effect
{
}
