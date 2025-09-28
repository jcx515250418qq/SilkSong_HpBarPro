using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Silk_HpBar_PRO
{
    /// <summary>
    /// BOSS血条管理器，负责管理多个BOSS血条的位置和显示逻辑
    /// </summary>
    public class BossHealthBarManager : MonoBehaviour
    {
        private static BossHealthBarManager _instance;
        public static BossHealthBarManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject managerObj = new GameObject("BossHealthBarManager");
                    _instance = managerObj.AddComponent<BossHealthBarManager>();
                    DontDestroyOnLoad(managerObj);
                }
                return _instance;
            }
        }

        private List<BossHealthBar> activeBossHealthBars = new List<BossHealthBar>();
        private  float HEALTH_BAR_SPACING = 50; // 血条间隔
        private  int MAX_HEALTH_BARS = 2; // 最大血条数量
        private  float SCREEN_TOP_THRESHOLD = 0.6f; // 屏幕上方阈值（0-1）

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 注册新的BOSS血条
        /// </summary>
        /// <param name="healthBar">要注册的血条</param>
        /// <returns>是否成功注册（false表示已达到最大数量）</returns>
        public bool RegisterHealthBar(BossHealthBar healthBar)
        {
            if (activeBossHealthBars.Count >= MAX_HEALTH_BARS)
            {
                return false; // 已达到最大数量，不创建新血条
            }

            activeBossHealthBars.Add(healthBar);
            ArrangeHealthBars();
            return true;
        }

        /// <summary>
        /// 注销BOSS血条
        /// </summary>
        /// <param name="healthBar">要注销的血条</param>
        public void UnregisterHealthBar(BossHealthBar healthBar)
        {
            if (activeBossHealthBars.Remove(healthBar))
            {
                ArrangeHealthBars();
            }
        }

        /// <summary>
        /// 重新排列所有血条的位置
        /// </summary>
        public void ArrangeHealthBars()
        {
            if (activeBossHealthBars.Count == 0) return;

            // 获取第一个血条的原始位置作为基准
            BossHealthBar firstHealthBar = activeBossHealthBars[0];

            if(activeBossHealthBars.Count == 1)
            {
                firstHealthBar.RefreshLayout();
            }

            RectTransform firstRect = firstHealthBar.GetHealthBarTransform();
            if (firstRect == null) return;

            Vector2 basePosition = firstRect.anchoredPosition;
            float healthBarHeight = firstRect.sizeDelta.y;

            // 判断第一个血条是否在屏幕上方
            bool isInUpperScreen = IsInUpperScreen(firstRect);

            for (int i = 0; i < activeBossHealthBars.Count; i++)
            {
                BossHealthBar healthBar = activeBossHealthBars[i];
                RectTransform healthBarRect = healthBar.GetHealthBarTransform();
                if (healthBarRect == null) continue;

                Vector2 newPosition = basePosition;

                if (i == 0)
                {
                    // 第一个血条保持原位置（如果需要上移则在后面处理）
                    if (!isInUpperScreen && activeBossHealthBars.Count > 1)
                    {
                        // 如果不在上方且有多个血条，第一个血条需要上移
                        newPosition.y += (healthBarHeight + HEALTH_BAR_SPACING);
                    }
                }
                else
                {
                    // 后续血条的位置计算
                    if (isInUpperScreen)
                    {
                        // 在上方，向下排列
                        newPosition.y -= i * (healthBarHeight + HEALTH_BAR_SPACING);
                    }
                    else
                    {
                        // 在下方，向上排列
                        newPosition.y += i * (healthBarHeight + HEALTH_BAR_SPACING);
                    }

                    // 新血条隐藏BossName组件
                    healthBar.SetBossName(true);
                }

                healthBarRect.anchoredPosition = newPosition;
            }
        }

        /// <summary>
        /// 判断血条是否在屏幕上方
        /// </summary>
        /// <param name="healthBarRect">血条的RectTransform</param>
        /// <returns>是否在屏幕上方</returns>
        private bool IsInUpperScreen(RectTransform healthBarRect)
        {
            if (healthBarRect == null) return false;

            Canvas canvas = healthBarRect.GetComponentInParent<Canvas>();
            if (canvas == null) return false;

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect == null) return false;

            // 将血条的世界位置转换为Canvas的本地位置
            Vector2 canvasPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, 
                RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, healthBarRect.position),
                canvas.worldCamera, 
                out canvasPosition
            );

            // 计算相对于Canvas高度的位置比例
            float canvasHeight = canvasRect.sizeDelta.y;
            float relativeY = (canvasPosition.y + canvasHeight * 0.5f) / canvasHeight;

            return relativeY > SCREEN_TOP_THRESHOLD;
        }

        /// <summary>
        /// 获取当前活跃的血条数量
        /// </summary>
        /// <returns>活跃血条数量</returns>
        public int GetActiveHealthBarCount()
        {
            return activeBossHealthBars.Count;
        }

        /// <summary>
        /// 清理所有血条
        /// </summary>
        public void ClearAllHealthBars()
        {
            activeBossHealthBars.Clear();
        }
    }
}