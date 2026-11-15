using UnityEngine;
using BanpoFri;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using DG.Tweening;
using UnityEngine.AddressableAssets;

// lasttowerdefense 인게임 진행 이식 버전.
// 주사위(Dice)/서브카메라 관련 로직은 전부 제외 — 카메라는 MainCam 하나만 사용한다.
// 광산(Mine)/슬라임 시스템은 제거되었다.
public partial class InGameBaseStage : MonoBehaviour
{
    // 상단 배틀맵(무한 스크롤 타일 + 맵 이동 컨트롤러 + ChapterMap_Base).
    [SerializeField]
    private TopBattleMap TopBattleMap;
    public TopBattleMap GetTopBattleMap => TopBattleMap;

    // 플레이어 유닛 스폰 위치.
    public Transform PlayerUnitRoot;

    private PlayerUnitBase PlayerUnit;
    private bool isPlayerUnitLoaded = false;

    // 맵 스크롤(InfiniteMapTileScroller/MoveMapController)이 추적하는 대표 플레이어 유닛.
    public PlayerUnitBase GetPlayerUnit => PlayerUnit;

    // 스테이지 전환 시 카메라/맵이 진행 방향(+X)으로 이동하는 거리.
    // (화면 폭의 ~80% — lasttowerdefense 의 8f/ortho9 비율을 DMK 카메라(ortho 15.6)에 맞게 스케일)
    private const float StageMoveDistance = 14f;

    // 스테이지/웨이브 전환 시 카메라 팬에 걸리는 시간(초). 클수록 카메라가 천천히 움직인다.
    private const float StageMoveDuration = 2.5f;

    // 스테이지 경과 시간(초). 전투 시작(StartWave) 시 0부터 누적.
    private float stageElapsedTime;
    private bool stageTimerRunning;

    // 스테이지 전환(이동 연출) 진행 중 중복 진입 방지.
    private bool stageTransitioning;

    // 현재 활성 WaveAttackSequence 코루틴 핸들 — 재진입 시 좀비 코루틴 중단용 (lasttowerdefense 동일).
    private Coroutine waveAttackCoroutine;

    // HeroInfo 스탯 테이블이 아직 미이식이라 전투 사이클이 돌 수 있도록 쓰는 임시 기본 스탯.
    private const int PlayerDefaultHp = 100;
    private const int PlayerDefaultDamage = 10;

    // 외부에서 읽는 스테이지 경과 시간(초 단위 정수).
    public int StageStartTime => Mathf.FloorToInt(stageElapsedTime);

    private void Update()
    {
        if (stageTimerRunning)
            stageElapsedTime += Time.deltaTime;
    }


    public void Init()
    {
        // 메인 복귀 시 꺼둔 인게임 플로팅 UI 컨테이너를 다시 켠다(개별 노출 여부는 각 컴포넌트 Init 이 결정).
        GameRoot.Instance.UISystem.SetFloatingUIActiveAll(true);

        // 상단 배틀맵 초기화 — 게임 시작 시점부터 ChapterMap_Base(씬 인스턴스) 위에
        // 현재 스테이지의 StageMap_{deco_idx} 어드레서블을 로드해 보여준다.
        if (TopBattleMap != null)
            TopBattleMap.Init();

        // 플레이어 유닛 소환 (lasttowerdefense InitStage 방식 — PlayerUnit_{장착영웅idx} 어드레서블).
        InitStage();

        // DMK 에는 아직 로비(PageLobbyBattle) 프리팹/전투 시작 버튼이 없다.
        // lasttowerdefense 의 "전투 시작" 버튼 역할을 대신해 게임 시작 시 곧바로
        // 전투 사이클(이동 → 적 조우 → 전투 → 다음 웨이브)로 진입한다.
        // (로비 UI 가 이식되면 이 자동 진입을 제거하고 버튼에서 StartWave 를 호출할 것)
        if (!GameRoot.Instance.UserData.InGamePlayerData.IsGameStartProperty.Value)
            StartWave();
    }

