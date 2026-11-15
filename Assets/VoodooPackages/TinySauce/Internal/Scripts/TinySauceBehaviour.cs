using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Voodoo.Sauce.Internal;
using Voodoo.Tiny.Sauce.Internal.ABTest;
using Voodoo.Tiny.Sauce.Internal.Analytics;
using Voodoo.Tiny.Sauce.Privacy;

namespace Voodoo.Tiny.Sauce.Internal
{
    internal class TinySauceBehaviour : MonoBehaviour
    {
        private const string TAG = "TinySauceBehaviour";
        public static TinySauceBehaviour Instance { get; private set; }
        public TinySauceSettings sauceSettings { get; private set; }
        private static IABTestManager _abTestManager;
        public static IABTestManager ABTestManager => _abTestManager;
        [HideInInspector] public PrivacyManager privacyManager;
        private readonly AdjustTrackerWrapper _adjust = new AdjustTrackerWrapper();
        private readonly FacebookTrackerWrapper _facebook = new FacebookTrackerWrapper();
        private bool _abTestStarted;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(this);
            
            VoodooLog.Initialize(VoodooLogLevel.WARNING);

            LoadSettings();
            InitTracking();

            if (transform != transform.root)
                throw new Exception("TinySauce prefab HAS to be at the ROOT level!");
        }

        private void InitABTest() //All initializations should be done like this. Would be useful for module/sdk addition/removal
        {
            if(GetAbTestingManager().Count == 0) return;
            _abTestManager = (IABTestManager) Activator.CreateInstance(GetAbTestingManager()[0]);
            _abTestManager.Init();
        }

        private static List<Type> GetAbTestingManager()
        {
            Type interfaceType = typeof(IABTestManager);
            List<Type> AbTest = GetTypes(interfaceType);
            return AbTest;
        }

        private void LoadSettings()
        {
            sauceSettings = TinySauceSettings.Load();
            if (sauceSettings == null)
                throw new Exception("Can't find TinySauce sauceSettings file. Please access the TinySauce Settings by using the top menu: TinySauce > TinySauce Settings > Edit Settings to automatically generate a new one.");
        }

        private async void InitTracking()
        {
            privacyManager = GetComponent<PrivacyManager>();
            if (privacyManager == null)
            {
                Debug.LogError("TinySauce requires a PrivacyManager before starting SDKs.", this);
                return;
            }

            PrivacyManager.OnConsentGiven += ApplyTrackingConsent;
            try
            {
                await privacyManager.CheckConsents();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
            }
        }

        private void ApplyTrackingConsent(bool attribution, bool analytics)
        {
            if (analytics && !_abTestStarted)
            {
                _abTestStarted = true;
                ApplyConsent(InitABTest);
            }

            ApplyConsent(() => AnalyticsManager.SetPrivacyConsent(sauceSettings, analytics));

            bool attributionAllowed = attribution && privacyManager.IdfaConsent;
            ApplyConsent(() => _adjust.SetConsent(attributionAllowed));
            ApplyConsent(() => _facebook.SetConsent(attributionAllowed));
        }

        private static void ApplyConsent(Action apply)
        {
            try
            {
                apply();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void OnDestroy()
        {
            PrivacyManager.OnConsentGiven -= ApplyTrackingConsent;
            if (Instance == this)
            {
                Instance = null;
                _abTestManager = null;
            }
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            // Brought forward after soft closing 
            if (!pauseStatus) {
                AnalyticsManager.OnApplicationResume();
            }
        }
        
        internal static void InvokeCoroutine(IEnumerator coroutine)
        {
            if (Instance == null) return;
            Instance.StartCoroutine(coroutine);
        }

        private static List<Type> GetTypes(Type toGetType)
        {
            List<Type> types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => toGetType.IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                .ToList();

            types.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));

            return types;
        }
    }
}
