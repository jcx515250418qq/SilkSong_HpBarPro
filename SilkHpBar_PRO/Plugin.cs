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
	[BepInPlugin("com.xiaohai.HealthBarPro", "HealthBarPro", "1.0.5")]
	public class Plugin : BaseUnityPlugin
	{
		public static Harmony harmony;
		public static ManualLogSource Log;
		// Boss血条参数
		public static ConfigEntry<bool> IsBossHealthBar;
		public static ConfigEntry<int> BossHealthThreshold;
		public static ConfigEntry<float> BossPositionX;
		public static ConfigEntry<float> BossPositionY;
		public static ConfigEntry<float> BossScale;
		public static ConfigEntry<float> BossWidthOffset;
		public static ConfigEntry<float> BossHeightOffset;
		public static ConfigEntry<float> BossExpandDuration;
		public static ConfigEntry<string> BossNeamPrefix;
		// 普通血条参数
		public static ConfigEntry<bool> IsNormal;
		public static ConfigEntry<short> NormalPositionX;
		public static ConfigEntry<short> NormalPositionY;
		public static ConfigEntry<float> NormalScale;
		public static ConfigEntry<float> NormalWidthOffset;
		public static ConfigEntry<float> NormalHeightOffset;
		public static ConfigEntry<float> NormalHideDelay;
		// 伤害显示参数
		public static ConfigEntry<bool> IsDamageText;
		public static ConfigEntry<float> DamageTextDuration;
		public static ConfigEntry<int> DamageTextFontSize;
		public static ConfigEntry<Color> DamageTextColor;
		public static ConfigEntry<bool> DamageTextUseSign;
		// 其他
		public static ConfigEntry<KeyboardShortcut> RefreshKey;
		public static ConfigEntry<string> ResourcePackSelection;
		public static AssetBundle healthBarAssetBundle;
		public static GameObject bossHealthBarPrefab;
		public static GameObject healthBarPrefab;
		// 内置参数
		private string lastResourcePackSelection;
		private int lastBossConfigHash = 0;
		private int lastNormalConfigHash = 0;
		private readonly List<string> availableResourceFiles = new List<string>();
		private readonly string resourcesFolderPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Resources");

		private void Awake()
		{
			this.ScanAvailableResourceFiles();
			Log = Logger;
			// 配置设置
			ResourcePackSelection = Config.Bind<string>("资源包设置", "资源包选择", "无",
			new ConfigDescription("选择要使用的资源包,*注:不要再战斗中更换", new AcceptableValueList<string>(this.GetAvailableResourceOptions())));
			BossHealthThreshold = Config.Bind<int>("BOSS识别设置", "血量阈值", 119, "血量大于此值的敌人将被识别为Boss并使用Boss血条");
			RefreshKey = Config.Bind<KeyboardShortcut>("配置快捷键", "刷新快捷键", new KeyboardShortcut(KeyCode.F5), "建议修改完配置后按下快捷键刷新，使其生效");

			IsBossHealthBar = Config.Bind<bool>("BOSS血条调整", "*显示Boss血条", true, "是否显示Boss血条");
			BossPositionX = Config.Bind<float>("BOSS血条调整", "位置偏移(左右)", 0.5f, new ConfigDescription("BOSS血条左右位置偏移量", new AcceptableValueRange<float>(0f, 1f)));
			BossPositionY = Config.Bind<float>("BOSS血条调整", "位置偏移(上下)", 0.8f, new ConfigDescription("BOSS血条上下位置偏移量", new AcceptableValueRange<float>(0f, 1f)));
			BossScale = Config.Bind<float>("BOSS血条调整", "缩放倍数", 1f, "BOSS血条缩放倍数");
			BossWidthOffset = Config.Bind<float>("BOSS血条调整", "偏移宽度", 1f, new ConfigDescription("BOSS血条宽度偏移量", new AcceptableValueRange<float>(0f, 2)));
			BossHeightOffset = Config.Bind<float>("BOSS血条调整", "偏移高度", 1f, new ConfigDescription("BOSS血条高度偏移量", new AcceptableValueRange<float>(0f, 2)));
			BossExpandDuration = Config.Bind<float>("BOSS血条调整", "延迟展开时长", 1f, "BOSS血条从左到右展开的动画时长（秒）");
			BossNeamPrefix = Config.Bind<string>("BOSS血条调整", "区域名称前缀", "盘踞于", "BOSS名称前缀");

			IsNormal = Config.Bind<bool>("小怪血条调整", "*显示小怪血条", true, "是否显示小怪血条");
			NormalPositionX = Config.Bind<short>("小怪血条调整", "位置偏移(左右)", 0, new ConfigDescription("小怪血条左右位置偏移量", new AcceptableValueRange<short>(-200, 200)));
			NormalPositionY = Config.Bind<short>("小怪血条调整", "位置偏移(上下)", 0, new ConfigDescription("小怪血条上下位置偏移量", new AcceptableValueRange<short>(-200, 200)));
			NormalScale = Config.Bind<float>("小怪血条调整", "缩放倍数", 1f, "小怪血条缩放倍数");
			NormalWidthOffset = Config.Bind<float>("小怪血条调整", "偏移宽度", 1f, new ConfigDescription("小怪血条宽度偏移量", new AcceptableValueRange<float>(0f, 2)));
			NormalHeightOffset = Config.Bind<float>("小怪血条调整", "偏移高度", 1f, new ConfigDescription("小怪血条高度偏移量", new AcceptableValueRange<float>(0f, 2)));
			NormalHideDelay = Config.Bind<float>("小怪血条调整", "延迟隐藏时长", 3f, "小怪血条在受到伤害后持续显示的时间（秒）");

			IsDamageText = Config.Bind<bool>("伤害文本", "*显示伤害字体", true, "是否显示伤害字体");
			DamageTextDuration = Config.Bind<float>("伤害文本", "伤害显示时长", 2f, "伤害文本显示持续时间（秒）");
			DamageTextFontSize = Config.Bind<int>("伤害文本", "伤害字体大小", 55, "伤害文本字体大小");
			DamageTextColor = Config.Bind<Color>("伤害文本", "伤害字体颜色", Color.white, new ConfigDescription("伤害文本颜色"));
			DamageTextUseSign = Config.Bind<bool>("伤害文本", "伤害字体前缀", true, "伤害文本是否显示符号?(Plus:+, Minus:-)");
			// 加载运行
			lastResourcePackSelection = ResourcePackSelection.Value;
			this.LoadAssetBundle();
			new Harmony("com.xiaohai.HealthBarPro").PatchAll();
			foreach (TMP_FontAsset tmp_FontAsset in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
			{
				Log.LogInfo("字体找到: " + tmp_FontAsset.name);
			}
		}

		private void Update()
		{
			if (lastResourcePackSelection != ResourcePackSelection.Value)
			{
				Log.LogInfo($"资源包选择已更改: {ResourcePackSelection.Value}");
				lastResourcePackSelection = ResourcePackSelection.Value;
				this.LoadAssetBundle();
				this.RefreshAllBossHealthBars();
				this.RefreshAllNormalHealthBars();
			}

			// BOSS配置变更检测
			int currentBossHash = CalculateConfigHash(
				BossPositionX.Value,
				BossPositionY.Value,
				BossScale.Value,
				BossWidthOffset.Value,
				BossHeightOffset.Value,
				BossExpandDuration.Value
			);
			
			if (currentBossHash != lastBossConfigHash)
			{
				lastBossConfigHash = currentBossHash;
				RefreshAllBossHealthBars();
			}

			// 普通血条配置变更检测
			int currentNormalHash = CalculateConfigHash(
				NormalPositionX.Value,
				NormalPositionY.Value,
				NormalScale.Value,
				NormalWidthOffset.Value,
				NormalHeightOffset.Value,
				NormalHideDelay.Value
			);
			
			if (currentNormalHash != lastNormalConfigHash)
			{
				lastNormalConfigHash = currentNormalHash;
				RefreshAllNormalHealthBars();
			}

			if (RefreshKey.Value.IsDown())
			{
				this.RefreshAllBossHealthBars();
				this.RefreshAllNormalHealthBars();
			}
		}
		private int CalculateConfigHash(params object[] values)
		{
			unchecked
			{
				int hash = 17;
				foreach (var value in values)
				{
					hash = hash * 23 + (value?.GetHashCode() ?? 0);
				}
				return hash;
			}
		}

		// 资源文件扫描
		private void ScanAvailableResourceFiles()
		{
			// 确保 Resources 文件夹存在
			if (!Directory.Exists(resourcesFolderPath))
			{
				try
				{
					Directory.CreateDirectory(resourcesFolderPath);
					Log.LogInfo($"已创建 Resources 文件夹: {resourcesFolderPath}");
				}
				catch (Exception ex)
				{
					Log.LogError($"创建 Resources 文件夹失败: {ex.Message}");
					return;
				}
			}

			availableResourceFiles.Clear();
			availableResourceFiles.Add("无");
			try
			{
				string[] files = Directory.GetFiles(resourcesFolderPath);
				foreach (string file in files)
				{
					string fileName = Path.GetFileName(file);
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
		private string[] GetAvailableResourceOptions()
		{
			return availableResourceFiles.ToArray();
		}

		private void LoadAssetBundle()
		{
			string resourcePath = "";
			
			// 根据配置选择资源路径
			if (ResourcePackSelection.Value == "无")
			{
				resourcePath = Path.Combine(resourcesFolderPath, "healthbar_pro");
			}
			else
			{
				resourcePath = Path.Combine(resourcesFolderPath, ResourcePackSelection.Value);
			}
			
			// 如果选择的资源不存在，尝试使用默认资源
			if (!File.Exists(resourcePath))
			{
				Log.LogWarning($"资源文件不存在: {resourcePath}");
				
				// 尝试默认资源
				string defaultResourcePath = Path.Combine(resourcesFolderPath, "healthbar_pro");
				if (File.Exists(defaultResourcePath))
				{
					resourcePath = defaultResourcePath;
					Log.LogInfo("使用默认资源文件");
				}
				else
				{
					Log.LogError("未找到任何可用资源文件");
					// 卸载现有资源
					healthBarAssetBundle?.Unload(true);
					healthBarAssetBundle = null;
					bossHealthBarPrefab = null;
					healthBarPrefab = null;
					return;
				}
			}

            // 如果当前已加载相同资源，则不重复加载
            healthBarAssetBundle?.Unload(true);
            healthBarAssetBundle = null;

            healthBarAssetBundle = AssetBundle.LoadFromFile(resourcePath);
			if (healthBarAssetBundle == null)
			{
				Log.LogError($"无法加载资源包: {resourcePath}");
				return;
			}
			
			Log.LogInfo($"成功加载资源包: (名称: {(ResourcePackSelection.Value == "无" ? "默认" : ResourcePackSelection.Value)})");
			
			bossHealthBarPrefab = healthBarAssetBundle.LoadAsset<GameObject>("BossHealthBar.prefab");
			if (bossHealthBarPrefab == null)
			{
				Log.LogWarning("BossHealthBar.prefab 未在资源包中找到");
			}
			else
			{
				Log.LogInfo("成功加载 BossHealthBar.prefab");
			}
			
			healthBarPrefab = healthBarAssetBundle.LoadAsset<GameObject>("HealthBar.prefab");
			if (healthBarPrefab == null)
			{
				Log.LogWarning("HealthBar.prefab 未在资源包中找到");
			}
			else
			{
				Log.LogInfo("成功加载 HealthBar.prefab");
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
		private void OnDestroy()
		{
			healthBarAssetBundle?.Unload(true);
			healthBarAssetBundle = null;
		}
	}
}