    // 플레이어 유닛을 스폰 위치에 (재)생성한다.
    public void InitStage()
    {
        var playeridx = GameRoot.Instance.UserData.Herogroudata.Equipplayeridx;

        if (PlayerUnit != null)
        {
            Destroy(PlayerUnit.gameObject);
            PlayerUnit = null;
        }

        isPlayerUnitLoaded = false;

        Addressables.InstantiateAsync($"PlayerUnit_{playeridx}").Completed += (handle) =>
        {
            if (handle.Result == null)
            {
                Debug.LogError($"[InGameBaseStage] PlayerUnit_{playeridx} 어드레서블 로드 실패");
                return;
            }

            if (PlayerUnitRoot != null)
                handle.Result.transform.position = PlayerUnitRoot.transform.position;

            var unit = handle.Result.GetComponent<PlayerUnitBase>();
            if (unit != null)
            {
                PlayerUnit = unit;
                PlayerUnit.Set(playeridx);
                TopBattleMap?.GetMapController?.SetPlayerUnit(PlayerUnit);
            }
            isPlayerUnitLoaded = true;
        };
    }


    // 전투 시작 — lasttowerdefense 의 StartBattle/BattleIntroSequence 흐름(주사위/적 스폰 제외).
    public void StartWave()
    {
        // 전투(재)진입 시 정리 — 로비 복귀(ReturnMainScreen) 때 껐던 플로팅 UI 를 다시 켠다.
        GameRoot.Instance.UISystem.SetFloatingUIActiveAll(true);

        // 전투 시작 시 메인 BGM 끄기. (메인 로비 복귀 시 ReturnMainScreen 에서 다시 켠다)
        SoundPlayer.Instance.StopBGM();

        // 인게임 실버코인 초기화.
        GameRoot.Instance.UserData.Ingamesilvercoin.Value = 0;

        // 전투 시작 플래그.
        GameRoot.Instance.UserData.InGamePlayerData.IsGameStartProperty.Value = true;

        GameRoot.Instance.UserData.Waveidx.Value = 1;

        // 플레이어 전투 스탯 초기화 — HeroInfo 스탯 테이블 미이식이라 임시 기본값 사용.
        // (StageClear() 가 Damage 를 0 으로 리셋하므로 매 전투 진입 시 다시 채워야 한다.)
        GameRoot.Instance.UserData.InGamePlayerData.Damage = PlayerDefaultDamage;
        GameRoot.Instance.UserData.InGamePlayerData.SetPlayerHp(PlayerDefaultHp);
        GameRoot.Instance.UserData.InGamePlayerData.KillCountProperty.Value = 0;

        // 이전 판에서 사망한 채 로비로 복귀했을 수 있으므로 상태 복구.
        if (PlayerUnit != null && PlayerUnit.CurState == PlayerUnitBase.PlayerUnitState.Dead)
            PlayerUnit.SetState(PlayerUnitBase.PlayerUnitState.Idle);

        // 머리 위 HP 바 갱신 (사망으로 숨겨졌던 바도 다시 표시).
        if (PlayerUnit != null)
            PlayerUnit.SetHp(PlayerDefaultHp, true);

        // 전투 돌입 — 스테이지 경과 시간 측정 시작(0부터).
        stageElapsedTime = 0f;
        stageTimerRunning = true;

        // 전투 돌입 = 클리어 시도 1회.
        GameRoot.Instance.UserData.AddRecordCount(Config.RecordCountKeys.TryStageClear, 1,
            GameRoot.Instance.UserData.Stageidx.Value);

        // 배틀 인트로 — 맵 스크롤 + 캐릭터 달리기 + 바람 이펙트 + MainCam 팬.
        StartCoroutine(BattleIntroSequence());
    }

