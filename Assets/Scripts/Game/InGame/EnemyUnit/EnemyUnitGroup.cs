using UnityEngine;
using BanpoFri;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine.AddressableAssets;

// lasttowerdefense 의 EnemyUnitGroup 이식.
// 주사위 계열 사망 보너스(AddSilverCoinDice/BombDice/PoisonDice — InGameLevelUpBuffSystem/AlimentSystem 기반)만 제거했고
// 웨이브 스폰(WaveInfo/Hard_WaveInfo), 풀링, 생존 조회 로직은 동일하다.
public class EnemyUnitGroup : MonoBehaviour
{
    [SerializeField]
    private List<GameObject> EnemyUnitRoots = new List<GameObject>();


    private List<EnemyUnitBase> ActiveEnemyUnitList = new List<EnemyUnitBase>();
    private List<EnemyUnitBase> DeadEnemyUnitList = new List<EnemyUnitBase>();

    private Coroutine currentWaveCoroutine = null;
    private int SpawnOrder = 0;

    private int CurrentWaveExpPerKill = 0;
    private int CurrentWaveExpRemainder = 0;
    private int CurrentWaveExpAssignedCount = 0;
    private int CurrentWaveTotalEnemyCount = 0;

    // 스폰 지점에서 화면 안쪽으로 걸어 들어오는 거리.
    // lasttowerdefense 는 -10 이었고, DMK 는 카메라 ortho 비율(15.63/9 ≈ 1.75)에 맞춰 스케일.
    private const float SpawnWalkInDistanceX = 17.5f;

    // 스폰 순서별 Y 슬롯 — 1번째 2.2, 2번째 0.5, 3번째 3.3, 4번째부터 다시 반복.
    private static readonly float[] SpawnYSlots = { 2.2f, 0.5f, 3.3f };

    // 코루틴 핸들 비교 대신 플래그 사용 — 적이 적은 웨이브는 StartWaveCoroutine 본문이 yield 없이
    // 같은 프레임에 전부 실행되는데, 그 경우 본문 끝의 currentWaveCoroutine = null 이 StartCoroutine 반환값
    // 대입보다 먼저 실행되어 핸들이 영영 남는 문제(스폰 완료 판정/재시작 불가)가 있었다.
    private bool waveSpawnRunning = false;

    public bool IsWaveSpawnComplete { get { return !waveSpawnRunning; } }

    // 이동 연출 중 미리 스폰할 때 사용 — 아직 팬(pan) 중인 카메라 대신 도착 시점 카메라 X 기준으로 정지 위치를 잡는다.
    // null 이면 기존처럼 현재 카메라 X 사용. 웨이브 스폰이 끝나면 자동 해제.
    public float? SpawnCamXOverride = null;

    public void HideEnemies()
    {
        foreach (var enemy in EnemyUnitRoots)
        {
            if (enemy != null)
                ProjectUtility.SetActiveCheck(enemy, false);
        }
    }

    public void ShowEnemies()
    {
        foreach (var enemy in EnemyUnitRoots)
        {
            if (enemy != null)
            {
                ProjectUtility.SetActiveCheck(enemy, true);

                var enemyTrans = enemy.transform;
                enemyTrans.localScale = Vector3.zero;
                enemyTrans.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
            }
        }
    }

    public void StopWave()
    {
        if (currentWaveCoroutine != null)
        {
            StopCoroutine(currentWaveCoroutine);
            currentWaveCoroutine = null;
        }
        waveSpawnRunning = false;
        SpawnCamXOverride = null;
    }

    public void StartWave()
    {
        if (waveSpawnRunning)
            return;

        SpawnOrder = 0;
        waveSpawnRunning = true;
        currentWaveCoroutine = StartCoroutine(StartWaveCoroutine());
    }

    public void ClearData()
    {
        StopWave();

        foreach (var unit in ActiveEnemyUnitList)
        {
            if (unit != null && unit.gameObject != null)
                Destroy(unit.gameObject);
        }
        foreach (var unit in DeadEnemyUnitList)
        {
            if (unit != null && unit.gameObject != null)
                Destroy(unit.gameObject);
        }

        ActiveEnemyUnitList.Clear();
        DeadEnemyUnitList.Clear();
        SpawnOrder = 0;
    }

