using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Analytics;


public enum IngameEventType
{
    None,
}

public sealed class TpParameter
{
    private string _key;
    private long _value;
    private double _dvalue;
    private string _strvalue;

    public TpParameter(string key, long value)
    {
        _key = key;
        _value = value;
        _dvalue = -1d;
        _strvalue = string.Empty;
    }

    public TpParameter(string key, string strvalue)
    {
        _key = key;
        _value = -1;
        _dvalue = -1d;
        _strvalue = strvalue;
    }

    public TpParameter(string key, double dvalue)
    {
        _key = key;
        _value = -1;
        _dvalue = dvalue;
        _strvalue = string.Empty;
    }

    public Parameter ConvertToFirebaseParameter()
    {
        if (_dvalue > -1d)
            return new Parameter(_key, _dvalue);
        else if (_value > -1)
            return new Parameter(_key, _value);
        else
            return new Parameter(_key, _strvalue);
    }

    public KeyValuePair<string, string> ConvertToKeyValuePair()
    {
        if (_dvalue > -1d)
            return new KeyValuePair<string, string>(_key, _dvalue.ToString());
        else if (_value > -1)
            return new KeyValuePair<string, string>(_key, _value.ToString());
        else
            return new KeyValuePair<string, string>(_key, _strvalue);
    }
}

public class TpAnalyticsProp
{
    public enum Analytics
    {
        Firebase,
    }


    public enum LogCostCashType
    {
        PurchaseChar = 0,
        CharRatioUpgrade = 1,
        StoneBuy = 2,
        CardBuy = 3,

        BuyTowerOrder = 4,

        ArtifactPopupPurchaseChar = 5,

        BuyDailyShop = 6,
        PassNextGem = 7,

        None = 9999,
    }


    private List<IAnalytics> analyticsList = new List<IAnalytics>() { new TpFirebaseProp() };
    public void AllEvent(IngameEventType eventType, string eventName, params object[] args)
    {
        foreach (var analytics in analyticsList)
        {
            analytics.Event(eventType, eventName, args);
        }
    }

    public List<TpParameter> GetBaseParam(bool isEmptyParam = false)
    {
        var paramList = new List<TpParameter>();
        if (!isEmptyParam)
        {
            paramList.Add(new TpParameter("stage", GameRoot.Instance.UserData.Stageidx.Value));

            var adWatchCount = GameRoot.Instance.UserData.GetRecordCount(Config.RecordCountKeys.AdWatchCount) + 1;
            paramList.Add(new TpParameter("ads_count", adWatchCount)); // 누적 광고 시청 횟수
        }

        return paramList;
    }

    public void TargetEvent(Analytics analytics, IngameEventType eventType, string eventName, params object[] args)
    {
        analyticsList[(int)analytics].Event(eventType, eventName, args);
    }

    public void InAppPurchaseEvent(params object[] args)
    {
        foreach (var analytics in analyticsList)
        {
            analytics.InAppPurchaseEvent(args);
        }
    }

}

public class TpFirebaseProp : IAnalytics
{
    public void Event(IngameEventType eventType, string eventName, params object[] args)
    {
#if TREEPLLA_LOG && !TREEPLLA_CHEAT_LOG
        return;
#endif

        if (!PluginSystem.IsFirebaseAvailable) return;

        if (args.Length > 0)
        {
            List<TpParameter> tpParams = args[0] as List<TpParameter>;
            List<Parameter> parameters = new List<Parameter>();

            if (tpParams != null)
            {
                foreach (var pm in tpParams)
                {
                    parameters.Add(pm.ConvertToFirebaseParameter());
                }
            }

#if UNITY_EDITOR
            Debug.Log("fb log ===" + eventName);
#endif
            FirebaseAnalytics.LogEvent(eventName, parameters.ToArray());
        }
        else
        {
            FirebaseAnalytics.LogEvent(eventName);
        }
    }

    public void InAppPurchaseEvent(params object[] args)
    {

    }
}

interface IAnalytics
{
    void Event(IngameEventType eventType, string eventName, params object[] args);
    void InAppPurchaseEvent(params object[] args);
}