    // lasttowerdefense 의 BattleIntroSequence 이식 — 주사위 숨김/등장, 적 등장 단계는 제외.
    private IEnumerator BattleIntroSequence()
    {
        var mapController = TopBattleMap != null ? TopBattleMap.GetMapController : null;

        // 1) 맵 + 플레이어 정지 (인트로 시작 시)
        if (mapController != null)
            mapController.Stop();

        // 2) PlayerUnit 로딩 완료 대기
        while (!isPlayerUnitLoaded)
            yield return null;

        // 3) 잠시 대기 후 이동 시작
        yield return new WaitForSeconds(1f);

        // 4) 맵 스크롤 + 플레이어 이동 시작 (MoveMapController 가 Run 애니메이션도 함께 처리)
        if (mapController != null)
        {
            mapController.SetPlayerUnit(PlayerUnit);
            mapController.Move();
        }
        GameRoot.Instance.UserData.InGamePlayerData.IsMapMovingProperty.Value = true;

        // 5) MainCam 팬 — 끝나갈 때쯤 적이 미리 걸어 들어온다.
        yield return PanAndPreSpawnEnemies();

        // 6) 이동 완료 - 맵 + 플레이어 정지 (Idle 전환 포함)
        if (mapController != null)
            mapController.Stop();
        GameRoot.Instance.UserData.InGamePlayerData.IsMapMovingProperty.Value = false;

        // 7) 전투 시퀀스 시작 (적은 이동 중에 미리 소환됨 — 도착 즉시 전투).
        //    AttackSequence 복귀 지점이 되는 originalPosition 을 이동이 끝난 현재 위치로 갱신.
        if (PlayerUnit != null)
            PlayerUnit.SaveOriginalPosition();
        StartWaveCombat(true);
    }

    // lasttowerdefense 의 StartWave 에 해당 — 적 소환 후 웨이브 공격 시퀀스를 (재)시작한다.
    private void StartWaveCombat(bool alreadySpawned = false)
    {
        // 웨이브 진입 시 휴식 상태 해제.
        GameRoot.Instance.UserData.InGamePlayerData.IsWaveRestProperty.Value = false;

        // 이동 연출 중 미리 스폰(PanAndPreSpawnEnemies)한 경우 중복 스폰 방지.
        if (!alreadySpawned)
            SpawnWaveEnemies();

        // 이전 웨이브에서 좀비로 살아남은 핸들이 있다면 중단 후 새로 시작.
        if (waveAttackCoroutine != null)
        {
            StopCoroutine(waveAttackCoroutine);
            waveAttackCoroutine = null;
        }
        waveAttackCoroutine = StartCoroutine(WaveAttackSequence(alreadySpawned));
    }

    // lasttowerdefense 의 WaveAttackSequence 이식 — 주사위 턴(슈팅/머지/알리먼트) 제거 버전.
    // 플레이어 1회 공격 ↔ 생존 적 전원 반격을 반복하다가 적 전멸 시 StartRest 로 넘어간다.
    private IEnumerator WaveAttackSequence(bool preSpawned = false)
    {
        // 1) 웨이브 스폰 완료 + 적들이 걸어 들어오는 연출(DOMove 1.2s) 대기.
        //    스폰 코루틴이 예외 등으로 완료 플래그를 못 내리는 경우에 대비해 15초 타임아웃.
        float spawnWait = 0f;
        while (!IsWaveSpawnComplete && spawnWait < 15f)
        {
            spawnWait += Time.deltaTime;
            yield return null;
        }
        if (!IsWaveSpawnComplete)
            Debug.LogWarning("[InGameBaseStage] 웨이브 스폰 완료 대기 타임아웃 — 전투 시퀀스를 강제로 진행합니다.");

        // 이동 중에 미리 스폰된 경우 적 걸어들어오기가 이미 끝나가므로 짧게만 대기.
        yield return new WaitForSeconds(preSpawned ? 0.2f : 1.3f);

        while (true)
        {
            if (PlayerUnit == null || EnemyUnitGroup == null)
                yield break;

            // 2) 플레이어 턴 — 가장 가까운 적에게 1회 공격 후 제자리 복귀.
            var target = EnemyUnitGroup.FindClosestAliveEnemy(PlayerUnit.transform.position);
            if (target != null)
            {
                yield return PlayerUnit.AttackSequence(target);
                yield return PlayerUnit.ReturnToOriginalPosition();
            }

            // 3) 적이 전멸했으면 보상 상자 연출 후 웨이브 종료.
            if (!HasAliveEnemy())
            {
                yield return ChestRewardSequence();
                StartRest();
                yield break;
            }

            // 4) 플레이어가 살아있으면 적 반격 턴.
            if (PlayerUnit != null && PlayerUnit.CurState != PlayerUnitBase.PlayerUnitState.Dead)
            {
                yield return new WaitForSeconds(0.3f);

                var aliveEnemies = GetAliveEnemyList();
                foreach (var enemy in aliveEnemies)
                {
                    if (enemy == null || enemy.IsDead) continue;
                    if (PlayerUnit == null || PlayerUnit.CurState == PlayerUnitBase.PlayerUnitState.Dead) break;

                    yield return enemy.AttackPlayerSequence(PlayerUnit);
                    yield return new WaitForSeconds(0.1f);
                }
            }

            // 5) 플레이어가 죽었으면 종료 (로비 복귀는 PlayerUnitBase.Dead 에서 처리).
            if (PlayerUnit == null || PlayerUnit.CurState == PlayerUnitBase.PlayerUnitState.Dead)
                yield break;

            yield return new WaitForSeconds(0.3f);
        }
    }

