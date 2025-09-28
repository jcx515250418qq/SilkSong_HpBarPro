# Unity 持久化断言错误修复说明

## 错误描述

```
Assertion failed on expression: '!(o->TestHideFlag(Object::kDontSaveInEditor) && (options & kAllowDontSaveObjectsToBePersistent) == 0)'
UnityEngine.GUIUtility:ProcessEvent (int,intptr,bool&)
```

## 错误原因分析

这个断言错误通常发生在以下情况：

1. **对象持久化标志冲突**: 当尝试保存一个标记为 `kDontSaveInEditor` 的对象时
2. **ScriptableObject 创建问题**: 使用不当的方法创建 ScriptableObject 实例
3. **资源管理不当**: 纹理、材质等资源的 hideFlags 设置不正确
4. **编辑器状态不一致**: 在编辑器和运行时之间的对象状态管理问题

## 在 TMProOld 字体创建工具中的具体表现

### 问题代码模式

```csharp
// 问题代码 - 可能导致持久化错误
TMP_FontAsset fontAsset = CreateInstance<TMP_FontAsset>();
Texture2D atlas = new Texture2D(width, height, TextureFormat.Alpha8, false);
Material material = new Material(shader);
// 没有正确设置 hideFlags
```

### 修复后的代码

```csharp
// 修复代码 - 正确处理对象持久化
TMP_FontAsset fontAsset = ScriptableObject.CreateInstance<TMP_FontAsset>();
fontAsset.hideFlags = HideFlags.None; // 确保对象可以被保存

Texture2D atlas = new Texture2D(width, height, TextureFormat.Alpha8, false);
atlas.hideFlags = HideFlags.None; // 确保纹理可以被正确管理

Material material = new Material(shader);
material.hideFlags = HideFlags.None; // 确保材质可以被正确管理
```

## 修复内容详解

### 1. ScriptableObject 创建方式修正

**修改文件**: `TMProOldFontCreator.cs`

- **原代码**: `CreateInstance<TMP_FontAsset>()`
- **修复代码**: `ScriptableObject.CreateInstance<TMP_FontAsset>()`
- **原因**: 使用完整的 ScriptableObject.CreateInstance 方法确保正确的对象创建流程

### 2. HideFlags 显式设置

**修改文件**: `TMProOldFontCreator.cs`, `TMProOldFontAssetCreator.cs`

为所有创建的 Unity 对象显式设置 `hideFlags = HideFlags.None`：

- **TMP_FontAsset**: 字体资源对象
- **Texture2D**: 字体图集纹理
- **Material**: 字体材质

### 3. 着色器查找优化

**修改文件**: `TMProOldFontCreator.cs`

```csharp
// 优化着色器查找，提供回退选项
Shader shader = Shader.Find("TextMeshPro/Distance Field") ?? 
                Shader.Find("UI/Default") ?? 
                Shader.Find("Sprites/Default");
```

### 4. 安全的对象销毁方式

**修改文件**: `TMProOldFontExample.cs`

**问题**: 在编辑器中使用 `DestroyImmediate` 可能导致持久化错误

```csharp
// 问题代码
DestroyImmediate(createdFontAsset.atlas);

// 修复代码 - 使用Undo系统安全销毁
#if UNITY_EDITOR
UnityEditor.Undo.DestroyObjectImmediate(createdFontAsset.atlas);
#endif
```

**原因**: `DestroyImmediate` 在编辑器中可能与对象的持久化状态冲突，使用 `Undo.DestroyObjectImmediate` 可以避免这个问题。

## HideFlags 详解

### HideFlags 的作用

- `HideFlags.None`: 对象正常显示和保存
- `HideFlags.HideInHierarchy`: 在层级视图中隐藏
- `HideFlags.HideInInspector`: 在检视器中隐藏
- `HideFlags.DontSaveInEditor`: 不在编辑器中保存（这是导致错误的标志）
- `HideFlags.NotEditable`: 不可编辑
- `HideFlags.DontSaveInBuild`: 不在构建中保存
- `HideFlags.DontUnloadUnusedAsset`: 不卸载未使用的资源

### 为什么设置 HideFlags.None

1. **确保持久化**: 明确告诉 Unity 这些对象需要被保存
2. **避免标志冲突**: 防止 Unity 内部设置冲突的 hideFlags
3. **编辑器兼容性**: 确保在编辑器和运行时都能正确处理

## 预防措施

### 1. 对象创建最佳实践

```csharp
// 创建 ScriptableObject
var scriptableObj = ScriptableObject.CreateInstance<YourScriptableObject>();
scriptableObj.hideFlags = HideFlags.None;

// 创建纹理
var texture = new Texture2D(width, height, format, mipChain);
texture.hideFlags = HideFlags.None;

// 创建材质
var material = new Material(shader);
material.hideFlags = HideFlags.None;
```

### 2. 资源管理检查清单

- [ ] 所有 ScriptableObject 使用 `ScriptableObject.CreateInstance`
- [ ] 所有创建的对象设置 `hideFlags = HideFlags.None`
- [ ] 着色器查找有回退机制
- [ ] 资源路径正确且存在
- [ ] 在编辑器中测试资源创建和保存

### 3. 调试技巧

```csharp
// 检查对象的 hideFlags
Debug.Log($"Object hideFlags: {obj.hideFlags}");

// 验证对象是否可以保存
if ((obj.hideFlags & HideFlags.DontSaveInEditor) != 0)
{
    Debug.LogWarning("Object marked as DontSaveInEditor!");
}
```

## 相关 Unity 版本说明

这个问题在不同 Unity 版本中可能有不同的表现：

- **Unity 2019.x**: 较为宽松的检查
- **Unity 2020.x+**: 更严格的持久化检查
- **Unity 2021.x+**: 增强的资源管理验证

## 总结

通过正确设置 `hideFlags` 和使用适当的对象创建方法，可以有效避免 Unity 的持久化断言错误。这些修复确保了 TMProOld 字体创建工具在各种 Unity 版本中的稳定运行。

### 关键要点

1. **始终使用** `ScriptableObject.CreateInstance` 创建 ScriptableObject
2. **显式设置** `hideFlags = HideFlags.None` 对所有需要保存的对象
3. **提供回退机制** 处理着色器和资源查找
4. **安全销毁对象** 在编辑器中使用 `Undo.DestroyObjectImmediate` 而非 `DestroyImmediate`
5. **测试验证** 在目标 Unity 版本中验证修复效果