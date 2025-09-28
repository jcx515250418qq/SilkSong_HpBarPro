using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GlobalEnums;
using HarmonyLib;
using HealthbarPlugin;
using HutongGames.PlayMaker.Actions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TeamCherry.Localization;
using UnityEngine;
using UnityEngine.UI;

namespace Silk_HpBar_PRO
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.xiaohai.HealthBarPro";
        public const string PLUGIN_NAME = "HealthBarPro";
        public const string PLUGIN_VERSION = "1.0.3";

        public static Harmony harmony;
        public static ManualLogSource Log;

        public static ConfigEntry<int> BossHealthThreshold;

        // BOSS血条调整配置
        public static ConfigEntry<float> BossPositionX;
        public static ConfigEntry<float> BossPositionY;
        public static ConfigEntry<float> BossScale;
        public static ConfigEntry<float> BossWidthOffset;
        public static ConfigEntry<float> BossHeightOffset;
        public static ConfigEntry<string> PreTitle;
        public static ConfigEntry<string> PostTitle;
        public static ConfigEntry<float> BossExpandDuration; // BOSS血条展开动画时长
        // 普通血条调整配置
        public static ConfigEntry<float> NormalPositionX;
        public static ConfigEntry<float> NormalPositionY;
        public static ConfigEntry<float> NormalScale;
        public static ConfigEntry<float> NormalWidthOffset;
        public static ConfigEntry<float> NormalHeightOffset;
        public static ConfigEntry<float> NormalHideDelay; // 普通血条隐藏延迟
        //伤害文本
        public static ConfigEntry<float> DamageTextDuration;
        public static ConfigEntry<int> DamageTextFontSize;
        public static ConfigEntry<string> DamageTextColor;
        public static ConfigEntry<bool> DamageTextUseSign;

        public static AssetBundle healthBarAssetBundle;
        public static GameObject bossHealthBarPrefab;
        public static GameObject healthBarPrefab;

        void Awake()
        {
            Log = Logger;

            BossHealthThreshold = Config.Bind("Boss识别设置", "血量阈值", 119,
                "Boss血量判定阈值，血量大于此值的敌人将被识别为Boss并使用Boss血条 / Boss health threshold, enemies with health above this value will be recognized as Boss and use Boss health bar");

            // 初始化BOSS血条调整配置
            BossPositionX = Config.Bind("BOSS血条调整/BossHpBar", "X轴位置偏移", 0f, "BOSS血条X轴位置偏移量 / Boss health bar X-axis position offset");
            BossPositionY = Config.Bind("BOSS血条调整/BossHpBar", "Y轴位置偏移", 0f, "BOSS血条Y轴位置偏移量 / Boss health bar Y-axis position offset");
            BossScale = Config.Bind("BOSS血条调整/BossHpBar", "缩放倍数", 1f, "BOSS血条缩放倍数 / Boss health bar scale multiplier");
            BossWidthOffset = Config.Bind("BOSS血条调整/BossHpBar", "宽度偏移", 0f, "BOSS血条宽度偏移量 / Boss health bar width offset");
            BossHeightOffset = Config.Bind("BOSS血条调整/BossHpBar", "高度偏移", 0f, "BOSS血条高度偏移量 / Boss health bar height offset");
            PreTitle= Config.Bind("BOSS血条调整/BossHpBar", "标题前缀", "盘踞于", "BOSS血条标题前缀 / Boss health bar title prefix");
            PostTitle= Config.Bind("BOSS血条调整/BossHpBar", "标题后缀", "的", "BOSS血条标题后缀 / Boss health bar title suffix");
            BossExpandDuration = Config.Bind("BOSS血条调整/BossHpBar", "A展开动画时长", 1.0f, "BOSS血条从左到右展开的动画时长（秒） / Boss health bar expand animation duration (seconds)");
            // 初始化普通血条调整配置
            NormalPositionX = Config.Bind("普通血条调整/NormalHpBar", "X轴位置偏移", 0f, "普通血条X轴位置偏移量 / Normal health bar X-axis position offset");
            NormalPositionY = Config.Bind("普通血条调整/NormalHpBar", "Y轴位置偏移", 0f, "普通血条Y轴位置偏移量 / Normal health bar Y-axis position offset");
            NormalScale = Config.Bind("普通血条调整/NormalHpBar", "缩放倍数", 1f, "普通血条缩放倍数 / Normal health bar scale multiplier");
            NormalWidthOffset = Config.Bind("普通血条调整/NormalHpBar", "宽度偏移", 0f, "普通血条宽度偏移量 / Normal health bar width offset");
            NormalHeightOffset = Config.Bind("普通血条调整/NormalHpBar", "高度偏移", 0f, "普通血条高度偏移量 / Normal health bar height offset");
            NormalHideDelay= Config.Bind("普通血条调整/NormalHpBar", "A隐藏延迟", 3.0f, "普通血条在受到伤害后持续显示的时间（秒） / Normal health bar display duration after taking damage (seconds)");

            // 伤害文本配置 / Damage Text Settings
            DamageTextDuration = Config.Bind<float>("A伤害文本/DamageText", "Duration", 2.0f, "伤害文本显示持续时间（秒） / Damage text display duration (seconds)");
            DamageTextFontSize = Config.Bind<int>("A伤害文本/DamageText", "FontSize", 55, "伤害文本字体大小 / Damage text font size");
            DamageTextColor = Config.Bind<string>("A伤害文本/DamageText", "DamageColor", "#0e0404ff", "伤害文本颜色（十六进制格式，如#FF0000为红色）颜色十六进制代码转换:http://pauli.cn/tool/color.htm / Damage text color (hex format, e.g. #FF0000 for red)");
            DamageTextUseSign = Config.Bind<bool>("A伤害文本/DamageText", "UseSign", true, "伤害文本是否显示符号?(Plus:+, Minus:-) / Whether to show signs in damage text (Plus:+, Minus:-)");

            LoadAssetBundle();
            new Harmony(PLUGIN_GUID).PatchAll();

            var fonts = Resources.FindObjectsOfTypeAll<TMProOld.TMP_FontAsset>();
            foreach (var font in fonts)
            {
                Plugin.Log.LogInfo($"字体找到: {font.name}");
            }
        }

        private void LoadAssetBundle()
        {
            string dllPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string assetBundlePath = Path.Combine(dllPath, "healthbar_pro");

            if (!File.Exists(assetBundlePath))
            {
                Plugin.Log.LogError($"healthbar_pro 为空");
                return;
            }
            
            healthBarAssetBundle = AssetBundle.LoadFromFile(assetBundlePath);
            if (healthBarAssetBundle == null)
            {
                Plugin.Log.LogError($"healthBarAssetBundle 为空");
                return;
            }
            

            bossHealthBarPrefab = healthBarAssetBundle.LoadAsset<GameObject>("BossHealthBar.prefab");
            if (bossHealthBarPrefab == null)
            {
                bossHealthBarPrefab= healthBarAssetBundle.LoadAsset<GameObject>("BossHealthBar_1.prefab");
                if(bossHealthBarPrefab == null)
                {
                    Plugin.Log.LogError($"BossHealthBar和BossHealthBar_1都为空");
                    return;
                }
               
            }


            healthBarPrefab = healthBarAssetBundle.LoadAsset<GameObject>("HealthBar.prefab");
            if (healthBarPrefab == null)
            {
                Plugin.Log.LogError($"HealthBar 为空");
                return;
            }



        }

        void Update()
        {
            // BOSS血条控制 (Ctrl + 按键)
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                // 检查是否有BOSS血条正在展开动画，如果是则暂时禁用快捷键调整
                var bossHealthBars = FindObjectsByType<BossHealthBar>(FindObjectsSortMode.None);
                bool anyBossExpanding = false;
                foreach (var bossBar in bossHealthBars)
                {
                    if (bossBar.IsExpanding())
                    {
                        anyBossExpanding = true;
                        break;
                    }
                }
                
                if (anyBossExpanding)
                {
                    return; // 有BOSS血条正在展开动画，跳过所有快捷键处理
                }
                bool needsRefresh = false;
                
                // 位置控制
                if (Input.GetKey(KeyCode.UpArrow))
                {
                    BossPositionY.Value += 0.1f;
                    needsRefresh = true;
                }
                if (Input.GetKey(KeyCode.DownArrow))
                {
                    BossPositionY.Value -= 0.1f;
                    needsRefresh = true;
                }
                if (Input.GetKey(KeyCode.LeftArrow))
                {
                    BossPositionX.Value -= 0.1f;
                    needsRefresh = true;
                }
                if (Input.GetKey(KeyCode.RightArrow))
                {
                    BossPositionX.Value += 0.1f;
                    needsRefresh = true;
                }
                
                // 缩放控制 (小键盘)
                if (Input.GetKey(KeyCode.KeypadPlus))
                {
                    BossScale.Value += 0.01f;
                    needsRefresh = true;
                }
                if (Input.GetKey(KeyCode.KeypadMinus))
                {
                    BossScale.Value -= 0.01f;
                    needsRefresh = true;
                }
                
                // 宽度控制 (非小键盘)
                if (Input.GetKey(KeyCode.Plus) || Input.GetKey(KeyCode.Equals))
                {
                    BossWidthOffset.Value += 0.1f;
                    needsRefresh = true;
                }
                if (Input.GetKey(KeyCode.Minus))
                {
                    BossWidthOffset.Value -= 0.1f;
                    needsRefresh = true;
                }
                
                // 高度控制
                if (Input.GetKey(KeyCode.LeftBracket))
                {
                    BossHeightOffset.Value += 0.1f;
                    needsRefresh = true;
                }
                if (Input.GetKey(KeyCode.RightBracket))
                {
                    BossHeightOffset.Value -= 0.1f;
                    needsRefresh = true;
                }
                
                // BOSS参数重置 (Ctrl + 小键盘5)
                if (Input.GetKeyDown(KeyCode.Keypad5))
                {
                    BossPositionX.Value = 0f;
                    BossPositionY.Value = 0f;
                    BossScale.Value = 1f;
                    BossWidthOffset.Value = 0f;
                    BossHeightOffset.Value = 0f;
                    needsRefresh = true;
                    Log.LogInfo("BOSS血条参数已重置");
                }
                
                // 普通血条参数重置 (Ctrl + 小键盘6)
                if (Input.GetKeyDown(KeyCode.Keypad6))
                {
                    NormalPositionX.Value = 0f;
                    NormalPositionY.Value = 0f;
                    NormalScale.Value = 1f;
                    NormalWidthOffset.Value = 0f;
                    NormalHeightOffset.Value = 0f;
                    needsRefresh = true;
                    Log.LogInfo("普通血条参数已重置");
                }
                
                if (needsRefresh)
                {
                    RefreshAllBossHealthBars();
                }
            }
            
            // 普通血条控制 (Alt + 按键)
            if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
            {
                bool needsRefresh = false;
                
                // 位置控制
                if (Input.GetKey(KeyCode.UpArrow))
                {
                    NormalPositionY.Value += 0.1f;
                    needsRefresh = true;
                }
                if (Input.GetKey(KeyCode.DownArrow))
                {
                    NormalPositionY.Value -= 0.1f;
                    needsRefresh = true;
                }
                if (Input.GetKey(KeyCode.LeftArrow))
                {
                    NormalPositionX.Value -= 0.1f;
                    needsRefresh = true;
                }
                if (Input.GetKey(KeyCode.RightArrow))
                {
                    NormalPositionX.Value += 0.1f;
                    needsRefresh = true;
                }
                
                // 缩放控制 (小键盘)
                if (Input.GetKey(KeyCode.KeypadPlus))
                {
                    NormalScale.Value += 0.01f;
                    needsRefresh = true;
                }
                if (Input.GetKey(KeyCode.KeypadMinus))
                {
                    NormalScale.Value -= 0.01f;
                    needsRefresh = true;
                }
                
                // 宽度控制 (非小键盘)
                if (Input.GetKey(KeyCode.Plus) || Input.GetKey(KeyCode.Equals))
                {
                    NormalWidthOffset.Value += 0.1f;
                    needsRefresh = true;
                }
                if (Input.GetKey(KeyCode.Minus))
                {
                    NormalWidthOffset.Value -= 0.1f;
                    needsRefresh = true;
                }
                
                // 高度控制
                if (Input.GetKey(KeyCode.LeftBracket))
                {
                    NormalHeightOffset.Value += 0.1f;
                    needsRefresh = true;
                }
                if (Input.GetKey(KeyCode.RightBracket))
                {
                    NormalHeightOffset.Value -= 0.1f;
                    needsRefresh = true;
                }
                
                if (needsRefresh)
                {
                    RefreshAllNormalHealthBars();
                }
            }
        }
        
        // 刷新所有BOSS血条
        private void RefreshAllBossHealthBars()
        {
            var bossHealthBars = FindObjectsByType<BossHealthBar>(FindObjectsSortMode.None);
            foreach (var bossBar in bossHealthBars)
            {
                bossBar.RefreshLayout();
            }
        }
        
        // 刷新所有普通血条
        private void RefreshAllNormalHealthBars()
        {
            var normalHealthBars = FindObjectsByType<HealthBar>(FindObjectsSortMode.None);
            foreach (var normalBar in normalHealthBars)
            {
                normalBar.RefreshLayout();
            }
        }



        private void OnDestroy()
        {
            if (healthBarAssetBundle != null)
            {
                healthBarAssetBundle.Unload(true);
                healthBarAssetBundle = null;
            }
        }
    }

    [HarmonyPatch]
    public class Patch
    {
        [HarmonyPatch(typeof(HealthManager), "Awake")]
        [HarmonyPostfix]
        public static void HealthManagerPatch(HealthManager __instance)
        {
            // 过滤initHp小于5的单位，不添加血条
            if (__instance.initHp < 5)
            {
                return;
            }
            
            var type = __instance.gameObject.AddComponent<HealthBarData>();
           
            if (__instance.initHp > Plugin.BossHealthThreshold.Value)
            {
                __instance.gameObject.AddComponent<BossHealthBar>();
                type.barType=HealthBarData.BarType.Boss;
            }
            else
            {
                __instance.gameObject.AddComponent<HealthBar>();
                type.barType = HealthBarData.BarType.Normal;
            }
        }
        [HarmonyPatch(typeof(HealthManager), "TakeDamage")]
        [HarmonyPrefix]
        public static void TakeDamagePrefix(HealthManager __instance)
        {
            var data = __instance.GetComponent<HealthBarData>();
            if (data == null) return;

            data.lastHp = __instance.hp;
        }

        [HarmonyPatch(typeof(HealthManager), "TakeDamage")]
        [HarmonyPostfix]
        public static void TakeDamagePatch(HealthManager __instance)
        {
            var data = __instance.GetComponent<HealthBarData>();
            if (data == null) return;
            
            //伤害文本
            var damage = data.lastHp - __instance.hp;
            if (damage != 0)
            {
                DamageTextManager.Instance.ShowDamageText(__instance.transform.position, damage);
               
            }
           

            //血条

            if(data.barType == HealthBarData.BarType.Boss)
            {
                var bossHealthBar = __instance.GetComponent<BossHealthBar>();
                if (bossHealthBar == null)
                {
                    bossHealthBar=__instance.gameObject.AddComponent<BossHealthBar>();
                    
                }
                bossHealthBar.OnTakeDamage();
                return;
            }
            else
            {
                var healthBar = __instance.GetComponent<HealthBar>();
                if (healthBar == null)
                {
                    healthBar = __instance.gameObject.AddComponent<HealthBar>();

                }

                healthBar.OnTakeDamage();

            }

           
        }
    }

    public class HealthBarData: MonoBehaviour
    {
        public enum BarType
        {
            Normal,
            Boss
        }
        public float lastHp;
        public BarType barType = BarType.Normal;
    }
    public class HealthBar : MonoBehaviour
    {
        private HealthManager healthManager;
        private float lastHealth;
        private float maxHealth;

        public float detectionDistance = 15f;
        private Transform playerTransform;
        private Collider2D colliders2D;

        private GameObject healthBarUI;
        private static Canvas sharedCanvas; // 使用共享Canvas
        private UnityEngine.UI.Image bgImage;
        private UnityEngine.UI.Image fillImage;

        public static float  baseScale = 1f;
 
        private float lastDamageTime;
        private bool isDamageVisible = false;
        private bool isUICreated = false; // 防止重复创建UI
        
      
        void Awake()
        {
            if (GetComponents<HealthBar>().Length > 1)
            {
                Destroy(this);
                return;
            }
            healthManager = GetComponent<HealthManager>();
            maxHealth = healthManager.initHp;
            lastHealth = healthManager.hp;
            playerTransform = FindFirstObjectByType<HeroController>().transform;
            colliders2D = GetComponent<Collider2D>();
            lastDamageTime = Time.time;

           
        }

        void Update()
        {
            if (healthManager == null) return;
            
            CheckVisibilityConditions();
            
            // 如果血条UI不存在且未创建过，创建它
            if (healthBarUI == null && !isUICreated)
            {
                CreateHealthBarUI();
            }

            float currentHealth = healthManager.hp;
            
            // 实时更新最大生命值（处理转阶段情况）
            UpdateMaxHealth();

            if (currentHealth != lastHealth)
            {
                lastHealth = currentHealth;
                UpdateHealthBar();
            }

            // 更新血条位置，使其跟随敌人
            if (healthBarUI != null)
            {
                UpdateHealthBarPosition();
            }
        }
        
        private void UpdateHealthBarPosition()
        {
            if (healthBarUI == null || sharedCanvas == null) return;
            
            // 计算敌人头部的世界坐标
            Vector3 headOffset = GetHeadOffset();
            Vector3 worldPosition = transform.position + headOffset;
            
            // 将世界坐标转换为屏幕坐标
            Camera mainCamera = GameCameras.instance?.mainCamera;
            if (mainCamera != null)
            {
                Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);
                
                // 屏幕边界检查
                if (screenPosition.z < 0 || 
                    screenPosition.x < 0 || screenPosition.x > Screen.width ||
                    screenPosition.y < 0 || screenPosition.y > Screen.height)
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
                
                Vector2 localPoint;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, null, out localPoint))
                {
                    // 应用位置偏移配置
                    localPoint.x += Plugin.NormalPositionX.Value;
                    localPoint.y += Plugin.NormalPositionY.Value;
                    healthBarRect.localPosition = localPoint;
                }
            }
            
            if (!healthBarUI.activeSelf && isDamageVisible) healthBarUI.SetActive(true);
        }

        private void CreateHealthBarUI()
        {
            if (healthBarUI != null) return;

            healthBarUI = UnityEngine.Object.Instantiate(Plugin.healthBarPrefab);
            
            // 移除预制体自带的Canvas组件及其依赖组件
            Canvas prefabCanvas = healthBarUI.GetComponent<Canvas>();
            if (prefabCanvas != null)
            {
                // 先移除依赖组件
                UnityEngine.UI.CanvasScaler canvasScaler = healthBarUI.GetComponent<UnityEngine.UI.CanvasScaler>();
                if (canvasScaler != null)
                {
                    UnityEngine.Object.DestroyImmediate(canvasScaler);
                }
                
                UnityEngine.UI.GraphicRaycaster graphicRaycaster = healthBarUI.GetComponent<UnityEngine.UI.GraphicRaycaster>();
                if (graphicRaycaster != null)
                {
                    UnityEngine.Object.DestroyImmediate(graphicRaycaster);
                }
                
                // 最后移除Canvas
                UnityEngine.Object.DestroyImmediate(prefabCanvas);
            }
            
            // 创建或获取共享Canvas
            if (sharedCanvas == null)
            {
                GameObject canvasObj = new GameObject("SharedHealthBarCanvas");
                sharedCanvas = canvasObj.AddComponent<Canvas>();
                sharedCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                sharedCanvas.sortingOrder = 100;
                canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                
            }
            healthBarUI.transform.SetParent(sharedCanvas.transform, false);
            
            
            Transform bgTransform = healthBarUI.transform.Find("BG");
            bgImage = bgTransform.GetComponent<UnityEngine.UI.Image>();
            fillImage = bgTransform.Find("Fill").GetComponent<UnityEngine.UI.Image>();
            fillImage.type = UnityEngine.UI.Image.Type.Filled;
            fillImage.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;

            SetupUILayout();
            UpdateHealthBar(); // 初始化血条填充
            ShowHealthBar(); // 创建后立即显示
            isUICreated = true; // 标记UI已创建
        }
        
        private Vector3 GetHeadOffset()
        {
            // 完全基于敌人对象的position，使用固定偏移值
            // 不再依赖碰撞体或渲染器边界
            return new Vector3(0, 1f, 0);
        }

        private void ShowHealthBar()
        {
            if (healthBarUI != null)
            {
                healthBarUI.SetActive(true);
            }
        }

        private void HideHealthBar()
        {
            if (healthBarUI != null)
            {
                healthBarUI.SetActive(false);
            }
        }
        private void SetupUILayout()
        {
            // HealthBar使用WorldSpace渲染模式，不需要设置锚点
            // 位置通过transform.position在世界坐标中设置
            if (bgImage != null)
            {
                RectTransform bgRect = bgImage.rectTransform;
                // 使用配置的缩放值
                bgRect.localScale = (Vector3.one * baseScale) * Plugin.NormalScale.Value;
                // 使用配置的宽高偏移
                bgRect.sizeDelta =  new Vector2(320+Plugin.NormalWidthOffset.Value, 32+Plugin.NormalHeightOffset.Value);
                fillImage.rectTransform.sizeDelta = bgRect.sizeDelta;   
            }
        }
        
        // 刷新布局方法，供Plugin调用
        public void RefreshLayout()
        {
            SetupUILayout();
        }

        private void UpdateMaxHealth()
        {
            if (healthManager == null) return;
            
            float currentHp = healthManager.hp;
            
            // 如果当前血量大于初始最大血量，且不超过3000的上限阈值，则更新最大血量
            if (currentHp > maxHealth && currentHp <= 3000)
            {
                maxHealth = currentHp;
            }
        }

        private void UpdateHealthBar()
        {
            if (fillImage == null || healthManager == null)
                return;

            float healthPercent = healthManager.hp / maxHealth;
            fillImage.fillAmount = healthPercent;
        }

        private void CheckVisibilityConditions()
        {
            if (healthBarUI == null || playerTransform == null) return;

            bool shouldShow = true;

            // 1. 检查距离
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            if (distance > detectionDistance)
            {
                shouldShow = false;
            }

            // 2. 检查对象激活状态
            if (!gameObject.activeInHierarchy)
            {
                shouldShow = false;
            }

            // 3. 检查2D碰撞体激活状态
            if (!colliders2D || !colliders2D.enabled)
            {
                shouldShow = false;
            }

            // 4. 检查3秒无伤害隐藏逻辑
            if (isDamageVisible && Time.time - lastDamageTime > Plugin.NormalHideDelay.Value)
            {
                isDamageVisible = false;
            }

            // 5. 只有在受过伤害时才显示血条
            if (!isDamageVisible)
            {
                shouldShow = false;
            }

            if (healthBarUI.activeSelf != shouldShow)
            {
                if (shouldShow)
                {
                    ShowHealthBar();
                }
                else
                {
                    HideHealthBar();
                }
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
            isUICreated = false; // 重置创建标记
            
            // 不销毁共享Canvas，让其他血条继续使用
            // 只有当没有其他血条时才销毁共享Canvas
        }
    }

    public class BossHealthBar : MonoBehaviour
    {
        private HealthManager healthManager;
        private float lastHealth;
        private float maxHealth;

        public float detectionDistance = 50f;
        private Transform playerTransform;

        private GameObject healthBarUI;
        private Canvas canvas;
        private UnityEngine.UI.Image bgImage;
        private UnityEngine.UI.Image fillImage;
        private Text bossNameText;
        private UnityEngine.UI.Image leftImage;
        private UnityEngine.UI.Image rightImage;

        public static float baseScale = 0.5f;
        private float fixedDistance = 150f;
        
        private bool isExpanding = false; // 是否正在展开动画
        private bool hasExpanded = false; // 是否已经展开过
        private Vector2 originalSize; // 原始尺寸
        
        // 公共方法：检查是否正在展开动画
        public bool IsExpanding()
        {
            return isExpanding;
        }
        
      
        PlayMakerFSM fsm;

        void Awake()
        { 
            if(GetComponents<BossHealthBar>().Length > 1)
            {
                Destroy(this);
                return;
            }
            var HB = GetComponent<HealthBar>();
            if (HB != null)
            {
                Destroy(HB);
            }
            healthManager = GetComponent<HealthManager>();
            if (gameObject.name == "Silk Boss" && healthManager.hp >= 1000)
            {
                maxHealth = 100;
                lastHealth = 100;
            }
            else
            {
                maxHealth = healthManager.hp;
                lastHealth = healthManager.hp;
            }
            Plugin.Log.LogInfo($"Boss对象名:{gameObject.name}");

            playerTransform = FindFirstObjectByType<HeroController>().transform;        
            fsm = gameObject.GetComponent<PlayMakerFSM>();

            if (healthBarUI == null)
            {
                CreateHealthBarUI();
            }
        }
        
        void Update()
        {
            if (healthManager == null) return;

            int currentIndex = BossHealthBarManager.Instance.GetActiveHealthBarCount() ;
            if (currentIndex < 2)
            {
                SetBossName(false);
            }

            float currentHealth = healthManager.hp;
            
            // 实时更新最大生命值（处理BOSS转阶段情况）
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

           

            // 检查各种隐藏条件
            CheckVisibilityConditions();
        }

        private void CreateHealthBarUI()
        {
            if (healthBarUI != null) return;

            // 检查是否可以创建新的血条（多血条兼容逻辑）
            if (!BossHealthBarManager.Instance.RegisterHealthBar(this))
            {
                // 已达到最大血条数量，不创建新血条
                return;
            }

            healthBarUI = UnityEngine.Object.Instantiate(Plugin.bossHealthBarPrefab);
            canvas = healthBarUI.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var canvasScaler = healthBarUI.GetComponent<UnityEngine.UI.CanvasScaler>();
            if (canvasScaler != null) UnityEngine.Object.DestroyImmediate(canvasScaler);

            Transform bgTransform = healthBarUI.transform.Find("BG");
            bgImage = bgTransform.GetComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(bgImage.color.r, bgImage.color.g, bgImage.color.b, 0.25f);

            fillImage = bgTransform.Find("Fill").GetComponent<UnityEngine.UI.Image>();
            fillImage.type = UnityEngine.UI.Image.Type.Filled;
            fillImage.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            bossNameText = bgTransform.Find("BossName").GetComponent<Text>();
            leftImage = bgTransform.Find("BossName").Find("Left").GetComponent<UnityEngine.UI.Image>();
            rightImage = bgTransform.Find("BossName").Find("Right").GetComponent<UnityEngine.UI.Image>();

            SetupUILayout();
            
            // 保存原始尺寸用于展开动画
            if (bgImage != null)
            {
                originalSize = bgImage.rectTransform.sizeDelta;
            }
            
            bossNameText.text = GetBossName();
            bossNameText.fontSize = 65;
            UpdateNameLayout();
            
            // 更新BOSS标题文本
           UpdateBossTitle();
            
            // 确保血条位置正确排列（在UI完全设置后重新排列）
            BossHealthBarManager.Instance.ArrangeHealthBars();
            
            HideHealthBar();
            
            // 确保当前血条的BossName状态正确（如果是第二个血条则隐藏）
            int currentIndex = BossHealthBarManager.Instance.GetActiveHealthBarCount() - 1;
            if (currentIndex > 0)
            {
                SetBossName(true);
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
        
        private System.Collections.IEnumerator ExpandHealthBarAnimation()
        {
            if (bgImage == null || fillImage == null) yield break;
            
            isExpanding = true;
            
            // 保存原始透明度和填充值
            float originalBgAlpha = bgImage.color.a;
            float originalFillAmount = fillImage.fillAmount;
            
            // 获取Fill下的Right组件
            RectTransform fillRect = fillImage.rectTransform;
            RectTransform rightTransform = fillRect.Find("Right")?.GetComponent<RectTransform>();
            Vector2 originalRightPosition = Vector2.zero;
            if (rightTransform != null)
            {
                originalRightPosition = rightTransform.anchoredPosition;
            }
            
            // 动画开始时隐藏BG，Fill填充设为0
            Color bgColor = bgImage.color;
            bgColor.a = 0f;
            bgImage.color = bgColor;
            fillImage.fillAmount = 0f;
            
            // Right组件初始位置设为血条最左边
            if (rightTransform != null)
            {
                float fillWidth = fillRect.sizeDelta.x;
                rightTransform.anchoredPosition = new Vector2((-fillWidth / 2f)+65f , originalRightPosition.y);
            }
            
            float elapsedTime = 0f;
            float duration = Plugin.BossExpandDuration.Value;
            
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / duration;
                
                // 使用缓动函数让动画更自然
                float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
                
                // Fill从0到1缓慢填充，实现从左到右展开效果
                fillImage.fillAmount = easedProgress;
                
                // Right组件同步从最左边向右移动
                if (rightTransform != null)
                {
                    float fillWidth = fillRect.sizeDelta.x;
                    float currentRightX = (-fillWidth / 2f) + 65f + (fillWidth * easedProgress);
                    rightTransform.anchoredPosition = new Vector2(currentRightX, originalRightPosition.y);
                }
                
                yield return null;
            }
            
            // 恢复原始状态
            fillImage.fillAmount = originalFillAmount;
            bgColor.a = originalBgAlpha;
            bgImage.color = bgColor;
            if (rightTransform != null)
            {
                rightTransform.anchoredPosition = originalRightPosition;
            }
            
            isExpanding = false;
            hasExpanded = true;
        }
        


        private void HideHealthBar()
        {
            if (healthBarUI != null)
            {
                healthBarUI.SetActive(false);
            }
        }
        private void SetupUILayout()
        {
            if (bgImage != null)
            {
                RectTransform bgRect = bgImage.rectTransform;
                
                // 总是设置锚点和初始位置，让ArrangeHealthBars后续重新排列
                // 设置锚点为屏幕底部居中
                bgRect.anchorMin = new Vector2(0.5f, 0f);
                bgRect.anchorMax = new Vector2(0.5f, 0f);
                bgRect.pivot = new Vector2(0.5f, 0f);
                // 设置初始位置为屏幕80%顶部，加上配置的偏移
                bgRect.anchoredPosition = new Vector2(Plugin.BossPositionX.Value, Screen.height * 0.82f + Plugin.BossPositionY.Value);
                
                // 使用配置的缩放值
                bgRect.localScale = (Vector3.one * baseScale ) * Plugin.BossScale.Value;
                // 使用配置的宽高偏移
                bgRect.sizeDelta = new Vector2(1572,35)+ new Vector2(Plugin.BossWidthOffset.Value, Plugin.BossHeightOffset.Value);
                RectTransform FillRt = bgRect.Find("Fill").GetComponent<RectTransform>();
                FillRt.sizeDelta = bgRect.sizeDelta;

                RectTransform Fill_RightRT= FillRt.Find("Right")?.GetComponent<RectTransform>();
                var Fill_LeftRT= FillRt.Find("Left")?.GetComponent<RectTransform>();
                if(Fill_LeftRT!= null && Fill_RightRT != null)
                {
                    // 计算当前Fill的实际宽度
                    float currentFillWidth = FillRt.sizeDelta.x;
                    // 保持Left和Right与Fill边缘的固定间距
                    // 默认情况：Fill宽度1572，Left位置-721，Right位置721
                    // 间距 = (1572 - (721 - (-721))) / 2 = (1572 - 1442) / 2 = 65
                    float edgeOffset = 65f; // Left和Right与Fill边缘的固定间距
                    float halfFillWidth = currentFillWidth / 2f;
                    
                    Fill_LeftRT.anchoredPosition = new Vector2(-halfFillWidth - edgeOffset, Fill_LeftRT.anchoredPosition.y);
                    Fill_RightRT.anchoredPosition = new Vector2(halfFillWidth + edgeOffset, Fill_RightRT.anchoredPosition.y);
                } 

              
            }
        }
        
        // 刷新布局方法，供Plugin调用
        public void RefreshLayout()
        {
            SetupUILayout();
        }

        private void UpdateNameLayout()
        {
            if (bossNameText == null || leftImage == null || rightImage == null) return;
            


            Vector2 textSize;
            try
            {
                TextGenerator textGen = new TextGenerator();
                TextGenerationSettings generationSettings = bossNameText.GetGenerationSettings(Vector2.zero);
                float width = textGen.GetPreferredWidth(bossNameText.text, generationSettings);
                float height = textGen.GetPreferredHeight(bossNameText.text, generationSettings);
                textSize = new Vector2(width, height);
                
            }
            catch (System.Exception)
            {
                textSize = new Vector2(200f, 30f);
            }

            // 调整BossName的RectTransform大小以适应文本内容，只扩充左右大小，上下尺寸保持不变
            RectTransform bossNameRect = bossNameText.rectTransform;
            bossNameRect.sizeDelta = new Vector2(textSize.x, bossNameRect.sizeDelta.y);
            

            RectTransform leftRect = leftImage.rectTransform;
            RectTransform rightRect = rightImage.rectTransform;

            float actualTextWidth = textSize.x;
            float halfTextWidth = actualTextWidth * 0.5f;

            // 保持Left和Right的固定间隔
            leftRect.anchoredPosition = new Vector2(-halfTextWidth - fixedDistance, 0f);
            rightRect.anchoredPosition = new Vector2(halfTextWidth + fixedDistance, 0f);
        }

        private void UpdateHealthBar()
        {
            if (fillImage == null || healthManager == null)
                return;

            float healthPercent = healthManager.hp / maxHealth;
            fillImage.fillAmount = healthPercent;
        }

        private void UpdateMaxHealth()
        {
            if (healthManager == null) return;
            
            float currentHp = healthManager.hp;
            
            // 如果当前血量大于初始最大血量，且不超过3000的上限阈值，则更新最大血量
            if (currentHp > maxHealth && currentHp <= 3000)
            {
                
                maxHealth = currentHp;
            }
        }

     
        private void CheckVisibilityConditions()
        {
            if (healthBarUI == null || playerTransform == null) return;

            bool shouldShow = true;
       
            //  检查距离
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            if (distance > detectionDistance)
            {
                shouldShow = false;
            }

          


            if (healthBarUI.activeSelf != shouldShow)
            {
                if (shouldShow)
                {
                    ShowHealthBar();
                }
                else
                {
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

        /// <summary>
        /// 获取当前地图区域
        /// </summary>
        /// <returns>当前MapZone枚举值</returns>
        private MapZone GetCurrentMapZone()
        {
            try
            {
                var gameManager = GameManager.instance;
                if (gameManager != null)
                {
                    var gameMap = gameManager.gameMap;
                    if (gameMap != null)
                    {
                        return gameMap.GetCurrentMapZone();
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"获取当前MapZone失败: {ex.Message}");
            }
            return MapZone.NONE;
        }

        /// <summary>
        /// 获取区域的本地化名称
        /// </summary>
        /// <param name="mapZone">地图区域枚举</param>
        /// <returns>本地化的区域名称</returns>
        private string GetLocalizedAreaName(MapZone mapZone)
        {
            try
            {
                // 根据MapZone枚举获取对应的本地化键值
                string localizationKey =mapZone.ToString();
                if (!string.IsNullOrEmpty(localizationKey))
                {
                    return Language.Get(localizationKey, "Map Zones");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"获取区域本地化名称失败: {ex.Message}");
            }
            return mapZone.ToString(); // 如果获取失败，返回枚举名称
        }

        
        /// <summary>
        /// 更新BOSS血条标题文本
        /// </summary>
        private void UpdateBossTitle()
        {
            try
            {
                // 获取当前地图区域
                MapZone currentMapZone = GetCurrentMapZone();
                
                // 获取本地化的区域名称
                string localAreaName = GetLocalizedAreaName(currentMapZone);
                
                // 查找Title对象的Text组件
                if (healthBarUI != null)
                {
                    Transform bgTransform = healthBarUI.transform.Find("BG");
                    if (bgTransform != null)
                    {
                        Transform titleTransform = bgTransform.Find("BossName/Title");
                        if (titleTransform != null)
                        {
                            Text titleText = titleTransform.GetComponent<Text>();
                            if (titleText != null)
                            {
                                // 更新文本内容为"盘踞于{LocalAreaName}的"
                                titleText.text = $"{Plugin.PreTitle.Value}{localAreaName}{Plugin.PostTitle.Value}";
                                // 为Title的字体大小增加10
                                titleText.fontSize += 10;
                                Plugin.Log.LogInfo($"BOSS标题已更新: {titleText.text}, 字体大小: {titleText.fontSize}");
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
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"更新BOSS标题失败: {ex.Message}");
            }
        }

        private string GetBossName()
        {
            string displayName = healthManager.gameObject.name;
            try
            {
                // 方法1: 尝试从EnemyDeathEffects的journalRecord获取
                var enemyDeathEffects = healthManager.gameObject.GetComponent<EnemyDeathEffects>();
                if (enemyDeathEffects != null && enemyDeathEffects.journalRecord != null)
                {
                    var localizedName = enemyDeathEffects.journalRecord.displayName;
                    if (!string.IsNullOrEmpty(localizedName))
                    {
                        displayName = localizedName;
                        return displayName;
                    }
                }
                
                
                // 方法2: 尝试通过EnemyJournalManager中的敌人记录匹配名称
                var allEnemies = EnemyJournalManager.GetAllEnemies();
                string gameObjectName = healthManager.gameObject.name;
                string[] gameObjectWords = gameObjectName.Split(new char[] { ' ', '(', ')', '_', '-' }, System.StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var enemy in allEnemies)
                {
                    // enemy.name和healthManager.gameObject.name的格式一般都是  AAA BBB CCC(也许空格分割开的多段名字  但是段数不确定)
                    //只要其中有两段字符匹配就算匹配成功. 比如healthManager.gameObject.name是 Moss Bone Mother 然后 allEnemies中有一个是Moss Mother,也视为匹配成功
                    //匹配成功后取其enemy.displayName作为Boss名称 
                    
                    if (enemy == null || string.IsNullOrEmpty(enemy.name))
                        continue;
                        
                    string[] enemyWords = enemy.name.Split(new char[] { ' ', '(', ')', '_', '-' }, System.StringSplitOptions.RemoveEmptyEntries);
                    
                    // 计算匹配的单词数量
                    int matchCount = 0;
                    foreach (string gameWord in gameObjectWords)
                    {
                        if (string.IsNullOrEmpty(gameWord) || gameWord.Length < 2)
                            continue;
                            
                        foreach (string enemyWord in enemyWords)
                        {
                            if (string.IsNullOrEmpty(enemyWord) || enemyWord.Length < 2)
                                continue;
                                
                            // 忽略大小写进行比较
                            if (string.Equals(gameWord, enemyWord, System.StringComparison.OrdinalIgnoreCase))
                            {
                                matchCount++;
                                break; // 找到匹配后跳出内层循环
                            }
                        }
                    }
                    
                    // 如果匹配了至少2个单词，认为是匹配成功
                    if (matchCount >= 2 && !string.IsNullOrEmpty(enemy.displayName))
                    {
                        Plugin.Log.LogInfo($"通过EnemyJournalManager匹配到Boss名称: {enemy.displayName} (匹配单词数: {matchCount})");
                        return enemy.displayName;
                    }
                }
                
                // 如果没有找到匹配度>=2的，尝试匹配度为1的作为备选
                foreach (var enemy in allEnemies)
                {
                    if (enemy == null || string.IsNullOrEmpty(enemy.name))
                        continue;
                        
                    string[] enemyWords = enemy.name.Split(new char[] { ' ', '(', ')', '_', '-' }, System.StringSplitOptions.RemoveEmptyEntries);
                    
                    foreach (string gameWord in gameObjectWords)
                    {
                        if (string.IsNullOrEmpty(gameWord) || gameWord.Length < 3) // 单个匹配时要求更长的单词
                            continue;
                            
                        foreach (string enemyWord in enemyWords)
                        {
                            if (string.IsNullOrEmpty(enemyWord) || enemyWord.Length < 3)
                                continue;
                                
                            if (string.Equals(gameWord, enemyWord, System.StringComparison.OrdinalIgnoreCase))
                            {
                                if (!string.IsNullOrEmpty(enemy.displayName))
                                {
                                    Plugin.Log.LogInfo($"通过EnemyJournalManager单词匹配到Boss名称: {enemy.displayName} (匹配单词: {gameWord})");
                                    return enemy.displayName;
                                }
                            }
                        }
                    }
                }

                // 方法2: 清理GameObject名称作为fallback
                displayName = CleanGameObjectName(healthManager.gameObject.name);

            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning($"获取Boss名称时发生异常: {ex.Message}");
                // 异常情况下的fallback
                displayName = CleanGameObjectName(healthManager.gameObject.name);
            }
            return displayName;
        }

        /// <summary>
        /// 尝试通过BossStatue获取Boss的本地化名称
        /// </summary>
       
        /// <summary>
        /// 清理GameObject名称，移除括号内容等
        /// </summary>
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
            
            // 移除常见的后缀
            string[] suffixesToRemove = { " Clone", "(Clone)", " Instance", "(Instance)" };
            foreach (string suffix in suffixesToRemove)
            {
                if (originalName.EndsWith(suffix))
                {
                    originalName = originalName.Substring(0, originalName.Length - suffix.Length).Trim();
                }
            }
            
            return string.IsNullOrEmpty(originalName) ? "未知Boss" : originalName;
        }

        /// <summary>
        /// 获取血条的RectTransform，用于位置管理
        /// </summary>
        /// <returns>血条的RectTransform</returns>
        public RectTransform GetHealthBarTransform()
        {
            if (healthBarUI != null && bgImage != null)
            {
                return bgImage.rectTransform;
            }
            return null;
        }

        /// <summary>
        /// 隐藏BossName组件（用于多血条时的第二个血条）
        /// </summary>
        public void SetBossName(bool isHide)
        {
            if (bossNameText != null )
            {
                // 隐藏整个BossName GameObject，包括文本和左右图片
                RectTransform bossNameTransform = bossNameText.rectTransform;
                bossNameTransform.gameObject.SetActive(!isHide);
            }
        }

        private void OnDestroy()
        {
            // 从管理器中注销血条
            if (BossHealthBarManager.Instance != null)
            {
                BossHealthBarManager.Instance.UnregisterHealthBar(this);
                


            }
            
            if (healthBarUI != null)
            {
                Destroy(healthBarUI);
            }
        }
    }
}
