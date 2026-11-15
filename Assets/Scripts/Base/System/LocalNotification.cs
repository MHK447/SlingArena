using System;
using UnityEngine;
using BanpoFri;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

#if UNITY_IOS
using Unity.Notifications.iOS;
#elif UNITY_ANDROID
using Unity.Notifications.Android;
using UnityEngine.Android;
#endif

public class LocalNotification
{
    private static readonly string ChannelId = "default_channel";

#if UNITY_ANDROID
    static AndroidNotificationChannel notificationChannel;
#endif

    public static void CallNotification(int pushID, double remainTime, string title, string context)
    {
#if UNITY_IOS
        var timeTrigger = new iOSNotificationTimeIntervalTrigger()
        {
            TimeInterval = new TimeSpan(0, 0, (int)remainTime),
            Repeats = false
        };
        var notification = new iOSNotification()
        {
            Title = title,
            Body = context,
            Trigger = timeTrigger,
            ForegroundPresentationOption = (PresentationOption.Alert | PresentationOption.Sound),
            CategoryIdentifier = "category_a",
            ThreadIdentifier = "thread1",
        };
        iOSNotificationCenter.ScheduleNotification(notification);
#elif UNITY_ANDROID
        DateTime toNotify = DateTime.Now.AddSeconds(remainTime);

        AndroidNotification notification = new AndroidNotification();
        notification.Title = title;
        notification.Text = context;
        notification.SmallIcon = "ic_launcher";
        notification.FireTime = toNotify;

        AndroidNotificationCenter.SendNotification(notification, ChannelId);
#endif
    }

    public static void AllClearNotification()
    {
#if UNITY_ANDROID
        AndroidNotificationCenter.CancelAllNotifications();
#elif UNITY_IOS
        iOSNotificationCenter.RemoveAllDeliveredNotifications();
        iOSNotificationCenter.RemoveAllScheduledNotifications();
#endif
    }

    public static void Init()
    {
#if UNITY_ANDROID
        notificationChannel = new AndroidNotificationChannel()
        {
            Id = ChannelId,
            Name = "Default",
            Importance = Importance.Default,
            Description = "Default notification channel"
        };
        AndroidNotificationCenter.RegisterNotificationChannel(notificationChannel);

        if (!Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS"))
        {
            Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS");
        }
#endif
    }
}
