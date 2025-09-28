using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using TMProOld;
using UnityEngine;

namespace SilkHpBar_PRO
{
	[BepInPlugin("com.xiaohai.HealthBarPro", "HealthBarPro", "1.0.2")]
	public class Plugin : BaseUnityPlugin
	{
		private void Awake()
		{
			this.ScanAvailableResourceFiles();
			Log = Logger;
			ResourcePackSelection = Config.Bind<string>("资源包设置", "资源包选择", "无",
			new ConfigDescription("选择要使用的资源包", new AcceptableValueList<string>(this.GetAvailableResourceOptions())));
			BossHealthThreshold = Config.Bind<int>("BOSS识别设置", "血量阈值", 119, "血量大于此值的敌人将被识别为Boss并使用Boss血条");
			isBossHealthBar = Config.Bind<bool>("BOSS血条调整", "*显示Boss血条", true, "是否显示Boss血条");
			BossPositionX = Config.Bind<short>("BOSS血条调整", "位置偏移(左右)", 0, new ConfigDescription("BOSS血条左右位置偏移量", new AcceptableValueRange<short>(-810, 810)));
			BossPositionY = Config.Bind<short>("BOSS血条调整", "位置偏移(上下)", 0, new ConfigDescription("BOSS血条上下位置偏移量", new AcceptableValueRange<short>(-100, 1000)));
			BossScale = Config.Bind<float>("BOSS血条调整", "缩放倍数", 1f, "BOSS血条缩放倍数");
			BossWidthOffset = Config.Bind<float>("BOSS血条调整", "宽度偏移", 0f, "BOSS血条宽度偏移量");
			BossHeightOffset = Config.Bind<float>("BOSS血条调整", "高度偏移", 0f, "BOSS血条高度偏移量");
			BossNeamPrefix = Config.Bind<string>("BOSS血条调整", "名称前缀", "盘踞于", "BOSS名称前缀");
			BossExpandDuration = Config.Bind<float>("BOSS血条调整", "延迟展开时长", 1f, "BOSS血条从左到右展开的动画时长（秒）");
			isNormal = Config.Bind<bool>("小怪血条调整", "*显示小怪血条", true, "是否显示小怪血条");
			NormalPositionX = Config.Bind<short>("小怪血条调整", "位置偏移(左右)", 0, new ConfigDescription("小怪血条左右位置偏移量", new AcceptableValueRange<short>(-200, 200)));
			NormalPositionY = Config.Bind<short>("小怪血条调整", "位置偏移(上下)", 0, new ConfigDescription("小怪血条上下位置偏移量", new AcceptableValueRange<short>(-200, 200)));
			NormalScale = Config.Bind<float>("小怪血条调整", "缩放倍数", 1f, "小怪血条缩放倍数");
			NormalWidthOffset = Config.Bind<float>("小怪血条调整", "宽度偏移", 0f, "小怪血条宽度偏移量");
			NormalHeightOffset = Config.Bind<float>("小怪血条调整", "高度偏移", 0f, "小怪血条高度偏移量");
			NormalHideDelay = Config.Bind<float>("小怪血条调整", "延迟隐藏时长", 3f, "小怪血条在受到伤害后持续显示的时间（秒）");
			isDamageText = Config.Bind<bool>("伤害文本", "*显示伤害字体", true, "是否显示伤害字体");
			DamageTextDuration = Config.Bind<float>("伤害文本", "伤害显示时长", 2f, "伤害文本显示持续时间（秒）");
			DamageTextFontSize = Config.Bind<int>("伤害文本", "伤害字体大小", 55, "伤害文本字体大小");
			DamageTextColor = Config.Bind<Color>("伤害文本", "伤害字体颜色", Color.white, new ConfigDescription("伤害文本颜色"));
			DamageTextUseSign = Config.Bind<bool>("伤害文本", "伤害字体前缀", true, "伤害文本是否显示符号?(Plus:+, Minus:-)");
			this.LoadAssetBundle();
			new Harmony("com.xiaohai.HealthBarPro").PatchAll();
			foreach (TMP_FontAsset tmp_FontAsset in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
			{
				Log.LogInfo("字体找到: " + tmp_FontAsset.name);
			}
		}

		private void ScanAvailableResourceFiles()
		{
			availableResourceFiles.Clear();
			availableResourceFiles.Add("无");

			if (Directory.Exists(resourcesFolderPath))
			{
				try
				{
					string[] files = Directory.GetFiles(resourcesFolderPath);
					foreach (string file in files)
					{
						string fileName = Path.GetFileName(file);
						// 直接将文件名(不含扩展名)作为显示名称
						string displayName = Path.GetFileNameWithoutExtension(fileName);
						if (!string.IsNullOrEmpty(displayName))
						{
							availableResourceFiles.Add(displayName);
						}
					}
				}
				catch (Exception ex)
				{
					Log.LogError($"扫描资源文件时出错: {ex.Message}");
				}
			}
			else
			{
				Log.LogWarning("Resources文件夹不存在");
			}
		}
		private string[] GetAvailableResourceOptions()
		{
			return availableResourceFiles.ToArray();
		}

		private void LoadAssetBundle()
		{
			string resourcePath = "";

			// 根据配置选择资源路径 - 直接使用文件名
			if (ResourcePackSelection.Value == "无")
			{
				resourcePath = Path.Combine(resourcesFolderPath, "healthbar_pro");
			}
			else
			{
				// 直接使用选择的名称构建资源路径
				resourcePath = Path.Combine(resourcesFolderPath, ResourcePackSelection.Value);
			}

			// 检查资源是否存在，如果不存在则回退到默认
			if (!File.Exists(resourcePath))
			{
				Log.LogWarning($"资源文件不存在: {resourcePath}");

				// 如果当前选择不是"无"，则回退到"无"
				if (ResourcePackSelection.Value != "无")
				{
					Log.LogWarning("资源文件不存在，回退到默认资源");
					ResourcePackSelection.Value = "无";
					resourcePath = Path.Combine(resourcesFolderPath, "healthbar_pro");

					// 再次检查默认资源
					if (!File.Exists(resourcePath))
					{
						Log.LogError("默认资源文件 healthbar_pro 也不存在");
						return;
					}
				}
				else
				{
					Log.LogError("默认资源文件 healthbar_pro 不存在");
					return;
				}
			}

			healthBarAssetBundle = AssetBundle.LoadFromFile(resourcePath);
			if (healthBarAssetBundle == null)
			{
				Log.LogError($"无法加载资源包: {resourcePath}");
				return;
			}

			Log.LogInfo($"成功加载资源包: (名称: {ResourcePackSelection.Value})");

			bossHealthBarPrefab = healthBarAssetBundle.LoadAsset<GameObject>("BossHealthBar.prefab");
			if (bossHealthBarPrefab == null)
			{
				Log.LogError("BossHealthBar.prefab 未在资源包中找到");
			}
			else
			{
				Log.LogInfo("成功加载 BossHealthBar.prefab");
			}

			healthBarPrefab = healthBarAssetBundle.LoadAsset<GameObject>("HealthBar.prefab");
			if (healthBarPrefab == null)
			{
				Log.LogError("HealthBar.prefab 未在资源包中找到");
			}
			else
			{
				Log.LogInfo("成功加载 HealthBar.prefab");
			}
		}

		private void Update()
		{
			if (Input.GetKey(KeyCode.F5))
			{
				this.RefreshAllBossHealthBars();
				this.RefreshAllNormalHealthBars();
			}
		}

		private void RefreshAllBossHealthBars()
		{
			BossHealthBar[] array = FindObjectsByType<BossHealthBar>(FindObjectsSortMode.None);
			for (int i = 0; i < array.Length; i++)
			{
				array[i].RefreshLayout();
			}
		}

		private void RefreshAllNormalHealthBars()
		{
			HealthBar[] array = FindObjectsByType<HealthBar>(FindObjectsSortMode.None);
			for (int i = 0; i < array.Length; i++)
			{
				array[i].RefreshLayout();
			}
		}

		private readonly string resourcesFolderPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources");
		private void OnDestroy()
		{
            healthBarAssetBundle?.Unload(true);
            healthBarAssetBundle = null;
        }

		public const string PLUGIN_GUID = "com.xiaohai.HealthBarPro";
		public const string PLUGIN_NAME = "HealthBarPro";
		public const string PLUGIN_VERSION = "1.0.2";
		public static Harmony harmony;
		public static ManualLogSource Log;
		public static ConfigEntry<int> BossHealthThreshold;
		public static ConfigEntry<short> BossPositionX;
		public static ConfigEntry<short> BossPositionY;
		public static ConfigEntry<float> BossScale;
		public static ConfigEntry<float> BossWidthOffset;
		public static ConfigEntry<float> BossHeightOffset;
		public static ConfigEntry<float> BossExpandDuration;
		public static ConfigEntry<short> NormalPositionX;
		public static ConfigEntry<short> NormalPositionY;
		public static ConfigEntry<float> NormalScale;
		public static ConfigEntry<float> NormalWidthOffset;
		public static ConfigEntry<float> NormalHeightOffset;
		public static ConfigEntry<float> NormalHideDelay;
		public static ConfigEntry<float> DamageTextDuration;
		public static ConfigEntry<int> DamageTextFontSize;
		public static ConfigEntry<Color> DamageTextColor;
		public static ConfigEntry<bool> DamageTextUseSign;
		public static AssetBundle healthBarAssetBundle;
		public static ConfigEntry<bool> isBossHealthBar;
		public static ConfigEntry<bool> isDamageText;
		public static ConfigEntry<bool> isNormal;
		public static ConfigEntry<string> BossNeamPrefix;
		public static ConfigEntry<string> ResourcePackSelection;
		private readonly List<string> availableResourceFiles = new List<string>();
		public static GameObject bossHealthBarPrefab;
		public static GameObject healthBarPrefab;
	}
}
