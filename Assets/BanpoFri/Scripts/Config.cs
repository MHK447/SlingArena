using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using System.Linq;
using BanpoFri;
using System;


public enum Language
{
    en,
    ko,
    es,
    ja,
    ptbr,
    th,
    tw,
    vi,
    bg,
    cn,
    cs,
    da,
    nl,
    et,
    fi,
    fr,
    de,
    el,
    hu,
    id,
    it,
    lv,
    lt,
    no,
    pl,
    pt,
    ro,
    ru,
    sk,
    sl,
    sv,
    tr,
    ua

}


[System.Serializable]
public class FontDefine
{
    public Language country;
    public Font font;
}


[System.Serializable]
public class Config : BanpoFri.SingletonScriptableObject<Config>, BanpoFri.ILoader
{
    public enum FloatingUIDepth
    {
        HpProgress,
        NewDice,
        AdAddDiceComponent,
        CastleRepairComponent,
    }
    public enum LandCondination
    {
        Great,
        Basic,
        Sad,
    }

    public enum InGameUpgradeIdx
    {
        ATTACK,
        ATTACKSPEED,
        ATTACKRANGE,
        ATTACKREGEN,
        HP,
        HPREGEN,
        CRITICALPERCENT,
        CRITICALMULTIPLE,
    }

    public enum LABUpgradeIdx
    {
        ATTACK,
        ATTACKREGEN,
        ATTACKRANGE,
        HP,
        HPREGEN,
        CRITICALPERCENT,
        CRITICALDAMAGE,
    }

    public enum RecordKeys
    {
        StagePlayTime,
        EventStagePlayTime,
        Init,
        FirstDayPlayTime,
        FirstDayLogTime,
        M_Rev_05,
        ABTest,
        ShopDailyPurchaseCnt,
        TryTowerClear,
        UseADTicketCnt,

        AdCycleCount,
        AccrueRev

    }

    public enum ItemInfoType
    {
        AllEquipmentItem = 0,
        HelmatItem = 1,
        Aromor = 2,
        Shoose = 3,
        Ring = 4,
    }

    public enum WeaponType
    {
        Base = 1,
        TrackEnemy,
    }

    public enum CurrencyID
    {
        Money = 1,
        Cash = 2,
        Material = 3,
        HeroUpgradeCard = 4,

        SilverCoin = 101,
    }
    // 인게임 업그레이드 선택지. 컨벤션: upgrade_value_1 = 수치(%/값), upgrade_value_2 = 확률(%).
    public enum InGameUpgradeChoice
    {
        // Dice
        DiceSupply = 1,                 // 주사위 보급
        DiceFaceEnhance = 2,            // 장착 주사위 1면 강화 — 랜덤 1면 +value_1 (누적)
        DiceAllFaceEnhance = 3,         // 모든 주사위 면 강화 — 모든 면 +value_1 (누적)

        // Castle
        CastleRecovery = 5,             // 성벽 회복 — 시작 HP의 value_1% 즉시 회복
        AddSilverCoinSpeed = 6,         // 실버코인 수급 속도 +value_1% (누적) — "은화 스피드 {0}% 증가"
        AddSilverCoin = 7,              // 실버코인 지급 — 현재 굴리기 가격 × value_1 즉시 지급 — "은화 +{0} 획득"

        // Warrior
        BerserkAwakening = 101,           // 분노 — 체력 50% 이하 시 공격력 +value_1%, 공격속도 +value_2%
        DeathProtection = 102,            // 죽음의 가호 — 치명상 1회 무효(유닛당 1회)
        LifeStealOnKill = 103,            // 처치 회복 — 적 처치 시 최대체력의 value_1% 회복 
        MaxHealthBoost = 104,             // 전사 체력 +value_1%
        AttackPowerBoost = 105,           // 전사 공격력 +value_1%

        // Archer
        AttackSpeedBoost = 201,           // 궁수 공격속도 +value_1%
        DoubleStrikeChance = 202,         // 연속 타격 — value_2% 확률로 2번 공격
        LowHealthDamageBoost = 203,       // 위기 공격 강화 — 체력 50% 이하 시 피해량 +value_1%
        PierceChanceBoost = 204,          // 관통 숙련 — value_2% 확률로 관통 +value_1
        ArcherAttackBoost = 205,          // 궁수 공격력 +value_1%

