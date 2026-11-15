using UnityEngine;
using System.Collections.Generic;

namespace BanpoFri
{
    [System.Serializable]
    public class WaveInfoData
    {
        [SerializeField]
		private int _stage_idx;
		public int stage_idx
		{
			get { return _stage_idx;}
			set { _stage_idx = value;}
		}
		[SerializeField]
		private int _wave_idx;
		public int wave_idx
		{
			get { return _wave_idx;}
			set { _wave_idx = value;}
		}
		[SerializeField]
		private int _spawn_idx;
		public int spawn_idx
		{
			get { return _spawn_idx;}
			set { _spawn_idx = value;}
		}
		[SerializeField]
		private List<int> _enemy_unit_idx;
		public List<int> enemy_unit_idx
		{
			get { return _enemy_unit_idx;}
			set { _enemy_unit_idx = value;}
		}
		[SerializeField]
		private List<int> _enemy_hp;
		public List<int> enemy_hp
		{
			get { return _enemy_hp;}
			set { _enemy_hp = value;}
		}
		[SerializeField]
		private List<int> _enemy_atk;
		public List<int> enemy_atk
		{
			get { return _enemy_atk;}
			set { _enemy_atk = value;}
		}
		[SerializeField]
		private int _select_reward_type;
		public int select_reward_type
		{
			get { return _select_reward_type;}
			set { _select_reward_type = value;}
		}
		[SerializeField]
		private int _select_reward_idx;
		public int select_reward_idx
		{
			get { return _select_reward_idx;}
			set { _select_reward_idx = value;}
		}
		[SerializeField]
		private int _select_reward_value;
		public int select_reward_value
		{
			get { return _select_reward_value;}
			set { _select_reward_value = value;}
		}

    }

    [System.Serializable]
    public class WaveInfo : Table<WaveInfoData, KeyValuePair<int,int>>
    {
    }
}