    public EnemyUnitBase FindClosestAliveEnemy(Vector3 fromPosition)
    {
        EnemyUnitBase closest = null;
        float closestDist = float.MaxValue;

        foreach (var enemy in ActiveEnemyUnitList)
        {
            if (enemy == null || enemy.gameObject == null) continue;
            if (!enemy.gameObject.activeSelf) continue;
            if (enemy.IsDead) continue;

            float dist = Mathf.Abs(enemy.transform.position.x - fromPosition.x);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = enemy;
            }
        }

        return closest;
    }

    public bool HasAliveEnemy()
    {
        foreach (var enemy in ActiveEnemyUnitList)
        {
            if (enemy == null || enemy.gameObject == null) continue;
            if (!enemy.gameObject.activeSelf) continue;
            if (enemy.IsDead) continue;
            return true;
        }
        return false;
    }

    public List<EnemyUnitBase> GetAliveEnemyList()
    {
        var list = new List<EnemyUnitBase>();
        foreach (var enemy in ActiveEnemyUnitList)
        {
            if (enemy == null || enemy.gameObject == null) continue;
            if (!enemy.gameObject.activeSelf) continue;
            if (enemy.IsDead) continue;
            list.Add(enemy);
        }
        return list;
    }

    // 마지막으로 죽은 적의 위치 — 웨이브 클리어 보상 상자 등장 위치로 사용.
    // EnemyUnitBase.Dead() 에서 기록한다.
    public Vector3 LastDeadPosition { get; set; }

    public void OnEnemyDead(EnemyUnitBase enemy)
    {
        if (ActiveEnemyUnitList.Remove(enemy))
        {
            DeadEnemyUnitList.Add(enemy);
            // Dead 애니메이션을 끝까지 보여주기 위해 비활성화는 애니메이션 이벤트에서 처리한다.
        }
    }

    public void AddUnit(int enemyidx, int unitdmg, int unithp)
    {
        var enemyTable = Tables.Instance.GetTable<EnemyInfo>();
        if (enemyTable == null)
        {
            Debug.LogError("[EnemyUnitGroup] EnemyInfo 테이블이 Tables.asset 에 등록되어 있지 않습니다 — 적 스폰 불가.");
            return;
        }

        var td = enemyTable.GetData(enemyidx);
        if (td == null)
        {
            Debug.LogWarning($"[EnemyUnitGroup] EnemyInfo 에 enemyidx={enemyidx} 데이터가 없어 스폰을 건너뜁니다.");
            return;
        }

        // DeadUnits에서 같은 enemyidx를 가진 유닛 재활용
        var find = DeadEnemyUnitList.FirstOrDefault(x => x.EnemyIdx == enemyidx);

        EnemyUnitBase instance;

        if (find != null)
        {
            instance = find;
            DeadEnemyUnitList.Remove(find);
            ActiveEnemyUnitList.Add(instance);
        }
        else
        {
            var handle = Addressables.InstantiateAsync(td.prefab, transform);
            var result = handle.WaitForCompletion();
            if (result == null)
            {
                Debug.LogError($"[EnemyUnitGroup] 적 프리팹 어드레서블 로드 실패: '{td.prefab}' (enemyidx={enemyidx})");
                return;
            }

            instance = result.GetComponent<EnemyUnitBase>();
            if (instance == null)
            {
                Debug.LogError($"[EnemyUnitGroup] '{td.prefab}' 프리팹 루트에 EnemyUnitBase 컴포넌트가 없습니다 (enemyidx={enemyidx}).");
                Destroy(result);
                return;
            }
            ActiveEnemyUnitList.Add(instance);
        }



        // 좌표는 마커 대신 코드로 결정:
        // - 최종 정지 X: 카메라 기준 +3.5~5 — 750x1334(세로) 화면의 반폭이 ortho 10.5 x (750/1334) ≈ 5.9 라
        //   화면 오른쪽 가장자리 안쪽에 선다. 플레이어(카메라 기준 약 -3.6)와의 간격 ≈ 7~8.5.
        // - 등장(스폰): 정지 지점보다 SpawnWalkInDistanceX 만큼 오른쪽(화면 밖)에서 걸어 들어온다.
        // - Y: 스폰 순서대로 2.2 → 0.5 → 3.3 반복.
        var mainCam = GameRoot.Instance.InGameSystem.CurInGame?.GetMainCam;
        float camX = SpawnCamXOverride ?? (mainCam != null ? mainCam.transform.position.x : 14f);
        float standX = camX + Random.Range(3.5f, 5f);
        float spawnY = SpawnYSlots[SpawnOrder % SpawnYSlots.Length];
        var spawnPos = new Vector3(standX + SpawnWalkInDistanceX, spawnY, 0f);
        var targetPos = new Vector3(standX, spawnY, 0f);

        // 재활용된 인스턴스에 이전 DOMove 트윈이 남아 있으면 새 위치로 워프하기 전에 충돌하므로 먼저 정리.
        instance.transform.DOKill();
        instance.transform.position = spawnPos;



        instance.Set(enemyidx, unithp, unitdmg);
        CurrentWaveExpAssignedCount++;
        int expBonus = (CurrentWaveExpAssignedCount > CurrentWaveTotalEnemyCount - CurrentWaveExpRemainder) ? 1 : 0;
        instance.ExpReward = CurrentWaveExpPerKill + expBonus;
        ProjectUtility.SetActiveCheck(instance.gameObject, true);

        // Running 상태로 목표 위치까지 이동 후 Idle
        instance.SetState(EnemyUnitBase.EnemyState.Run);
        instance.transform.DOMove(targetPos, 1.2f).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            if (instance != null)
                instance.SetState(EnemyUnitBase.EnemyState.Idle);
        });

        SpawnOrder++;
    }

    // 같은 프레임에 여러 마리가 동시에 스폰돼서 이동 중 겹쳐 보이는 것을 방지하기 위한 stagger 딜레이.
    private const float SPAWN_STAGGER_SECONDS = 0.18f;

    private IEnumerator StartWaveCoroutine()
    {
        bool isHardStage = GameRoot.Instance.UserData.DifficultyTypeProperty.Value == DifficultyType.Hard;
        var stageidx = isHardStage ? GameRoot.Instance.UserData.Hardmodestageidx.Value : GameRoot.Instance.UserData.Stageidx.Value;
        var waveidx = GameRoot.Instance.UserData.Waveidx.Value;

        List<int> unit_idx = null;
        List<int> unit_dmg = null;
        List<int> unit_hp = null;
        List<int> unit_count = null;
        List<int> unit_appear_time = null;
        int waveExp = 0;

        if (isHardStage)
        {
            var hardTable = Tables.Instance.GetTable<Hard_WaveInfo>();
            if (hardTable == null)
            {
                Debug.LogError("[EnemyUnitGroup] Hard_WaveInfo 테이블이 Tables.asset 에 등록되어 있지 않습니다 — 적 스폰 불가.");
            }
            else
            {
                var hardTd = hardTable.GetData(new KeyValuePair<int, int>(stageidx, waveidx));
                if (hardTd != null && hardTd.unit_idx != null && hardTd.unit_idx.Count > 0)
                {
                    unit_idx = hardTd.unit_idx;
                    unit_dmg = hardTd.unit_dmg;
                    unit_hp = hardTd.unit_hp;
                    unit_count = hardTd.unit_count;
                    unit_appear_time = hardTd.unit_appear_time;
                }
            }
        }
        else
        {
            var waveTable = Tables.Instance.GetTable<WaveInfo>();
            if (waveTable == null)
            {
                Debug.LogError("[EnemyUnitGroup] WaveInfo 테이블이 Tables.asset 에 등록되어 있지 않습니다 — 적 스폰 불가.");
            }
            else
            {
                var td = waveTable.GetData(new KeyValuePair<int, int>(stageidx, waveidx));
                if (td != null && td.enemy_unit_idx != null && td.enemy_unit_idx.Count > 0)
                {
                    // 새 WaveInfo 스키마: unit_idx/unit_dmg/unit_hp -> enemy_unit_idx/enemy_atk/enemy_hp.
                    // wave_exp 는 삭제됨(보상은 select_reward 계열로 이관) — 킬 경험치는 0 으로 둔다.
                    unit_idx = td.enemy_unit_idx;
                    unit_dmg = td.enemy_atk;
                    unit_hp = td.enemy_hp;
                }
            }
        }

        if (unit_idx == null)
            Debug.LogWarning($"[EnemyUnitGroup] 웨이브 데이터 없음 (hard={isHardStage}, stage={stageidx}, wave={waveidx}) — 이번 웨이브는 적이 스폰되지 않습니다.");

        // 총 적 수 계산: 일반 모드는 unit_idx.Count, 하드 모드는 unit_count 합산.
        int totalEnemyCount = 0;
        if (unit_idx != null)
        {
            if (unit_count != null && unit_count.Count > 0)
            {
                for (int i = 0; i < unit_idx.Count; i++)
                    totalEnemyCount += (i < unit_count.Count) ? unit_count[i] : 1;
            }
            else
            {
                totalEnemyCount = unit_idx.Count;
            }
        }

        CurrentWaveExpPerKill = (totalEnemyCount > 0 && waveExp > 0) ? (waveExp / totalEnemyCount) : 0;
        CurrentWaveExpRemainder = (totalEnemyCount > 0 && waveExp > 0) ? (waveExp % totalEnemyCount) : 0;
        CurrentWaveExpAssignedCount = 0;
        CurrentWaveTotalEnemyCount = totalEnemyCount;

        if (unit_idx != null)
        {
            for (int i = 0; i < unit_idx.Count; i++)
            {
                // 첫 유닛 타입 스폰 전 대기 (하드모드 전용)
                if (i == 0 && unit_appear_time != null && i < unit_appear_time.Count)
                {
                    float waitTime = unit_appear_time[i] / 100f;
                    yield return new WaitForSeconds(waitTime);

                    if (!GameRoot.Instance.UserData.InGamePlayerData.IsGameStartProperty.Value)
                    {
                        currentWaveCoroutine = null;
                        waveSpawnRunning = false;
                        yield break;
                    }
                }

                // 일반 모드: unit_count 없으면 1마리씩, 하드모드: unit_count 적용
                int spawnCount = (unit_count != null && i < unit_count.Count) ? unit_count[i] : 1;

                for (int j = 0; j < spawnCount; j++)
                {
                    if (!GameRoot.Instance.UserData.InGamePlayerData.IsGameStartProperty.Value)
                    {
                        currentWaveCoroutine = null;
                        waveSpawnRunning = false;
                        yield break;
                    }

                    int unitDmg = (unit_dmg != null && unit_dmg.Count > 0) ? (i < unit_dmg.Count ? unit_dmg[i] : unit_dmg.Last()) : 1;
                    int unitHp = (unit_hp != null && unit_hp.Count > 0) ? (i < unit_hp.Count ? unit_hp[i] : unit_hp.Last()) : 1;

                    AddUnit(unit_idx[i], unitDmg, unitHp);

                    // 같은 type 내 마지막 마리가 아니면 stagger.
                    if (j < spawnCount - 1)
                        yield return new WaitForSeconds(SPAWN_STAGGER_SECONDS);
                }

                // 유닛 타입 간 대기. 하드모드는 unit_appear_time 사용, 일반 모드도 동시 스폰 방지를 위해 기본 stagger.
                bool isLastUnitType = (i == unit_idx.Count - 1);
                if (!isLastUnitType)
                {
                    float waitTime = (unit_appear_time != null && i < unit_appear_time.Count)
                        ? unit_appear_time[i] / 100f
                        : SPAWN_STAGGER_SECONDS;
                    yield return new WaitForSeconds(waitTime);

                    if (!GameRoot.Instance.UserData.InGamePlayerData.IsGameStartProperty.Value)
                    {
                        currentWaveCoroutine = null;
                        waveSpawnRunning = false;
                        yield break;
                    }
                }
            }
        }

        currentWaveCoroutine = null;
        waveSpawnRunning = false;
        SpawnCamXOverride = null;
    }
}
