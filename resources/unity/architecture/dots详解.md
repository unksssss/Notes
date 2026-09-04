---
title: "DOTS详解"
type: resource
tags: [unity, DOTS, ECS]
created: "2026-04-28"
updated: "2026-04-29"
status: active
summary: "Unity DOTS (ECS + Jobs + Burst) 完整讲解"
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
