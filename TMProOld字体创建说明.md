# TMProOld 字体资源创建工具

本工具集提供了为TMProOld组件创建兼容字体资源的完整解决方案。由于Unity编辑器制作的标准TMP字体资源无法直接用于TMProOld组件，这些工具可以帮助您创建专门的TMProOld字体资源。

## 文件说明

### 1. TMProOldFontCreator.cs
- **类型**: Unity编辑器窗口工具
- **功能**: 提供图形界面来创建TMProOld字体资源
- **使用方式**: 通过菜单 `Tools > TMProOld Font Creator` 打开

### 2. TMProOldFontAssetCreator.cs
- **类型**: 静态工具类
- **功能**: 提供程序化创建TMProOld字体资源的方法
- **使用方式**: 在代码中调用静态方法

### 3. TMProOldFontExample.cs
- **类型**: MonoBehaviour示例脚本
- **功能**: 演示如何使用字体创建工具和测试字体资源
- **使用方式**: 添加到GameObject上作为组件使用

## 使用方法

### 方法一：使用编辑器工具（推荐）

1. **打开工具窗口**
   - 在Unity菜单栏选择 `Tools > TMProOld Font Creator`

2. **配置参数**
   - **源字体**: 选择要转换的Unity字体文件
   - **字体资源名称**: 设置生成的字体资源名称
   - **字体大小**: 设置字体渲染大小（建议32-64）
   - **图集宽度/高度**: 设置字体图集纹理尺寸（建议512x512或1024x1024）
   - **字符间距**: 设置字符之间的填充距离
   - **字符集**: 输入需要包含的所有字符

3. **创建字体资源**
   - 点击"创建TMProOld字体资源"按钮
   - 工具会自动生成字体资源、图集纹理和材质
   - 生成的文件会保存在Assets目录下

### 方法二：使用代码创建

```csharp
// 获取源字体
Font sourceFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

// 创建TMProOld字体资源
TMP_FontAsset fontAsset = TMProOldFontAssetCreator.CreateFontAsset(
    sourceFont,           // 源字体
    32,                  // 字体大小
    "ABC123",            // 字符集
    512,                 // 图集宽度
    512,                 // 图集高度
    5                    // 填充
);

// 应用到TMProOld组件
TextMeshPro textComponent = GetComponent<TextMeshPro>();
textComponent.font = fontAsset;
```

### 方法三：使用示例组件

1. **添加示例组件**
   - 将 `TMProOldFontExample` 脚本添加到场景中的GameObject上

2. **配置参数**
   - 在Inspector中设置源字体和其他参数
   - 可选择性地指定TextMeshPro和TextMeshProUGUI组件用于测试

3. **创建和测试**
   - 右键点击组件，选择"创建字体资源"
   - 使用其他上下文菜单选项测试字体功能

## 重要说明

### 字符集配置
- **基础字符集**: `ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789`
- **标点符号**: `.,!?-_+=/*\\()[]{}:;"'<>|`~@#$%^&*`
- **中文支持**: 需要包含具体的中文字符，建议使用常用汉字集
- **特殊字符**: 空格、换行符等会自动添加

### 图集尺寸建议
- **小字符集** (< 100字符): 256x256 或 512x512
- **中等字符集** (100-500字符): 512x512 或 1024x1024
- **大字符集** (> 500字符): 1024x1024 或 2048x2048
- **中文字体**: 建议使用2048x2048或更大

### 性能考虑
- 图集纹理越大，内存占用越多
- 字符集越大，加载时间越长
- 建议只包含实际需要的字符
- 可以创建多个专用字体资源（如：UI字体、数字字体等）

## 限制和注意事项

### 当前限制
1. **字体渲染**: 当前版本创建的是简化的字体资源，实际字形渲染需要更复杂的实现
2. **SDF支持**: 距离场字体需要专门的渲染管线
3. **字距调整**: 当前版本不包含详细的字距调整信息

### 解决方案
1. **真实字体渲染**: 可以集成FreeType或其他字体渲染库
2. **SDF生成**: 可以使用msdfgen等工具生成距离场纹理
3. **字距优化**: 可以从原字体文件中提取字距信息

### 兼容性
- 支持Unity 2019.4及以上版本
- 兼容TMProOld命名空间
- 支持TextMeshPro和TextMeshProUGUI组件

## 故障排除

### 常见问题

**Q: 创建的字体资源显示为空白**
A: 检查字符集是否正确，确保包含要显示的字符

**Q: 字体显示模糊或像素化**
A: 增加字体大小或图集分辨率

**Q: 部分字符显示为方块**
A: 这些字符不在字符集中，需要添加到字符集里

**Q: 内存占用过高**
A: 减少图集尺寸或字符集大小

### 调试方法

1. **使用示例组件的测试功能**
   ```csharp
   // 测试字符支持
   example.TestCharacterSupport();
   
   // 显示字体信息
   example.ShowFontInfo();
   ```

2. **检查字体资源结构**
   ```csharp
   Debug.Log($"字符字典大小: {fontAsset.characterDictionary.Count}");
   Debug.Log($"图集尺寸: {fontAsset.atlas.width}x{fontAsset.atlas.height}");
   ```

3. **验证字符包含**
   ```csharp
   bool hasChar = fontAsset.HasCharacter('A');
   Debug.Log($"包含字符A: {hasChar}");
   ```

## 扩展开发

### 添加新功能
1. **自定义字体渲染器**: 实现真实的字体光栅化
2. **SDF生成器**: 添加距离场纹理生成
3. **字距调整**: 从字体文件中提取kerning信息
4. **批量处理**: 支持批量转换多个字体

### 集成第三方库
- **FreeType**: 用于字体渲染
- **msdfgen**: 用于多通道距离场生成
- **HarfBuzz**: 用于复杂文本布局

## 许可证

本工具集基于MIT许可证发布，可以自由使用和修改。

## 更新日志

### v1.0.0
- 初始版本发布
- 支持基础字体资源创建
- 提供编辑器工具和代码API
- 包含完整的使用示例

---

如有问题或建议，请联系开发者或提交Issue。