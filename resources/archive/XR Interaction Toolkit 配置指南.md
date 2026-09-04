---
title: "XR Interaction Toolkit 配置指南"
type: resource
tags: [unity, VR, XR Interaction Toolkit, XR]
created: "2026-06-16"
updated: "2026-06-16"
status: active
summary: "XR Interaction Toolkit 从安装到交互的完整配置流程，覆盖拿物体、UI点击、射线交互、开门等消防VR项目的全部需求。"
source: ""
related: []
---

# XR Interaction Toolkit 配置指南

## 安装

```
Window → Package Manager → Unity Registry → 搜 XR Interaction Toolkit → Install
```

如果弹出 Samples → 导入 Starter Assets（含预设好的 Controller Prefab）。

---

## 场景基础搭建

### 1. 创建 XR Origin

```
Hierarchy 右键 → XR → XR Origin (VR)
```

自动生成：
```
XR Origin
├── Camera Offset
│   └── Main Camera（VR 头盔视角）
├── Left Controller（左手柄）
└── Right Controller（右手柄）
```

### 2. 配置 Input Action

```
Project 窗口 → 右键 → Create → Input Actions
命名 "XRInputActions"

双击打开 → 添加 Action Map "XR":
  Position (Value, Vector2)     → 绑 Thumbstick
  Rotation (Value, Vector2)     → 绑 Thumbstick（右手）
  Select (Button)               → 绑 Grip 或 Trigger
  Activate (Button)             → 绑 Trigger
  
XR Origin → XR Interaction Manager → 拖入 Input Action 资产
```

### 3. 手柄控制器配置

```
Left Controller:
  XR Controller → Controller Node: Left Hand
  XR Ray Interactor（添加此组件）
  XR Direct Interactor（添加此组件）
  Line Renderer（自动挂载，用于射线可视）

Right Controller 同理，Node 选 Right Hand
```

---

## 三大交互模式

### 模式一：Direct Interactor — 拿物体

玩家手靠近物体 → 抓住 → 拿起。

**物体上挂载：**

```
添加组件 → XR Grab Interactable
  Movement Type: Velocity Tracking（物理跟随）
  Throw On Detach: 勾上（可以扔出去）
  Smooth Position: 勾上（防抖动）
```

**手柄触发：** Grip 键按下 → 抓住；松开 → 放下。

**消防项目用途：** 拿灭火器、拿验电器、拿干沙子桶。

### 模式二：Ray Interactor — 远距离点击

射线指到物体 → 点击交互。

**手柄上已有 XR Ray Interactor：**

```
Ray Interactor:
  Line Type: Straight Line 或 Projectile Curve（抛物线）
  Raycast Mask: 只命中 Interactable 层
  
交互物体上：
  XR Simple Interactable → On Select 事件 → 绑定方法
```

**消防项目用途：** 选中水枪阵地、按配电柜按钮、点击确认面板。

### 模式三：UI 交互

在 VR 里点击 Canvas 按钮。

```
Canvas:
  Render Mode: World Space
  Canvas Scaler: Dynamic Pixels Per Unit → 10

Canvas 上添加:
  XR UI Input Module（替换 Standalone Input Module）
  Tracked Device Graphic Raycaster（替换 Graphic Raycaster）
```

**消防项目用途：** 装备面板、确认对话框、汇报表。

---

## 开门交互

**方案一：手抓门把**

```
门：
  Rigidbody + Hinge Joint（固定在门框上旋转轴）
  XR Grab Interactable
    Movement Type: Velocity Tracking
    Track Position: 不勾（门不位移，只旋转）
    Track Rotation: 勾
```

**方案二：点击开门**

```
门把手：
  XR Simple Interactable → On Select → 调 BackdraftCtrl.OpenDoor()
```

---

## 移动方式

### 连续移动

```
XR Origin → 添加 XR Locomotion System
Left Controller → 添加 XR Continuous Move Provider
  Move Speed: 3
```

### 传送

```
XR Origin → 添加 XR Teleportation Provider
场景地面 → 添加 Teleportation Area
```

---

## 交互层级管理

避免射线和手柄同时触发同一个物体：

```
Layer: Interactable → 只给可交互物体
手柄 Ray Interactor → Raycast Mask → 只勾 Interactable 层
手柄 Direct Interactor → 不需要（近处直接碰）
```

---

## Input Action 绑定速查

| 动作 | 绑定键 | 用途 |
|---|---|---|
| Select | Grip | 抓住物体 |
| Activate | Trigger | 扣扳机（灭火器喷射） |
| Position | Thumbstick | 移动 |
| UI Click | Trigger | 点击 UI 按钮 |

---

## 常见问题

**Q: 射线看不到？**
→ 手柄上要有 Line Renderer 组件，或 XR Interactor Line Visual。

**Q: 手穿过物体拿不了？**
→ Direct Interactor 的 Select Action Trigger 设成 State（按住触发）而非 State Change。

**Q: 物体拿了后抖动？**
→ XR Grab Interactable → Smooth Position + Smooth Rotation 勾上。

**Q: VR 里 UI 点不了？**
→ Canvas 必须挂了 Tracked Device Graphic Raycaster，且不是 Screen Space Overlay。

