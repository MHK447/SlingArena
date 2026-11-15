using UnityEngine;
using System.Collections.Generic;
using BanpoFri;
using System.Linq;

namespace BanpoFri
{
    [System.Serializable]
    public class StageInfoData
    {
        [SerializeField]
		private int _stage_idx;
		public int stage_idx
		{
			get { return _stage_idx;}
			set { _stage_idx = value;}
		}
		[SerializeField]
		private int _stage_deco_idx;
		public int stage_deco_idx
		{
			get { return _stage_deco_idx;}
			set { _stage_deco_idx = value;}
		}
		[SerializeField]
		private string _map_img;
		public string map_img
		{
			get { return _map_img;}
			set { _map_img = value;}
		}
		[SerializeField]
		private int _diffiyculty;
		public int diffiyculty
		{
			get { return _diffiyculty;}
			set { _diffiyculty = value;}
		}
		[SerializeField]
		private int _stage_clear_gold_value;
		public int stage_clear_gold_value
		{
			get { return _stage_clear_gold_value;}
			set { _stage_clear_gold_value = value;}
		}

    }

    [System.Serializable]
    public class StageInfo : Table<StageInfoData, int>
    {
    }
}
