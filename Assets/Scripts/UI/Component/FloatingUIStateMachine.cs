using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BanpoFri;

public class FloatingUIStateMachine : StateMachineBehaviour
{
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var ui = animator.gameObject.GetComponent<InGameFloatingUI>();
        if (ui)
        {
            if (stateInfo.IsName("Hide"))
                ProjectUtility.SetActiveCheck(ui.gameObject, false);
        }
    }
}
