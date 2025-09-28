using System.Collections.Generic;
using UnityEngine;

namespace SilkHpBar_PRO
{
	public class BossHealthBarManager : MonoBehaviour
	{
		public static BossHealthBarManager Instance
		{
			get
			{
				if (BossHealthBarManager._instance == null)
				{
					GameObject gameObject = new GameObject("BossHealthBarManager");
					BossHealthBarManager._instance = gameObject.AddComponent<BossHealthBarManager>();
					global::UnityEngine.Object.DontDestroyOnLoad(gameObject);
				}
				return BossHealthBarManager._instance;
			}
		}

		private void Awake()
		{
			if (BossHealthBarManager._instance == null)
			{
				BossHealthBarManager._instance = this;
				global::UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
				return;
			}
			if (BossHealthBarManager._instance != this)
			{
				global::UnityEngine.Object.Destroy(base.gameObject);
			}
		}

		public bool RegisterHealthBar(BossHealthBar healthBar)
		{
			bool flag;
			if (this.activeBossHealthBars.Count >= this.MAX_HEALTH_BARS)
			{
				flag = false;
			}
			else
			{
				this.activeBossHealthBars.Add(healthBar);
				this.ArrangeHealthBars();
				flag = true;
			}
			return flag;
		}

		public void UnregisterHealthBar(BossHealthBar healthBar)
		{
			if (this.activeBossHealthBars.Remove(healthBar))
			{
				this.ArrangeHealthBars();
			}
		}

		public void ArrangeHealthBars()
		{
			if (this.activeBossHealthBars.Count != 0)
			{
				BossHealthBar bossHealthBar = this.activeBossHealthBars[0];
				if (this.activeBossHealthBars.Count == 1)
				{
					bossHealthBar.RefreshLayout();
				}
				RectTransform healthBarTransform = bossHealthBar.GetHealthBarTransform();
				if (!(healthBarTransform == null))
				{
					Vector2 anchoredPosition = healthBarTransform.anchoredPosition;
					float y = healthBarTransform.sizeDelta.y;
					bool flag = this.IsInUpperScreen(healthBarTransform);
					for (int i = 0; i < this.activeBossHealthBars.Count; i++)
					{
						BossHealthBar bossHealthBar2 = this.activeBossHealthBars[i];
						RectTransform healthBarTransform2 = bossHealthBar2.GetHealthBarTransform();
						if (!(healthBarTransform2 == null))
						{
							Vector2 vector = anchoredPosition;
							if (i == 0)
							{
								if (!flag && this.activeBossHealthBars.Count > 1)
								{
									vector.y += y + this.HEALTH_BAR_SPACING;
								}
							}
							else
							{
								if (flag)
								{
									vector.y -= (float)i * (y + this.HEALTH_BAR_SPACING);
								}
								else
								{
									vector.y += (float)i * (y + this.HEALTH_BAR_SPACING);
								}
								bossHealthBar2.SetBossName(true);
							}
							healthBarTransform2.anchoredPosition = vector;
						}
					}
				}
			}
		}

		private bool IsInUpperScreen(RectTransform healthBarRect)
		{
			bool flag;
			if (healthBarRect == null)
			{
				flag = false;
			}
			else
			{
				Canvas componentInParent = healthBarRect.GetComponentInParent<Canvas>();
				if (componentInParent == null)
				{
					flag = false;
				}
				else
				{
					RectTransform component = componentInParent.GetComponent<RectTransform>();
					if (component == null)
					{
						flag = false;
					}
					else
					{
						Vector2 vector;
						RectTransformUtility.ScreenPointToLocalPointInRectangle(component, RectTransformUtility.WorldToScreenPoint(componentInParent.worldCamera, healthBarRect.position), componentInParent.worldCamera, out vector);
						float y = component.sizeDelta.y;
						flag = (vector.y + y * 0.5f) / y > this.SCREEN_TOP_THRESHOLD;
					}
				}
			}
			return flag;
		}

		public int GetActiveHealthBarCount()
		{
			return this.activeBossHealthBars.Count;
		}

		public void ClearAllHealthBars()
		{
			this.activeBossHealthBars.Clear();
		}

		public BossHealthBarManager()
		{
			this.activeBossHealthBars = new List<BossHealthBar>();
			this.HEALTH_BAR_SPACING = 50f;
			this.MAX_HEALTH_BARS = 2;
			this.SCREEN_TOP_THRESHOLD = 0.6f;
		}

		private static BossHealthBarManager _instance;
		private List<BossHealthBar> activeBossHealthBars;
		private float HEALTH_BAR_SPACING;
		private int MAX_HEALTH_BARS;
		private float SCREEN_TOP_THRESHOLD;
	}
}
