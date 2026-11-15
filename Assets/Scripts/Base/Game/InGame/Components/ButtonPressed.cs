using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonPressed : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [SerializeField]
    private string NormalTrigger = "Normal";
    [SerializeField]
    private string PressedTrigger = "Pressed";
    [SerializeField]
    private string DisabledTrigger = "Disabled";

    [SerializeField]
    private bool IsCountFast = false;

    [SerializeField]
    private float SuperFastDelayTime = 1.5f;

    private float FastDelTime = 0f;

    private bool StartSuperFast = false;

    private bool interactable = true;
    public bool Interactable
    {
        get
        {
            return interactable;
        }
        set
        {
            if (interactable != value)
            {
                if (value)
                {
                    if (animator != null)
                        animator.Play(NormalTrigger, 0, 0f);
                }
                else
                {
                    pressedCnt = 0;
                    disableDeltaTime = 0f;
                    isDisableAniWait = true;
                }
            }
            interactable = value;
            
            //RefreshState();
        }
    }
    public float click_interval = 0.5f;
    public float click_interval_fast = 0.03f;
    public int fastPressCnt = 10;
    private bool pressed = false;
    private float deltaTime = 0f;
    private int pressedCnt = 0;
    public System.Action OnPressed = null;
    private Animator animator;

    private int MaxSpeed = 30;

    private int CurSpeedCnt = 0;
    public float DisableInterval = 0f;
    private float disableDeltaTime = 0f;
    private bool isDisableAniWait = false;
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        RefreshState();
    }

    void OnEnable()
    {
        RefreshState();
    }

    void RefreshState()
    {
        if (interactable)
        {
            if (animator != null)
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (!stateInfo.IsName(NormalTrigger))
                {
                    animator.Play(NormalTrigger, 0, 0f);
                }
            }
        }
        else
        {
            pressedCnt = 0;

            if (animator != null)
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (!stateInfo.IsName(DisabledTrigger))
                {
                    animator.Play(DisabledTrigger, 0, 0f);
                }
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!interactable)
            return;

        pressed = true;
        FastDelTime = 0f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!interactable)
            return;

        pressed = false;
        if (pressedCnt < 1)
        {
            if (animator != null)
                animator.Play(PressedTrigger, 0, 0f);
            OnPressed?.Invoke();
            SoundPlayer.Instance.PlaySound("btn");
        }
        pressedCnt = 0;
        deltaTime = 0f;
        FastDelTime = 0f;
        StartSuperFast = false;
        CurSpeedCnt = 0;

    }


    private void OnDisable()
    {
        pressed = false;
        pressedCnt = 0;
        deltaTime = 0f;
        StartSuperFast = false;
        FastDelTime = 0f;
    }

    public void OnDrag(PointerEventData eventData)
    {
    }

    public void OnCancel()
    {
        pressed = false;
        pressedCnt = 0;
        deltaTime = 0;
    }

    private void Update()
    {
        if(isDisableAniWait)
        {
            disableDeltaTime += Time.deltaTime;
            if(disableDeltaTime >= DisableInterval)
            {
                isDisableAniWait = false;
                RefreshState();
            }
        }

        if (pressed)
        {
            if(IsCountFast && !StartSuperFast)
            {
                FastDelTime += Time.deltaTime;
                if(FastDelTime >= SuperFastDelayTime)
                {
                    StartSuperFast = true;
                    FastDelTime = 0f;
                }
            }

            if (!interactable)
            {
                pressed = false;
                return;
            }

            if ((pressedCnt < fastPressCnt ? click_interval : click_interval_fast) < deltaTime)
            {
                if (animator != null)
                    animator.Play(PressedTrigger, 0, 0f);
                SoundPlayer.Instance.PlaySound("btn");
                if (StartSuperFast)
                {   
                    if(CurSpeedCnt < MaxSpeed)
                    {
                        ++CurSpeedCnt;
                    }

                    for(int i = 0 ; i < MaxSpeed; ++i)
                    {
                        OnPressed?.Invoke();
                    }
                }
                else
                {
                    OnPressed?.Invoke();
                }
                ++pressedCnt;
                deltaTime = 0f;
            }
            deltaTime += Time.deltaTime;
        }
    }
}
