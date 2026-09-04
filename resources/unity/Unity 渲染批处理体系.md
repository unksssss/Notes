---
title: Unity 渲染批处理体系
type: resource
tags:
  - unity
  - rendering
  - batch
  - static-batching
  - gpu-instancing
  - srp-batcher
created: 2026-07-28
updated: 2026-09-01
status: active
summary: 静态批处理 vs GPU Instancing vs SRP Batcher 对比、条件、陷阱、MPB 易混口诀
---

# Unity 渲染批处理体系

## 三种批处理技术总览

| 特性 | 静态批处理 | GPU Instancing | SRP Batcher |
|------|-----------|---------------|-------------|
| 合并时机 | Build / Enter Play Mode | Render 时 | Render 时 |
| 顶点上限 | ~64K（GPU 索引缓冲限制） | 无（受显存限制） | 无 |
| 材质要求 | **完全相同**的 Material 实例 | 可不同（属性可差异） | **相同 Shader Variant** |
| 网格要求 | 不同网格可合并 | **相同网格** | 不同网格可合并 |
| 内存开销 | 合并网格常驻显存（大） | 低（实例化数据流） | 中等（CBV 缓存） |
| 动态物体 | ❌ 不支持 | ✅ 支持 | ✅ 支持 |
| 光照贴图不同 | ❌ 拆批 | ✅ 可以 | ✅ 可以 |
| Unity 版本 | 所有版本 | Unity 5.4+ | 2019.1+（URP/HDRP） |

---

## 静态批处理（Static Batching）

### 原理
将标记为 Batching Static 的物体网格在**构建时**合并为一个大网格，用**一次 DrawCall** 绘制所有内容。每个子物体保留自己的 Transform 信息，在 GPU 侧通过索引偏移量定位。

### 条件检查清单

- ✅ 物体标记 `Batching Static`（Inspector Static 标记组）
- ✅ 使用**完全相同的材质球**（同一个 Material 实例）
- ✅ 网格格式支持合并（32-bit index buffer = ~64K 顶点上限）
- ❌ 光照贴图不同 → 材质不同 → 拆批
- ❌ 光照探针偏移不同 → 可能拆批

### 陷阱

1. **显存开销大**：合并后的网格常驻显存，静态场景越复杂，合并后网格越大
2. **内存翻倍**：原始网格 + 合并网格同时存在于内存中
3. **64K 顶点限制**：合并后总顶点数超过 ~64K → 拆成多个批次
4. **无法运行时修改**：Build 后网格已固化
5. **Editor Play Mode 也生效**：Stats 窗口的 Saved by batching 在 Play Mode 就能看到

---

## GPU Instancing

### 原理
对**相同网格 + 不同材质属性**的物体，GPU 在单次 DrawCall 中绘制多个实例。GPU 从实例化缓冲区读取每个实例的变换矩阵和材质属性。

### 启用方式
```csharp
// 材质启用 GPU Instancing
MaterialPropertyBlock block = new MaterialPropertyBlock();
block.SetColor("_Color", Color.red);
renderer.SetPropertyBlock(block);

// Shader 需要支持 Instancing
// 在 Shader 中添加 #pragma multi_compile_instancing
```

### 条件检查清单

- ✅ **相同网格**（Mesh 必须完全一样）
- ✅ Shader 支持 Instancing（`#pragma multi_compile_instancing`）
- ✅ 材质变量通过 `MaterialPropertyBlock` 设置（不修改 SharedMaterial）
- ❌ 不同网格 → 无法合并
- ❌ Shader 不支持 Instancing → 退化为普通 DrawCall

### 陷阱

1. **必须相同网格**：这是最大的限制条件
2. **不支持蒙皮网格**（SkinnedMeshRenderer 不使用 GPU Instancing 标准路径）
3. **实例化数据上限**：GPU 每个批次实例数受硬件限制（通常 ~500~1023）
4. **`MaterialPropertyBlock` 使用注意**：频繁修改 MPB 会触发 SetPass 调用，可能得不偿失
5. **ShadowCaster 也需要 Instancing**：否则阴影绘制无法合批

---

## SRP Batcher（Scriptable Render Pipeline Batcher）

### 原理
SRP Batcher 在 URP/HDRP 中工作，将**相同 Shader Variant** 的物体的 Per-Material 数据缓存在 GPU CBV（Constant Buffer View）中。切换不同材质时，只更新变化的部分，大幅降低 SetPass 调用的 CPU 开销。

```
没有 SRP Batcher：
  Draw A → 上传 Material 数据 → Draw B → 上传 Material 数据 → ...

有 SRP Batcher：
  Draw A → CBV 中的数据已在 GPU → Draw B → 只需更新差异 → ...
```

### 条件检查清单

- ✅ **相同 Shader Variant**（相同的 Shader + 相同的 Keywords 组合）
- ✅ URP/HDRP 渲染管线
- ✅ SRP Batcher 在 Project Settings 中启用
- ❌ 内置渲染管线不可用
- ❌ Shader 不支持 SRP Batcher（自定义 Shader 可能需要改造）

### 与 MaterialPropertyBlock 的互斥

