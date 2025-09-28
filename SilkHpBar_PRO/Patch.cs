using HarmonyLib;
using HealthbarPlugin;
using System;

namespace SilkHpBar_PRO
{
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
				HealthBarData healthBarData = __instance.gameObject.AddComponent<HealthBarData>();
				if (maxHealth > Plugin.BossHealthThreshold.Value)
				{
					__instance.gameObject.AddComponent<BossHealthBar>();
					healthBarData.barType = HealthBarData.BarType.Boss;
				}
				else
				{
					__instance.gameObject.AddComponent<HealthBar>();
					healthBarData.barType = HealthBarData.BarType.Normal;
				}
			}
		}

		[HarmonyPatch(typeof(HealthManager), "TakeDamage")]
		[HarmonyPrefix]
		public static void TakeDamagePrefix(HealthManager __instance)
		{
			HealthBarData component = __instance.GetComponent<HealthBarData>();
			if (!(component == null))
			{
				component.lastHp = (float)__instance.hp;
			}
		}

		[HarmonyPatch(typeof(HealthManager), "TakeDamage")]
		[HarmonyPostfix]
		public static void TakeDamagePatch(HealthManager __instance)
		{
			HealthBarData component = __instance.GetComponent<HealthBarData>();
			if (!(component == null))
			{
				float num = component.lastHp - (float)__instance.hp;
				if (component.lastHp - (float)__instance.hp != 0f)
				{
                    DamageTextManager.Instance.ShowDamageText(__instance.transform.position, num);
				}
				if (component.barType == HealthBarData.BarType.Boss)
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
