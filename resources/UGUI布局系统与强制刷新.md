---
title: "UGUI 布局系统与强制刷新"
type: resource
tags: [unity, 项目实战, UGUI]
created: "2026-08-04"
updated: "2026-08-06"
status: active
summary: "LayoutGroup + ContentSizeFitter 动态加子项尺寸不刷新的解法：LayoutRebuilder.ForceRebuildLayoutImmediate / Canvas.ForceUpdateCanvases；Start 时机读 UI 尺寸用 GetWorldCorners；ScrollRect 三种 MovementType"
---

# UGUI 布局系统与强制刷新

## 问题现象

运行时用 `VerticalLayoutGroup + ContentSizeFitter` 做自适应列表，**动态 AddChild 后父物体尺寸不更新、内容被裁切**。

**根因**：布局系统走**脏标记（dirty flag）**刷新，运行时加子物体不一定立刻触发父级重建。

## 正确解法

```csharp
// 方式一：强制重建指定 LayoutGroup
LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

// 方式二：强制整个 Canvas 布局刷新（更彻底，推荐先调这个）
Canvas.ForceUpdateCanvases();
```

> 注意：`LayoutRebuilder.ForceRebuildLayoutImmediate` **只对挂 LayoutGroup 的节点有效**；纯 anchor 拉伸的节点（无 LayoutGroup）不会触发，此时 `sizeDelta` 兜底也无效（anchor 拉伸时 sizeDelta 恒为 0）。

## Start 时机读 UI 尺寸的坑（实战教训）

**问题**：`StepScrollView.Init()` 在 Start 读 `viewport.rect`，Canvas 布局未完成 → rect = (0,0)。

**有效方案**：
```csharp
Canvas.ForceUpdateCanvases();            // 1. 强制整个 Canvas 布局刷新
Vector3[] corners = new Vector3[4];
viewport.GetWorldCorners(corners);       // 2. 世界坐标 corners 兜底（比 sizeDelta 可靠）
```

**经验**：Start 时机读 UI 尺寸 → `Canvas.ForceUpdateCanvases()` 是正解；取尺寸用 `GetWorldCorners` 比 `sizeDelta` 可靠。

## ScrollRect 三种 MovementType（Day 17 补充）

| 模式 | 行为 | 适用 |
|---|---|---|
| **Unrestricted**（无限制） | 想拖多远拖多远，**不回弹不卡边** | ⭐ 无限轮播（配合代码回绕）、无限列表 |
| **Clamped**（钳制） | 到边界**硬卡住**，拖不动 | 固定列表、到尽头就停 |
| **Elastic**（弹性） | 可拖出边界，松手**带阻尼回弹** | 默认手感，普通列表 |

**⚠️ 无限轮播的坑（Day 17 失分点）**：
- 无限轮播（滚出视野就换数据 + 瞬移回绕）必须 **Unrestricted**——Clamped 滚到边界直接卡死，循环断裂；Elastic 回弹会和代码回绕的位置计算打架
- 自写槽位轮转（如 VerticalBackPack）不用 ScrollRect，是同一思路的另一实现

## 相关

- [[UGUI事件接口与EventTrigger]]