        //Spear
        SpearmanAttackBoost = 301,        // 창병 공격력 +value_1%
        SpearmanHealthBoost = 302,        // 창병 체력 +value_1%
        SpearmanBleedChance = 303,        // 출혈 찌르기 — 공격 시 value_2% 확률로 출혈(공격력의 value_1% 추가 피해)
        SpearmanDashOnKill = 304,         // 처치 돌진 — 적 처치 시 다음 적에게 돌진
        SpearmanAttackSpeedBoost = 305,   // 창병 공격속도 +value_1%

        //Magicer
        MagicAttackBoost = 401,           // 마법사 공격력 +value_1%
        MagicAttackSpeedBoost = 402,      // 마법사 공격속도 +value_1%
        RangeBoost = 403,                 // 마법사 공격 사거리 +value_1%
        SplashDamageOnAttack = 404,       // 파열 공격 — 공격 시 주변에 피해량의 value_1% 범위 피해
        FirstHitShieldBlock = 405,        // 선제 방어막 — 첫 피해 1회 무조건 방어(유닛당 1회)

        //Shileder — 로컬라이즈/시트 기준 번호(501~506). ShieldRegen(구 502)은 미사용이라 제거됨.
        ReflectDamageOnHit = 501,         // 가시 갑옷 — 피격 시 받은 피해의 value_1% 반사
        DamageReduction = 502,            // 단단해지기 — 방패병 받는 피해 -value_1%
        LowHealthDamageReduction = 503,   // 불굴의 의지 — 체력 20% 이하 시 받는 피해 -value_1% 추가
        ShieldDamageBoost = 504,          // 방패병 공격력 +value_1%
        ShieldHealthBoost = 505,          // 방패병 체력 +value_1%
        GuardChanceBoost = 506,           // 절대 방어 — value_2% 확률로 피해 완전 무효
    }

    public enum UnitType
    {
        Enemy,
        Player,
    }

    public enum DiceUnitType
    {
        Warrior = 1,
        Archer = 2,
        Spear = 3,
        Magic = 4,
        Shield = 5,
    }

    public enum RewardType
    {
        Currency = 1,
        Card = 2,
        Item = 3,
        HeroUpgrade = 4,
        HeroEquipment = 5,
        RandHeroItem = 6,
        BuffReward = 7,
    }

    public enum RecordCountKeys
    {
        FirstEcpm,
        Init,
        StartStage,
        Navi_Start,
        FreeGemCount,
        AdGemCount,
        AdCycleCount,
        TutorialStageCount,
        FirstSwayAdd,
        AdWatchCount,
        BuyInAppCountTotal,
        StageFailedCount,
        TrainingLevelUp,
        ReviewPopup,

        TrainingAdWatchCount,
        StageReward,

        NewEnemyBlockSpawnerReward,

        NewChapter,

        FREEREVIVALCOUNT,

        EQUIPWEAPONCOUNT,

        REWARDBOOKWEAPONCOUNT,
        AttendanceCount,
        AdAttendanceCount,
        StageLifeTime,

        AdsEquipCount,

        TryStageClear,

        ShowInterAdCount,
        StarterPackage,

        FreeCashCount,
        OneTimeEntryCount,
        OneTimeCumlativeCount,
        OneTimeCumlativeCheck,
        FirstEnterRank,

        HardStageReward,
        FreeGameSpeed,
        DailyFreecashUIClickCount,
        StageFreecashCount,
        FreecashLinked,
        M_Rev_05,
        UseADTicketCnt,

        FreeCashFirstOpen,
        AccrueRev,
        FreeAddDice,
        CastleRepairFreeUse,

        DailyDiceCardCount,
    }

    public enum BoxTypeIdx
    {
        Rare,
        Epic,
        Legend,
    }



    public enum WeaponUnit
    {
        Knife = 1,
        Spear = 2,
        FireBallMagic = 3,
        BigSword = 4,
        Sickle = 5,
        Bow = 6,
        Shiled = 7,
        Shuriken = 8,
        LightSaber = 9,
        Katana = 10, // 구연 안함 아직 검기 
        Lifle = 11,
        Hammer = 12,
        Chainsaw = 13,

    }

    public enum ItemType
    {
        TreasureTicket = 1,
        OneTimeBuff = 2,
        OneTimeValue = 3,
        TreasureChance = 7,
        ArtifactNormalKey = 8,
        ArtifactSpecialKey = 9,
        VIPTicket = 10,
        LuckyVerseTicket = 11,
        LuckyVerseRankBuff = 12,
        LuckyVerseLabTimeBuff = 13,
        LuckyVerseCoinBuff = 14,
        LuckyVerseTicketMaxBuff = 15,
    }



    [System.Serializable]
    public class ColorDefine
    {
        public string key_string;
        public Color color;
    }

