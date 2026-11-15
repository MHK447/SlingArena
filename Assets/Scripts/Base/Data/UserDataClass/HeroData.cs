using System;
using System.Collections.Generic;
using UniRx;
using Google.FlatBuffers;

public partial class UserDataSystem
{



}

public class HeroData
{
    public int Heroidx { get; set; } = 0;

    public IReactiveProperty<int> Level { get; set; } = new ReactiveProperty<int>(0);

}
