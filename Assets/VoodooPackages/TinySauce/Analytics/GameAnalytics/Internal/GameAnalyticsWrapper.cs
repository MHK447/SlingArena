using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using GameAnalyticsSDK;
using UnityEngine;
using Voodoo.Sauce.Internal;
using Object = UnityEngine.Object;

namespace Voodoo.Tiny.Sauce.Internal.Analytics
{
    public static class GameAnalyticsWrapper
    {
        private const string TAG = "GameAnalyticsWrapper";
        private const byte GA_CUSTOM_EVEN_MAX_CHAR_LENGTH = 64;

        private static bool _isInitialized;
        private static bool _isInitializing;
        private static bool _isDisabled = true;

        private static readonly Queue<GameAnalyticsEvent> QueuedEvents = new Queue<GameAnalyticsEvent>();


        #region Initialisation

        internal static void SetPrivacyConsent(bool analytics)
        {
            bool changed = _isDisabled == analytics;
            _isDisabled = !analytics;
            if (_isDisabled)
                QueuedEvents.Clear();

            if (changed && _isInitialized)
            {
                try
                {
                    ApplyNativePrivacyConsent();
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"GameAnalytics consent update failed ({exception.GetType().Name}).");
                }
            }
        }

        private static void ApplyNativePrivacyConsent()
        {
            // GameAnalytics measures use. Adjust and Meta handle install attribution separately.
            GameAnalytics.EnableAdvertisingIdTracking(false);
            GameAnalytics.SetEnabledEventSubmission(!_isDisabled, false);
        }
        
        internal static bool Initialize(bool consent)
        {
            if (!consent) {
                Disable();
                return _isInitialized;
            }

            if (_isInitialized || _isInitializing) return true;
            _isInitializing = true;
            InitializeWhenReady();
            return true;
        }

        private static async void InitializeWhenReady()
        {
            try
            {
                await PrepareAndroidFrameSampler();
                if (_isDisabled) return;

                InstantiateGameAnalytics();
                _isInitialized = true;
                VoodooLog.Log(TAG, "GameAnalytics initialized, tracking pending events: " + QueuedEvents.Count);
                while (QueuedEvents.Count > 0) QueuedEvents.Dequeue().Track();
            }
            catch (Exception exception)
            {
                QueuedEvents.Clear();
                if (!_isInitialized) _isDisabled = true;
                Debug.LogWarning($"GameAnalytics setup failed ({exception.GetType().Name}).");
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private static Task PrepareAndroidFrameSampler()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            // GameAnalytics starts a ValueAnimator during its first native state access,
            // even with FPS sampling disabled. Android's UI thread owns its Looper.
            var prepared = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                try
                {
                    GameAnalytics.EnableFpsHistogram(false);
                    prepared.TrySetResult(true);
                }
                catch (Exception exception)
                {
                    prepared.TrySetException(exception);
                }
            }));
            return prepared.Task;
#else
            return Task.CompletedTask;
