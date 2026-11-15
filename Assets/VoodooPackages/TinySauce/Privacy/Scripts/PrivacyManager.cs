using System;
using System.Threading.Tasks;
using UnityEngine;
using Voodoo.Tiny.Sauce.Internal;

#if UNITY_IOS && !UNITY_EDITOR
using Voodoo.Sauce.Internal.IdfaAuthorization;
#endif

namespace Voodoo.Tiny.Sauce.Privacy
{
    public class PrivacyManager : MonoBehaviour
    {
        [SerializeField] private PrivacyScreenBehaviour privacyScreenPrefab;
        [SerializeField] private PrivacyPartnersScreenBehaviour privacyPartnersScreenPrefab;

        private const string LegacyFirstLaunchKey = "FirstStart";
        private const string ChoiceVersionKey = "PrivacyChoiceVersion";
        private const int ChoiceVersion = 1;
        private const string AdConsentKey = "AdConsent";
        private const string AnalyticsConsentKey = "AnalyticsConsent";

        private Task _openScreenTask;
        private bool _checkedConsents;
        private string _language = "GB";
#if UNITY_IOS && !UNITY_EDITOR
        private bool _requestingIdfaConsent;
#endif

        public static bool AdConsent { get; private set; }
        public static bool AnalyticsConsent { get; private set; }
        public static bool ConsentReady { get; private set; }
        public bool IdfaConsent { get; private set; }

        // Attribution first, analytics second. Reports the effective defaults while the first choice is pending.
        public static event Action<bool, bool> OnConsentGiven;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            AdConsent = false;
            AnalyticsConsent = false;
            ConsentReady = false;
            OnConsentGiven = null;
        }

        public Task CheckConsents()
        {
            if (_checkedConsents)
                return _openScreenTask ?? Task.CompletedTask;

            _checkedConsents = true;
            _language = GetSystemLanguage();

            int savedAdConsent = PlayerPrefs.GetInt(AdConsentKey, -1);
            int savedAnalyticsConsent = PlayerPrefs.GetInt(AnalyticsConsentKey, -1);
            ConsentReady = PlayerPrefs.GetInt(ChoiceVersionKey) == ChoiceVersion
                && (savedAdConsent == 0 || savedAdConsent == 1)
                && (savedAnalyticsConsent == 0 || savedAnalyticsConsent == 1);
            AdConsent = ConsentReady && savedAdConsent == 1;
            AnalyticsConsent = ConsentReady && savedAnalyticsConsent == 1;

            // Older builds wrote grants even without a user decision. Require a new choice before restoring either grant.
            if (!ConsentReady && (PlayerPrefs.HasKey(LegacyFirstLaunchKey) || savedAdConsent != -1 || savedAnalyticsConsent != -1))
            {
                PlayerPrefs.SetInt(AdConsentKey, 0);
                PlayerPrefs.SetInt(AnalyticsConsentKey, 0);
                PlayerPrefs.DeleteKey(LegacyFirstLaunchKey);
                PlayerPrefs.DeleteKey(ChoiceVersionKey);
                PlayerPrefs.Save();
            }

            PublishConsents();
            return ConsentReady ? Task.CompletedTask : WaitForFirstChoice();
        }

        private async Task WaitForFirstChoice()
        {
            try
            {
                await OpenPrivacyScreen();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }

            if (!ConsentReady)
            {
                // A missing or interrupted dialog must leave both grants off and let gameplay proceed.
                ConsentReady = true;
            }
        }

        public static async void OpenSettings()
        {
            var manager = TinySauceBehaviour.Instance?.privacyManager;
            if (manager == null)
                return;

            try
            {
                await manager.OpenPrivacyScreen();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        public static void OpenPrivacyPolicy()
        {
            string privacyUrl = GetPrivacyPolicyUrl(GetSystemLanguage());
            if (!string.IsNullOrEmpty(privacyUrl))
                Application.OpenURL(privacyUrl);
        }

        internal static string GetPrivacyPolicyUrl(string language)
        {
            string configuredUrl = TinySauceSettings.Load()?.privacyPolicyURL;
            if (!Uri.TryCreate(configuredUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                return null;

            return language == "KR" ? new Uri(uri, "ko/").AbsoluteUri : uri.AbsoluteUri;
        }

        public Task OpenPrivacyScreen(string currentCountryCode = "EN")
        {
            if (_openScreenTask == null || _openScreenTask.IsCompleted)
            {
                string language = string.IsNullOrEmpty(currentCountryCode) || currentCountryCode == "EN"
                    ? _language
                    : currentCountryCode;
                _openScreenTask = ShowChoices(language);
            }

            return _openScreenTask;
        }

        private async Task ShowChoices(string language)
        {
            if (privacyScreenPrefab == null)
            {
                Debug.LogError("The privacy choice prefab is missing.", this);
                return;
            }

            var screen = Instantiate(privacyScreenPrefab);
            try
            {
                DontDestroyOnLoad(screen.gameObject);
                screen.ConfirmWaitTask = new TaskCompletionSource<bool[]>();
                screen.Localize(language);
                screen.SetToggleStates(AdConsent, AnalyticsConsent);
                if (ConsentReady)
                    screen.SetToSettings();
            }
            catch
            {
                Destroy(screen.gameObject);
                throw;
            }

            var choice = await screen.ConfirmWaitTask.Task;
            if (choice == null)
                return;
            bool changed = !ConsentReady || AdConsent != choice[0] || AnalyticsConsent != choice[1];
            AdConsent = choice[0];
            AnalyticsConsent = choice[1];
            ConsentReady = true;
            PlayerPrefs.SetInt(AdConsentKey, AdConsent ? 1 : 0);
            PlayerPrefs.SetInt(AnalyticsConsentKey, AnalyticsConsent ? 1 : 0);
            PlayerPrefs.SetInt(ChoiceVersionKey, ChoiceVersion);
            PlayerPrefs.Save();

            if (changed)
                PublishConsents();
        }

        private void PublishConsents()
        {
#if UNITY_IOS && !UNITY_EDITOR
            var status = AdConsent ? NativeWrapper.GetAuthorizationStatus() : IdfaAuthorizationStatus.Denied;
            IdfaConsent = AdConsent && status == IdfaAuthorizationStatus.Authorized;
            NotifyListeners();
            if (AdConsent && status == IdfaAuthorizationStatus.NotDetermined && !_requestingIdfaConsent)
            {
                _requestingIdfaConsent = true;
                NativeWrapper.RequestAuthorization(result =>
                {
                    _requestingIdfaConsent = false;
                    IdfaConsent = AdConsent && result == IdfaAuthorizationStatus.Authorized;
                    NotifyListeners();
                });
            }
#else
            IdfaConsent = AdConsent;
            NotifyListeners();
#endif
        }

        private static void NotifyListeners()
        {
            var listeners = OnConsentGiven;
            if (listeners == null)
                return;

            foreach (Action<bool, bool> listener in listeners.GetInvocationList())
            {
                try
                {
                    listener(AdConsent, AnalyticsConsent);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        public void OpenPrivacyPartnersScreen()
        {
            if (privacyPartnersScreenPrefab == null)
                return;

            var screen = Instantiate(privacyPartnersScreenPrefab);
            screen.Localize(_language);
        }

        private static string GetSystemLanguage()
        {
            return Application.systemLanguage == SystemLanguage.Korean ? "KR" : "GB";
        }
    }
}
