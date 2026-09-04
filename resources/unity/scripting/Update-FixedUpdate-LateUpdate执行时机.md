---
title: "Update / FixedUpdate / LateUpdate 执行时机"
type: resource
tags: [unity, 面试, 生命周期]
created: "2026-08-04"
updated: "2026-08-04"
status: active
summary: "三 Update 分工：FixedUpdate 固定步长跑物理、Update 帧率相关跑逻辑、LateUpdate 相机跟随；物理与渲染是解耦的两套时钟"
---

# Update / FixedUpdate / LateUpdate 执行时机

## 三者分工

| 方法 | 调用时机 | 特点 | 典型用途 |
|------|---------|------|---------|
| **FixedUpdate** | **固定时间步长**（默认 0.02s = 50 次/秒），物理系统驱动 | 与帧率无关；帧耗时超步长时一帧内可能调用多次 | **物理操作**：Rigidbody、碰撞、AddForce |
| **Update** | 每帧 1 次 | 帧率相关，帧率越高调用越频繁 | 逻辑更新、输入检测、动画状态 |
| **LateUpdate** | Update 之后每帧 1 次 | 帧率相关 | **相机跟随** |

## 核心认知：两套时钟是解耦的

- 物理步长（FixedUpdate）和渲染帧（Update）**不是严格 1:1**：30fps 渲染时物理可能已跑 1~2 次
- 每帧内顺序：`FixedUpdate`（可能 0~n 次）→ `Update` → `LateUpdate`

## 常见面试陷阱

1. 把**物理操作**（AddForce、移动 Rigidbody）放 Update → 帧率不稳时物理表现不一致 ❌
2. 相机在 Update 里跟随 → 物体在 LateUpdate 里移动时看到相机"先到位"的抖动 ❌
3. 以为 FixedUpdate 和 Update 严格交替执行 → 错，与帧率无关

## 项目实战判断

- **Rigidbody + AddForce / velocity 操作** → FixedUpdate（物理引擎驱动）
- **CharacterController.Move** → Update（它是直接驱动位移的 API，内部自己处理碰撞，不属于物理引擎驱动；官方标准写法）
- **相机跟随玩家** → LateUpdate（等所有物体 Update 移动完，相机再跟，杜绝"相机先动、物体后动"的穿帮）

## 相关

- [[MonoBehaviour生命周期与SetActive的坑]]
