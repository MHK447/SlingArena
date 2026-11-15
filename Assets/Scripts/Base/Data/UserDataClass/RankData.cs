using System;
using System.Collections.Generic;
using UniRx;
using Google.FlatBuffers;

public partial class UserDataSystem
{



}


public class RankData : IClientData
{
    public List<RankUserData> Userdata = new List<RankUserData>();

    public enum E_RankGraphType
    {
        None,
        EaseIn,
        EaseOut,
        EaseInOut,
        EaseOutIn,
        EaseLinear
    }

    public enum E_RankGraphPower
    {
        None,
        Linear,
        Quadratic,
        Cubic,
        Quartic,
        Quintic
    }
    public DateTime StartRankTime;
    public DateTime EndRankTime;
    public DateTime RefreshTime;
    public IReactiveProperty<int> RemainTime = new ReactiveProperty<int>(-1);
    public List<RankUserData> UserList = new List<RankUserData>();
}