#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using Firebase.Analytics;
using UnityEngine;
using Voodoo.Tiny.Sauce.Privacy;

internal static class FirebaseConsentBridge
{
    private const string CrashReportsAllowedKey = "FirebaseCrashReportsAllowedV1";
    private static bool hasApplied;
    private static bool lastAnalytics;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Subscribe()
    {
        PrivacyManager.OnConsentGiven += Apply;
    }

    private static void Apply(bool attribution, bool analytics)
    {
        bool isFirstChoiceForProcess = !hasApplied;
        if (!isFirstChoiceForProcess && lastAnalytics == analytics)
            return;

        hasApplied = true;
        lastAnalytics = analytics;
        try
        {
            if (!analytics)
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(false);

            FirebaseAnalytics.SetConsent(new Dictionary<ConsentType, ConsentStatus>
            {
                { ConsentType.AnalyticsStorage, analytics ? ConsentStatus.Granted : ConsentStatus.Denied },
                { ConsentType.AdStorage, ConsentStatus.Denied },
                { ConsentType.AdUserData, ConsentStatus.Denied },
                { ConsentType.AdPersonalization, ConsentStatus.Denied }
            });

            if (analytics)
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
            else
                FirebaseAnalytics.ResetAnalyticsData();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Could not update Firebase Analytics consent ({exception.GetType().Name}).");
        }

        ApplyCrashReporting(isFirstChoiceForProcess, analytics);
    }

    private static void ApplyCrashReporting(bool isFirstChoiceForProcess, bool analytics)
    {
        // Crashlytics cannot turn off automatic uploads until the next process start.
        // Keep automatic uploads off and only send queued reports across opted-in sessions.
        bool maySend = isFirstChoiceForProcess && analytics && PlayerPrefs.GetInt(CrashReportsAllowedKey) == 1;
        if (isFirstChoiceForProcess || !analytics)
        {
            PlayerPrefs.SetInt(CrashReportsAllowedKey, analytics ? 1 : 0);
            PlayerPrefs.Save();
        }

        if (!isFirstChoiceForProcess && analytics)
            return;

        try
        {
            using var crashlyticsClass = new AndroidJavaClass("com.google.firebase.crashlytics.FirebaseCrashlytics");
            using var crashlytics = crashlyticsClass.CallStatic<AndroidJavaObject>("getInstance");
            crashlytics.Call(maySend ? "sendUnsentReports" : "deleteUnsentReports");
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Could not update Firebase crash reporting ({exception.GetType().Name}).");
        }
    }
}
#endif
