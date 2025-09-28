using System;
using System.Collections.Generic;
using UnityEngine;
using TMProOld;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// TMProOld字体资源创建工具
/// 用于创建兼容TMProOld组件的字体资源
/// </summary>
public class TMProOldFontCreator : EditorWindow
{
    private Font sourceFont;
    private int fontSize = 32;
    private int atlasWidth = 512;
    private int atlasHeight = 512;
    private int padding = 5;
    private string characterSet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789.,!?-";
    private string fontAssetName = "NewTMProOldFont";
    
    [MenuItem("Tools/TMProOld Font Creator")]
    public static void ShowWindow()
    {
        GetWindow<TMProOldFontCreator>("TMProOld Font Creator");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("TMProOld字体资源创建工具", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        sourceFont = (Font)EditorGUILayout.ObjectField("源字体", sourceFont, typeof(Font), false);
        fontAssetName = EditorGUILayout.TextField("字体资源名称", fontAssetName);
        
        EditorGUILayout.Space();
        
        fontSize = EditorGUILayout.IntField("字体大小", fontSize);
        atlasWidth = EditorGUILayout.IntField("图集宽度", atlasWidth);
        atlasHeight = EditorGUILayout.IntField("图集高度", atlasHeight);
        padding = EditorGUILayout.IntField("字符间距", padding);
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("字符集:");
        characterSet = EditorGUILayout.TextArea(characterSet, GUILayout.Height(60));
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("创建TMProOld字体资源"))
        {
            CreateTMProOldFontAsset();
        }
        
        EditorGUILayout.Space();
        
        EditorGUILayout.HelpBox("使用说明:\n1. 选择源字体文件\n2. 设置字体参数\n3. 输入需要的字符集\n4. 点击创建按钮生成TMProOld字体资源", MessageType.Info);
    }
    
    private void CreateTMProOldFontAsset()
    {
        if (sourceFont == null)
        {
            EditorUtility.DisplayDialog("错误", "请先选择源字体文件", "确定");
            return;
        }
        
        try
        {
            // 创建字体资源 - 使用ScriptableObject.CreateInstance避免持久化标志问题
            TMP_FontAsset fontAsset = ScriptableObject.CreateInstance<TMP_FontAsset>();
            fontAsset.name = fontAssetName;
            fontAsset.hideFlags = HideFlags.None; // 确保对象可以被保存
            
            // 设置字体类型
            fontAsset.fontAssetType = TMP_FontAsset.FontAssetTypes.SDF;
            
            // 创建字体面信息
            FaceInfo faceInfo = new FaceInfo();
            faceInfo.Name = sourceFont.name;
            faceInfo.PointSize = fontSize;
            faceInfo.Scale = 1.0f;
            faceInfo.LineHeight = fontSize * 1.2f;
            faceInfo.Baseline = fontSize * 0.8f;
            faceInfo.Ascender = fontSize * 0.8f;
            faceInfo.Descender = fontSize * -0.2f;
            faceInfo.CenterLine = fontSize * 0.3f;
            faceInfo.AtlasWidth = atlasWidth;
            faceInfo.AtlasHeight = atlasHeight;
            faceInfo.Padding = padding;
            
            fontAsset.AddFaceInfo(faceInfo);
            
            // 创建字形信息
            List<TMP_Glyph> glyphList = new List<TMP_Glyph>();
            
            // 为每个字符创建字形数据
            for (int i = 0; i < characterSet.Length; i++)
            {
                char character = characterSet[i];
                
                TMP_Glyph glyph = new TMP_Glyph();
                glyph.id = (int)character;
                
                // 简单的字形布局计算（实际应用中需要更复杂的字体渲染）
                int row = i / 16;
                int col = i % 16;
                float glyphWidth = (float)atlasWidth / 16f - padding * 2;
                float glyphHeight = (float)atlasHeight / 16f - padding * 2;
                
                glyph.x = col * (atlasWidth / 16f) + padding;
                glyph.y = row * (atlasHeight / 16f) + padding;
                glyph.width = glyphWidth;
                glyph.height = glyphHeight;
                glyph.xOffset = 0;
                glyph.yOffset = fontSize * 0.8f;
                glyph.xAdvance = glyphWidth;
                glyph.scale = 1.0f;
                
                glyphList.Add(glyph);
            }
            
            fontAsset.AddGlyphInfo(glyphList.ToArray());
            
            // 创建空的字距调整表
            KerningTable kerningTable = new KerningTable();
            kerningTable.kerningPairs = new List<KerningPair>();
            fontAsset.AddKerningInfo(kerningTable);
            
            // 创建字体创建设置
            FontCreationSetting creationSettings = new FontCreationSetting();
            creationSettings.fontSourcePath = AssetDatabase.GetAssetPath(sourceFont);
            creationSettings.fontSize = fontSize;
            creationSettings.fontPadding = padding;
            creationSettings.fontAtlasWidth = atlasWidth;
            creationSettings.fontAtlasHeight = atlasHeight;
            creationSettings.fontKerning = true;
            
            fontAsset.fontCreationSettings = creationSettings;
            
            // 创建简单的白色纹理作为字体图集（实际应用中需要渲染真实字体）
            Texture2D atlas = new Texture2D(atlasWidth, atlasHeight, TextureFormat.Alpha8, false);
            atlas.hideFlags = HideFlags.None; // 确保纹理可以被保存
            Color32[] pixels = new Color32[atlasWidth * atlasHeight];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }
            atlas.SetPixels32(pixels);
            atlas.Apply();
            atlas.name = fontAssetName + "_Atlas";
            
            fontAsset.atlas = atlas;
            
            // 创建材质
            Shader shader = Shader.Find("TextMeshPro/Distance Field") ?? Shader.Find("UI/Default") ?? Shader.Find("Sprites/Default");
            Material material = new Material(shader);
            material.hideFlags = HideFlags.None; // 确保材质可以被保存
            material.name = fontAssetName + "_Material";
            material.mainTexture = atlas;
            fontAsset.material = material;
            
            // 保存资源
            string assetPath = "Assets/" + fontAssetName + ".asset";
            string atlasPath = "Assets/" + fontAssetName + "_Atlas.asset";
            string materialPath = "Assets/" + fontAssetName + "_Material.mat";
            
            AssetDatabase.CreateAsset(atlas, atlasPath);
            AssetDatabase.CreateAsset(material, materialPath);
            AssetDatabase.CreateAsset(fontAsset, assetPath);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("成功", $"TMProOld字体资源已创建: {assetPath}", "确定");
            
            // 选中创建的资源
            Selection.activeObject = fontAsset;
            EditorGUIUtility.PingObject(fontAsset);
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("错误", $"创建字体资源时发生错误: {e.Message}", "确定");
            Debug.LogError($"TMProOld字体创建错误: {e}");
        }
    }
}
#endif