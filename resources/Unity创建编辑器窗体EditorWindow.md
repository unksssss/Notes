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
            if (!string.IsNullOrEmpty(filter) && !rope.name.Contains(filter)) continue;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(rope, typeof(HookRope), true);
            if (rope.ropeStart == null || rope.ropeEnd == null)
                EditorGUILayout.LabelField("⚠ 未配置端点", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
    }
}
```

## 相关笔记
- [[Unity编辑器扩展开发入门]]
- [[Unity编辑器相关特性]]