    // 웨이브 클리어 보상 상자 인스턴스 (어드레서블 "InGameChest" — 최초 1회 로드 후 재사용).
    private InGameChest chestInstance;
    private bool chestLoading;

    // 웨이브 클리어 보상 장비 등급 결정.
    // WaveInfo.select_reward_type 이 -1 이 아니면 지정 등급(select_reward_idx)을 그대로 쓰고,
    // -1 이면 WaveRewardRatio 의 ratio 가중치 랜덤으로 등급(reward_idx)을 뽑는다.
    private int GetWaveEquipGrade()
    {
        bool isHardStage = GameRoot.Instance.UserData.DifficultyTypeProperty.Value == DifficultyType.Hard;
        if (!isHardStage)
        {
            var waveTable = Tables.Instance.GetTable<WaveInfo>();
            var stageidx = GameRoot.Instance.UserData.Stageidx.Value;
            var waveidx = GameRoot.Instance.UserData.Waveidx.Value;
            var waveData = waveTable?.DataList.Find(x => x.stage_idx == stageidx && x.wave_idx == waveidx);
            if (waveData != null && waveData.select_reward_type != -1 && waveData.select_reward_idx > 0)
                return waveData.select_reward_idx;
        }

        // 가중치 랜덤 — ratio 합 기준으로 굴려서 걸린 행의 등급을 사용.
        var ratioTable = Tables.Instance.GetTable<WaveRewardRatio>();
        var rows = ratioTable?.DataList.FindAll(x => x.ratio > 0);
        if (rows == null || rows.Count == 0)
            return 1;

        int total = 0;
        foreach (var row in rows) total += row.ratio;

        int roll = Random.Range(0, total);
        foreach (var row in rows)
        {
            roll -= row.ratio;
            if (roll < 0)
                return row.reward_idx;
        }
        return rows[rows.Count - 1].reward_idx;
    }

    // 마지막 적 처치 후 — 등급 컬러 상자 등장 → 오픈 연출 → 다음 진행.
    private IEnumerator ChestRewardSequence()
    {
        int grade = GetWaveEquipGrade();

        if (chestInstance == null)
        {
            if (!chestLoading)
            {
                chestLoading = true;
                Addressables.InstantiateAsync("InGameChest").Completed += handle =>
                {
                    if (handle.Result != null)
                        chestInstance = handle.Result.GetComponent<InGameChest>();
                    chestLoading = false;
                };
            }

            // 로드 대기 (실패 대비 5초 타임아웃 — 실패 시 연출 없이 진행).
            float wait = 0f;
            while (chestInstance == null && chestLoading && wait < 5f)
            {
                wait += Time.deltaTime;
                yield return null;
            }
            if (chestInstance == null)
                yield break;
        }

        // 마지막으로 죽은 적 자리에 등장 (Set 에서 살짝 위에서 떨어지는 연출 처리).
        var basePos = EnemyUnitGroup != null
            ? EnemyUnitGroup.LastDeadPosition
            : (PlayerUnit != null ? PlayerUnit.transform.position + new Vector3(3f, 0f, 0f) : Vector3.zero);
        chestInstance.transform.position = basePos;
        chestInstance.gameObject.SetActive(true);
        chestInstance.Set(grade);

        yield return new WaitForSeconds(0.6f);   // 낙하 등장 연출 대기
        chestInstance.Open();
        yield return new WaitForSeconds(1.1f);   // 오픈 연출 + 여운

        OpenSelectEquipmentPopup(grade);

        chestInstance.gameObject.SetActive(false);

        // 장비 선택이 끝날 때까지(팝업이 닫힐 때까지) 다음 진행(StartRest) 블로킹.
        yield return new WaitUntil(() => !selectEquipPopupOpen);
    }

