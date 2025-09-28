using UnityEngine;

namespace SilkHpBar_PRO
{
	public class HealthBarData : MonoBehaviour
	{
		public HealthBarData()
		{
			this.barType = BarType.Normal;
		}
		public float lastHp;
		public BarType barType;
		public enum BarType
		{
			Normal,
			Boss
		}
	}
}
