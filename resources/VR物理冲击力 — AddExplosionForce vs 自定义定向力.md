---
title: "VR物理冲击力 — AddExplosionForce vs 自定义定向力"
type: resource
tags: [unity, 物理, VR, 面试]
created: "2026-08-12"
updated: "2026-08-12"
status: active
summary: "AddExplosionForce 三坑（方向不可控/必须全挂 RB/质心不可预测）与自定义定向力方案（dir+衰减+Impulse），VR 冲击波首选自定义"
---

# VR物理冲击力 — AddExplosionForce vs 自定义定向力

> 场景：VR 消防里想让灭火器气流/爆炸把纸箱、杂物"吹飞"，有人直接上 `Rigidbody.AddExplosionForce`，在 VR 里一用就翻车。

## AddExplosionForce 的机制

- 以爆炸点为圆心，**自动**给半径内**所有挂了 Rigidbody + Collider** 的物体施加**全向放射状**冲量
- 方向 = 从爆炸点指向**每个物体的质心**，距离越远力越小（固定衰减公式）

## VR 里的三个坑

1. **方向不可控** 🎯
   力自动"爆炸点 → 质心"，没法单独决定谁被推、往哪推、多大力——想只推面前的箱子，结果身后的椅子也飞了。
2. **必须全挂 Rigidbody + Collider** 🧱
   没挂 RB 的物体纹丝不动；对半径内所有 RB 逐个遍历施力，范围大/物体多时性能白白开销。VR 最怕**玩家手里拿的物体被一起炸飞**（拿灭火器结果自己被弹飞，体验直接崩）。
3. **方向和力度不可预测** 🎲
   方向基于**质心**，形状不规则时方向很怪；固定衰减公式难调出"近处猛推、远处轻吹"；还会把物体炸上天穿墙。

## 自定义定向力（推荐）

```csharp
// 1. 自己算方向：从爆炸点指向受击物体，归一化
Vector3 dir = (obj.position - explosionPoint).normalized;
// 2. 自己算衰减：距离越远力越小（可换平方反比/自定义曲线）
float dist = Vector3.Distance(obj.position, explosionPoint);
float force = baseForce * (1f - dist / maxRadius);
// 3. 自己施力：Impulse = 瞬间冲量（VR 冲击波首选）
rb.AddForce(dir * force, ForceMode.Impulse);
```

**优点全写在脸上**：
- 方向完全可控：只推特定物体（按列表/标签挑选）、加随机扰动、**锁定 Y 轴防止炸上天**
- 衰减曲线自己定：线性 / 平方反比 / AnimationCurve
- 不用遍历所有物体，性能可控

## 面试一句话

> AddExplosionForce 是"引擎帮你自动决定一切"，自定义定向力是"你自己接管方向、衰减、作用对象"——VR 里要的就是后者。

## 相关

- [[ParticleSystem详解]]
- [[Physics Raycast 与 NonAlloc]]
- [[协程原理与unitask]]
