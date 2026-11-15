using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using Voodoo.Tiny.Sauce.Internal;

namespace Voodoo.Tiny.Sauce.Privacy
{
    public class PrivacyPrebuild : IPreprocessBuildWithReport
    {
        private const string TAG = "PrivacyPreBuild";
        public int callbackOrder => 1;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android && report.summary.platform != BuildTarget.iOS)
                return;

            var settings = TinySauceSettings.Load();
            if (settings == null || UsesTracking(settings))
                CheckAndUpdatePrivacySettingsOnBuild(settings);
        }

        internal static bool UsesTracking(TinySauceSettings settings)
        {
            return settings.doesYourGameDisplayAds || settings.UseVoodooAnalytics || settings.UseRemoteConfig ||
                !string.IsNullOrEmpty(settings.facebookAppId) ||
                !string.IsNullOrEmpty(settings.gameAnalyticsAndroidGameKey) ||
                !string.IsNullOrEmpty(settings.gameAnalyticsIosGameKey) ||
                !string.IsNullOrEmpty(settings.adjustAndroidToken) ||
                !string.IsNullOrEmpty(settings.adjustIOSToken);
        }

        public static void CheckAndUpdatePrivacySettingsOnBuild(TinySauceSettings sauceSettings)
        {
            if (sauceSettings == null || string.IsNullOrEmpty(sauceSettings.companyName.Trim()))
            {
                throw new BuildFailedException("Company Name is empty");
            }

            if (sauceSettings == null ||
                string.IsNullOrEmpty(sauceSettings.privacyPolicyURL.Trim()))
            {
                throw new BuildFailedException("Privacy Policy is empty");
            }

            if (sauceSettings == null ||
                string.IsNullOrEmpty(sauceSettings.developerContactEmail.Trim()))
            {
                throw new BuildFailedException("Developer Contact Email is empty");
            }

        }
        public static bool CheckAndUpdatePrivacySettings(TinySauceSettings sauceSettings)
        {
            if (sauceSettings == null || string.IsNullOrEmpty(sauceSettings.companyName.Trim()))
            {
                return false;
            }

            if (sauceSettings == null ||
                string.IsNullOrEmpty(sauceSettings.privacyPolicyURL.Trim()))
            {
                return false;
            }

            if (sauceSettings == null ||
                string.IsNullOrEmpty(sauceSettings.developerContactEmail.Trim()))
            {
                return false;
            }

            return true;
        }
        
        public static bool CheckPrivacyURLValidity(TinySauceSettings sauceSettings)
        {
            if (!(sauceSettings.privacyPolicyURL.StartsWith("http://") || sauceSettings.privacyPolicyURL.StartsWith("https://")))
            {
                return false;
            }

            return true;
        }
        
        public static bool CheckPrivacyEmailValidity(TinySauceSettings sauceSettings)
        {
            if (!sauceSettings.developerContactEmail.Contains("@"))
            {
                return false;
            }

            return true;
        }
    }
}
