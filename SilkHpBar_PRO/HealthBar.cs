using UnityEngine;
using UnityEngine.UI;

namespace SilkHpBar_PRO
{
	public class HealthBar : MonoBehaviour
	{
		private HealthManager healthManager;
		private float lastHealth;
		private float maxHealth;
		public float detectionDistance = 15f;
		private Transform playerTransform;
		private Collider2D colliders2D;
		private GameObject healthBarUI;
		private static Canvas sharedCanvas;
		private Image bgImage;
		private Image fillImage;
		private Vector2 bgOriginalSize;
		private Vector2 fillOriginalSize;
		public static float baseScale = 1f;
		private float lastDamageTime;
		private bool isDamageVisible = false;
		private bool isUICreated = false;
		private float screenHeight;
		private float screenWidth;

		private void Awake()
		{
			if (GetComponents<HealthBar>().Length > 1)
			{
				Destroy(this);
			}
			else
			{
				healthManager = GetComponent<HealthManager>();
				maxHealth = (float)healthManager.hp;      //源文件HealthManager类中initHp不予访问无法进行引用
				lastHealth = (float)healthManager.hp;
				playerTransform = FindFirstObjectByType<HeroController>().transform;
				colliders2D = GetComponent<Collider2D>();
				lastDamageTime = Time.time;
			}
			screenHeight = Screen.height;
			screenWidth = Screen.width;
		}

		private void Update()
		{
			if (healthManager != null && Plugin.IsNormal.Value)
			{
				CheckVisibilityConditions();
				// 如果血条UI不存在且未创建过，创建它
				if (healthBarUI == null && !isUICreated)
				{
					CreateHealthBarUI();
				}
				float num = (float)healthManager.hp;
				// 实时更新最大生命值（处理转阶段情况）
				UpdateMaxHealth();
				if (num != lastHealth)
				{
					lastHealth = num;
					UpdateHealthBar();
				}
				// 更新血条位置，使其跟随敌人
				if (healthBarUI != null)
				{
					UpdateHealthBarPosition();
				}
			}
		}

