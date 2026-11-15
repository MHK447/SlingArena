using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_ANDROID && !UNITY_EDITOR
using Google.Play.Review;
#elif UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using BanpoFri;


public class PluginSystem
{
    public TpAdxProp ADProp = new TpAdxProp();
    public TpAnalyticsProp AnalyticsProp = new TpAnalyticsProp();
    public TpPlatformLoginProp LoginProp = new TpPlatformLoginProp();
    public TpFirebaseDataProp DataProp = new TpFirebaseDataProp();

#if UNITY_ANDROID && !UNITY_EDITOR
    private static string GoogleStoreUrl => $"https://play.google.com/store/apps/details?id={Application.identifier}";
    private ReviewManager _reviewManager;
    private PlayReviewInfo _playReviewInfo = null;
#elif UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    public static extern void IOSCallReview();
#endif

    private bool initSDK = false;
    internal static bool IsFirebaseAvailable { get; private set; }



    public IEnumerator Init()
    {
        GameRoot.Instance.StartCoroutine(ProjectUtility.FetchCountryCodeByIP());
        //review
#if UNITY_ANDROID && !UNITY_EDITOR
        InitSDK();
        _reviewManager = new ReviewManager();
        GameRoot.Instance.StartCoroutine(ReviewInProgressBefore());
#endif
        //localnoti
        LocalNotification.Init();

        IsFirebaseAvailable = false;
        var firebase = InitializeFirebaseAsync();
        yield return new WaitUntil(() => firebase.IsCompleted);
        IsFirebaseAvailable = firebase.Result;
    }

    private async Task<bool> InitializeFirebaseAsync()
    {
        try
        {
            var status = await Firebase.FirebaseApp.CheckAndFixDependenciesAsync();
            if (status != Firebase.DependencyStatus.Available)
            {
                Debug.LogWarning($"Firebase unavailable: {status}");
                return false;
            }

            _ = Firebase.FirebaseApp.DefaultInstance;
#if !UNITY_EDITOR
            DataProp.Init();
            Firebase.Messaging.FirebaseMessaging.TokenReceived += OnTokenReceived;
            Firebase.Messaging.FirebaseMessaging.MessageReceived += OnMessageReceived;
#endif
            return true;
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"Firebase initialization skipped: {exception.GetBaseException().Message}");
            return false;
        }
    }

    // 외부 SDK(광고/AppsFlyer/Facebook) 모두 제거됨. 광고 래퍼(스텁)만 초기화.
    // TODO(새 퍼블리셔 SDK): 광고·어트리뷰션 SDK 초기화(동의/ATT 포함)를 여기서 하고 완료 후 InitAction 호출.
    public void InitSDK(System.Action InitAction = null)
    {
        if (initSDK)
        {
            InitAction?.Invoke();
            return;
        }
        initSDK = true;

        ADProp.Init();
        InitAction?.Invoke();
    }

    public void Update()
    {
        LoginProp.Update();
    }

    bool onReview = false;
    IEnumerator coEndReview()
    {
        onReview = true;
        yield return new WaitForSeconds(120.0f);
        onReview = false;
    }

    public void StartReview()
    {
        GameRoot.Instance.StartCoroutine(coEndReview());
#if UNITY_ANDROID && !UNITY_EDITOR
        if(_playReviewInfo == null)
        {
            Application.OpenURL(GoogleStoreUrl);
            return;
        }
        GameRoot.Instance.StartCoroutine(ReviewInProgressAfter());
#elif UNITY_IOS && !UNITY_EDITOR
        if (UnityEngine.iOS.Device.RequestStoreReview())
        {
            BpLog.Log("UnityEngine.iOS.Device.RequestStoreReview");
        }
        else
        {
            IOSCallReview();
        }
#endif
    }
#if UNITY_ANDROID && !UNITY_EDITOR
    private IEnumerator ReviewInProgressBefore()
    {
        yield return null;
        var requestFlowOperation = _reviewManager.RequestReviewFlow();
        yield return requestFlowOperation;
        if (requestFlowOperation.Error != ReviewErrorCode.NoError)
        {            
            // Log error. For example, using requestFlowOperation.Error.ToString().
            BpLog.Log(requestFlowOperation.Error.ToString());
            yield break;
        }
        _playReviewInfo = requestFlowOperation.GetResult();
    }

    private IEnumerator ReviewInProgressAfter()
    {
        yield return null;
        var launchFlowOperation = _reviewManager.LaunchReviewFlow(_playReviewInfo);
        yield return launchFlowOperation;
        _playReviewInfo = null; // Reset the object
        if (launchFlowOperation.Error != ReviewErrorCode.NoError)
        {
            Application.OpenURL(GoogleStoreUrl);
            // Log error. For example, using requestFlowOperation.Error.ToString().
            BpLog.Log(launchFlowOperation.Error.ToString());
            yield break;
        }
    }
#endif

    public void OnTokenReceived(object sender, Firebase.Messaging.TokenReceivedEventArgs token)
    {
        BpLog.Log("Received Registration Token: " + token.Token);
    }

    public void OnMessageReceived(object sender, Firebase.Messaging.MessageReceivedEventArgs e)
    {
        BpLog.Log("Received a new message from: " + e.Message.From);
    }

    //어플 중지, 종료시 호출.
    public void OnApplicationPause(bool value)
    {
        if (value)
        {
            TimeSystem.GetCurTime();
            if (GameRoot.Instance == null)
            {
                Debug.Log("GameRoot is Null");
                return;
            }
            if (!GameRoot.LoadComplete)
            {
                return;
            }
            if (GameRoot.Instance.UserData == null)
            {
                Debug.Log("UserData is Null");
                return;
            }
            if (GameRoot.Instance.UserData.CurMode == null)
            {
                Debug.Log("CurMode is Null");
                return;
            }

            LocalNotification.AllClearNotification(); // 기존 로컬 푸시 삭제

            
           var list = Tables.Instance.GetTable<LocalPush>().DataList;
           var curTime = TimeSystem.GetCurTime();
           foreach (var element in list)
           {
               double remainTime = -1;

               switch (element.idx)
               {
                   case 1:
                       remainTime = 10;
                       break;
               }

               if (remainTime < 0)
                   continue;

            //    LocalNotification.CallNotification(
            //        element.index,
            //        remainTime,
            //        Tables.Instance.GetTable<Localize>().GetString(element.title),
            //        Tables.Instance.GetTable<Localize>().GetString(element.desc));
           }
        }
    }
    
}
