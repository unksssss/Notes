---
title: "XR 射线交互与 World Space Canvas"
type: resource
tags: [unity, xr, ui, 项目实战]
created: "2026-08-03"
updated: "2026-08-03"
status: active
summary: "VR 手柄射线点击 World Space UI 三件套：TrackedDeviceGraphicRaycaster 替换 GraphicRaycaster、Event Camera 指定 XR 相机、XRRayInteractor UI interaction；3D 物理射线 vs UI Raycaster 两套命中系统；排查思路"
---

# XR 射线交互与 World Space Canvas

## 问题：VR 里 World Space Canvas 按钮默认点不到！

普通 UI 用 **GraphicRaycaster** 处理射线，但 **XR 射线不是鼠标射线**——默认 GraphicRaycaster 只认 EventSystem 的 Pointer 事件，**XR 射线的命中它根本不理会**！所以 VR 里 World Space Canvas 上的按钮默认点了没反应。

## 两套完全独立的命中系统（Day 25 考点）

灯皇 Day 25 的翻车点：以为"能点中 3D 物体"就一定能点中 UI。其实是两套机制：

| | 3D 物体 | UI（Canvas） |
|---|---|---|
| 命中机制 | **Physics 射线**（Raycast 数学） | **Raycaster 图形命中**（RectTransform + Graphic） |
| 依赖组件 | Collider + XR Ray Interactor | Canvas 挂 **GraphicRaycaster**（XR 下换 TrackedDeviceGraphicRaycaster）+ EventSystem |
| 是否要 Collider | ✅ 必须 | ❌ 不需要（UI 是图形命中，不是物理碰撞） |

> **排查金句**：3D 能点中、UI 点不动 → 问题一定在 Canvas 侧（Raycaster 缺失/类型错、Event Camera 未指定、射线距离不够），别去给 UI 挂 Collider！

## ✅ 三件套（缺一不可）

### 1. TrackedDeviceGraphicRaycaster（替换默认 Raycaster）
把 Canvas 上的 GraphicRaycaster **换成 `TrackedDeviceGraphicRaycaster`**（XR Interaction Toolkit 提供）——专门处理 XR 控制器的射线命中 UI，配合 XRRayInteractor 才能生效。

### 2. Event Camera（必须指定 XR 相机）
World Space Canvas 的 **Event Camera** 属性，鼠标 UI 时代经常留空（GraphicRaycaster 自己找），但 XR 场景不指定 UI 事件坐标换算会乱。**必须把 XR Origin 的 Camera 拖进去**。

### 3. XRRayInteractor（UI interaction 勾选）
- 射线交互器挂手柄/控制器，确认勾选 **UI interaction**（默认开）
- 注意**遮挡管理**：射线要能穿过 3D 物体命中 UI
- Canvas 的 sortingOrder / 距离别太远，射线有有效距离，UI 放在手够得着的 World Space 位置（如腰前操作面板）

## 排查清单（按钮点了没反应）

| 检查项 | 说明 |
|---|---|
| Canvas 的 Raycaster 类型 | 必须是 TrackedDeviceGraphicRaycaster，不是 GraphicRaycaster |
| Event Camera | 是否已指定 XR Origin 的 Camera |
| XRRayInteractor | UI interaction 是否勾选 |
| UI 距离 | 是否超出射线有效距离 |
| 3D 物体交互正常吗 | 正常 → 问题一定在 Canvas 侧（前三条） |

## 一句话总结

> VR 点 UI = `TrackedDeviceGraphicRaycaster` + `Event Camera`（XR 相机）+ `XRRayInteractor` 射线，三者缺一不可！

## 相关

- [[XR Interaction Toolkit 配置指南]]
- [[XR Grab Interactable 完整参考手册]]
- [[UGUI事件接口与EventTrigger]]
