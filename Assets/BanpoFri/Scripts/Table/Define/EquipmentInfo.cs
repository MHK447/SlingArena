using UnityEngine;
using System.Collections.Generic;

namespace BanpoFri
{
    [System.Serializable]
    public class EquipmentInfoData
    {
        [SerializeField]
		private int _equip_idx;
		public int equip_idx
		{
			get { return _equip_idx;}
			set { _equip_idx = value;}
		}
		[SerializeField]
		private int _equip_type;
		public int equip_type
		{
			get { return _equip_type;}
			set { _equip_type = value;}
		}
		[SerializeField]
		private List<int> _status_type;
		public List<int> status_type
		{
			get { return _status_type;}
			set { _status_type = value;}
		}
		[SerializeField]
		private List<int> _status_value;
		public List<int> status_value
		{
			get { return _status_value;}
			set { _status_value = value;}
		}
		[SerializeField]
		private int _grade;
		public int grade
		{
			get { return _grade;}
			set { _grade = value;}
		}
		[SerializeField]
		private string _name;
		public string name
		{
			get { return _name;}
			set { _name = value;}
		}
		[SerializeField]
		private string _desc;
		public string desc
		{
			get { return _desc;}
			set { _desc = value;}
		}
		[SerializeField]
		private string _img;
		public string img
		{
			get { return _img;}
			set { _img = value;}
		}

    }

    [System.Serializable]
    public class EquipmentInfo : Table<EquipmentInfoData, KeyValuePair<int,int>>
    {
    }
}

