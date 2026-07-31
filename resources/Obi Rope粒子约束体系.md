---
title: "Obi Rope 粒子约束体系"
type: resource
tags: [unity, obi, 物理, 项目实战]
created: "2026-07-31"
updated: "2026-07-31"
status: active
summary: "Obi Rope 粒子 + 约束架构：Distance/Bend/Pin/Attachment 约束、初始爆炸根因（粒子堆叠自碰撞 + Pin 高空拉回）"
---

# Obi Rope 粒子约束体系

## 核心架构

Obi 把绳索拆成**一串粒子（ObiParticle）**，由**全局唯一的 Obi Solver** 统一做物理模拟，粒子之间靠**约束（Constraints）**建立关系：

| 约束类型 | 作用 | 类比 |
|---------|------|------|
| **Distance Constraint** | 限制相邻粒子距离（rest length）→ **绳索的本质**！ | 绳子纤维 |
| **Bend Constraint** | 抗弯曲，让绳索有刚性 | 关节 |
| **Pin Constraint** | 把粒子钉到某个物体上（锚点） | 钉子 |
| **ParticleAttachment** | 更高级的固定（Static 模式直接 `invMass = 0` + 每帧锁定位置） | 胶水 |
| **Collision / Self-Collision** | 粒子与物体/粒子之间的碰撞 | 互斥 |

> Distance constraint 通过迭代求解把"被拉长的距离"分摊修正回去，所以绳子看起来**不可拉伸**。

## 项目实战：初始爆炸根因（两种机制叠加）

1. **粒子堆叠自碰撞**：50m 绳索 = 500 粒子全部生成在**同一位置** → 自碰撞约束疯狂排斥 → 炸开！
2. **Pin/Attachment 高空瞬间拉回**：锚点在低处、粒子在高空 → 固定/距离约束一帧内强行拉回 → 约束反弹 → 粒子被弹飞！

## 已明确的解决方向

- 路径终点下沉到地下（Y=-1），多余绳索藏在地下被地面遮住，玩家走远时再被拉出地面 → 物理正确 + 初始稳定两全

## 生成要点速记（项目积累）

- Blueprint path 控制点必须在绳索 actor 的**局部坐标系**；绳索 transform 放在一个端点位置，使 CP0=(0,0,0) 对应端点世界坐标
- 用 `GenerateImmediate()` 而非 yield return `Generate()`
- **重力**：必须设 `solver.gravity`（顶层字段），`solver.parameters.gravity` 每帧被覆盖
- **端点固定**：用 `ObiParticleAttachment`（Static 模式）替代手动 Pin 约束
- MCP execute_code 会冻结游戏循环，无法测试物理模拟

## 相关

- [[Unity 渲染批处理体系]]
