using GlobalEnums;
using System;
using System.Collections;
using System.Collections.Generic;
using TeamCherry.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace SilkHpBar_PRO
{
	public class BossHealthBar : MonoBehaviour
	{
		private HealthManager healthManager;
		private float lastHealth;
		private float maxHealth;
		public float detectionDistance = 50f;
		private Transform playerTransform;
		private GameObject healthBarUI;
		private Canvas canvas;
		private Image bgImage;
		private Image fillImage;
		private Text bossNameText;
		private Image nameLeftImage;
		private Image nameRightImage;
		public static float baseScale = 0.5f;
		private readonly float fixedDistance = 150f;
		private bool isExpanding = false;
		private bool hasExpanded = false;
		private float screenHeight;
		private float screenWidth;
		private Vector2 bgOriginalSize;
		private Vector2 fillOriginalSize;

		private PlayMakerFSM fsm;
		private HeroController heroController;
		private GameMap gameMap;

		// 公共方法：检查是否正在展开动画
		public bool IsExpanding()
		{
			return isExpanding;
		}

		private void Awake()
		{
			heroController = FindFirstObjectByType<HeroController>();
			playerTransform = heroController?.transform;
			if (GameManager.instance != null)
				gameMap = GameManager.instance.gameMap;

			if (GetComponents<BossHealthBar>().Length > 1)
			{
				Destroy(this);
				return;
			}
			HealthBar component = GetComponent<HealthBar>();
			if (component != null)
			{
				Destroy(component);
			}
			healthManager = GetComponent<HealthManager>();
			if (gameObject.name == "Silk Boss" && healthManager.hp >= 1000)
			{
				maxHealth = 100f;
				lastHealth = 100f;
			}
			else
			{
				maxHealth = (float)healthManager.hp;
				lastHealth = (float)healthManager.hp;
			}
			// Plugin.Log.LogInfo($"Boss对象名:{gameObject.name}");
			playerTransform = FindFirstObjectByType<HeroController>().transform;
			fsm = gameObject.GetComponent<PlayMakerFSM>();
			
			screenHeight = Screen.height;
			screenWidth = Screen.width;
		}

		private void Update()
		{
			// 减少重复调用 BossHealthBarManager.Instance
			var manager = BossHealthBarManager.Instance;
			
			if (healthManager == null || !Plugin.IsBossHealthBar.Value) return;
			
			if (manager.GetActiveHealthBarCount() < 2)
			{
				SetBossName(false);
			}
			
			float currentHealth = (float)healthManager.hp;
			UpdateMaxHealth();

			if (currentHealth != lastHealth)
			{
				lastHealth = currentHealth;
				if (healthBarUI == null)
				{
					CreateHealthBarUI();
				}
				UpdateHealthBar();
				ShowHealthBar();
			}
			
			CheckVisibilityConditions();
		}

		private Transform bgTFCached;
		private Transform bossNameTFCached;
		private void CreateHealthBarUI()
		{
			if (healthBarUI != null || !Plugin.IsBossHealthBar.Value || !BossHealthBarManager.Instance.RegisterHealthBar(this)) return;
			try
			{
				// 提取常量以增强可维护性
				const string FILL_PATH = "Fill";
				const string LEFT_PATH = "Left";
				const string RIGHT_PATH = "Right";

				healthBarUI = Instantiate(Plugin.bossHealthBarPrefab);
				canvas = healthBarUI.GetComponent<Canvas>();
				if (canvas != null)
				{
					canvas.renderMode = RenderMode.ScreenSpaceOverlay;
					canvas.sortingOrder = 100;
				}

				CanvasScaler canvasScaler = healthBarUI.GetComponent<CanvasScaler>();
				if (canvasScaler != null)
				{
					DestroyImmediate(canvasScaler);
				}

				bgTFCached = healthBarUI.transform.Find("BG");
				if (bgTFCached == null)
				{
					Plugin.Log.LogError("在'BosshealthBar'UI中找不到'BG'。");
					return;
				}

				bgImage = bgTFCached.GetComponent<Image>();
				if (bgImage != null)
				{
					bgImage.color = new Color(bgImage.color.r, bgImage.color.g, bgImage.color.b, 0.25f);
					bgOriginalSize = bgImage.rectTransform.sizeDelta;
				}

				Transform fillTransform = bgTFCached.Find(FILL_PATH);
				if (fillTransform != null)
				{
					fillImage = fillTransform.GetComponent<Image>();
					if (fillImage != null)
					{
						fillImage.type = Image.Type.Filled;
						fillImage.fillMethod = Image.FillMethod.Horizontal;
						fillOriginalSize = fillImage.rectTransform.sizeDelta;
					}
				}

				bossNameTFCached = bgTFCached.Find("BossName");
				if (bossNameTFCached != null)
				{
					bossNameText = bossNameTFCached.GetComponent<Text>();
					Transform nameLeftTF = bossNameTFCached.Find(LEFT_PATH);
					if (nameLeftTF != null)
						nameLeftImage = nameLeftTF.GetComponent<Image>();

					Transform nameRightTF = bossNameTFCached.Find(RIGHT_PATH);
					if (nameRightTF != null)
						nameRightImage = nameRightTF.GetComponent<Image>();
				}

				SetupUILayout();
				// 保存原始尺寸用于展开动画
				if (bgImage != null)
				{
					bgOriginalSize = bgImage.rectTransform.sizeDelta;
				}

				if (bossNameText != null)
				{
					bossNameText.text = GetBossName();
					bossNameText.fontSize = 60;
				}
				UpdateNameLayout();
				// 更新BOSS标题文本
				UpdateBossTitle();
				BossHealthBarManager.Instance.ArrangeHealthBars();
				HideHealthBar();
				// 确保当前血条的BossName状态正确（如果是第二个血条则隐藏）
				if (BossHealthBarManager.Instance.GetActiveHealthBarCount() - 1 > 0)
				{
					SetBossName(true);
				}
			}
			catch (Exception ex)
			{
				Plugin.Log.LogError($"创建Boss血条时出错 UI: {ex.Message}");
				// 可选：销毁已创建的部分UI以避免残缺对象
				if (healthBarUI != null)
				{
					DestroyImmediate(healthBarUI);
					healthBarUI = null;
				}
			}
		}

		private void ShowHealthBar()
		{
			if (healthBarUI != null)
			{
				healthBarUI.SetActive(true);
				// 如果还没有展开过，启动展开动画
				if (!hasExpanded && !isExpanding)
				{
					StartCoroutine(ExpandHealthBarAnimation());
				}
			}
		}

		// 添加字段缓存动画参数
		private WaitForSeconds animationFrameWait;

		private void InitializeAnimationHelpers()
		{
			if (animationFrameWait == null)
			{
				animationFrameWait = new WaitForSeconds(0.016f); // ~60 FPS
			}
		}

		// BOSS开场展开动画
		private IEnumerator ExpandHealthBarAnimation()
		{
			InitializeAnimationHelpers();
			isExpanding = true;
			float duration = 0.5f;
			float elapsed = 0f;

			if (bgImage != null)
			{
				bgImage.rectTransform.sizeDelta = new Vector2(0f, bgOriginalSize.y);
			}
			if (fillImage != null)
			{
				fillImage.fillAmount = 0f;
			}

			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				float num = Mathf.Clamp01(elapsed / duration);

				if (bgImage != null)
				{
					bgImage.rectTransform.sizeDelta = new Vector2(bgOriginalSize.x * num, bgOriginalSize.y);
				}

				if (fillImage != null)
				{
					fillImage.fillAmount = (float)healthManager.hp / maxHealth * num;
				}

				yield return animationFrameWait; // 使用预创建的等待对象
			}

			if (bgImage != null)
			{
				bgImage.rectTransform.sizeDelta = bgOriginalSize;
			}
			if (fillImage != null)
			{
				fillImage.fillAmount = (float)healthManager.hp / maxHealth;
			}

			isExpanding = false;
			hasExpanded = true;
		}

		private void HideHealthBar()
		{
			healthBarUI?.SetActive(false);
		}

		private void SetupUILayout()
		{
		    if (bgImage == null) return;
		
		    try
		    {
		        RectTransform bgRectTF = bgImage.rectTransform;
				// 总是设置锚点和初始位置，让ArrangeHealthBars后续重新排列
                // 设置锚点为屏幕底部居中
		        bgRectTF.anchorMin = new Vector2(0f, 0f);
		        bgRectTF.anchorMax = new Vector2(0f, 0f);
		        bgRectTF.pivot = new Vector2(0.5f, 0f);
				// 设置初始位置，加上配置的偏移
		        bgRectTF.anchoredPosition = new Vector2(screenWidth * Plugin.BossPositionX.Value, screenHeight * Plugin.BossPositionY.Value);
				// 使用配置的缩放值
		        float scale = baseScale * Plugin.BossScale.Value;
		        bgRectTF.localScale = Vector3.one * scale;
				// 使用配置的宽高偏移
		        Vector2 offset = new Vector2(Plugin.BossWidthOffset.Value, Plugin.BossHeightOffset.Value);
		        bgRectTF.sizeDelta = bgOriginalSize * offset;
		
		        if (fillImage == null) return;
		
		        RectTransform fillRectTF = fillImage.rectTransform;
		        fillRectTF.sizeDelta = fillOriginalSize * offset;
		
		        AdjustSidePosition(fillRectTF, "Left", -1);
		        AdjustSidePosition(fillRectTF, "Right", 1);
		    }
		    catch (Exception ex)
		    {
		        // 可根据项目日志系统替换为具体日志方法
		        Plugin.Log.LogWarning($"Boss SetupUILayout error: {ex.Message}");
		    }
		}

		private void AdjustSidePosition(RectTransform parent, string childName, int direction)
		{
			Transform childTransform = parent.Find(childName);
			if (childTransform == null) return;

			RectTransform rectTF = childTransform.GetComponent<RectTransform>();
			if (rectTF == null) return;

			float halfWidth = parent.sizeDelta.x / 2;
			const short sideOffset = 64; // 可根据需要提取为类常量或配置项

			float targetX = direction * (halfWidth + sideOffset);
			rectTF.anchoredPosition = new Vector2(targetX, rectTF.anchoredPosition.y);
		}

		public void RefreshLayout()
		{
			SetupUILayout();
		}

		// 在类中添加缓存字段
		private TextGenerator textGenerator;
		private TextGenerationSettings generationSettings;

		private void InitializeTextGenerator()
		{
			if (textGenerator == null)
			{
				textGenerator = new TextGenerator();
			}
		}

		private void UpdateNameLayout()
		{
			if (bossNameText != null && nameLeftImage != null && nameRightImage != null)
			{
				Vector2 textSize;
				try
				{
					InitializeTextGenerator();
					if (textGenerator.characterCount == 0 || generationSettings.font != bossNameText.font) 
					{
						generationSettings = bossNameText.GetGenerationSettings(Vector2.zero);
					}
					
					float preferredWidth = textGenerator.GetPreferredWidth(bossNameText.text, generationSettings);
					float preferredHeight = textGenerator.GetPreferredHeight(bossNameText.text, generationSettings);
					textSize = new Vector2(preferredWidth, preferredHeight);
				}
				catch (Exception)
				{
					textSize = new Vector2(200f, 30f);
				}
				
				RectTransform nameTextRectTF = bossNameText.rectTransform;
				nameTextRectTF.sizeDelta = new Vector2(textSize.x, nameTextRectTF.sizeDelta.y);
				RectTransform nameLeftRectTF = nameLeftImage.rectTransform;
				RectTransform nameRightRectTF = nameRightImage.rectTransform;
				float num = textSize.x * 0.5f;
				nameLeftRectTF.anchoredPosition = new Vector2(-num - fixedDistance, 0f);
				nameRightRectTF.anchoredPosition = new Vector2(num + fixedDistance, 0f);
			}
		}

		private void UpdateHealthBar()
		{
			if (fillImage != null && healthManager != null)
			{
				float healthPercent = (float)healthManager.hp / maxHealth;
				fillImage.fillAmount = healthPercent;
			}
		}

		private void UpdateMaxHealth()
		{
			if (!(healthManager == null))
			{
				float currentHp = (float)healthManager.hp;
				// 如果当前血量大于初始最大血量，且不超过3000的上限阈值，则更新最大血量
				if (currentHp > maxHealth && currentHp <= 3000f)
				{
					maxHealth = currentHp;
				}
			}
		}

		private void CheckVisibilityConditions()
		{
			if (healthBarUI != null && playerTransform != null)
			{
				bool shouldShow = true;
				//  检查距离
				if (Vector3.Distance(transform.position, playerTransform.position) > detectionDistance)
				{
					shouldShow = false;
				}

				if (healthBarUI.activeSelf != shouldShow)
				{
					if (shouldShow)
					{
						ShowHealthBar();
						return;
					}
					HideHealthBar();
				}
			}
		}

		public void OnTakeDamage()
		{
			if (healthBarUI != null)
			{
				ShowHealthBar();
			}
		}

		/// 获取当前地图区域
        /// <returns>当前MapZone枚举值</returns>
		private MapZone GetCurrentMapZone()
		{
			try
			{
				GameManager instance = GameManager.instance;
				if (instance != null)
				{
					GameMap gameMap = instance.gameMap;
					if (gameMap != null)
					{
						return gameMap.GetCurrentMapZone();
					}
				}
			}
			catch (Exception ex)
			{
				Plugin.Log.LogError("获取当前MapZone失败: " + ex.Message);
			}
			return MapZone.NONE;
		}

		/// 获取区域的本地化名称
        /// <param name="mapZone">地图区域枚举</param>
        /// <returns>本地化的区域名称</returns>
		private string GetLocalizedAreaName(MapZone mapZone)
		{
			try
			{
				// 根据MapZone枚举获取对应的本地化键值
				string text = mapZone.ToString();
				if (!string.IsNullOrEmpty(text))
				{
					return Language.Get(text, "Map Zones");
				}
			}
			catch (Exception ex)
			{
				Plugin.Log.LogError("获取区域本地化名称失败: " + ex.Message);
			}
			return mapZone.ToString(); // 如果获取失败，返回枚举名称
		}

        /// 更新BOSS血条标题文本
		private Text bossTitleText;
		private void UpdateBossTitle()
		{
		    try
		    {
		        // 缓存查找结果，避免重复 Find
		        if (bossTitleText == null)
		        {
		            if (healthBarUI != null)
		            {
		                Transform bgTransform = healthBarUI.transform.Find("BG");
		                if (bgTransform != null)
		                {
		                    Transform titleTransform = bgTransform.Find("BossName/Title");
		                    if (titleTransform != null)
		                    {
		                        bossTitleText = titleTransform.GetComponent<Text>();
		                        if (bossTitleText == null)
		                        {
		                            Plugin.Log.LogWarning("未找到<Title>对象的<Text>组件");
		                        }
		                    }
		                    else
		                    {
		                        Plugin.Log.LogWarning("未找到<BossHealthBar>.<BG>对象下的<Title>对象");
		                    }
		                }
		                else
		                {
		                    Plugin.Log.LogWarning("未找到<BossHealthBar>对象下的<BG>对象");
		                }
		            }
		        }
		
		        if (bossTitleText != null)
		        {
		            MapZone currentMapZone = GetCurrentMapZone();
		            string localizedAreaName = GetLocalizedAreaName(currentMapZone);
		            // 防止 BossNeamPrefix 为 null
		            string prefix = Plugin.BossNeamPrefix.Value ?? string.Empty;
		            bossTitleText.text = prefix + localizedAreaName;
		            // 防止字体不断减小，改为设置固定值（或只设置一次）
		            if (bossTitleText.fontSize > 10) // 示例判断，可根据需求调整
		            {
		                bossTitleText.fontSize -= 10;
		            }
		
		            Plugin.Log.LogInfo($"Boss标题已更新: {bossTitleText.text}, 字体大小: {bossTitleText.fontSize}");
		        }
		    }
		    catch (Exception ex)
		    {
		        Plugin.Log.LogError("更新BOSS标题失败: " + ex);
		    }
		}

		private readonly char[] nameSeparator = new char[] { ' ', '(', ')', '_', '-' };
		// 提取为类成员
		private string GetBossName()
		{
			string text = healthManager.gameObject.name;
			try
			{
				List<EnemyJournalRecord> allEnemies = EnemyJournalManager.GetAllEnemies();
				string name = healthManager.gameObject.name;
				// 使用预定义的分隔符数组，避免重复创建
				string[] nameParts = name.Split(nameSeparator, StringSplitOptions.RemoveEmptyEntries);
				foreach (EnemyJournalRecord record in allEnemies)
				{
					if (record == null || string.IsNullOrEmpty(record.name)) continue;

					string[] recordParts = record.name.Split(nameSeparator, StringSplitOptions.RemoveEmptyEntries);
					int matchCount = 0;

					foreach (string part in nameParts)
					{
						if (string.IsNullOrEmpty(part) || part.Length < 2) continue;

						foreach (string recordPart in recordParts)
						{
							if (!string.IsNullOrEmpty(recordPart) && recordPart.Length >= 2 &&
								string.Equals(part, recordPart, StringComparison.OrdinalIgnoreCase))
							{
								matchCount++;
								break;
							}
						}
					}

					if (matchCount >= 2 && !string.IsNullOrEmpty(record.DisplayName))
					{
						Plugin.Log.LogInfo(string.Format("匹配到Boss名称: {0} (匹配单词数: {1})", record.DisplayName, matchCount));
						return record.DisplayName;
					}
				}

				text = CleanGameObjectName(healthManager.gameObject.name);
			}
			catch (Exception ex)
			{
				Plugin.Log.LogWarning("获取Boss名称时发生异常: " + ex.Message);
				text = CleanGameObjectName(healthManager.gameObject.name);
			}

			return text;
		}

		private readonly List<string> cloneSuffixes = new List<string>
			{
				" Clone", "(Clone)", " Instance", "(Instance)"
			};
		/// 清理GameObject名称，移除括号内容等
		private string CleanGameObjectName(string originalName)
		{
			if (string.IsNullOrEmpty(originalName))
				return "未知Boss";

			// 移除括号及其内容
			int bracketIndex = originalName.IndexOf('(');
			if (bracketIndex >= 0)
			{
				originalName = originalName.Substring(0, bracketIndex).Trim();
			}

			foreach (string suffix in cloneSuffixes)
			{
				if (originalName.EndsWith(suffix))
				{
					originalName = originalName.Substring(0, originalName.Length - suffix.Length).Trim();
					break; // 找到一个就足够了
				}
			}

			return string.IsNullOrEmpty(originalName) ? "未知Boss" : originalName;
		}

		/// 获取血条的RectTransform，用于位置管理
        /// <returns>血条的RectTransform</returns>
		public RectTransform GetHealthBarTransform()
		{
			if (healthBarUI != null && bgImage != null)
            {
                return bgImage.rectTransform;
            }
            return null;
		}

		/// 隐藏BossName组件（用于多血条时的第二个血条）
		public void SetBossName(bool isHide)
		{
			if (bossNameText != null)
			{
				var gameObject = bossNameText.rectTransform?.gameObject;
				gameObject?.SetActive(!isHide);
			}
		}
		// 销毁预制体
		private void OnDestroy()
		{
			BossHealthBarManager.Instance?.UnregisterHealthBar(this);
			if (healthBarUI != null)
			{
				Destroy(healthBarUI);
			}
		}
	}
}
