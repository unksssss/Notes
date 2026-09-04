---
title: "VR门交互 — Hinge Joint + XR Grab Interactable"
type: resource
tags: [unity, VR, XR Interaction Toolkit, Hinge Joint, 门交互]
created: "2026-06-17"
updated: "2026-06-17"
status: active
summary: "用 Hinge Joint 实现VR里真实的开关门交互，结合 XR Grab Interactable 让手抓住门把手推开拉开关门。"
source: ""
related: [["XR Interaction Toolkit 配置指南"], ["XR Grab Interactable 完整参考手册"]]
---

# VR门交互 — Hinge Joint + XR Grab Interactable

## 场景结构

```
门框（Static）
  └── 门（Rigidbody + Hinge Joint + XR Grab Interactable）
        └── 门把手（空物体，作为 Attach Transform）
        └── 门板（Mesh、Collider）
```

---

## 一步步配置

### 第一步：门框

```
Hierarchy 右键 → Create Empty → 命名 "DoorFrame"
  Position: 门的位置
  → 不参与物理，只当锚点
```

### 第二步：门本体

```
门框下右键 → 3D Object → Cube → 命名 "Door"
  Scale: (0.1, 2.5, 1.2)（厚度、高度、宽度）
  Position: 贴着门框一侧

Inspector:
  Tag: Door
  Layer: Interactable
```

### 第三步：Rigidbody

```
Door 上添加 Rigidbody:
  Mass: 5~10（门的重量感）
  Drag: 2~5（阻力，模拟门不会一直晃）
  Angular Drag: 5~10（旋转阻力）
  Use Gravity: 不勾
  Is Kinematic: 不勾
```

### 第四步：Hinge Joint ⭐ 核心

```
Door 上添加 Hinge Joint:
  Connected Body: 不拖（None = 锚定在世界空间）

  Anchor: (0, 0, -0.5)       ← 旋转轴在门侧边（Z=门宽度的一半）
  Axis: (0, 1, 0)             ← 绕 Y 轴旋转（上下方向）
  Auto Configure Connected Anchor: 不勾

  Use Spring: 勾上
    Spring: 50~100            ← 弹簧力度。越大门越"弹"
    Damper: 10~20             ← 阻尼。防止门来回晃停不下来
    Target Position: 0        ← 弹簧目标角度（0=关上）

  Use Limits: 勾上
    Min: 0                    ← 最小角度（0°=关着，不能穿墙反开）
    Max: 90~110               ← 最大开合角。推拉门 90°，双开门 110°
    Bounciness: 0             ← 撞到限位不反弹
    Contact Distance: 1       ← 快到限位就开始减速
```

### 第五步：Collider

```
Door 上的 Box Collider:
  自动挂载（从 Cube 来的）
  Is Trigger: 不勾（需要物理碰撞阻止穿墙）
```

### 第六步：XR Grab Interactable

```
Door 上添加 XR Grab Interactable:
  Movement Type: Velocity Tracking
  Track Position: 不勾        ← ⭐ 门只转不移
  Track Rotation: 勾
  Smooth Position/Rotation: 勾
  Throw On Detach: 不勾
  Select Mode: Single
  Retain Transform Parent: 勾 ← ⭐ 被抓时保持是门框的子物体，不脱离铰链
```

### 第七步：Attach Transform

```
Door 下右键 → Create Empty → 命名 "HandlePoint"
  放在门把手的位置（门板边缘，离旋转轴最远的位置）
  
Door → XR Grab Interactable → Attach Transform: 拖 HandlePoint
```

---

## 参数调优

| 问题 | 调什么 |
|---|---|
| 门太轻，一碰就飞 | Rigidbody → Mass 加大 |
| 门太重，推不动 | Rigidbody → Mass 减小 + Drag 减小 |
| 松手后门来回晃半天 | Hinge Joint → Spring Damper 加大 |
| 门弹回太快（弹簧太猛） | Hinge Joint → Spring 减小 |
| 门能推开超过 90° | Hinge Joint → Limits → Max 减小 |
| 门能反方向穿过门框 | Hinge Joint → Limits → Min = 0 |
| 没抓住门也能推开门 | 加 Trigger 区域 + 手靠近自动激活 |
| 抓住门后门脱离门框 | Retain Transform Parent 没勾 |

---

## 完整 Inspector 速查

```
Door:
  ✅ Rigidbody (Mass:8, Drag:3, AngularDrag:8)
  ✅ Hinge Joint
     ├─ Anchor: (0, 0, -0.6)
     ├─ Axis: (0, 1, 0)
     ├─ Spring: 80, Damper: 15, Target: 0
     └─ Limits: Min=0, Max=90
  ✅ XR Grab Interactable
     ├─ Track Position: ❌
     ├─ Track Rotation: ✅
     ├─ Retain Transform Parent: ✅
     └─ Attach Transform: HandlePoint
  ✅ Box Collider (IsTrigger: ❌)
```

---

## 进阶：关门自动触发事件

```cs
public class Door : MonoBehaviour
{
    [SerializeField] private float _closeThreshold = 5f; // 小于这个角度=关了
    public UnityEvent OnDoorClosed;

    private HingeJoint _hinge;

    void Start() => _hinge = GetComponent<HingeJoint>();

    void Update()
    {
        if (_hinge.angle <= _closeThreshold)
            OnDoorClosed?.Invoke();
    }
}
```

连到 `BackdraftCtrl.OpenDoor()` 或 `StepManager` 即可 ✧

---

