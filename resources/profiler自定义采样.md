---
title: "Profiler自定义采样"
type: resource
tags: [unity, 性能优化, Profiler]
created: "2026-04-28"
updated: "2026-04-29"
status: active
summary: "Unity Profiler 自定义采样标记定位性能瓶颈"
source: ""
related: ["[[unity知识点-2026-04-28]]", "[[unity知识点-2026-04-29]]", "[[scriptableobject数据驱动设计]]", "[[unity-inputsystem详解]]", "[[unity网络同步方案-状态同步vs帧同步]]"]
---

# Profiler 自定义采样

## 为什么要自定义采样？

Unity Profiler 默认只显示引擎函数名（Update、FixedUpdate 等），你无法直接知道**你自己代码的哪一段最慢**。

## 基本用法

```csharp
using UnityEngine.Profiling;

public void ComplexUpdate()
{
    Profiler.BeginSample("AI_Pathfinding");
    // 寻路逻辑
    Profiler.EndSample();

    Profiler.BeginSample("UI_Refresh");
    // 刷新 UI
    Profiler.EndSample();
}
```

在 Profiler → CPU Usage 窗口中，你会看到 `AI_Pathfinding` 和 `UI_Refresh` 两个自定义区域，一眼找出最耗时代码段。

## 进阶用法：嵌套采样

```csharp
Profiler.BeginSample("Character");
{
    Profiler.BeginSample("Movement");
    // 移动逻辑
    Profiler.EndSample();

    Profiler.BeginSample("Animation");
    // 动画更新
    Profiler.EndSample();
}
Profiler.EndSample();
```

## 常见误区

| 错误做法 | 正确做法 |
|---------|---------|
| 只靠感觉猜性能瓶颈 | 用 Profiler 标记精确测量 |
| 采样名重复 | 用唯一的字符串，避免混淆 |
| 忘记 EndSample | 编辑器会报错，运行时静默失败 |

## 相关笔记

- [[DOTS详解]] — 对比 MonoBehaviour 和 DOTS 的性能差异
- [[对象池实现]] — 测量有无对象池的 GC 差异