    private bool selectEquipPopupOpen;

    // 상자 오픈 후 장비 선택 팝업 — 상자 등급과 같은 등급의 장비를 EquipmentInfo 에서 랜덤으로 뽑아 표시.
    private void OpenSelectEquipmentPopup(int grade)
    {
        var equipTable = Tables.Instance.GetTable<EquipmentInfo>();
        var candidates = equipTable?.DataList.FindAll(x => x.grade == grade);
        if (candidates == null || candidates.Count == 0)
            return;

        var pick = candidates[Random.Range(0, candidates.Count)];
        selectEquipPopupOpen = true;
        GameRoot.Instance.UISystem.OpenUI<PopupSelectEquipment>(popup =>
        {
            popup.Set(pick.equip_idx, pick.equip_type, grade, 1);
            popup.OnUIHideAfter = () => selectEquipPopupOpen = false;
        });
    }

    // 웨이브 클리어 — lasttowerdefense 의 StartRest 이식(주사위/쉴드/이어하기 스냅샷 제외).
    private void StartRest()
    {
        GameRoot.Instance.UserData.InGamePlayerData.IsWaveRestProperty.Value = true;

        List<TpParameter> parameters = new List<TpParameter>();
        parameters.Add(new TpParameter("stage", GameRoot.Instance.UserData.Stageidx.Value));
        parameters.Add(new TpParameter("waveidx", GameRoot.Instance.UserData.Waveidx.Value));
        GameRoot.Instance.PluginSystem.AnalyticsProp.AllEvent(IngameEventType.None, "m_wave_clear", parameters);

        bool isHardStage = GameRoot.Instance.UserData.DifficultyTypeProperty.Value == DifficultyType.Hard;
        var stageIdxForWave = isHardStage ? GameRoot.Instance.UserData.Hardmodestageidx.Value : GameRoot.Instance.UserData.Stageidx.Value;
        var hardWaveTable = Tables.Instance.GetTable<Hard_WaveInfo>();
        var waveTable = Tables.Instance.GetTable<WaveInfo>();
        // 새 WaveInfo 는 spawn_idx 로 한 웨이브가 여러 행일 수 있어 wave_idx 를 Distinct 해서 웨이브 수를 센다.
        var wavecount = isHardStage
            ? (hardWaveTable != null ? hardWaveTable.DataList.FindAll(x => x.stage == stageIdxForWave).Count : 0)
            : (waveTable != null ? waveTable.DataList.Where(x => x.stage_idx == stageIdxForWave).Select(x => x.wave_idx).Distinct().Count() : 0);

        bool isend = GameRoot.Instance.UserData.Waveidx.Value >= wavecount;

        if (isend)
        {
            // 마지막 웨이브 클리어 — 결과 팝업(PopupStageResult)은 미이식이라 바로 다음 스테이지 이동 연출로 진행.
            GoNextStage();
        }
        else
        {
            GameRoot.Instance.UserData.Waveidx.Value += 1;
            StartCoroutine(NextWaveSequence());
        }
    }

    // 다음 웨이브 이동 연출 — lasttowerdefense 의 NextWaveSequence 이식(보상 상자/주사위 리셋 제외).
    private IEnumerator NextWaveSequence()
    {
        yield return new WaitForSeconds(0.5f);

        // 1) 맵 스크롤 + 플레이어 이동.
        var mapController = TopBattleMap != null ? TopBattleMap.GetMapController : null;
        if (mapController != null)
        {
            mapController.SetPlayerUnit(PlayerUnit);
            mapController.Move();
        }
        GameRoot.Instance.UserData.InGamePlayerData.IsMapMovingProperty.Value = true;

        // 2) MainCam 팬 — 끝나갈 때쯤 적이 미리 걸어 들어온다.
        yield return PanAndPreSpawnEnemies();

        // 3) 이동 완료 - 맵 + 플레이어 정지.
        if (mapController != null)
            mapController.Stop();
        GameRoot.Instance.UserData.InGamePlayerData.IsMapMovingProperty.Value = false;

        if (PlayerUnit != null)
            PlayerUnit.SaveOriginalPosition();

        // 4) 다음 웨이브 공격 시퀀스 시작 (적은 이동 중에 미리 소환됨 — 도착 즉시 전투).
        StartWaveCombat(true);
    }

