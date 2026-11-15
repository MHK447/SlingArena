using System;
using System.Collections.Generic;
using UniRx;
using Google.FlatBuffers;

public partial class UserDataSystem
{



}

public class RankUserData
{
    public int Currentscore { get; set; } = 0;

    public int GraphType { get; set; } = 0;
    public int FinalScore { get; set; } = 0;
    public int CurScore { get; set; } = 0;
    public string Profile { get; set; } = "";
    public bool Me { get; set; } = false;
    public int GraphPower { get; set; } = 0;
    public string Nickname { get; set; } = "";
    public int Rank { get; set; } = 0;
}