```csharp
// ⚠️ 关键陷阱：MPB 会导致 SRP Batcher 回退
renderer.SetPropertyBlock(block);  // ← 这个操作让 SRP Batcher 对当前渲染器失效！

// SRP Batcher 要求直接使用 Material 数据
// 一旦用了 MPB，Unity 无法确定 GPU CBV 缓存和 MPB 数据的关系
// → 回退到传统逐物体设置数据的模式（SetPass 开销重新出现）
```

**回退原因**：SRP Batcher 依赖 Material 的恒定数据在 GPU 端缓存，MPB 注入的是运行时可变的 Per-Renderer 数据，打破了缓存一致性假设。
一旦调用MaterailPropertyBlock,引擎就会认为这个Render有私有属性覆盖,没法再和其他Render共享GPU常量缓冲区了，于是回退到传统DrawCall单独绘制.

**⚡ 易混口诀（Day 32 灯皇又把 MPB 说反了，必背）**：

> **MPB 帮 Instancing（每实例差异化属性靠它传），砸 SRP Batcher（用了它兼容性就破）**

- GPU Instancing：MPB / `[PerRendererData]` 是**队友**——同 mesh 同材质下传每实例颜色/偏移，正是 Instancing 的差异化手段
- SRP Batcher：MPB 是**克星**——注入 Per-Renderer 数据打破 CBV 缓存一致性 → 回退传统 DrawCall
- 一句话记法：**MPB 是 Instancing 的调料、SRP Batcher 的毒药**；Batch 两兄弟里它对 Instancing 友好、对 Batcher 致命
### 陷阱

1. **MPB 导致回退**：使用 `MaterialPropertyBlock` 后 SRP Batcher 对该渲染器失效（常见坑！）
2. **不同 Shader Keywords 导致拆批**：Keyword 不同的两个 Material 走不同 Variant → 无法合批
3. **`_Time` 等内置变量不影响合批**：Unity 统一处理
4. **CBV 缓存有上限**：GPU 端 CBV 数量有限，极端复杂材质可能触发 flush
5. **Debug 查看**：Frame Debugger 中可以看到 SRP Batch 的相关信息

---

## 如何选择

```csharp
// 选型决策：
if (静态物体 && 相同材质)
    用静态批处理；
else if (大量相同网格 && 不同的颜色/缩放)
    用 GPU Instancing；
else if (URP/HDRP && 复杂材质)
    用 SRP Batcher 兜底；
else
    // 三种都不满足 = 无法合批，检查能否优化
```

### 实际应用建议

| 场景 | 推荐方案 |
|------|---------|
| 场景中大量相同树/石头 | GPU Instancing |
| 建筑/墙体/楼板 | 静态批处理 |
| URP 项目，材质丰富的场景 | SRP Batcher |
| 动态角色 + 相同 Shader | SRP Batcher |
| 不同颜色的相同物体 | GPU Instancing + MPB |
| 组合使用 | 静态批处理 + SRP Batcher 不冲突，可共存 |

## Stats 窗口读法

Game 视图 Stats 面板：
- **Saved by batching**：已合并的 DrawCall 数（越高越好）
- **Batches**：实际提交的 DrawCall 数
- **SetPass calls**：切换 Shader Pass 的次数（SRP Batcher 主要优化这个）

---

## Frame Debugger 排查合批失败（Day 30 先讲后答 ⚠️）

### 用法

**Window → Analysis → Frame Debugger** → 点 Enable → 左侧逐 DrawCall 浏览当前帧 → 点击某条看右侧详情，**合批失败时会直接给出 Break cause（断开原因）**。

### 常见 Break cause 速查表

| Break Cause | 含义 | 解决 |
|---|---|---|
| Different Mesh | Mesh 不同 | 静态批处理要求**相同 Mesh + 相同材质** |
| Different Material Instance | 材质被运行时实例化 | 代码里 `renderer.material`（自动克隆）→ 改 `sharedMaterial` 或 MPB |
| Different Shader / SRP Batcher 不兼容 | Shader 不兼容 | URP 下用了 Built-in shader，或自定义 shader 无 CBUFFER(UnityPerMaterial) |
| Dynamic batching 限制 | 顶点 >900 / 缩放不一致 / 不同材质 | 换静态批处理或 GPU Instancing |
| Lightmap / 阴影设置不同 | 光照通道不同 | 统一光照设置 |

### 核心记忆点：同材质 ≠ 同材质实例

Inspector 显示同一材质球，但只要代码写过 `GetComponent<Renderer>().material.xxx = ...`，Unity 就会**悄悄克隆材质** → 两个物体各用各的实例 → 合批作废！

- 改**共享属性** → `renderer.sharedMaterial`（不克隆）
- 改**个体差异** → `MaterialPropertyBlock`（不破坏合批）
- ⚠️ 千万别在 Update 里每帧改 `.material`（每秒克隆 N 次，DrawCall 爆炸）

## 参考

- [Static Batching - Unity Manual](https://docs.unity3d.com/Manual/StaticBatching.html)
- [GPU Instancing - Unity Manual](https://docs.unity3d.com/Manual/GPUInstancing.html)
- [SRP Batcher - Unity Manual](https://docs.unity3d.com/Manual/SRPBatcher.html)
- [Frame Debugger - Unity Manual](https://docs.unity3d.com/Manual/FrameDebugger.html)
