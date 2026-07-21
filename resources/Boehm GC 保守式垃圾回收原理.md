---
title: "Boehm GC 保守式垃圾回收原理"
type: resource
tags: [unity, gc, memory, csharp]
created: "2026-07-21"
updated: "2026-07-21"
status: active
summary: "Unity Mono 使用的 Boehm GC 是保守式(Conservative)GC，不精确追踪引用，而是扫描堆栈内存判断指针"
---

# Boehm GC 保守式垃圾回收原理

## 背景

Unity Mono 运行时默认使用 **Boehm-Demers-Weiser GC**（简称 Boehm GC），属于**保守式（Conservative）GC**。.NET 4.x 下的 Mono 仍使用 Boehm GC（不启用并发 GC），直到 IL2CPP 后端 + 2019.1 以上才引入分代 GC 选项。

## 核心机制

### 保守 vs 精确 — 根本区别

| 特性 | 保守式 GC (Boehm) | 精确式 GC (SGen/现代) |
|:----|:-------------------|:---------------------|
| **引用追踪** | 扫描所有内存，**猜测**哪些值是指针 | 通过 GC Root 元数据**精确知道**引用位置 |
| **误报存活** | ⚠️ 整数刚好等于一个地址 → 对象被误判存活 | 零误报 |
| **对象移动** | ❌ 不能压缩/移动对象（指针可能被整数伪装） | ✅ 可以压缩/移动对象 |
| **内存碎片** | ❌ 碎片化严重，大对象分配失败风险高 | ✅ 压缩后可消除碎片 |
| **分配速度** | 慢（保守扫描） | 快（分代+精确追踪） |

### 工作原理三步走

```
┌─────────────────────────────────────────────────────┐
│  1. Mark（标记）                                      │
│     ↓ 从全局变量、栈、寄存器扫描所有看起来像指针的值     │
│     ↓ 猜测：如果 0xABCD 在堆范围内 → 标记为存活       │
├─────────────────────────────────────────────────────┤
│  2. Sweep（清除）                                     │
│     ↓ 扫描堆，未标记的对象视为垃圾                     │
│     ↓ 回收内存（但不压缩，地址不变）                    │
├─────────────────────────────────────────────────────┤
│  3. ⚠️ 误报影响                                      │
│     ↓ 未使用的已释放内存被"假指针"误标记               │
│     ↓ 本该回收的对象无法回收 → 内存泄漏假象             │
└─────────────────────────────────────────────────────┘
```

### 为什么 Boehm 不能压缩

```cs
// 假设堆上有对象 obj，地址为 0x12345678
// 栈上有一个 int 变量恰好值为 0x12345678

int guess = 0x12345678;  // 这只是个整数

// Boehm 扫描时看到 0x12345678 → 认为是指针
// → obj 被标记为存活
// → 如果 GC 移动了 obj，这个"指针"就变成野指针
// → 所以 Boehm 绝对不能移动对象 ❌
```

## Incremental GC

Boehm GC 支持 Incremental 模式（开启后可分拆到多帧）：

```cs
// 在 Player Settings 中开启
// → GC 标记-清除被拆散到多帧执行
// → 减少单帧卡顿（STW 暂停）
// → 但仍无法消除所有暂停

// Effects:
// ✅ 每帧暂停时间缩短（通常 < 5ms）
// ❌ 总 GC 耗时增加（因为分拆开销）
// ❌ 仍然是保守式，不能压缩
```

## 与精确式 GC 对比（SGen / Unity 分代 GC）

```cs
// === 精确式 GC（SGen / 现代 Unity）===
// 1. 有准确的 Root 信息 → 零误报存活
// 2. 分代收集（Gen 0/1/2）→ 新生代回收极快
// 3. 可压缩对象 → 消除碎片
// 4. 支持并发标记（部分平台）

// === 保守式 GC（Boehm / 旧 Mono）===
// 1. 扫描全部内存猜指针 → 有误报
// 2. 不能压缩 → 碎片化
// 3. 无分代 → 回收整堆
// 4. GC.Collect() 阻塞主线程（完全 STW）
```

## 对 Unity 开发的实践影响

### 1. 大对象分配要警惕

```cs
// ❌ 大数组分配后又被释放 → 碎片留在堆上
float[] bigData = new float[100000];
// 使用后释放
bigData = null;  
GC.Collect();  
// ⚠️ 100000 * 4 = 400KB 的空洞留在堆中
```

### 2. 避免栈上残留"假指针"

```cs
// ⚠️ 场景：结构体中的整数恰好等于某对象地址
struct PackedData
{
    public int a;
    public float b;
    public long c;
}

void Process()
{
    PackedData data = GetData();
    // data 在栈上，Boehm 扫描栈区域
    // 如果 data.c 的值恰好 = 某个堆对象地址
    // → 该对象被误标记存活，无法被回收
    // → 间接造成"内存泄漏"
}
```

### 3. `GC.Collect()` 在主线程全阻塞

```cs
// Mono + Boehm 下：
GC.Collect();
// ↑ 主线程在此暂停，整堆扫描标记-清除
// ↓ 可能导致几十毫秒甚至上百毫秒卡顿
```

## 何时升级到分代 GC

- **Unity 2019.1+** 可在 Player Settings → Configuration → **Use Incremental GC** 开启
- **IL2CPP + 分代 GC** 方案在 2020.3+
- 如果在 Boehm 下遇到 GC 引起的卡顿/碎片问题，考虑：
  1. 升级到分代 GC（切换脚本后端或修改 Player Settings）
  2. 使用对象池减少分配
  3. 避免频繁大对象分配

## 参考

- [Boehm GC 官方文档](https://www.hboehm.info/gc/)
- Unity Manual: Automatic Memory Management
- [Unity GC Best Practices](https://unity.com/how-to/optimize-garbage-collection-unity)
