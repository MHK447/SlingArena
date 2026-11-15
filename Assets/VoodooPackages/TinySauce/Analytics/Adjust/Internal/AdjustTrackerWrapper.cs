using UnityEngine;
using com.adjust.sdk;

namespace Voodoo.Tiny.Sauce.Internal.Analytics
{
    public class AdjustTrackerWrapper : IAdTrackerWrapper
    {
        private const string TAG = "AdjustTrackerWrapper";
        private bool _started;

        public void SetConsent(bool allowed)
        {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            if (_started)
            {
                Adjust.setEnabled(allowed);
            }
            else if (allowed)
            {
                Init(true);
            }
#endif
        }

        public void Init(bool idfaConsent)
        {
            if (!idfaConsent || _started)
                return;

#if UNITY_IOS
            AdjustConfig adjustConfig = new AdjustConfig(TinySauceBehaviour.Instance.sauceSettings.adjustIOSToken, AdjustEnvironment.Production);
#elif UNITY_ANDROID
            AdjustConfig adjustConfig = new AdjustConfig(TinySauceBehaviour.Instance.sauceSettings.adjustAndroidToken, AdjustEnvironment.Production);
#else
            Debug.LogWarning("Please make sur to select iOS or Android platform on your build settings");
            AdjustConfig adjustConfig = new AdjustConfig("", AdjustEnvironment.Sandbox);
#endif
            Adjust.start(adjustConfig);
            Adjust.setEnabled(true);
            _started = true;
        }
    }
}
