using HarmonyLib;
using UnityEngine;

namespace SilkHpBar_PRO
{
	// 决定血条类似是Boss还是普通小怪
	public class HealthBarType : MonoBehaviour
	{
        public enum BarType
        {
            Normal,
            Boss
        }
        public float lastHp;
        public BarType barType = BarType.Normal;
    }

	// 使用 Harmony 补丁在游戏运行时修改 HealthManager 类的行为
	[HarmonyPatch]
	public class Patch
	{
		[HarmonyPatch(typeof(HealthManager), "Awake")]
		[HarmonyPostfix]
		public static void HealthManagerPatch(HealthManager __instance)
		{
			int maxHealth = __instance.hp;
			if (maxHealth >= 5)
			{
				HealthBarType healthBarData = __instance.gameObject.AddComponent<HealthBarType>();
				if (maxHealth > Plugin.BossHealthThreshold.Value)
				{
					__instance.gameObject.AddComponent<BossHealthBar>();
					healthBarData.barType = HealthBarType.BarType.Boss;
				}
				else
				{
					__instance.gameObject.AddComponent<HealthBar>();
					healthBarData.barType = HealthBarType.BarType.Normal;
				}
			}
		}

		[HarmonyPatch(typeof(HealthManager), "TakeDamage")]
		[HarmonyPrefix]
		public static void TakeDamagePrefix(HealthManager __instance)
		{
			HealthBarType component = __instance.GetComponent<HealthBarType>();
			if (!(component == null))
			{
				component.lastHp = (float)__instance.hp;
			}
		}

		[HarmonyPatch(typeof(HealthManager), "TakeDamage")]
		[HarmonyPostfix]
		public static void TakeDamagePatch(HealthManager __instance)
		{
			HealthBarType component = __instance.GetComponent<HealthBarType>();
			if (!(component == null))
			{
				float num = component.lastHp - (float)__instance.hp;
				if (component.lastHp - (float)__instance.hp != 0f)
				{
					DamageText.Instance.ShowDamageText(__instance.transform.position, num);
				}
				if (component.barType == HealthBarType.BarType.Boss)
				{
					BossHealthBar bossHealthBar = __instance.GetComponent<BossHealthBar>() ?? __instance.gameObject.AddComponent<BossHealthBar>();
					bossHealthBar.OnTakeDamage();
					return;
				}
				HealthBar healthBar = __instance.GetComponent<HealthBar>() ?? __instance.gameObject.AddComponent<HealthBar>();
				healthBar.OnTakeDamage();
			}
		}
	}
}
