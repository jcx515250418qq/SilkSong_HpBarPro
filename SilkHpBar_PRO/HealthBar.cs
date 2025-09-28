using UnityEngine;
using UnityEngine.UI;

namespace SilkHpBar_PRO
{
	public class HealthBar : MonoBehaviour
	{
		private void Awake()
		{
			if (base.GetComponents<HealthBar>().Length > 1)
			{
				UnityEngine.Object.Destroy(this);
			}
			else
			{
				this.healthManager = base.GetComponent<HealthManager>();
				this.maxHealth = (float)this.healthManager.hp;		//源文件HealthManager类中initHp不予访问无法进行引用
				this.lastHealth = (float)this.healthManager.hp;
				this.playerTransform = UnityEngine.Object.FindFirstObjectByType<HeroController>().transform;
				this.colliders2D = base.GetComponent<Collider2D>();
				this.lastDamageTime = Time.time;
			}
		}

		private void Update()
		{
			if (!(this.healthManager == null) && Plugin.isNormal.Value)
			{
				this.CheckVisibilityConditions();
				if (this.healthBarUI == null && !this.isUICreated)
				{
					this.CreateHealthBarUI();
				}
				float num = (float)this.healthManager.hp;
				this.UpdateMaxHealth();
				if (num != this.lastHealth)
				{
					this.lastHealth = num;
					this.UpdateHealthBar();
				}
				if (this.healthBarUI != null)
				{
					this.UpdateHealthBarPosition();
				}
			}
		}

		private void UpdateHealthBarPosition()
		{
			if (!(this.healthBarUI == null) && !(HealthBar.sharedCanvas == null))
			{
				Vector3 headOffset = this.GetHeadOffset();
				Vector3 vector = base.transform.position + headOffset;
				GameCameras instance = GameCameras.instance;
				Camera camera = instance != null ? instance.mainCamera : null;
				if (camera != null)
				{
					Vector3 vector2 = camera.WorldToScreenPoint(vector);
					if (vector2.z < 0f || vector2.x < 0f || vector2.x > (float)Screen.width || vector2.y < 0f || vector2.y > (float)Screen.height)
					{
						if (this.healthBarUI.activeSelf)
						{
							this.healthBarUI.SetActive(false);
						}
						return;
					}
					RectTransform component = this.healthBarUI.GetComponent<RectTransform>();
					Vector2 vector3;
					if (RectTransformUtility.ScreenPointToLocalPointInRectangle(HealthBar.sharedCanvas.GetComponent<RectTransform>(), vector2, null, out vector3))
					{
						vector3.x += Plugin.NormalPositionX.Value;
						vector3.y += Plugin.NormalPositionY.Value;
						component.localPosition = vector3;
					}
				}
				if (!this.healthBarUI.activeSelf && this.isDamageVisible)
				{
					this.healthBarUI.SetActive(true);
				}
			}
		}

		private void CreateHealthBarUI()
		{
			if (!(this.healthBarUI != null) && Plugin.isNormal.Value)
			{
				this.healthBarUI = UnityEngine.Object.Instantiate<GameObject>(Plugin.healthBarPrefab);
				Canvas component = this.healthBarUI.GetComponent<Canvas>();
				if (component != null)
				{
					CanvasScaler component2 = this.healthBarUI.GetComponent<CanvasScaler>();
					if (component2 != null)
					{
						UnityEngine.Object.DestroyImmediate(component2);
					}
					GraphicRaycaster component3 = this.healthBarUI.GetComponent<GraphicRaycaster>();
					if (component3 != null)
					{
						UnityEngine.Object.DestroyImmediate(component3);
					}
					UnityEngine.Object.DestroyImmediate(component);
				}
				if (HealthBar.sharedCanvas == null)
				{
					GameObject gameObject = new GameObject("SharedHealthBarCanvas");
					HealthBar.sharedCanvas = gameObject.AddComponent<Canvas>();
					HealthBar.sharedCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
					HealthBar.sharedCanvas.sortingOrder = 100;
					gameObject.AddComponent<CanvasScaler>();
					gameObject.AddComponent<GraphicRaycaster>();
				}
				this.healthBarUI.transform.SetParent(HealthBar.sharedCanvas.transform, false);
				Transform transform = this.healthBarUI.transform.Find("BG");
				this.bgImage = transform.GetComponent<Image>();
				this.fillImage = transform.Find("Fill").GetComponent<Image>();
				this.fillImage.type = Image.Type.Filled;
				this.fillImage.fillMethod = Image.FillMethod.Horizontal;
				this.SetupUILayout();
				this.UpdateHealthBar();
				this.ShowHealthBar();
				this.isUICreated = true;
			}
		}

		private Vector3 GetHeadOffset()
		{
			return new Vector3(0f, 1f, 0f);
		}

		private void ShowHealthBar()
		{
			this.healthBarUI?.SetActive(true);
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
				rectTransform.localScale = Vector3.one * HealthBar.baseScale * Plugin.NormalScale.Value;
				rectTransform.sizeDelta = new Vector2(240f + Plugin.NormalWidthOffset.Value, 36f + Plugin.NormalHeightOffset.Value);
				this.fillImage.rectTransform.sizeDelta = rectTransform.sizeDelta;
			}
		}

		public void RefreshLayout()
		{
			this.SetupUILayout();
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

		private void UpdateHealthBar()
		{
			if (!(this.fillImage == null) && !(this.healthManager == null))
			{
				float num = (float)this.healthManager.hp / this.maxHealth;
				this.fillImage.fillAmount = num;
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
				if (!base.gameObject.activeInHierarchy)
				{
					flag = false;
				}
				if (!this.colliders2D || !this.colliders2D.enabled)
				{
					flag = false;
				}
				if (this.isDamageVisible && Time.time - this.lastDamageTime > Plugin.NormalHideDelay.Value)
				{
					this.isDamageVisible = false;
				}
				if (!this.isDamageVisible)
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
			this.lastDamageTime = Time.time;
			this.isDamageVisible = true;
			if (this.healthBarUI != null)
			{
				this.ShowHealthBar();
			}
		}

		private void OnDestroy()
		{
			if (this.healthBarUI != null)
			{
				UnityEngine.Object.Destroy(this.healthBarUI);
				this.healthBarUI = null;
			}
			this.isUICreated = false;
		}

		public HealthBar()
		{
			this.detectionDistance = 15f;
			this.isDamageVisible = false;
			this.isUICreated = false;
		}

		static HealthBar()
		{
			HealthBar.baseScale = 1f;
		}

		private HealthManager healthManager;
		private float lastHealth;
		private float maxHealth;
		public float detectionDistance;
		private Transform playerTransform;
		private Collider2D colliders2D;
		private GameObject healthBarUI;
		private static Canvas sharedCanvas;
		private Image bgImage;
		private Image fillImage;
		public static float baseScale;
		private float lastDamageTime;
		private bool isDamageVisible;
		private bool isUICreated;
	}
}
