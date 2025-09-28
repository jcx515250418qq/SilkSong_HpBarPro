using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HealthbarPlugin
{
	public class DamageTextManager : MonoBehaviour
	{
		public static DamageTextManager Instance
		{
			get
			{
				if (DamageTextManager._instance == null)
				{
					GameObject gameObject = new GameObject("DamageTextManager");
					DamageTextManager._instance = gameObject.AddComponent<DamageTextManager>();
					global::UnityEngine.Object.DontDestroyOnLoad(gameObject);
				}
				return DamageTextManager._instance;
			}
		}

		public static Font SharedFont
		{
			get
			{
				return DamageTextManager.Instance.damageFont;
			}
		}

		private void Awake()
		{
			if (DamageTextManager._instance == null)
			{
				DamageTextManager._instance = this;
				global::UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
				this.LoadDamageFont();
				base.StartCoroutine(this.CleanupDamageTexts());
				return;
			}
			if (DamageTextManager._instance != this)
			{
				global::UnityEngine.Object.Destroy(base.gameObject);
			}
		}

		private void LoadDamageFont()
		{
			try
			{
				Font[] array = Resources.FindObjectsOfTypeAll<Font>();
				this.damageFont = array.FirstOrDefault((Font f) => f.name.Contains("TrajanPro-Regular"));
				if (this.damageFont == null)
				{
					this.damageFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
				}
				if (this.damageFont == null)
				{
                    SilkHpBar_PRO.Plugin.Log.LogWarning("[DamageTextManager] 未找到合适的字体，将使用默认字体");
				}
			}
			catch (Exception ex)
			{
                SilkHpBar_PRO.Plugin.Log.LogError("[DamageTextManager] 加载字体时出错: " + ex.Message);
			}
		}

		public void ShowDamageText(Vector2 worldPosition, float damage)
		{
			if (!SilkHpBar_PRO.Plugin.isDamageText.Value)
			{
				return;
			}
			try
			{
				GameObject gameObject = new GameObject("DamageTextCanvas");
				Canvas canvas = gameObject.AddComponent<Canvas>();
				canvas.renderMode = RenderMode.WorldSpace;
				canvas.sortingOrder = 1000;
				float num = UnityEngine.Random.Range(-0.5f, 0.5f);
				float num2 = UnityEngine.Random.Range(1f, 2f);
				Vector3 vector = new Vector3(worldPosition.x + num, worldPosition.y + num2, 0f);
				gameObject.transform.position = vector;
				RectTransform component = gameObject.GetComponent<RectTransform>();
				float num3 = (float)Mathf.Max(200, SilkHpBar_PRO.Plugin.DamageTextFontSize.Value * 4);
				float num4 = Mathf.Max(50f, (float)SilkHpBar_PRO.Plugin.DamageTextFontSize.Value * 1.5f);
				component.sizeDelta = new Vector2(num3, num4);
				GameObject gameObject2 = new GameObject("DamageText");
				gameObject2.transform.SetParent(gameObject.transform);
				Text text = gameObject2.AddComponent<Text>();
				if (SilkHpBar_PRO.Plugin.DamageTextUseSign.Value)
				{
					text.text = damage > 0f ? string.Format("- {0}", damage) : string.Format("+ {0}", Mathf.Abs(damage));
				}
				else
				{
					text.text = damage.ToString();
				}
				text.fontSize = SilkHpBar_PRO.Plugin.DamageTextFontSize.Value;
				text.alignment = TextAnchor.MiddleCenter;
				if (damage > 0f)
				{
					Color currentDamageColor = SilkHpBar_PRO.Plugin.DamageTextColor.Value;
					if (currentDamageColor != cachedDamageTextColor)
					{
						cachedDamageTextColor = currentDamageColor;
					}
					text.color = cachedDamageTextColor;
				}
				else
				{
					text.color = Color.green;
}
				if (this.damageFont != null)
				{
					text.font = this.damageFont;
				}
				RectTransform component2 = gameObject2.GetComponent<RectTransform>();
				component2.anchorMin = Vector2.zero;
				component2.anchorMax = Vector2.one;
				component2.offsetMin = Vector2.zero;
				component2.offsetMax = Vector2.zero;
				this.activeDamageTexts.Add(gameObject.gameObject);
				gameObject.transform.localScale = Vector3.one * 0.01f;
				base.StartCoroutine(this.AnimateDamageText(gameObject.gameObject, text));
			}
			catch (Exception ex)
			{
                SilkHpBar_PRO.Plugin.Log.LogError("[DamageTextManager] 显示伤害文本时出错: " + ex.Message);
			}
		}

		private IEnumerator AnimateDamageText(GameObject damageTextCanvas, Text damageText)
		{
			float animationDuration = 1f;
			float elapsed = 0f;
			damageTextCanvas.GetComponent<RectTransform>();
			Vector3 startPosition = damageTextCanvas.transform.position;
			while (elapsed < animationDuration)
			{
				elapsed += Time.deltaTime;
				float num = startPosition.y + elapsed / animationDuration * 2f;
				damageTextCanvas.transform.position = new Vector3(startPosition.x, num, startPosition.z);
				Color color = damageText.color;
				color.a = 1f - elapsed / animationDuration;
				damageText.color = color;
				yield return null;
			}
			if (damageTextCanvas != null)
			{
				UnityEngine.Object.Destroy(damageTextCanvas);
				this.activeDamageTexts.Remove(damageTextCanvas);
			}
			yield break;
		}

		private IEnumerator CleanupDamageTexts()
		{
			for (; ; )
			{
				yield return new WaitForSeconds(5f);
				for (int i = this.activeDamageTexts.Count - 1; i >= 0; i--)
				{
					if (this.activeDamageTexts[i] == null)
					{
						this.activeDamageTexts.RemoveAt(i);
					}
				}
			}
		}

		public void ClearAllDamageTexts()
		{
			try
			{
				foreach (GameObject gameObject in this.activeDamageTexts)
				{
					if (gameObject != null)
					{
						global::UnityEngine.Object.Destroy(gameObject);
					}
				}
				this.activeDamageTexts.Clear();
			}
			catch (Exception ex)
			{
                SilkHpBar_PRO.Plugin.Log.LogError("DamageTextManager: 清理伤害文本时发生错误: " + ex.Message);
			}
		}

		private void OnDestroy()
		{
			foreach (GameObject gameObject in this.activeDamageTexts)
			{
				if (gameObject != null)
				{
					global::UnityEngine.Object.Destroy(gameObject);
				}
			}
			this.activeDamageTexts.Clear();
		}

		public DamageTextManager()
		{
			this.activeDamageTexts = new List<GameObject>();
		}
		private static DamageTextManager _instance;
		private List<GameObject> activeDamageTexts;
		private Font damageFont;
		private Color cachedDamageTextColor;
	}
}