    [HideInInspector]
    [SerializeField]
    private List<ColorDefine> _textColorDefines = new List<ColorDefine>();
    [HideInInspector]
    [SerializeField]
    private List<ColorDefine> _eventTextColorDefines = new List<ColorDefine>();
    private Dictionary<string, Color> _textColorDefinesDic = new Dictionary<string, Color>();

    [SerializeField]
    private Font mainfont;
    [SerializeField]
    private List<FontDefine> _fontDefines = new List<FontDefine>();


    public List<Material> DiceEdgeGradeMaterialList = new List<Material>();

    public List<Material> DiceGradeMaterialList = new List<Material>();



    public List<ColorDefine> TextColorDefines
    {
        get
        {
            return _textColorDefines;
        }
    }
    public List<ColorDefine> EventTextColorDefines
    {
        get
        {
            return _eventTextColorDefines;
        }
    }

    public List<Material> TextFontMaterialList = new List<Material>();


    [HideInInspector]
    [SerializeField]
    private List<ColorDefine> _imageColorDefines = new List<ColorDefine>();
    [HideInInspector]
    [SerializeField]
    private List<ColorDefine> _eventImgaeColorDefines = new List<ColorDefine>();
    private Dictionary<string, Color> _imageColorDefinesDic = new Dictionary<string, Color>();
    public List<ColorDefine> ImageColorDefines
    {
        get
        {
            return _imageColorDefines;
        }
    }
    public List<ColorDefine> EventImageColorDefines
    {
        get
        {
            return _eventImgaeColorDefines;
        }
    }

    public Material SkeletonGraphicMat;
    public Material DisableSpriteMat;
    public Material EnableSpriteMat;
    public Material ImgAddtiveMat;
    public Material SolidMat;

    public Material DefaultMat;

    public Material GraySpriteMat;

    public List<Material> TextMaterialList = new List<Material>();




    public Color GetTextColor(string key)
    {
        if (_textColorDefinesDic.ContainsKey(key))
            return _textColorDefinesDic[key];

        return Color.white;
    }

    public Color GetBoxGradeTextColor(int grade)
    {
        switch (grade)
        {
            case 2:
                return GetTextColor("Blue");
            case 3:
                return GetTextColor("Purple");
            case 4:
                return GetTextColor("Yellow");
        }
        return Color.white;
    }


    public Color GetImageColor(string key)
    {
        if (_imageColorDefinesDic.ContainsKey(key))
            return _imageColorDefinesDic[key];

        return Color.white;
    }


    public Color GetUnitGradeColor(int grade)
    {
        switch (grade)
        {
            case 1:
                return GetImageColor("Unit_Grade_1");
            case 2:
                return GetImageColor("Unit_Grade_2");
            case 3:
                return GetImageColor("Unit_Grade_3");
        }

        return Color.white;
    }

    public Color GetInGameEquipmentGradeColor(int grade)
    {
        return GetImageColor("InGameEquipment_Grade_Color_" + grade);
    }

    public void UpdateFallbackOrder(Language CurLangauge)
    {
        if (CurLangauge != Language.ja &&
            CurLangauge != Language.tw) return;

        // foreach (var name in mainfont.fontNames)
        //     Debug.Log("before :" + name);

        var list = mainfont.fontNames.ToList();

        var font = _fontDefines.Where(x => x.country == CurLangauge).FirstOrDefault();
        if (font != null)
        {
            list.Remove(font.font.fontNames[0]);
            list.Insert(1, font.font.fontNames[0]);

            var new_array = new string[list.Count];

            for (int i = 0; i < mainfont.fontNames.Length; i++)
            {
                new_array[i] = list[i];
            }

            mainfont.fontNames = new_array;

            new_array = null;

            // foreach (var name in mainfont.fontNames)
            //     Debug.Log("after :" + name);

        }
    }



    public void Load()
    {
        _textColorDefinesDic.Clear();
        foreach (var cd in _textColorDefines)
        {
            _textColorDefinesDic.Add(cd.key_string, cd.color);
        }
        foreach (var cd in _eventTextColorDefines)
        {
            _textColorDefinesDic.Add(cd.key_string, cd.color);
        }
        _imageColorDefinesDic.Clear();
        foreach (var cd in _imageColorDefines)
        {
            _imageColorDefinesDic.Add(cd.key_string, cd.color);
        }
        foreach (var cd in _eventImgaeColorDefines)
        {
            _imageColorDefinesDic.Add(cd.key_string, cd.color);
        }
    }

}
