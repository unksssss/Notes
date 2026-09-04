---
title: "DOTS详解"
type: resource
tags: [unity, DOTS, ECS]
created: "2026-04-28"
updated: "2026-09-04"
status: active
summary: "Unity DOTS (ECS + Jobs + Burst) 完整讲解；含 Burst SIMD 向量化三要素（blittable/连续/对齐）"
source: ""
related: ["[[unity知识点-2026-04-28]]", "[[unity知识点-2026-04-29]]", "[[scriptableobject数据驱动设计]]", "[[unity-inputsystem详解]]", "[[unity网络同步方案-状态同步vs帧同步]]"]
---

# DOTS（Data-Oriented Tech Stack）

Unity 面向数据编程的核心理念，旨在榨干多核 CPU 性能。

## 三大支柱

### ECS（Entity Component System）
- **Entity**：只是一个 ID，代表游戏对象
- **Component**：纯数据，没有方法（struct）
- **System**：处理逻辑，遍历所有拥有特定 Component 的 Entity

### Jobs System
- 将工作分配到多个 CPU 线程
- 避免主线程瓶颈
- 必须保证数据安全（无竞态条件）

### Burst Compiler
- 将 C# 代码编译为高度优化的原生代码
- 基于 LLVM，对数据密集型运算提升 10x-100x

### Burst SIMD 向量化三要素（Day 35 真实面经 ✅）
Burst 把 IL → LLVM IR → 原生机器码时，会尝试自动**向量化**（一条 SIMD 指令同时算 4~8 个数据，如一次算 4 个 float 位置）。能否向量化取决于数据是否满足三要素（缺一不可）：

1. **blittable（可直译）**：内存布局与 C/C++ 一致、能整体拷贝。一旦字段出现 `string` / `object` / `delegate` / 接口引用这类**托管引用**，整个类型被标记为 `__managed`，SIMD 直接归零——编译器不知道引用指向哪、多长，无法摆进定长的向量寄存器
2. **连续**：数据必须物理地址连续（`T[]` / `NativeArray`）；链表、跳跃索引会让编译器放弃向量化
3. **对齐 / 布局确定**：字段要对齐（如 float4 需 16 字节）；`[StructLayout(LayoutKind.Auto)]` 自动排布会阻断向量化，需显式 Sequential/Explicit

**排查工具**：Burst Inspector（菜单 Jobs > Burst Inspector）查看是否生成 `vector.*` 指令；Profiler 的 Burst 标记项有 SIMD Width 列，0 = 向量化失败。

## 完整示例：移动系统

```csharp
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;

// Component：纯数据
public struct Velocity : IComponentData
{
    public float3 Value;
}

// System：处理逻辑
[BurstCompile]
public partial struct MoveSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (transform, velocity) in
                 SystemAPI.Query<RefRW<LocalTransform>, RefRO<Velocity>>())
        {
            transform.ValueRW.Position +=
                velocity.ValueRO.Value * SystemAPI.Time.DeltaTime;
        }
    }
}
```

## 对比传统 MonoBehaviour

| 维度 | MonoBehaviour | DOTS |
|------|--------------|------|
| 数据存储 | 每个对象独立（散落内存） | 连续数组（CPU 缓存友好） |
| 线程 | 主线程单线程 | 多线程并行 |
| 性能 | 对象越多越慢 | 万级对象依然流畅 |
| 适用场景 | 中小型项目 | 海量对象、大世界 |

## 相关笔记

- [[Profiler自定义采样]] — 用 Profiler 对比 DOTS 和传统方式的性能差异
- [[对象池实现]] — 另一种性能优化手段
