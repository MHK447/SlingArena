using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using BanpoFri;

public class GameSpeedSystem
{
    public IReactiveProperty<bool> IsGameSpeed = new ReactiveProperty<bool>(false);
    public IReactiveProperty<float> CurGameSpeedValue = new ReactiveProperty<float>(1);
    public IReactiveProperty<float> CurGameSpeedValue2 = new ReactiveProperty<float>(1);
    public IReactiveProperty<float> InitGameSpeedValue = new ReactiveProperty<float>(1);
    public ReactiveProperty<bool> IsGameSpeedPackageValue = new ReactiveProperty<bool>();

    CompositeDisposable disposables = new CompositeDisposable();

    public float[] GameSpeedCountList = { 1, 1.5f };

    public int AdAddSpeedTime = 600;

    /// <summary>
    /// 인게임(전투 중)에서만 선택한 배속 적용, 그 외(메인/로비)에서는 무조건 timeScale 1.
    /// </summary>
    void UpdateTimeScale()
    {
        bool inBattle = GameRoot.Instance?.UserData?.InGamePlayerData?.IsGameStartProperty?.Value ?? false;
        Time.timeScale = inBattle ? (CurGameSpeedValue.Value * CurGameSpeedValue2.Value) : 1f;
    }

    public void Create()
    {
        CheckGameSpeedPackage();
        InitGameSpeedValue.Value = 1f;
        CurGameSpeedValue2.Value = 1f; // Create 재호출 시 누적 배속 방지

        disposables.Clear();

        CurGameSpeedValue.Subscribe(_ => UpdateTimeScale()).AddTo(disposables);
        CurGameSpeedValue2.Subscribe(_ => UpdateTimeScale()).AddTo(disposables);
        GameRoot.Instance.UserData.InGamePlayerData.IsGameStartProperty
            .Subscribe(_ => UpdateTimeScale())
            .AddTo(disposables);

        IsGameSpeed.Value = GameRoot.Instance.GameSpeedSystem.IsGameSpeedPackageValue.Value || GameRoot.Instance.UserData.ADSpeedUpTimeProperty.Value > 0;

        // 광고 스피드업 남은 시간 1초마다 감소 (게임 중일 때만, TimeText 카운트다운용) - unscaled time으로 timeScale 영향 없이 1배 속도 유지
        Observable.Interval(TimeSpan.FromSeconds(1), Scheduler.MainThreadIgnoreTimeScale)
            .ObserveOnMainThread()
            .Where(_ => GameRoot.Instance?.UserData != null
                && GameRoot.Instance.UserData.ADSpeedUpTimeProperty.Value > 0
                && !IsGameSpeedPackageValue.Value
                && GameRoot.Instance.UserData.InGamePlayerData.IsGameStartProperty.Value) // 게임 중(전투 시작 후)일 때만 감소
            .Subscribe(_ =>
            {
                var userData = GameRoot.Instance.UserData;
                userData.ADSpeedUpTimeProperty.Value = Mathf.Max(0, userData.ADSpeedUpTimeProperty.Value - 1);

                // 광고 스피드업 시간이 끝나면 1배속으로 복귀
                if (userData.ADSpeedUpTimeProperty.Value <= 0)
                {
                    userData.GameSpeedCount = 0;
                    InitGameSpeedValue.Value = 1f;
                    CurGameSpeedValue.Value = 1f;
                    CurGameSpeedValue2.Value = 1f;
                    IsGameSpeed.Value = false;
                }
            })
            .AddTo(disposables);
    }


    public void Dispose()
    {
        disposables.Clear();
    }

    public void ResetGameSpeed()
    {
        CurGameSpeedValue.Value = InitGameSpeedValue.Value;
        CurGameSpeedValue2.Value = 1f; // 1로 고정해 곱해져서 속도가 누적되는 것 방지 (실제 배속은 CurGameSpeedValue만 사용)
    }

    /// <summary>
    /// 메인 복귀 시 호출. 현재만 1배로 두고, 사용자 선택(GameSpeedCount)은 유지해서 다음 게임 시작 시 이전 배속(1.5 등)이 다시 적용되도록 함.
    /// InitGameSpeedValue도 1로 두어, 이후 StageClear()의 ResetGameSpeed()가 CurGameSpeedValue를 1.5로 되돌리지 않도록 함.
    /// </summary>
    public void ResetGameSpeedOnBattleEnd()
    {
        InitGameSpeedValue.Value = 1f;
        CurGameSpeedValue.Value = 1f;
        CurGameSpeedValue2.Value = 1f;
    }


    public void CheckGameSpeedPackage()
    {
        IsGameSpeedPackageValue.Value = GameRoot.Instance.UserData.BuyInappIds.Contains(InAppPurchaseManager.SpeedUp);

        if (IsGameSpeedPackageValue.Value)
        {
            GameRoot.Instance.GameSpeedSystem.IsGameSpeed.Value = true;
        }
    }


    public void ClickGameSpeed()
    {
        GameRoot.Instance.UserData.GameSpeedCount += 1;

        if (GameSpeedCountList.Length <= GameRoot.Instance.UserData.GameSpeedCount)
        {
            GameRoot.Instance.UserData.GameSpeedCount = 0;
        }
        CurGameSpeedValue.Value = InitGameSpeedValue.Value = GameSpeedCountList[GameRoot.Instance.UserData.GameSpeedCount];
    }

    public void GameSpeedActiveCheck(bool isbattlestart)
    {
        if (GameRoot.Instance.UserData.GameSpeedCount <= 0 && !IsGameSpeedPackageValue.Value) 
        {
            GameRoot.Instance.UserData.GameSpeedCount = 0;
        }

        GameRoot.Instance.GameSpeedSystem.CurGameSpeedValue.Value = InitGameSpeedValue.Value = isbattlestart ? GameSpeedCountList[GameRoot.Instance.UserData.GameSpeedCount] : 1f;
    }

    public void StopGameSpeed(bool isStop, bool isSecond = false)
    {
        if (isStop)
        {
            Time.timeScale = 0f;
        }
        else
        {
            UpdateTimeScale(); // 인게임 여부에 따라 1 또는 선택 배속 적용
        }
    }


    public void AddAdSpeedTime()
    {
        GameRoot.Instance.UserData.ADSpeedUpTimeProperty.Value += AdAddSpeedTime;
        GameRoot.Instance.UserData.Save(true); // 광고 스피드업 시간 즉시 저장
        IsGameSpeed.Value = true;
    }
}
