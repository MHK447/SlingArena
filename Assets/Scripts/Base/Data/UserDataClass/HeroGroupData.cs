using System;
using System.Collections.Generic;
using UniRx;
using Google.FlatBuffers;
using System.Linq;
public partial class UserDataSystem
{
    public HeroGroupData Herogroudata { get; private set; } = new HeroGroupData();
    private void SaveData_HeroGroupData(FlatBufferBuilder builder)
    {
        // 선언된 변수들은 모두 저장되어야함

        // Herogroudata 단일 저장
        // Herogroudata.Herodatas 처리 GenerateItemSaveCode IsCustom
        Offset<BanpoFri.Data.HeroData>[] herogroudata_herodatas_Array = null;
        VectorOffset herogroudata_herodatas_Vector = default;

        if(Herogroudata.Herodatas.Count > 0){
            herogroudata_herodatas_Array = new Offset<BanpoFri.Data.HeroData>[Herogroudata.Herodatas.Count];
            int herogroudata_herodatas_idx = 0;
            foreach(var herogroudata_herodatas_pair in Herogroudata.Herodatas){
                var herogroudata_herodatas_item = Herogroudata.Herodatas[herogroudata_herodatas_idx];
                herogroudata_herodatas_Array[herogroudata_herodatas_idx++] = BanpoFri.Data.HeroData.CreateHeroData(
                    builder,
                    herogroudata_herodatas_item.Heroidx,
                    herogroudata_herodatas_item.Level.Value
                );
            }
            herogroudata_herodatas_Vector = BanpoFri.Data.HeroGroupData.CreateHerodatasVector(builder, herogroudata_herodatas_Array);
        }

        // Herogroudata 최종 생성 및 추가
        var herogroudata_Offset = BanpoFri.Data.HeroGroupData.CreateHeroGroupData(
            builder,
            Herogroudata.Equipplayeridx,
            herogroudata_herodatas_Vector
        );


        Action cbAddDatas = () => {
            // BanpoFri.Data.UserData.AddHerogroudata(builder, herogroudata_Offset);
        };

        cb_SaveAddDatas += cbAddDatas;

    }
    private void LoadData_HeroGroupData()
    {
        // 로드 함수 내용

        // Herogroudata 로드
        // var fb_Herogroudata = flatBufferUserData.Herogroudata;
        // if (fb_Herogroudata.HasValue)
        // {
        //     Herogroudata.Equipplayeridx = fb_Herogroudata.Value.Equipplayeridx;

        //     // Herodatas 로드
        //     Herogroudata.Herodatas.Clear();
        //     int herodatasLength = fb_Herogroudata.Value.HerodatasLength;
        //     for (int j = 0; j < herodatasLength; j++)
        //     {
        //         var fbHerodatasItem = fb_Herogroudata.Value.Herodatas(j);
        //         if (fbHerodatasItem.HasValue)
        //         {
        //             var nested_item = new HeroData
        //             {
        //                 Heroidx = fbHerodatasItem.Value.Heroidx,
        //                 Level = new ReactiveProperty<int>(fbHerodatasItem.Value.Level)
        //             };
        //             Herogroudata.Herodatas.Add(nested_item);
        //         }
        //     }
        // }
    }

}

public class DiceInfoData
{
    public int DiceIdx = 0;

    public int DiceGrade = 0;

}

public class HeroGroupData
{
    public List<HeroData> Herodatas = new List<HeroData>();

    public int Equipplayeridx { get; set; } = 1;

    public void AddHero(int heroidx)
    {
        var finddata = FindHero(heroidx);
        
        if (finddata != null)
        {
            return;
        }

        var addhero = new HeroData() { Heroidx = heroidx, Level = new ReactiveProperty<int>(1) };
        Herodatas.Add(addhero);
    }

    public HeroData FindHero(int heroidx)
    {
        return Herodatas.Where(x => x.Heroidx == heroidx).FirstOrDefault();
    }

}
