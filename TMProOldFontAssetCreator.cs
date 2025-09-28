using System.Collections.Generic;
using UnityEngine;
using TMProOld;

/// <summary>
/// TMProOld字体资源运行时创建器
/// 提供程序化创建TMProOld字体资源的方法
/// </summary>
public static class TMProOldFontAssetCreator
{
    /// <summary>
    /// 从Unity字体创建TMProOld字体资源
    /// </summary>
    /// <param name="sourceFont">源Unity字体</param>
    /// <param name="fontSize">字体大小</param>
    /// <param name="characterSet">字符集</param>
    /// <param name="atlasWidth">图集宽度</param>
    /// <param name="atlasHeight">图集高度</param>
    /// <param name="padding">字符间距</param>
    /// <returns>创建的TMProOld字体资源</returns>
    public static TMP_FontAsset CreateFontAsset(Font sourceFont, int fontSize = 32, string characterSet = null, int atlasWidth = 512, int atlasHeight = 512, int padding = 5)
    {
        if (sourceFont == null)
        {
            Debug.LogError("源字体不能为空");
            return null;
        }
        
        // 默认字符集
        if (string.IsNullOrEmpty(characterSet))
        {
            characterSet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,!?-_+=/\\()[]{}:;\"'<>|`~@#$%^&*";
        }
        
        // 创建字体资源
        TMP_FontAsset fontAsset = ScriptableObject.CreateInstance<TMP_FontAsset>();
        fontAsset.name = sourceFont.name + "_TMProOld";
        fontAsset.hideFlags = HideFlags.None; // 确保对象可以被正确管理
        
        // 设置字体类型
        fontAsset.fontAssetType = TMP_FontAsset.FontAssetTypes.SDF;
        
        // 创建字体面信息
        FaceInfo faceInfo = CreateFaceInfo(sourceFont, fontSize, atlasWidth, atlasHeight, padding);
        fontAsset.AddFaceInfo(faceInfo);
        
        // 创建字形信息
        TMP_Glyph[] glyphs = CreateGlyphs(characterSet, fontSize, atlasWidth, atlasHeight, padding);
        fontAsset.AddGlyphInfo(glyphs);
        
        // 创建字距调整表
        KerningTable kerningTable = CreateKerningTable();
        fontAsset.AddKerningInfo(kerningTable);
        
        // 设置字体创建设置
        fontAsset.fontCreationSettings = CreateFontCreationSettings(sourceFont, fontSize, atlasWidth, atlasHeight, padding);
        
        // 创建图集纹理
        fontAsset.atlas = CreateAtlasTexture(atlasWidth, atlasHeight, fontAsset.name);
        
        // 创建材质
        fontAsset.material = CreateFontMaterial(fontAsset.atlas, fontAsset.name);
        
        return fontAsset;
    }
    
    /// <summary>
    /// 创建字体面信息
    /// </summary>
    private static FaceInfo CreateFaceInfo(Font sourceFont, int fontSize, int atlasWidth, int atlasHeight, int padding)
    {
        FaceInfo faceInfo = new FaceInfo();
        faceInfo.Name = sourceFont.name;
        faceInfo.PointSize = fontSize;
        faceInfo.Scale = 1.0f;
        faceInfo.LineHeight = fontSize * 1.2f;
        faceInfo.Baseline = fontSize * 0.8f;
        faceInfo.Ascender = fontSize * 0.8f;
        faceInfo.CapHeight = fontSize * 0.7f;
        faceInfo.Descender = fontSize * -0.2f;
        faceInfo.CenterLine = fontSize * 0.3f;
        faceInfo.SuperscriptOffset = fontSize * 0.4f;
        faceInfo.SubscriptOffset = fontSize * -0.1f;
        faceInfo.SubSize = fontSize * 0.5f;
        faceInfo.Underline = fontSize * -0.1f;
        faceInfo.UnderlineThickness = fontSize * 0.05f;
        faceInfo.TabWidth = fontSize * 4f;
        faceInfo.Padding = padding;
        faceInfo.AtlasWidth = atlasWidth;
        faceInfo.AtlasHeight = atlasHeight;
        
        return faceInfo;
    }
    
