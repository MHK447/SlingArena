using UnityEngine;
using BanpoFri;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class DiceShowComponent : MonoBehaviour
{
    private int DiceIdx = 0;

    public void Set(int diceidx)
    {
        DiceIdx = diceidx;
    }


}

