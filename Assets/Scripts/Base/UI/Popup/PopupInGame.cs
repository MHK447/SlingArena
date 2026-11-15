using UnityEngine;
using BanpoFri;
using TMPro;
using UniRx;

/// <summary>
/// Displays the current stage and wave for ZombieDefense.
/// </summary>
[UIPath("UI/Popup/PopupInGame")]
public class PopupInGame : UIBase
{

    [SerializeField]
    private TextMeshProUGUI StageText;

    // Init 이 다시 불려도 구독이 중복되지 않도록 매번 비우고 다시 구독한다.
    private readonly CompositeDisposable stageTextDisposables = new CompositeDisposable();

    public void Init()
    {

        // 스테이지/웨이브 값이 바뀔 때마다 텍스트 갱신(구독 시점에 현재 값으로 1회 즉시 호출됨).
        stageTextDisposables.Clear();
        if (StageText == null) return;
        GameRoot.Instance.UserData.Stageidx
            .CombineLatest(GameRoot.Instance.UserData.Waveidx, (stage, wave) => (stage, wave))
            .Subscribe(v => StageText.text = $"Stage {v.stage}-{v.wave + 1}")
            .AddTo(stageTextDisposables);
    }

    private void OnDestroy()
    {
        stageTextDisposables.Dispose();
    }
}
