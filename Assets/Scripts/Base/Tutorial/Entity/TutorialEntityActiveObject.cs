using UnityEngine;
using BanpoFri;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class TutorialEntityActiveObject : TutorialEntity
{   
    [System.Serializable]
    public enum ActiveType
    {
        AdAddDiceComponent,
    }


    [SerializeField]
    private ActiveType activeType;

    public override void StartEntity()
    {
        base.StartEntity(); 

        switch(activeType)
        {
            case ActiveType.AdAddDiceComponent:
                {
                }
                break;
        }

        Done();
    }


}

