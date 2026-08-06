---
title: "Physics Raycast 与 NonAlloc"
type: resource
tags: [物理, 性能优化, 项目实战]
created: "2026-08-06"
updated: "2026-08-06"
status: active
summary: "Physics.Raycast 性能要点：LayerMask 过滤减少检测数量、同步立即返回；RaycastAll 每次返回新数组有 GC，RaycastNonAlloc 写入预分配数组零分配（VR 首选）"
---

# Physics Raycast 与 NonAlloc

## Raycast 基础认知

- **同步 API**：调用完立刻返回结果，不存在等下一帧（❌ 异步说法错误）
- **有真实开销**：走物理引擎 Broadphase 粗筛 + 窄相精检，不是免费
- **所有碰撞器类型都能检测**：Box / Mesh / Capsule / Sphere 都行（❌ "只能检测球体"错误）
- **LayerMask 过滤**：传入 layerMask 让物理引擎提前排除无关层碰撞器 → 检测数量大幅减少，性能优化第一招

```csharp
Physics.Raycast(ray, out hit, maxDist, LayerMask.GetMask("可交互"));
```

## ⚠️ RaycastAll 的 GC 陷阱

`RaycastAll(ray, maxDist)` 每次调用**返回一个新的 RaycastHit[] 数组** → 每帧调用 = 每帧堆分配。

VR 里手柄射线每帧检测，90 帧/秒就是 90 次数组分配 → GC 卡顿 → 掉帧头晕。

## ✅ 正解：RaycastNonAlloc（零分配）

```csharp
RaycastHit[] hits = new RaycastHit[8];   // Start 里预分配一次，反复复用
int count = Physics.RaycastNonAlloc(ray, hits, 100f);   // 返回实际写入数量
for (int i = 0; i < count; i++) {
    // 处理 hits[i]
}
```

三个关键点：

1. **返回值从数组变成 int 数量**——结果写入传入数组，返回写了几个
2. **数组容量要提前预估**：实际命中数超过容量时，**多余的直接被丢弃**！开大一点
3. **遍历用 count 不用 hits.Length**——否则会访问未写入的空槽位

## NonAlloc 家族

高频检测一律用 NonAlloc 系列：

- `Physics.RaycastNonAlloc`
- `Physics.SphereCastNonAlloc`
- `Physics.OverlapBoxNonAlloc` / `OverlapSphereNonAlloc`
- `Physics2D` 同样有 `Raycast`（带 List 参数版本）

## 相关

- [[XR射线交互与World Space Canvas]]
- [[UGUI事件接口与EventTrigger]]