		private void UpdateHealthBarPosition()
		{
			if (healthBarUI != null && sharedCanvas != null)
			{
				// 计算敌人头部的世界坐标
				Vector3 headOffset = GetHeadOffset();
				Vector3 vector = transform.position + headOffset;
				// 将世界坐标转换为屏幕坐标
				Camera maincamera = GameCameras.instance?.mainCamera;
				// 屏幕边界检查
				if (maincamera != null)
				{
					Vector3 screenPosition = maincamera.WorldToScreenPoint(vector);
					if (screenPosition.z < 0f ||
						screenPosition.x < 0f || screenPosition.x > screenWidth || screenPosition.y < 0f || screenPosition.y > screenHeight)
					{
						// 如果超出屏幕边界，隐藏血条
						if (healthBarUI.activeSelf)
						{
							healthBarUI.SetActive(false);
						}
						return;
					}
					// 血条现在是Canvas的子对象，直接转换为Canvas本地坐标
					RectTransform healthBarRect = healthBarUI.GetComponent<RectTransform>();
					RectTransform canvasRect = sharedCanvas.GetComponent<RectTransform>();

                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, null, out Vector2 localPoint))
                    {
                        // 应用位置偏移配置
                        localPoint.x += Plugin.NormalPositionX.Value;
                        localPoint.y += Plugin.NormalPositionY.Value;
                        healthBarRect.localPosition = localPoint;
                    }
                }
				if (!healthBarUI.activeSelf && isDamageVisible)
				{
					healthBarUI.SetActive(true);
				}
			}
		}

		private void CreateHealthBarUI()
		{
		    // 如果已经创建或者不需要显示，则跳过
		    if (healthBarUI != null || !Plugin.IsNormal.Value)
		        return;
		    // 实例化新的 Health Bar UI Prefab
		    healthBarUI = Instantiate(Plugin.healthBarPrefab);
			// 移除预制体自带的Canvas组件及其依赖组件
		    Canvas uiCanvas = healthBarUI.GetComponent<Canvas>();
		    if (uiCanvas != null)
		    {
				// 先移除依赖组件
		        CanvasScaler uiCanvasScaler = healthBarUI.GetComponent<CanvasScaler>();
		        if (uiCanvasScaler != null)
		        {
		            DestroyImmediate(uiCanvasScaler);
		        }
		        GraphicRaycaster uiGraphicRaycaster = healthBarUI.GetComponent<GraphicRaycaster>();
		        if (uiGraphicRaycaster != null)
		        {
		            DestroyImmediate(uiGraphicRaycaster);
		        }
				// 最后移除Canvas
		        DestroyImmediate(uiCanvas);
		    }
			// 创建或获取共享Canvas
		    if (sharedCanvas == null)
		    {
		        GameObject gameObject = new GameObject("SharedHealthBarCanvas");
		        sharedCanvas = gameObject.AddComponent<Canvas>();
		        sharedCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
		        sharedCanvas.sortingOrder = 100;
		        gameObject.AddComponent<CanvasScaler>();
		        gameObject.AddComponent<GraphicRaycaster>();
		    }
		    healthBarUI.transform.SetParent(sharedCanvas.transform, false);

		    Transform bgTransform = healthBarUI.transform.Find("BG");
		    if (bgTransform == null)
		    {
		        Plugin.Log.LogError("<HealthBar>对象下未找到<BG>对象");
		        return;
		    }
		    bgImage = bgTransform.GetComponent<Image>();
			bgOriginalSize = bgImage.rectTransform.sizeDelta;

		    Transform fillTransform = bgTransform.Find("Fill");
		    if (fillTransform == null)
		    {
		        Plugin.Log.LogError("<HealthBar>对象中的<BG>对象下未找到<Fill>对象");
		        return;
		    }
		    fillImage = fillTransform.GetComponent<Image>();
		    if (fillImage == null)
		    {
		        Plugin.Log.LogError("<HealthBar>对象中的<Fill>对象没有添加<Image>");
		        return;
		    }
			fillOriginalSize = fillImage.rectTransform.sizeDelta;
		    fillImage.type = Image.Type.Filled;
		    fillImage.fillMethod = Image.FillMethod.Horizontal;
		    SetupUILayout();
		    UpdateHealthBar(); // 初始化血条填充
		    ShowHealthBar(); // 创建后立即显示
		    isUICreated = true; // 标记UI已创建
		}

		private Vector3 GetHeadOffset()
		{
			// 完全基于敌人对象的position，使用固定偏移值
            // 不再依赖碰撞体或渲染器边界
			return new Vector3(0f, 1f, 0f);
		}

		private void ShowHealthBar()
		{
			healthBarUI?.SetActive(true);
		}

		private void HideHealthBar()
		{
			healthBarUI?.SetActive(false);
		}

		private void SetupUILayout()
		{
			// HealthBar使用WorldSpace渲染模式，不需要设置锚点
            // 位置通过transform.position在世界坐标中设置
		    if (bgImage != null && fillImage != null)
			{
				RectTransform bgRectTF = bgImage.rectTransform;
				Vector2 newSizeDelta = new Vector2(Plugin.NormalWidthOffset.Value, Plugin.NormalHeightOffset.Value);
				float scale = baseScale * Plugin.NormalScale.Value;

				bgRectTF.localScale = Vector3.one * scale;
				bgRectTF.sizeDelta = bgOriginalSize * newSizeDelta;
				fillImage.rectTransform.sizeDelta = fillOriginalSize * newSizeDelta;
			}
		}

		public void RefreshLayout()
		{
			SetupUILayout();
		}

		private void UpdateMaxHealth()
		{
			if (healthManager != null)
			{
				float num = (float)healthManager.hp;
				// 如果当前血量大于初始最大血量，且不超过3000的上限阈值，则更新最大血量
				if (num > maxHealth && num <= 3000f)
				{
					maxHealth = num;
				}
			}
		}

		private void UpdateHealthBar()
		{
			if (fillImage != null && healthManager != null)
			{
				float num = (float)healthManager.hp / maxHealth;
				fillImage.fillAmount = num;
			}
		}

		private void CheckVisibilityConditions()
		{
		    if (healthBarUI == null || playerTransform == null)
		        return;
		    bool shouldBeVisible = true;
		    // 检查距离条件
		    float distanceSquared = (transform.position - playerTransform.position).sqrMagnitude;
		    if (distanceSquared > detectionDistance * detectionDistance)
		    {
		        shouldBeVisible = false;
		    }
		    // 检查游戏对象激活状态
		    if (!gameObject.activeInHierarchy)
		    {
		        shouldBeVisible = false;
		    }
		    // 检查碰撞器状态
		    if (!colliders2D || !colliders2D.enabled)
		    {
		        shouldBeVisible = false;
		    }
		    // 检查伤害显示时间
		    if (isDamageVisible && Time.time - lastDamageTime > Plugin.NormalHideDelay.Value)
		    {
		        isDamageVisible = false;
		    }
			// 检查在受过伤害时显示血条
		    if (!isDamageVisible)
			{
				shouldBeVisible = false;
			}
		    // 更新血条显示状态
		    if (healthBarUI.activeSelf != shouldBeVisible)
		    {
		        if (shouldBeVisible)
		        {
		            ShowHealthBar();
		            return;
		        }
		        HideHealthBar();
		    }
		}

		public void OnTakeDamage()
		{
			lastDamageTime = Time.time;
			isDamageVisible = true;
			if (healthBarUI != null)
			{
				ShowHealthBar();
			}
		}

		private void OnDestroy()
		{
			if (healthBarUI != null)
			{
				Destroy(healthBarUI);
				healthBarUI = null;
			}
			isUICreated = false;
			// 不销毁共享Canvas，让其他血条继续使用
            // 只有当没有其他血条时才销毁共享Canvas
		}
	}
}
