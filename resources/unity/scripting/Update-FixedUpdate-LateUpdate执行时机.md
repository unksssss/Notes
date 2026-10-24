---
title: "Update / FixedUpdate / LateUpdate 执行时机"
type: resource
tags: [unity, 面试, 生命周期]
created: "2026-08-04"
updated: "2026-10-24"
status: active
summary: "三 Update 分工：FixedUpdate 固定步长跑物理、Update 帧率相关跑逻辑、LateUpdate 相机跟随；物理与渲染是解耦的两套时钟；含「Update 采样 vs 物理推进」错位导致薄物体穿透的实战修法"
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

## 进阶一：Update 采样 vs FixedUpdate 推进 → 薄物体穿透

**现象**：跳跃上升时头会「穿出」薄天花板，落地时脚会穿过薄地板，但下一帧又被拉回来。

**根因**：检测和推进跑在两个时钟上。

```
FixedUpdate（物理，每 0.02s 推进一次）  ──► 位置真正改变
Update（每帧采样一次）                  ──► 才发现「我撞到东西了」
```

算一下：上升速度 `8.5 m/s`，一个物理帧位移 `8.5 × 0.02 = 0.17 m`。厚度 `0.1 m` 的天花板**会被整段跨过去**——物理帧开始时在下方、下一帧已经在上面，中间没有任何一帧处于「相交」状态。等 `Update` 里的传感器检测到时，角色已经在方块上方，钳位来不及。

**修法：把「归零速度」这件事交给碰撞回调，而不是 Update 里的检测**

```cs
// 只在上升时清零，下落中不要干扰（否则会卡在方块里不掉）
public void StopRise()
{
    Vector3 v = rb.velocity;
    if (v.y <= 0f) return;
    v.y = 0f;
    rb.velocity = v;
}

// Enter 管第一次接触，Stay 防止贴住后被后续推力再顶上去
private void OnCollisionEnter(Collision c) => HandleCeilingContact(c);
private void OnCollisionStay(Collision c)  => HandleCeilingContact(c);

private void HandleCeilingContact(Collision c)
{
    if (((ceilingMask.value >> c.gameObject.layer) & 1) == 0) return;
    motor.StopRise();
}
```

**为什么碰撞回调拦得住**：碰撞回调由物理系统在**求解碰撞的同一时刻**给出（`FixedUpdate` 内部、位置修正之后），此时速度还没被积分到下一帧。在这里清零，等于在穿透发生的那一步就掐住。

| 方案 | 能否拦穿透 | 代价 |
| --- | --- | --- |
| Update 里 OverlapSphere 检测 + 钳位 | ❌ 薄物体必穿 | 便宜 |
| **碰撞回调里归零速度** | ✅ | 需要正确的 Layer Collision Matrix |
| `Rigidbody.collisionDetectionMode = ContinuousDynamic` | ✅ 防穿（扫掠检测） | 稍贵，但**必须配**，光靠回调不够稳 |
| 射线/SphereCast 预判 | ✅ 且可控 | 要额外配置 `obstacleMask`，成本最高 |

> ⚠️ 两个前提：① `Project Settings → Physics → Layer Collision Matrix` 里两层必须勾选碰撞，否则**根本不产生碰撞，回调也不会触发**；② 双方 Collider 都不能是 Trigger。

## 进阶二：per-actor 重力倍率（Unity 没有 UE 那种 Gravity Scale）

`Rigidbody.useGravity` 只有开关，重力大小只能改全局 `Physics.gravity`。想单独给某个角色调重力手感，就在 `FixedUpdate` 里**补一份差额加速度**：

```cs
// extra = |g| × (scale - 1)，scale = 1.5 → 总重力变成 1.5 倍
float extra = Physics.gravity.magnitude * (config.gravityScale - 1f);
if (Mathf.Abs(extra) < 0.001f) return;

// extra 为负时 Vector3.down × 负值 = 向上，正好对应 scale < 1 的减小重力
rb.AddForce(Vector3.down * extra, ForceMode.Acceleration);
```

- 用 `ForceMode.Acceleration` 可以忽略质量，和真实重力的表现一致（真实重力也是加速度）
- `useGravity == false`（吸附/浮空）时直接跳过，不要重复下压
- 也可以临时改 `Physics.gravity` + 自己维护「谁在用」，但多角色就崩了

### 改重力后跳跃参数怎么跟着调

跳跃初速 `v`、重力 `g`、倍率 `k`（有效重力 `g·k`）：

| 目标 | 公式 | 结论 |
| --- | --- | --- |
| 滞空时间 | `T = 2v / (g·k)` | 保持 T 不变 → `v' = v × k` |
| 跳高 | `H = v² / (2g·k)` | 保持 H 不变 → `v' = v × √k` |

`v` 原为 `6`、`g = 9.81` 时：`k = 1.5 → 7.35`、`k = 2.0 → 8.49`、`k = 2.5 → 9.49`。

> 经验：**只调 `k` 不调 `v`，手感会变成「跳得又快又矮」**。想要「同样的高度、更干脆的落地」，两个一起改。

## 相关

- [[MonoBehaviour生命周期与SetActive的坑]]
- [[Physics Raycast 与 NonAlloc]] — 传感器用 `OverlapSphereNonAlloc` 复用的写法
- [[Rigidbody 睡眠与 Trigger Collider]] — 碰撞回调不触发的另一类原因（睡眠 / Trigger）
- [[平面反射 — 镜像相机、斜投影裁剪与反射贴图采样]] — 相机侧每帧更新的另一条链路