    // 레벨업(업그레이드 선택 완료) 시 호출 — Waveidx 는 통계용 카운트로만 증가.
    public void StartNextWave()
    {
        if (!GameRoot.Instance.UserData.InGamePlayerData.IsGameStartProperty.Value)
        {
            Debug.LogWarning("[WaveDBG] StartNextWave 무시됨 — IsGameStart=false");
            return;
        }

        GameRoot.Instance.UserData.Waveidx.Value++;

        // 다음 웨이브 적 소환.
        SpawnWaveEnemies();
    }

    // 인게임에서 메인(로비)으로 복귀. 페이드 중간에 인게임 정리 후 로비/HUD 재오픈.
    public void ReturnMainScreen(System.Action fadeaction = null)
    {
        GameRoot.Instance.GameSpeedSystem.StopGameSpeed(false, false);
        GameRoot.Instance.PluginSystem.ADProp.HideBanner();

        // 메인 로비 복귀 — 전투 시작 시 껐던 메인 BGM 을 다시 켠다(유저 설정 반영).
        GameRoot.Instance.BgmOn();

        // 메인 복귀 시 게임 스피드 1배로 초기화.
        GameRoot.Instance.GameSpeedSystem.ResetGameSpeedOnBattleEnd();

        // 메인 복귀 시 웰컴백(재진입) 데이터 초기화
        GameRoot.Instance.UserData.ClearInGameResumeData(true);

        StageClear();

        // 인게임 플로팅 UI 는 페이드 위에 떠 있어 페이드 시작 전에 전부 꺼서 잔상이 보이지 않게 한다.
        GameRoot.Instance.UISystem.SetFloatingUIActiveAll(false);

        fadeaction += StartMainUI;

        GameRoot.Instance.UISystem.OpenUI<PageFade>(page =>
        {
            page.Set(fadeaction);
        }, null, false);
    }

    // 페이드(검은 화면) 중에 호출 — 인게임 UI 닫고 로비/HUD 재오픈.
    private void StartMainUI()
    {
        // 통화 HUD부터 다시 열어 참조를 재구성한 뒤 로비/HUD를 표시
        GameRoot.Instance.InitCurrencyTop(() =>
        {
        });

        // 로비 UI 미이식 상태 — 복귀할 화면이 없으므로 전투를 처음부터 다시 시작해 사이클을 유지한다.
        // (사망 → ReturnMainScreen → 페이드 → 여기서 재시작)
        StartWave();
    }

    public void StageClear()
    {
        StopAllCoroutines();
        waveAttackCoroutine = null;
        stageTimerRunning = false;
        stageElapsedTime = 0f;
        stageTransitioning = false;

        GameRoot.Instance.UserData.InGamePlayerData.IsGameStartProperty.Value = false;
        GameRoot.Instance.UserData.InGamePlayerData.IsMapMovingProperty.Value = false;

        // 남아있는 적 정리.
        ClearEnemies();

        // 맵 이동 컨트롤러의 플레이어 참조 해제 — 재진입 시 새 유닛 위치로 재캡처되도록.
        var mapController = TopBattleMap != null ? TopBattleMap.GetMapController : null;
        if (mapController != null)
        {
            mapController.Stop();
            mapController.ClearPlayerUnit();
        }
    }

    // 다음 스테이지로 진행 — lasttowerdefense 식 이동 연출(맵 스크롤 + 캐릭터 달리기 + MainCam 팬).
    // 외부(웨이브 클리어/컨텐츠 로직)에서 호출한다.
    public void GoNextStage()
    {
        if (stageTransitioning) return;
        stageTransitioning = true;
        StartCoroutine(NextStageRoutine());
    }

