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
		public bool IsExpanding()
		{
			return this.isExpanding;
		}

		private void Awake()
		{
			if (base.GetComponents<BossHealthBar>().Length > 1)
			{
                Destroy(this);
				return;
			}
			HealthBar component = base.GetComponent<HealthBar>();
			if (component != null)
			{
                Destroy(component);
			}
			this.healthManager = base.GetComponent<HealthManager>();
			if (base.gameObject.name == "Silk Boss" && this.healthManager.hp >= 1000)
			{
				this.maxHealth = 100f;
				this.lastHealth = 100f;
			}
			else
			{
				this.maxHealth = (float)this.healthManager.hp;
				this.lastHealth = (float)this.healthManager.hp;
			}
			Plugin.Log.LogInfo("Boss对象名:" + base.gameObject.name);
			this.playerTransform = global::UnityEngine.Object.FindFirstObjectByType<HeroController>().transform;
			this.fsm = base.gameObject.GetComponent<PlayMakerFSM>();
		}

		private void Update()
		{
			if (!(this.healthManager == null) && Plugin.isBossHealthBar.Value)
			{
				if (BossHealthBarManager.Instance.GetActiveHealthBarCount() < 2)
				{
					this.SetBossName(false);
				}
				float num = (float)this.healthManager.hp;
				this.UpdateMaxHealth();
				if (num != this.lastHealth)
				{
					this.lastHealth = num;
					if (this.healthBarUI == null)
					{
						this.CreateHealthBarUI();
					}
					this.UpdateHealthBar();
					this.ShowHealthBar();
				}
				this.CheckVisibilityConditions();
			}
		}

		private void CreateHealthBarUI()
		{
			if (this.healthBarUI != null || !Plugin.isBossHealthBar.Value || !BossHealthBarManager.Instance.RegisterHealthBar(this)) return;
			try
			{
				// 提取常量以增强可维护性
				const string BG_PATH = "BG";
				const string FILL_PATH = "Fill";
				const string BOSS_NAME_PATH = "BossName";
				const string LEFT_PATH = "Left";
				const string RIGHT_PATH = "Right";

				this.healthBarUI = Instantiate(Plugin.bossHealthBarPrefab);
				this.canvas = this.healthBarUI.GetComponent<Canvas>();
				if (this.canvas != null)
				{
					this.canvas.renderMode = RenderMode.ScreenSpaceOverlay;
					this.canvas.sortingOrder = 100;
				}

				CanvasScaler component = this.healthBarUI.GetComponent<CanvasScaler>();
				if (component != null)
				{
					DestroyImmediate(component);
				}

				Transform bgTransform = this.healthBarUI.transform.Find(BG_PATH);
				if (bgTransform == null)
				{
					Debug.LogError("在'healthBar'UI中找不到'BG'。");
					return;
				}

				this.bgImage = bgTransform.GetComponent<Image>();
				if (this.bgImage != null)
				{
					this.bgImage.color = new Color(this.bgImage.color.r, this.bgImage.color.g, this.bgImage.color.b, 0.25f);
					this.originalSize = this.bgImage.rectTransform.sizeDelta;
				}

				Transform fillTransform = bgTransform.Find(FILL_PATH);
				if (fillTransform != null)
				{
					this.fillImage = fillTransform.GetComponent<Image>();
					if (this.fillImage != null)
					{
						this.fillImage.type = Image.Type.Filled;
						this.fillImage.fillMethod = Image.FillMethod.Horizontal;
					}
				}

				Transform bossNameTransform = bgTransform.Find(BOSS_NAME_PATH);
				if (bossNameTransform != null)
				{
					this.bossNameText = bossNameTransform.GetComponent<Text>();
					Transform leftTransform = bossNameTransform.Find(LEFT_PATH);
					if (leftTransform != null)
						this.leftImage = leftTransform.GetComponent<Image>();

					Transform rightTransform = bossNameTransform.Find(RIGHT_PATH);
					if (rightTransform != null)
						this.rightImage = rightTransform.GetComponent<Image>();
				}

				this.SetupUILayout();

				if (this.bossNameText != null)
				{
					this.bossNameText.text = this.GetBossName();
					this.bossNameText.fontSize = 60;
				}

				this.UpdateNameLayout();
				this.UpdateBossTitle();
				BossHealthBarManager.Instance.ArrangeHealthBars();
				this.HideHealthBar();

				if (BossHealthBarManager.Instance.GetActiveHealthBarCount() - 1 > 0)
				{
					this.SetBossName(true);
				}
			}
			catch (Exception ex)
			{
				Debug.LogError($"创建Boss血条时出错 UI: {ex.Message}");
				// 可选：销毁已创建的部分UI以避免残缺对象
				if (this.healthBarUI != null)
				{
					DestroyImmediate(this.healthBarUI);
					this.healthBarUI = null;
				}
			}
		}

		private void ShowHealthBar()
		{
			if (this.healthBarUI != null)
			{
				this.healthBarUI.SetActive(true);
				if (!this.hasExpanded && !this.isExpanding)
				{
					base.StartCoroutine(this.ExpandHealthBarAnimation());
				}
			}
		}

		private IEnumerator ExpandHealthBarAnimation()
		{
			this.isExpanding = true;
			float duration = 0.5f;
			float elapsed = 0f;
			if (this.bgImage != null)
			{
				this.bgImage.rectTransform.sizeDelta = new Vector2(0f, this.originalSize.y);
			}
			if (this.fillImage != null)
			{
				this.fillImage.fillAmount = 0f;
			}
			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				float num = Mathf.Clamp01(elapsed / duration);
				if (this.bgImage != null)
				{
					this.bgImage.rectTransform.sizeDelta = new Vector2(this.originalSize.x * num, this.originalSize.y);
				}
				if (this.fillImage != null)
				{
					this.fillImage.fillAmount = (float)this.healthManager.hp / this.maxHealth * num;
				}
				yield return null;
			}
			if (this.bgImage != null)
			{
				this.bgImage.rectTransform.sizeDelta = this.originalSize;
			}
			if (this.fillImage != null)
			{
				this.fillImage.fillAmount = (float)this.healthManager.hp / this.maxHealth;
			}
			this.isExpanding = false;
			this.hasExpanded = true;
			yield break;
		}

		private void HideHealthBar()
		{
			this.healthBarUI?.SetActive(false);
		}

		private void SetupUILayout()
		{
			if (this.bgImage != null)
			{
				RectTransform rectTransform = this.bgImage.rectTransform;
				rectTransform.anchorMin = new Vector2(0.5f, 0f);
				rectTransform.anchorMax = new Vector2(0.5f, 0f);
				rectTransform.pivot = new Vector2(0.5f, 0f);
				rectTransform.anchoredPosition = new Vector2(Plugin.BossPositionX.Value, Screen.height * 0.82f + Plugin.BossPositionY.Value);
				rectTransform.localScale = Vector3.one * BossHealthBar.baseScale * Plugin.BossScale.Value;
				rectTransform.sizeDelta = new Vector2(1572f, 35f) + new Vector2(Plugin.BossWidthOffset.Value, Plugin.BossHeightOffset.Value);
				RectTransform component = rectTransform.Find("Fill").GetComponent<RectTransform>();
				component.sizeDelta = rectTransform.sizeDelta;
				Transform transform = component.Find("Right");
				RectTransform rectTransform2 = transform != null ? transform.GetComponent<RectTransform>() : null;
				Transform transform2 = component.Find("Left");
				RectTransform rectTransform3 = transform2 != null ? transform2.GetComponent<RectTransform>() : null;
				if (rectTransform3 != null && rectTransform2 != null)
				{
					float x = component.sizeDelta.x;
					float num = 65;
					float num2 = x / 2;
					rectTransform3.anchoredPosition = new Vector2(-num2 - num, rectTransform3.anchoredPosition.y);
					rectTransform2.anchoredPosition = new Vector2(num2 + num, rectTransform2.anchoredPosition.y);
				}
			}
		}

		public void RefreshLayout()
		{
			this.SetupUILayout();
		}

		private void UpdateNameLayout()
		{
			if (!(this.bossNameText == null) && !(this.leftImage == null) && !(this.rightImage == null))
			{
				Vector2 vector;
				try
				{
					TextGenerator textGenerator = new TextGenerator();
					TextGenerationSettings generationSettings = this.bossNameText.GetGenerationSettings(Vector2.zero);
					float preferredWidth = textGenerator.GetPreferredWidth(this.bossNameText.text, generationSettings);
					float preferredHeight = textGenerator.GetPreferredHeight(this.bossNameText.text, generationSettings);
					vector = new Vector2(preferredWidth, preferredHeight);
				}
				catch (Exception)
				{
					vector = new Vector2(200f, 30f);
				}
				RectTransform rectTransform = this.bossNameText.rectTransform;
				rectTransform.sizeDelta = new Vector2(vector.x, rectTransform.sizeDelta.y);
				RectTransform rectTransform2 = this.leftImage.rectTransform;
				RectTransform rectTransform3 = this.rightImage.rectTransform;
				float num = vector.x * 0.5f;
				rectTransform2.anchoredPosition = new Vector2(-num - this.fixedDistance, 0f);
				rectTransform3.anchoredPosition = new Vector2(num + this.fixedDistance, 0f);
			}
		}

		private void UpdateHealthBar()
		{
			if (!(this.fillImage == null) && !(this.healthManager == null))
			{
				float num = (float)this.healthManager.hp / this.maxHealth;
				this.fillImage.fillAmount = num;
			}
		}

		private void UpdateMaxHealth()
		{
			if (!(this.healthManager == null))
			{
				float num = (float)this.healthManager.hp;
				if (num > this.maxHealth && num <= 3000f)
				{
					this.maxHealth = num;
				}
			}
		}

		private void CheckVisibilityConditions()
		{
			if (!(this.healthBarUI == null) && !(this.playerTransform == null))
			{
				bool flag = true;
				if (Vector3.Distance(base.transform.position, this.playerTransform.position) > this.detectionDistance)
				{
					flag = false;
				}
				if (this.healthBarUI.activeSelf != flag)
				{
					if (flag)
					{
						this.ShowHealthBar();
						return;
					}
					this.HideHealthBar();
				}
			}
		}

		public void OnTakeDamage()
		{
			if (this.healthBarUI != null)
			{
				this.ShowHealthBar();
			}
		}

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

		private string GetLocalizedAreaName(MapZone mapZone)
		{
			try
			{
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
			return mapZone.ToString();
		}

		private void UpdateBossTitle()
		{
			try
			{
				MapZone currentMapZone = this.GetCurrentMapZone();
				string localizedAreaName = this.GetLocalizedAreaName(currentMapZone);
				if (this.healthBarUI != null)
				{
					Transform transform = this.healthBarUI.transform.Find("BG");
					if (transform != null)
					{
						Transform transform2 = transform.Find("BossName/Title");
						if (transform2 != null)
						{
							Text component = transform2.GetComponent<Text>();
							if (component != null)
							{
								component.text = Plugin.BossNeamPrefix.Value + localizedAreaName;
								component.fontSize -= 10;
								Plugin.Log.LogInfo(string.Format("Boss标题已更新: {0}, 字体大小: {1}", component.text, component.fontSize));
							}
							else
							{
								Plugin.Log.LogWarning("未找到Title对象的Text组件");
							}
						}
						else
						{
							Plugin.Log.LogWarning("未找到BG对象下的Title对象");
						}
					}
					else
					{
						Plugin.Log.LogWarning("未找到healthBarUI下的BG对象");
					}
				}
			}
			catch (Exception ex)
			{
				Plugin.Log.LogError("更新BOSS标题失败: " + ex.Message);
			}
		}

		private string GetBossName()
		{
			string text = this.healthManager.gameObject.name;
			try
			{
				// 尝试通过 EnemyJournalManager 匹配 displayName
				List<EnemyJournalRecord> allEnemies = EnemyJournalManager.GetAllEnemies();
				string name = this.healthManager.gameObject.name;
				string[] nameParts = name.Split(new char[] { ' ', '(', ')', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
		
				foreach (EnemyJournalRecord record in allEnemies)
				{
					if (record == null || string.IsNullOrEmpty(record.name)) continue;
		
					string[] recordParts = record.name.Split(new char[] { ' ', '(', ')', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
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
						Plugin.Log.LogInfo(string.Format("匹配到Boss名称: {0} (匹配单词数: {1})", record.DisplayName, matchCount)); //通过EnemyJournalManager匹配
						return record.DisplayName;
					}
				}
		
				text = this.CleanGameObjectName(this.healthManager.gameObject.name);
			}
			catch (Exception ex)
			{
				Plugin.Log.LogWarning("获取Boss名称时发生异常: " + ex.Message);
				text = this.CleanGameObjectName(this.healthManager.gameObject.name);
			}
		
			return text;
		}

		private string CleanGameObjectName(string originalName)
		{
			string text;
			if (string.IsNullOrEmpty(originalName))
			{
				text = "未知Boss";
			}
			else
			{
				int num = originalName.IndexOf('(');
				if (num >= 0)
				{
					originalName = originalName.Substring(0, num).Trim();
				}
				foreach (string text2 in new string[] { " Clone", "(Clone)", " Instance", "(Instance)" })
				{
					if (originalName.EndsWith(text2))
					{
						originalName = originalName.Substring(0, originalName.Length - text2.Length).Trim();
					}
				}
				text = string.IsNullOrEmpty(originalName) ? "未知Boss" : originalName;
			}
			return text;
		}

		public RectTransform GetHealthBarTransform()
		{
			RectTransform rectTransform;
			if (this.healthBarUI != null && this.bgImage != null)
			{
				rectTransform = this.bgImage.rectTransform;
			}
			else
			{
				rectTransform = null;
			}
			return rectTransform;
		}

		public void SetBossName(bool isHide)
		{
			this.bossNameText?.rectTransform.gameObject.SetActive(!isHide);
		}

		private void OnDestroy()
		{
			BossHealthBarManager.Instance?.UnregisterHealthBar(this);
			if (this.healthBarUI != null)
			{
                Destroy(this.healthBarUI);
			}
		}

		public BossHealthBar()
		{
			this.detectionDistance = 50f;
			this.fixedDistance = 150f;
			this.isExpanding = false;
			this.hasExpanded = false;
		}

		static BossHealthBar()
		{
			BossHealthBar.baseScale = 0.5f;
		}
		private HealthManager healthManager;
		private float lastHealth;
		private float maxHealth;
		public float detectionDistance;
		private Transform playerTransform;
		private GameObject healthBarUI;
		private Canvas canvas;
		private Image bgImage;
		private Image fillImage;
		private Text bossNameText;
		private Image leftImage;
		private Image rightImage;
		public static float baseScale;
		private readonly float fixedDistance;
		private bool isExpanding;
		private bool hasExpanded;
		private Vector2 originalSize;
		private PlayMakerFSM fsm;
	}
}
