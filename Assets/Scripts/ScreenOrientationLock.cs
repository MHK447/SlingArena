using UnityEngine;
using BanpoFri;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

/// <summary>
/// 모바일 환경에서 화면을 세로(Portrait)로 고정합니다.
/// 어떤 GameObject에도 붙일 필요 없이 게임 시작 시 자동 실행됩니다.
/// </summary>
public static class ScreenOrientationLock
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void LockToPortrait()
    {
        // 자동 회전을 모두 비활성화
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;

        // 세로 방향으로 고정
        Screen.orientation = ScreenOrientation.Portrait;
    }
}

