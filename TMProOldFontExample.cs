using UnityEngine;
using TMProOld;

/// <summary>
/// TMProOld字体资源使用示例
/// 展示如何创建和使用TMProOld字体资源
/// </summary>
public class TMProOldFontExample : MonoBehaviour
{
    [Header("字体创建参数")]
    public Font sourceFont;
    public int fontSize = 32;
    public string characterSet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,!?-";
    public int atlasWidth = 512;
    public int atlasHeight = 512;
    public int padding = 5;
    
    [Header("创建的字体资源")]
    public TMP_FontAsset createdFontAsset;
    
    [Header("测试组件")]
    public TextMeshPro textMeshPro;
    public TextMeshProUGUI textMeshProUGUI;
    
    private void Start()
    {
        // 如果没有指定源字体，尝试使用默认字体
        if (sourceFont == null)
        {
            sourceFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
    
    /// <summary>
    /// 创建TMProOld字体资源
    /// </summary>
    [ContextMenu("创建字体资源")]
    public void CreateFontAsset()
    {
        if (sourceFont == null)
        {
            Debug.LogError("请先指定源字体");
            return;
        }
        
        // 使用字体创建器创建字体资源
        createdFontAsset = TMProOldFontAssetCreator.CreateFontAsset(
            sourceFont, 
            fontSize, 
            characterSet, 
            atlasWidth, 
            atlasHeight, 
            padding
        );
        
        if (createdFontAsset != null)
        {
            Debug.Log($"成功创建TMProOld字体资源: {createdFontAsset.name}");
            
            // 自动应用到测试组件
            ApplyFontToComponents();
        }
        else
        {
            Debug.LogError("创建字体资源失败");
        }
    }
    
    /// <summary>
    /// 将创建的字体应用到测试组件
    /// </summary>
    [ContextMenu("应用字体到组件")]
    public void ApplyFontToComponents()
    {
        if (createdFontAsset == null)
        {
            Debug.LogError("没有可用的字体资源");
            return;
        }
        
        // 应用到TextMeshPro组件
        if (textMeshPro != null)
        {
            textMeshPro.font = createdFontAsset;
            textMeshPro.text = "TextMeshPro测试文本\nTesting TMProOld Font";
            Debug.Log("字体已应用到TextMeshPro组件");
        }
        
        // 应用到TextMeshProUGUI组件
        if (textMeshProUGUI != null)
        {
            textMeshProUGUI.font = createdFontAsset;
            textMeshProUGUI.text = "TextMeshProUGUI测试文本\nTesting TMProOld Font";
            Debug.Log("字体已应用到TextMeshProUGUI组件");
        }
    }
    
    /// <summary>
    /// 测试字体资源的字符支持
    /// </summary>
    [ContextMenu("测试字符支持")]
    public void TestCharacterSupport()
    {
        if (createdFontAsset == null)
        {
            Debug.LogError("没有可用的字体资源");
            return;
        }
        
        string testText = "Hello World! 你好世界! 123";
        
        Debug.Log($"测试文本: {testText}");
        
        foreach (char c in testText)
        {
            bool hasChar = createdFontAsset.HasCharacter(c);
            Debug.Log($"字符 '{c}' (ASCII: {(int)c}): {(hasChar ? "支持" : "不支持")}");
        }
        
        // 测试缺失字符
        System.Collections.Generic.List<char> missingChars;
        bool hasAllChars = createdFontAsset.HasCharacters(testText, out missingChars);
        
        if (hasAllChars)
        {
            Debug.Log("所有字符都被支持!");
        }
        else
        {
            Debug.LogWarning($"缺失 {missingChars.Count} 个字符: {string.Join(", ", missingChars)}");
        }
    }
    
    /// <summary>
    /// 显示字体资源信息
    /// </summary>
    [ContextMenu("显示字体信息")]
    public void ShowFontInfo()
    {
        if (createdFontAsset == null)
        {
            Debug.LogError("没有可用的字体资源");
            return;
        }
        
        var fontInfo = createdFontAsset.fontInfo;
        
        Debug.Log($"字体信息:\n" +
                 $"名称: {fontInfo.Name}\n" +
                 $"点大小: {fontInfo.PointSize}\n" +
                 $"缩放: {fontInfo.Scale}\n" +
                 $"字符数量: {fontInfo.CharacterCount}\n" +
                 $"行高: {fontInfo.LineHeight}\n" +
                 $"基线: {fontInfo.Baseline}\n" +
                 $"上升高度: {fontInfo.Ascender}\n" +
                 $"下降高度: {fontInfo.Descender}\n" +
                 $"图集尺寸: {fontInfo.AtlasWidth} x {fontInfo.AtlasHeight}\n" +
                 $"填充: {fontInfo.Padding}");
        
        Debug.Log($"字体类型: {createdFontAsset.fontAssetType}");
        Debug.Log($"字符字典大小: {createdFontAsset.characterDictionary?.Count ?? 0}");
    }
    
    /// <summary>
    /// 清理创建的资源
    /// </summary>
    [ContextMenu("清理资源")]
    public void CleanupResources()
    {
        if (createdFontAsset != null)
        {
            // 清理纹理
            if (createdFontAsset.atlas != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(createdFontAsset.atlas);
                }
                else
                {
                    // 在编辑器中安全销毁，避免持久化错误
                    #if UNITY_EDITOR
                    UnityEditor.Undo.DestroyObjectImmediate(createdFontAsset.atlas);
                    #endif
                }
            }
            
            // 清理材质
            if (createdFontAsset.material != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(createdFontAsset.material);
                }
                else
                {
                    // 在编辑器中安全销毁，避免持久化错误
                    #if UNITY_EDITOR
                    UnityEditor.Undo.DestroyObjectImmediate(createdFontAsset.material);
                    #endif
                }
            }
            
            // 清理字体资源
            if (Application.isPlaying)
            {
                Destroy(createdFontAsset);
            }
            else
            {
                // 在编辑器中安全销毁，避免持久化错误
                #if UNITY_EDITOR
                UnityEditor.Undo.DestroyObjectImmediate(createdFontAsset);
                #endif
            }
            
            createdFontAsset = null;
            
            Debug.Log("资源已清理");
        }
    }
    
