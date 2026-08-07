---
title: Unity编辑器扩展开发入门
type: resource
tags: [Unity, 编辑器扩展, Editor, MenuItem, EditorWindow]
created: 2026-08-07
updated: 2026-08-07
status: 学习中
summary: 编辑器扩展知识体系地图 + 入门篇核心（文件夹规范/菜单入口四兄弟/Selection），来源 CSDN 虚拟喵系列（10 篇）
---

# Unity 编辑器扩展开发入门

> 学习来源：CSDN《Unity 编辑器扩展总结》系列（作者：虚拟喵，共 10 篇）
> 系列第一篇（本文对应）：编辑器开发入门

## 一、知识体系地图（系列 10 篇）

1. **① 基础层**（本篇）：Editor 文件夹规范 + 菜单入口 + Selection
2. **② 绘制层**（系列 3~7 篇）：自定义 Inspector（OnInspectorGUI）、EditorWindow 窗体、Gizmos 调试、Scene 视图扩展、PropertyDrawer（数组/List 显示）
3. **③ 状态与资源层**（系列 8~10 篇）：EditorPrefs / ScriptableObject / Undo、GUIStyle / GUISkin、AssetPostprocessor 资源导入管线

学完三层 = 能写完整的生产力工具链

## 二、入门篇核心

### 1. 三个特殊文件夹
- **Editor/**：编辑器脚本专用，任何位置可放、可有多个，打包不进包；不放 Editor 需用 `#if UNITY_EDITOR` 包裹
- **Editor Default Resources/**：必须在 Assets 根目录，编辑器图片用 `EditorGUIUtility.Load()` 读取
- **Gizmos/**：`Gizmos.DrawIcon()` 图标资源

### 2. [MenuItem] 菜单栏按钮
- 三参数：路径 / 是否验证方法 / 优先级（默认 1000，差 >10 分栏）
- 必须是**静态方法**
- 快捷键：`%`=Ctrl `#`=Shift `&`=Alt，如 `%_q`=Ctrl+Q
- 标准写法（验证 + 可撤销）：
```csharp
[MenuItem("MyTool/DeleteAllObj", true)]
private static bool DeleteValidate() => Selection.objects.Length > 0;

[MenuItem("MyTool/DeleteAllObj", false)]
private static void MyToolDelete()
{
    foreach (Object item in Selection.objects)
        Undo.DestroyObjectImmediate(item);
}
```

### 3. CONTEXT + MenuCommand（组件右键菜单）
- `[MenuItem("CONTEXT/组件名/按钮名")]`，**CONTEXT 必须大写**
- 通过 `MenuCommand` 参数取当前右键的组件：`cmd.context as 组件类型`

### 4. ContextMenu / ContextMenuItem（齿轮菜单/属性右键）
- `[ContextMenu("名字")]` 实例方法 → 组件小齿轮菜单
- `[ContextMenuItem("显示名", "方法名")]` → 字段右键菜单
- **属于 UnityEngine 命名空间**，普通脚本可用，无需 using UnityEditor

### 5. Selection 三兄弟
- `activeGameObject`：第一个选中的场景对象
- `gameObjects`：场景多选
- `objects`：场景 + Project 全选

### 6. 最大坑
- 编辑器无缓存区：**必须 DestroyImmediate() 而非 Destroy()**
- 最佳实践：`Undo.DestroyObjectImmediate()` / `Undo.RecordObject()` 记录撤销

## 三、自定义 Editor 常见坑（实战踩坑 2026-08-07）

### ❌ target 遮蔽导致的 StackOverflowException
**现象**：自定义 Editor（继承 `Editor`）里声明了名为 `target` 的属性，getter 无限递归 → `StackOverflowException: get_target ()` 刷屏。

```csharp
// ❌ 错误：target 遮蔽基类 Editor.target，getter 里 return target 递归调用自己
private InspectorExample target { get { return target as InspectorExample; } }
```

**原因**：C# 成员解析"当前类优先于基类"（名称遮蔽）。属性名 `target` 把基类 `Editor.target`（当前选中的组件实例）藏住了，getter 体内 `target` 引用的是属性自身。

**修复**：属性改名即可：
```csharp
// ✅ 正确：Target 的 getter 里 target 才解析到基类 Editor.target
private InspectorExample Target { get { return target as InspectorExample; } }
```

**教训**：自定义 Editor 里**不要声明与基类同名成员**（target / serializedObject 等），同名必炸。

### ✅ 批量给选中物体添加组件（实战 2026-08-07）
**4 条铁律**：
1. **`Undo.AddComponent<T>(go)` 而非 `go.AddComponent<T>()`**——编辑器一切操作可撤销，AddComponent 撤不回来
2. **先查重**：`GetComponent<T>() == null` 才添加，防组件叠罗汉
3. **过滤 Project 资产**：`Selection.objects` 含预制体资产，用 `if (!(o is GameObject go) || !go.scene.IsValid()) continue;` 只处理场景对象
4. **VR 抓取前提**：XRGrabInteractable 需要 Collider，没有就补一个

```csharp
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SetGoInteractable
{
    [MenuItem("Tools/SetGoInteractable", true)]
    static bool CheckGo() => Selection.activeGameObject != null;

    [MenuItem("Tools/SetGoInteractable", false)]
    static void SetGo()
    {
        int count = 0;
        foreach (var o in Selection.objects)
        {
            if (!(o is GameObject go) || !go.scene.IsValid()) continue;
            if (go.GetComponent<Collider>() == null)
                Undo.AddComponent<BoxCollider>(go);
            if (go.GetComponent<XRGrabInteractable>() == null)
                Undo.AddComponent<XRGrabInteractable>(go);
            count++;
        }
        Debug.Log($"✅ 已为 {count} 个物体配置可抓取");
    }
}
```
- `[MenuItem]` 静态方法**不需要继承 Editor**，普通类即可
- 进阶：想"选任意组件批量加"，用 `MonoScript.GetClass()` + `Undo.AddComponent(go, type)` 做成窗口

## 四、项目实战思路（配电房）

- **场景体检工具**：扫描所有 HookRope 检查 ropeStart/ropeEnd 是否配置（曾发现 SM_消防栓箱 ropeEnd=NULL）
- 批量检查缺失引用、一键摆位、批量生成 Obi Rope 配置
- 学完绘制层后做 EditorWindow 带界面工具

## 相关笔记
- [[Unity 渲染批处理体系]]
- [[Obi Rope粒子约束体系]]
- [[scriptableobject数据驱动设计]]
- [[profiler自定义采样]]
