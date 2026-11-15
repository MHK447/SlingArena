using System;
using System.Collections.Generic;
using UniRx;
using Google.FlatBuffers;

public partial class UserDataSystem
{
    public List<BlessData> Blessdatas { get; private set; } = new List<BlessData>();
    private void SaveData_BlessData(FlatBufferBuilder builder)
    {
        // 선언된 변수들은 모두 저장되어야함

        // Blessdatas Array 저장
        Offset<BanpoFri.Data.BlessData>[] blessdatas_Array = null;
        VectorOffset blessdatas_Vector = default;

        if(Blessdatas.Count > 0){
            blessdatas_Array = new Offset<BanpoFri.Data.BlessData>[Blessdatas.Count];
            int index = 0;
            foreach(var pair in Blessdatas){
                var item = pair;
                blessdatas_Array[index++] = BanpoFri.Data.BlessData.CreateBlessData(
                    builder,
                    item.Blesstype,
                    item.Blesstime.Ticks
                );
            }
            // blessdatas_Vector = BanpoFri.Data.UserData.CreateBlessdatasVector(builder, blessdatas_Array);
        }



        Action cbAddDatas = () => {
            // BanpoFri.Data.UserData.AddBlessdatas(builder, blessdatas_Vector);
        };

        cb_SaveAddDatas += cbAddDatas;

    }
    private void LoadData_BlessData()
    {
        // 로드 함수 내용

        // Blessdatas 로드
        Blessdatas.Clear();
        // int Blessdatas_length = flatBufferUserData.BlessdatasLength;
        // for (int i = 0; i < Blessdatas_length; i++)
        // {
        //     var Blessdatas_item = flatBufferUserData.Blessdatas(i);
        //     if (Blessdatas_item.HasValue)
        //     {
        //         var blessdata = new BlessData
        //         {
        //             Blesstype = Blessdatas_item.Value.Blesstype,
        //             Blesstime = new DateTime(Blessdatas_item.Value.Blesstime)
        //         };

        //         // Blesstime DateTime 로드
        //         blessdata.Blesstime = new DateTime(Blessdatas_item.Value.Blesstime);
        //         Blessdatas.Add(blessdata);
        //     }
        // }
    }

}

public class BlessData
{
    public int Blesstype { get; set; } = 0;
    public DateTime Blesstime { get; set; } = new DateTime();

}
