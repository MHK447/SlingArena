using UnityEngine;
using BanpoFri;
using System.Collections.Generic;

// 적 소환 파트 — lasttowerdefense 의 EnemyUnitGroup(웨이브 스폰/풀링) 방식.
// 씬(InGameTycoon)의 EnemyUnitGroup 컴포넌트에 위임한다.
public partial class InGameBaseStage
{
    [SerializeField]
    private EnemyUnitGroup EnemyUnitGroup;
    public EnemyUnitGroup GetEnemyUnitGroup => EnemyUnitGroup;

    // 웨이브 스폰이 모두 완료되었는지 (스폰 코루틴 종료 여부).
    public bool IsWaveSpawnComplete { get { return EnemyUnitGroup == null || EnemyUnitGroup.IsWaveSpawnComplete; } }

    // 현재 스테이지/웨이브(WaveInfo·Hard_WaveInfo)의 적 웨이브 스폰 시작.
    public void SpawnWaveEnemies()
    {
        if (EnemyUnitGroup == null)
        {
            Debug.LogWarning("[InGameBaseStage] EnemyUnitGroup 이 씬에 연결되어 있지 않습니다.");
            return;
        }

        EnemyUnitGroup.StartWave();
    }

    // 살아있는 적 전부 제거 (스테이지 종료/로비 복귀 시).
    public void ClearEnemies()
    {
        if (EnemyUnitGroup == null)
            return;

        EnemyUnitGroup.ClearData();
    }

    public bool HasAliveEnemy()
    {
        return EnemyUnitGroup != null && EnemyUnitGroup.HasAliveEnemy();
    }

    public List<EnemyUnitBase> GetAliveEnemyList()
    {
        return EnemyUnitGroup != null ? EnemyUnitGroup.GetAliveEnemyList() : new List<EnemyUnitBase>();
    }
}
