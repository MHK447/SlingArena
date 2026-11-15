using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

public class GameNotificationMultiRegister : MonoBehaviour
{
    [Serializable]
    public class Data
    {
        public bool awakeRegit = false;
        public GameNotificationSystem.NotificationCategory category;
        public int targetIdx = -1;
        public int targetSubIdx = -1;
        public bool reverse = false;
        [HideInInspector]
        public bool on = false;
        public IDisposable disposables = null;
    }
    [SerializeField]
    private GameObject redDot;
    [SerializeField]
    private List<Data> listNoti = new List<Data>();
    private CompositeDisposable disposables = new CompositeDisposable();

    private void Awake()
    {
        foreach (var data in listNoti)
        {
            if (data.awakeRegit)
            {
                Register(data);
            }
        }

        UpdateActive();
    }

    public void Init(GameNotificationSystem.NotificationCategory category, int _targetIdx, int _targetSubIdx)
    {
        var find = listNoti.Find(x => x.category == category);
        if (find != null && !find.awakeRegit)
        {
            Register(find);
            UpdateActive();
        }
    }

    public void Add(GameNotificationSystem.NotificationCategory _category, int _targetIdx, int _targetSubIdx, bool _reverse)
    {
        var newNoti = new Data()
        {
            category = _category,
            targetIdx = _targetIdx,
            targetSubIdx = _targetSubIdx,
            awakeRegit = false,
            reverse = _reverse
        };
        listNoti.Add(newNoti);

        Register(newNoti);
        UpdateActive();
    }

    // 카테고리의 noti(ReactiveProperty)에 구독한다.
    // GameNotification.Create() 가 다시 호출되면 notifications 딕셔너리가 재생성되므로
    // ObserveAdd 로 재구독하여 옛 ReactiveProperty 에 묶여 죽는 문제를 방지한다.
    private void Register(Data data)
    {
        var capture = data;

        // 딕셔너리에 해당 카테고리가 (재)추가될 때마다 재구독
        GameRoot.Instance.GameNotification.GetNotifications
            .ObserveAdd()
            .Where(x => x.Key == capture.category)
            .Subscribe(_ => SubscribeNoti(capture))
            .AddTo(disposables);

        SubscribeNoti(capture);
    }

    private void SubscribeNoti(Data data)
    {
        // 이전 구독 해제 (재생성 시 옛 구독 정리)
        if (data.disposables != null)
        {
            if (!disposables.Remove(data.disposables))
                data.disposables.Dispose();
            data.disposables = null;
        }

        var noti = GameRoot.Instance.GameNotification.GetData(data.category, data.targetIdx, data.targetSubIdx);
        if (noti == null) return;

        data.disposables = noti.on.Subscribe(x =>
        {
            data.on = data.reverse ? !x : x;
            UpdateActive();
        }).AddTo(disposables);

        UpdateActive();
    }

    public void UpdateActive()
    {
        var totalActive = false;
        foreach (var data in listNoti)
        {
            if (data.on)
            {
                totalActive = true;
                break;
            }
        }
        ProjectUtility.SetActiveCheck(redDot, totalActive);
    }

    public void UpdateRedDotPos(Vector3 worldPos)
    {
        redDot.transform.position = worldPos;
    }

    private void OnDestroy()
    {
        disposables.Clear();
    }
}