    private IEnumerator NextStageRoutine()
    {
        // WaveInfo 에 다음 스테이지 1웨이브 데이터가 있으면 다음 스테이지, 없으면 처음(1스테이지)부터 반복.
        var waveTable = Tables.Instance.GetTable<WaveInfo>();
        int next = GameRoot.Instance.UserData.Stageidx.Value + 1;
        if (waveTable == null || waveTable.GetData(new KeyValuePair<int, int>(next, 1)) == null)
            next = 1;

        GameRoot.Instance.UserData.Stageidx.Value = next;
        GameRoot.Instance.UserData.Waveidx.Value = 1;
        GameRoot.Instance.UserData.Save();

        // 1) 이동 연출 시작 — 플레이어 달리기 + 바람 이펙트 + 배경 스크롤.
        var mapController = TopBattleMap != null ? TopBattleMap.GetMapController : null;
        if (mapController != null)
        {
            mapController.SetPlayerUnit(PlayerUnit);
            mapController.Move();
        }
        GameRoot.Instance.UserData.InGamePlayerData.IsMapMovingProperty.Value = true;

        // 2) 다음 스테이지 배경(StageMap_{deco_idx}) 갱신 — 같은 deco 면 그대로 재사용.
        if (TopBattleMap != null)
            TopBattleMap.SetMoveMap();

        // 이전 스테이지의 남은(풀링된) 적 정리 — 팬 중에 미리 스폰하므로 먼저 비운다.
        ClearEnemies();

        // 3) MainCam 팬 — 시퀀스 동안 카메라 팔로우는 잠시 끈다. 끝나갈 때쯤 다음 스테이지 적이 걸어 들어온다.
        yield return PanAndPreSpawnEnemies();

        // 4) 이동 완료 — 맵/플레이어 정지.
        if (mapController != null)
            mapController.Stop();
        GameRoot.Instance.UserData.InGamePlayerData.IsMapMovingProperty.Value = false;

        // 스테이지 경과 시간 리셋 — 연출이 끝나는 순간 다음 스테이지 시작.
        stageElapsedTime = 0f;
        stageTransitioning = false;

        if (PlayerUnit != null)
            PlayerUnit.SaveOriginalPosition();

        // 새 스테이지 1웨이브 전투 시퀀스 시작 (적은 이동 중에 미리 소환됨).
        StartWaveCombat(true);
    }

    // 적 걸어들어오는 연출(DOMove 1.2s)이 팬 종료와 맞물리도록, 팬이 끝나기 이만큼 전에 미리 스폰을 시작한다.
    private const float EnemyPreSpawnLead = 1.3f;

    // 카메라 팬과 적 스폰을 겹친다 — 플레이어가 거의 도착할 때쯤 적이 걸어 들어와
    // 도착과 동시에 바로 전투를 시작할 수 있게 한다.
    private IEnumerator PanAndPreSpawnEnemies()
    {
        var mainCam = GameRoot.Instance.InGameSystem.CurInGame?.GetMainCam;
        float finalCamX = (mainCam != null ? mainCam.transform.position.x : 14f) + StageMoveDistance;

        var pan = StartCoroutine(PanMainCamera(StageMoveDistance, StageMoveDuration));

        float lead = Mathf.Min(EnemyPreSpawnLead, StageMoveDuration);
        yield return new WaitForSeconds(StageMoveDuration - lead);

        // 스폰 정지 위치는 아직 이동 중인 카메라가 아니라 도착 시점 카메라 X 기준으로 잡는다.
        if (EnemyUnitGroup != null)
            EnemyUnitGroup.SpawnCamXOverride = finalCamX;
        SpawnWaveEnemies();

        yield return pan;
    }

    // MainCam 하나만 moveDistance 만큼 옆으로 부드럽게 이동시킨다 (SmoothStep, lasttowerdefense 이식).
    // 이동 중에는 CameraController 팔로우를 꺼서 팬과 팔로우가 서로 밀고 당기지 않게 한다.
    private IEnumerator PanMainCamera(float moveDistance, float moveDuration)
    {
        if (CamController != null)
            CamController.SetFollowEnabled(false);

        var mainCam = GameRoot.Instance.InGameSystem.CurInGame?.GetMainCam;
        Vector3 cameraStartPos = Vector3.zero;
        Vector3 cameraTargetPos = Vector3.zero;

        if (mainCam != null)
        {
            cameraStartPos = mainCam.transform.position;
            cameraTargetPos = cameraStartPos + new Vector3(moveDistance, 0f, 0f);
        }

        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            if (mainCam != null)
                mainCam.transform.position = Vector3.Lerp(cameraStartPos, cameraTargetPos, smooth);

            yield return null;
        }

        if (CamController != null)
            CamController.SetFollowEnabled(true);
    }
}
