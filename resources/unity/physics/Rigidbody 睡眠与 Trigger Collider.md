---
title: "Rigidbody 睡眠与 Trigger Collider"
type: resource
tags: [unity, 物理, Rigidbody, Collider, Trigger, 真实面经]
created: "2026-09-07"
updated: "2026-09-07"
status: active
summary: "Rigidbody 睡眠机制（速度低于 SleepThreshold 且无外力 → 停模拟省 CPU；传送不自动唤醒要用 MovePosition/WakeUp）+ Trigger vs Collider 核心区别（碰撞求解 vs 重叠检测、OnCollisionEnter vs OnTriggerEnter、都需要至少一方有刚体）"
related:
  - "[[CharacterController移动 — Move vs SimpleMove]]"
  - "[[Physics Raycast 与 NonAlloc]]"
---

# Rigidbody 睡眠与 Trigger Collider

来源：2025 高频 Unity 面试题集（renrendoc）原题"Rigidbody 的 Sleep 和 WakeUp 机制？Trigger 和 Collider 的核心区别？"

## 睡眠机制（Sleep / WakeUp）

- **入睡条件**：刚体速度低于 **Sleep Threshold** 且持续无外力（静止稳定）→ 物理引擎自动标记睡眠；
- **睡眠省什么**：跳过碰撞求解/模拟更新——**停止物理计算**，纯省 CPU（大量静止物体的场景收益巨大）；
- **唤醒**：被碰撞、被加力、或脚本调 `WakeUp()`；
- **🚨 传送大坑**：睡眠中的刚体被直接改 `transform.position` **不会自动唤醒**（物理引擎没在盯它）→ 表现为"改了位置却没物理反应"。正确做法：`Rigidbody.MovePosition()` 走物理通道，或先 `WakeUp()` 再改。

## Trigger vs Collider

| | Collider（IsTrigger=false） | Trigger（IsTrigger=true） |
|---|---|---|
| 物理行为 | 阻挡、反弹、参与**碰撞求解** | **不做碰撞求解**，只做**重叠检测** |
| 回调 | `OnCollisionEnter/Stay/Exit` | `OnTriggerEnter/Stay/Exit` |
| 刚体要求 | 至少一方有**非 kinematic** 刚体 | 至少一方有 Rigidbody（**Kinematic 也行**） |
| CPU 开销 | 较高（要解碰撞响应） | 更低（只有重叠检测） |
| 典型用途 | 实体碰撞（箱子、地面） | 区域触发、拾取判定、伤害圈 |

**坑点**：
1. 两个纯 Collider（无刚体）互相之间**不产生任何回调**；
2. Trigger 也要 Rigidbody 参与，Kinematic 刚体 + Trigger = 最常见的"移动感应区"组合；
3. 事件类型对不上也是"没反应"常见原因——监听了 OnCollision 但物体那边是 Trigger（走 OnTrigger）。

## 实战排查：机关撞飞箱子没反应

① 箱子刚体**处于睡眠**（头号嫌疑，先 WakeUp/用 MovePosition 推）→ ② 箱子 kinematic = true（不受普通力）→ ③ 事件类型不匹配（撞锤是 Trigger 而代码只监听 OnCollision）→ ④ 碰撞层（Collision Matrix）没勾选互撞。

## 面试一句话

"刚体速度低于阈值且稳定无外力会睡眠、省去模拟；被撞/加力/ WakeUp 唤醒；传送要 MovePosition。Trigger 不参与碰撞求解只做重叠检测，走 OnTrigger 回调，开销更小，但触发同样需要至少一方有刚体。"
