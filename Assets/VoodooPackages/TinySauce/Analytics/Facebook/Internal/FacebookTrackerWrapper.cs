using Facebook.Unity;
using UnityEngine;

namespace Voodoo.Tiny.Sauce.Internal.Analytics
{
    public class FacebookTrackerWrapper : IAdTrackerWrapper
    {
        private const string TAG = "FacebookTrackerWrapper";
        private bool _consent;
        private bool _initializing;
        private bool _active;
        
        public void Init(bool idfaConsent)
        {
            SetConsent(idfaConsent);
        }

        public void SetConsent(bool allowed)
        {
            _consent = allowed;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (FB.IsInitialized)
            {
                ApplyConsent();
                return;
            }

            if (!_consent || _initializing)
                return;

            _initializing = true;
            try
            {
                FB.Init(InitCallback, OnHideUnity);
            }
            catch
            {
                _initializing = false;
                throw;
            }
#endif
        }

        private void InitCallback()
        {
            _initializing = false;
            if (FB.IsInitialized)
                ApplyConsent();
            else if (_consent)
                Debug.LogWarning("Failed to initialize the Facebook SDK.");
        }

        private void ApplyConsent()
        {
#if UNITY_IOS
            FB.Mobile.SetAdvertiserTrackingEnabled(_consent);
#endif
            FB.Mobile.SetAdvertiserIDCollectionEnabled(_consent);
            FB.Mobile.SetAutoLogAppEventsEnabled(_consent);
            if (_consent && !_active)
                FB.ActivateApp();
            _active = _consent;
        }

        private void OnHideUnity(bool isGameShown)
        {
            if (!isGameShown)
            {
                // Pause the game - we will need to hide
                Time.timeScale = 0;
            }
            else
            {
                // Resume the game - we're getting focus again
                Time.timeScale = 1;
            }
        }
    }
}
