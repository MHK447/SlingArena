using UnityEngine;
using System.Collections.Generic;
using BanpoFri;
using System.Linq;

namespace BanpoFri
{
    [System.Serializable]
    public class TrainingInfoData
    {
        [SerializeField]
		private int _idx;
		public int idx
		{
			get { return _idx;}
			set { _idx = value;}
		}
		[SerializeField]
		private float _base_value;
		public float base_value
		{
			get { return _base_value;}
			set { _base_value = value;}
		}
		[SerializeField]
		private float _value_increase;
		public float value_increase
		{
			get { return _value_increase;}
			set { _value_increase = value;}
		}
		[SerializeField]
		private int _base_cost;
		public int base_cost
		{
			get { return _base_cost;}
			set { _base_cost = value;}
		}
		[SerializeField]
		private int _cost_increase;
		public int cost_increase
		{
			get { return _cost_increase;}
			set { _cost_increase = value;}
		}

    }

    [System.Serializable]
    public class TrainingInfo : Table<TrainingInfoData, int>
    {
    }
}