    private void OnDestroy()
    {
        // 自动清理资源
        if (Application.isPlaying)
        {
            CleanupResources();
        }
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// 在编辑器中保存字体资源到文件
    /// </summary>
    [ContextMenu("保存字体资源到文件")]
    public void SaveFontAssetToFile()
    {
        if (createdFontAsset == null)
        {
            Debug.LogError("没有可用的字体资源");
            return;
        }
        
        string path = UnityEditor.EditorUtility.SaveFilePanel(
            "保存TMProOld字体资源",
            "Assets",
            createdFontAsset.name,
            "asset"
        );
        
        if (!string.IsNullOrEmpty(path))
        {
            // 转换为相对路径
            if (path.StartsWith(Application.dataPath))
            {
                path = "Assets" + path.Substring(Application.dataPath.Length);
            }
            
            // 保存图集
            string atlasPath = path.Replace(".asset", "_Atlas.asset");
            UnityEditor.AssetDatabase.CreateAsset(createdFontAsset.atlas, atlasPath);
            
            // 保存材质
            string materialPath = path.Replace(".asset", "_Material.mat");
            UnityEditor.AssetDatabase.CreateAsset(createdFontAsset.material, materialPath);
            
            // 保存字体资源
            UnityEditor.AssetDatabase.CreateAsset(createdFontAsset, path);
            
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            
            Debug.Log($"字体资源已保存到: {path}");
            
            // 选中保存的资源
            UnityEditor.Selection.activeObject = createdFontAsset;
            UnityEditor.EditorGUIUtility.PingObject(createdFontAsset);
        }
    }
    #endif
}