    /// <summary>
    /// 创建字形数组
    /// </summary>
    private static TMP_Glyph[] CreateGlyphs(string characterSet, int fontSize, int atlasWidth, int atlasHeight, int padding)
    {
        List<TMP_Glyph> glyphs = new List<TMP_Glyph>();
        
        // 计算每行每列可以放置的字符数
        int charsPerRow = Mathf.FloorToInt(atlasWidth / (fontSize + padding * 2));
        int charsPerCol = Mathf.FloorToInt(atlasHeight / (fontSize + padding * 2));
        
        float glyphWidth = fontSize;
        float glyphHeight = fontSize;
        
        for (int i = 0; i < characterSet.Length; i++)
        {
            char character = characterSet[i];
            
            // 计算字符在图集中的位置
            int row = i / charsPerRow;
            int col = i % charsPerRow;
            
            if (row >= charsPerCol)
            {
                Debug.LogWarning($"字符集过大，超出图集容量。字符 '{character}' 将被跳过。");
                break;
            }
            
            TMP_Glyph glyph = new TMP_Glyph();
            glyph.id = (int)character;
            glyph.x = col * (fontSize + padding * 2) + padding;
            glyph.y = row * (fontSize + padding * 2) + padding;
            glyph.width = glyphWidth;
            glyph.height = glyphHeight;
            glyph.xOffset = 0;
            glyph.yOffset = fontSize * 0.8f;
            glyph.xAdvance = GetCharacterAdvance(character, fontSize);
            glyph.scale = 1.0f;
            
            glyphs.Add(glyph);
        }
        
        return glyphs.ToArray();
    }
    
    /// <summary>
    /// 获取字符的前进宽度
    /// </summary>
    private static float GetCharacterAdvance(char character, int fontSize)
    {
        // 根据字符类型返回不同的前进宽度
        switch (character)
        {
            case ' ':
                return fontSize * 0.25f;
            case 'i':
            case 'l':
            case 'I':
            case '1':
            case '!':
            case '|':
                return fontSize * 0.3f;
            case 'w':
            case 'W':
            case 'm':
            case 'M':
                return fontSize * 0.8f;
            default:
                return fontSize * 0.6f;
        }
    }
    
    /// <summary>
    /// 创建字距调整表
    /// </summary>
    private static KerningTable CreateKerningTable()
    {
        KerningTable kerningTable = new KerningTable();
        kerningTable.kerningPairs = new List<KerningPair>();
        
        // 可以在这里添加常见的字距调整对
        // 例如: A-V, T-o, W-a 等
        
        return kerningTable;
    }
    
    /// <summary>
    /// 创建字体创建设置
    /// </summary>
    private static FontCreationSetting CreateFontCreationSettings(Font sourceFont, int fontSize, int atlasWidth, int atlasHeight, int padding)
    {
        FontCreationSetting settings = new FontCreationSetting();
        settings.fontSourcePath = "";
        settings.fontSizingMode = 0;
        settings.fontSize = fontSize;
        settings.fontPadding = padding;
        settings.fontPackingMode = 0;
        settings.fontAtlasWidth = atlasWidth;
        settings.fontAtlasHeight = atlasHeight;
        settings.fontCharacterSet = 0;
        settings.fontStyle = 0;
        settings.fontStlyeModifier = 0f;
        settings.fontRenderMode = 0;
        settings.fontKerning = true;
        
        return settings;
    }
    
    /// <summary>
    /// 创建图集纹理
    /// </summary>
    private static Texture2D CreateAtlasTexture(int width, int height, string name)
    {
        Texture2D atlas = new Texture2D(width, height, TextureFormat.Alpha8, false);
        atlas.name = name + "_Atlas";
        atlas.hideFlags = HideFlags.None; // 确保纹理可以被正确管理
        
        // 创建白色纹理（实际应用中应该渲染真实的字体字形）
        Color32[] pixels = new Color32[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color32(255, 255, 255, 255);
        }
        
        atlas.SetPixels32(pixels);
        atlas.Apply();
        
        return atlas;
    }
    
    /// <summary>
    /// 创建字体材质
    /// </summary>
    private static Material CreateFontMaterial(Texture2D atlas, string name)
    {
        // 尝试找到合适的着色器
        Shader shader = Shader.Find("TextMeshPro/Distance Field");
        if (shader == null)
        {
            shader = Shader.Find("UI/Default");
        }
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }
        
        Material material = new Material(shader);
        material.name = name + "_Material";
        material.hideFlags = HideFlags.None; // 确保材质可以被正确管理
        material.mainTexture = atlas;
        
        return material;
    }
    
    /// <summary>
    /// 从现有的TMP字体资源转换为TMProOld字体资源
    /// </summary>
    /// <param name="tmpFontAsset">TMP字体资源</param>
    /// <returns>转换后的TMProOld字体资源</returns>
    public static TMP_FontAsset ConvertFromTMPFontAsset(UnityEngine.TextCore.Text.FontAsset tmpFontAsset)
    {
        if (tmpFontAsset == null)
        {
            Debug.LogError("TMP字体资源不能为空");
            return null;
        }
        
        Debug.LogWarning("TMP到TMProOld的转换功能需要根据具体的TMP版本实现");
        
        // 这里需要根据具体的TextMeshPro版本来实现转换逻辑
        // 由于不同版本的TMP结构可能不同，这里只提供框架
        
        return null;
    }
}