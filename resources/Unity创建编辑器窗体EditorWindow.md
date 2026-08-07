---
title: Unity创建编辑器窗体EditorWindow
type: resource
tags: [Unity, 编辑器扩展, EditorWindow, 工具窗口]
created: 2026-08-07
updated: 2026-08-07
status: 学习中
summary: EditorWindow 生命周期/OnGUI 立即模式/刷新三兄弟/EditorPrefs 持久化，配场景体检窗口完整示例
---

# Unity 创建编辑器窗体（EditorWindow）

> 学习来源：CSDN《Unity 编辑器扩展总结》系列第 4 篇主题（原文付费墙，知识点为公开技术，参考官方文档 https://docs.unity3d.com/ScriptReference/EditorWindow.html ）

## 一、最小结构（三步启动）

```csharp
using UnityEditor;
using UnityEngine;

public class SceneHealthWindow : EditorWindow
{
    [MenuItem("FireTools/场景体检窗口 %#h")]        // ① 菜单入口 Ctrl+Shift+H
    static void Open() => GetWindow<SceneHealthWindow>("场景体检"); // ② 打开/复用

    void OnGUI() { /* ③ 画 UI */ }
}
```

- `GetWindow<T>()` **单例式**：重复调用复用同一窗口
- 窗口标题、图标：`titleContent`

## 二、OnGUI = 立即模式 UI

- 每帧 + 每次事件（鼠标移动/点击/焦点变化）都会**重画**
- OnGUI 里**只写绘制，不写耗时逻辑**（FindObjectsOfType 这类要放缓存/Update）
- `GUILayout`/`EditorGUILayout` 自动布局；`BeginHorizontal/Vertical` 排列
- 滚动：`BeginScrollView(scrollPos)`

## 三、刷新三兄弟

| 回调 | 频率 | 用途 |
|------|------|------|
| OnGUI | 事件驱动（高频） | 画 UI |
| Update | 每帧 | 后台逻辑，配合 `Repaint()` 刷新界面 |
| OnInspectorUpdate | ~10 次/秒 | 低频刷新，比 Update 省 |

## 四、数据持久化

- 窗口普通字段**关掉就丢**（编辑器窗口不序列化）
- 跨会话：`EditorPrefs.SetString/GetString`（存注册表）
- 会话内：`SessionState`

## 五、生命周期

`GetWindow` → `OnEnable`（初始化+读配置）→ `OnGUI`（绘制循环）→ 关闭 → `OnDisable` → `OnDestroy`

## 六、实战示例：场景体检窗口

```csharp
using UnityEditor;
using UnityEngine;

public class SceneHealthWindow : EditorWindow
{
    private string filter = "";
    private Vector2 scrollPos;

    [MenuItem("FireTools/场景体检窗口 %#h")]
    static void Open() => GetWindow<SceneHealthWindow>("场景体检");

    void OnEnable() { filter = EditorPrefs.GetString("SceneHealth.filter", ""); }

    void OnGUI()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        filter = EditorGUILayout.TextField("过滤关键字", filter);
        EditorPrefs.SetString("SceneHealth.filter", filter);
        EditorGUILayout.EndVertical();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        foreach (var rope in FindObjectsOfType<HookRope>())
        {
            bool match = string.IsNullOrEmpty(filter) || rope.name.Contains(filter);
            EditorGUILayout.BeginHorizontal();          // 无条件成对，不 continue
            GUI.enabled = match;                        // 过滤用灰显，不删控件
            EditorGUILayout.ObjectField(rope, typeof(HookRope), true);
            EditorGUILayout.LabelField(rope.ropeStart == null || rope.ropeEnd == null ? "⚠ 未配置端点" : "");
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }
}
```

## 七、常见坑：Invalid GUILayout state（实战踩坑 2026-08-07）

**现象**：`GUI Error: Invalid GUILayout state ... Verify that all layout Begin/End calls match`

**根因**：OnGUI 一帧内跑多个事件（Layout 算尺寸 → Repaint 绘制 → 鼠标事件），GUILayout 要求**每个事件绘制完全相同的 Begin/End 序列**。一旦控件数量依赖"可变数据"（用户在 TextField 输入改 filter、`FindObjectsOfType` 动态结果、循环里 continue），事件间数量不一致 → 状态机崩。

**黄金法则**：**控件数量只能由事件间不变的数据决定**
1. 列表缓存：`FindObjectsOfType` 移出 OnGUI，用 `EditorApplication.hierarchyChanged += Refresh` 驱动刷新
2. 分支恒等：Begin/End 无条件成对，**绝不 continue/return 跳过**
3. 过滤用灰显：`GUI.enabled = match` 而非删行
4. 事件要解绑：`OnDisable` 里 `-= Refresh`，防泄漏
5. 顺带：`EditorPrefs.SetString` 别放 OnGUI 裸调（每帧写注册表），用 `EditorGUI.BeginChangeCheck/EndChangeCheck` 包住

## 八、ScriptableWizard vs EditorWindow（2026-08-07）

**一句话**：EditorWindow = 常驻工作台；ScriptableWizard = 一次性向导弹窗

| 维度 | EditorWindow | ScriptableWizard |
|------|-------------|-----------------|
| 形态 | 可停靠常驻窗口 | 模态对话框（锁焦点） |
| 打开 | `GetWindow<T>()` 单例 | `DisplayWizard<T>(标题, 确认, 取消)` |
| 按钮 | 自己画 | 自带 Create/Cancel，确认自动关窗回调 |
| 状态 | 自己写提示 | `helpString` / `errorString` / `isValid`（控制按钮灰显） |
| public 字段 | 不自动显示 | **自动显示在窗体上**，零 GUI 代码 |
| 回调 | OnGUI | `OnWizardUpdate()`（输入变化）+ `OnWizardCreate()`（确认主逻辑） |
| 适用 | 长期工具面板 | 输入参数→确认→执行的向导流程 |

**选型**：需要常驻反复操作 → EditorWindow；一次性创建/批量流程（如"保存 prefab 向导"）→ ScriptableWizard

### Wizard 实战范例：保存 prefab 向导（灯皇独立完成 2026-08-07）
```csharp
public class SavePrefabWizard : ScriptableWizard
{
    public GameObject target;   // public 字段自动显示

    [MenuItem("MyTools/保存为Prefab")]
    static void Open() => DisplayWizard<SavePrefabWizard>("保存向导", "保存", "取消");

    void OnWizardUpdate()
    {
        isValid = target != null;
        helpString = target ? $"将保存:{target.name}.prefab" : "请拖入要保存的物体";
    }

    void OnWizardCreate()
    {
        // ⚠️ CreateFolder 要求父目录已存在！Resources 目录可能不存在，要逐级建
        string parent = "Assets/Resources", folder = parent + "/SavePrefab";
        if (!AssetDatabase.IsValidFolder(parent)) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(parent, "SavePrefab");

        string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{target.name}.prefab");
        PrefabUtility.SaveAsPrefabAsset(target, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(path); // 加分：自动选中
    }
}
```
- **坑**：`AssetDatabase.CreateFolder(parent, name)` 父目录不存在会返回 -1 静默失败
- `GenerateUniqueAssetPath` 同名自动加 (1)(2)，想覆盖需先 LoadAssetAtPath 检查 + 确认弹窗
- 进阶：保存目录做成 public 字段 + 目录选择器，配置化

## 相关笔记
- [[Unity编辑器扩展开发入门]]
- [[Unity编辑器相关特性]]