#endif
        }
        
        private static void SetBuildVersion(string buildVersion)
        {
            int platformIndex = -1;
#if UNITY_ANDROID
            platformIndex = GameAnalytics.SettingsGA.Platforms.IndexOf(RuntimePlatform.Android);
#elif UNITY_IOS
            platformIndex = GameAnalytics.SettingsGA.Platforms.IndexOf(RuntimePlatform.IPhonePlayer);
#endif
            if (platformIndex >= 0)
            {
                GameAnalytics.SettingsGA.Build[platformIndex] = buildVersion;
            }
        }
        

        private static void InstantiateGameAnalytics()
        {
            var gameAnalyticsComponent = Object.FindObjectOfType<GameAnalytics>();
            if (gameAnalyticsComponent == null) {
                var gameAnalyticsGameObject = new GameObject("GameAnalytics");
                gameAnalyticsGameObject.AddComponent<GameAnalytics>();
                gameAnalyticsGameObject.SetActive(true);
            } else {
                gameAnalyticsComponent.gameObject.name = "GameAnalytics";
            }
            

            string appVersionName = Application.version;
            
            if (Type.GetType("Voodoo.Sauce.Internal.Ads.TSAdsManager") != null)
                if ((bool) Type.GetType("Voodoo.Sauce.Internal.Ads.TSAdsManager").GetField("_areAdsEnabled", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null))
                    appVersionName += "-Ads";

            if (!string.IsNullOrEmpty(TinySauce.GetABTestCohort()))
            {
                SetBuildVersion($"{appVersionName}-ABCohort:{TinySauce.GetABTestCohort() ?? "Default"}");
            }
            else
            {
                SetBuildVersion($"{appVersionName}");
            }
            
            SetCustomFields();

            ApplyNativePrivacyConsent();
            GameAnalytics.Initialize();
        }

        private static void SetCustomFields()
        {
            Dictionary<string, object> customFields = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(TinySauce.GetABTestCohort()))
            {
                customFields.Add("ABCohort", TinySauce.GetABTestCohort());
            }
            customFields.Add("TSVersion", TinySauce.Version);
            GameAnalytics.SetGlobalCustomEventFields(customFields);
        }

        private static void Disable()
        {
            VoodooLog.Log(TAG, "Disabling GameAnalytics No User Consent.");
            SetPrivacyConsent(false);
        }

        #endregion

        #region ProgressionEvents
            
       
        internal static void TrackProgressEvent(GAProgressionStatus status, string progress1, string progress2, string progress3, int? score)
        {
            if (_isDisabled) return;

            GAProgressionEvent progressEvent = null;
            progressEvent = new GAProgressionEvent(status, progress1, progress2, progress3, score);
            
            if (!_isInitialized) {
                VoodooLog.Log(TAG, "GameAnalytics NOT initialized queuing event..." + status);
                QueuedEvents.Enqueue(progressEvent);
            } else {
                VoodooLog.Log(TAG, "Sending event \"" + status + "\" to GameAnalytics");
                progressEvent.Track();
            }
        }

        #endregion

        #region DesignEvents

        internal static void TrackDesignEvent(string eventName, float? eventValue)
        {
            if (_isDisabled) return;

            var designEvent = new GADesignEvent(eventName, eventValue);
            
            if (!_isInitialized) {
                VoodooLog.Log(TAG, "GameAnalytics NOT initialized queuing event..." + eventName);
                QueuedEvents.Enqueue(designEvent);
            } else {
                if(eventName.Length >= GA_CUSTOM_EVEN_MAX_CHAR_LENGTH)
                {
                    VoodooLog.LogE(TAG, "[VoodooLogError] Your EventName exceeds 64 characters.\n" +
                                   "EventName: " + eventName + "\n=> GameAnalytics CustomEvent not sent. " +
                                   "Reduce the length of your dimensions names.\n");
                    return;
                }
                
                VoodooLog.Log(TAG, "Sending event \"" + eventName + "\" to GameAnalytics");
                designEvent.Track();
            }
        }
            
        #endregion

        #region AdEvents
        
        internal static void TrackAdEvent(GAAdAction adAction, GAAdType adType, string adSdkName, string adPlacement)
        {
            if (_isDisabled) return;

            var adEvent = new GAAdEvent(adAction, adType, adSdkName, adPlacement);

            if (!_isInitialized) {
                VoodooLog.Log(TAG, "GameAnalytics NOT initialized queuing event..." + adType);
                QueuedEvents.Enqueue(adEvent);
            } else {
                VoodooLog.Log(TAG, "Sending event " + adType + " to GameAnalytics");
                adEvent.Track();
            }
        }

        #endregion

        #region ResourceEvents

        internal static void DeclareCurrency(string currency)
        {
            GameAnalytics.SettingsGA.ResourceCurrencies.Add(currency);
        }

        internal static void DeclareItemType(string itemType)
        {
            GameAnalytics.SettingsGA.ResourceItemTypes.Add(itemType);
        }

        internal static void TrackResourceEvent(GAResourceFlowType flowType, string currency, int amount, string itemType, string itemName)
        {
            if (_isDisabled) return;

            var resourceEvent = new GAResourceEvent(flowType, currency, amount, itemType, itemName);
            if (!_isInitialized) {
                VoodooLog.Log(TAG, "GameAnalytics NOT initialized queuing event..." + flowType);
                QueuedEvents.Enqueue(resourceEvent);
            } else {
                VoodooLog.Log(TAG, "Sending event " + flowType + " to GameAnalytics");
                resourceEvent.Track();
            }
        }

        #endregion

        #region Business Events

        internal static void TrackBusinessEvent(string currency, int amount, string itemType, string itemID, string cartType)
        {
            
            if (_isDisabled) return;

            var businessEvent = new GABusinessEvent(currency, amount, itemType, itemID, cartType);
            if (!_isInitialized) {
                VoodooLog.Log(TAG, "GameAnalytics NOT initialized queuing event... Business Event : " + itemType + ", "+ itemID + ":" + amount);
                QueuedEvents.Enqueue(businessEvent);
            } else {
                VoodooLog.Log(TAG, "Sending event Business Event : " + itemType + ", "+ itemID + ":" + amount + " to GameAnalytics");
                businessEvent.Track();
            }
        }
        
        /*
        internal static void TrackDesignEvent(string eventName, float? eventValue)
        {
            if (_isDisabled) return;

            var designEvent = new GADesignEvent(eventName, eventValue);
            
            if (!_isInitialized) {
                VoodooLog.Log(TAG, "GameAnalytics NOT initialized queuing event..." + eventName);
                QueuedEvents.Enqueue(designEvent);
            } else {
                VoodooLog.Log(TAG, "Sending event \"" + eventName + "\" to GameAnalytics");
                designEvent.Track();
            }
        }*/

        #endregion

        

    }
}
