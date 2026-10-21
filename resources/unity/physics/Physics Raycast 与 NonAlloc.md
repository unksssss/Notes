---
title: "Physics Raycast 与 NonAlloc"
type: resource
tags: [物理, 性能优化, 项目实战]
created: "2026-08-06"
updated: "2026-10-21"
status: active
summary: "Physics.Raycast 性能要点：LayerMask 过滤减少检测数量、同步立即返回；RaycastAll 每次返回新数组有 GC，RaycastNonAlloc 写入预分配数组零分配（VR 首选）；**2D 版本（Physics2D）**：3D 返回 bool vs 2D 直接返回 RaycastHit2D、AI 检测玩家的最小实现与四条排查清单、LayerMask 传层索引的坑、射线的成本判断（只有遮挡才需要）、ContactFilter2D 与 queriesHitTriggers"
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

## 2D 射线检测（Physics2D）

### 3D 与 2D 的 API 对照

| | 3D | 2D |
| --- | --- | --- |
| 类 | `Physics` | `Physics2D` |
| 单条检测 | `Physics.Raycast(ray, out hit, dist, mask)` → 返回 **bool** | `Physics2D.Raycast(origin, dir, dist, mask)` → 返回 **RaycastHit2D** |
| 命中结构 | `RaycastHit` | `RaycastHit2D` |
| 多条（有 GC） | `Physics.RaycastAll` → `RaycastHit[]` | `Physics2D.RaycastAll` → `RaycastHit2D[]` |
| 多条（零分配） | `Physics.RaycastNonAlloc(ray, hits, dist, mask)` | `Physics2D.RaycastNonAlloc(origin, dir, results, dist, mask)` |

⚠️ **最容易混的一点**：3D 的 `Raycast` 返回 **`bool`**（结果走 `out` 参数），2D 的**直接返回命中结构**（所以判空是判 `hit.collider`）。

### 最小实现：AI 检测玩家

```csharp
[SerializeField] private float detectDistance = 10f;
[SerializeField] private LayerMask playerMask;      // Inspector 勾 Player 层

void FixedUpdate()
{
    Vector2 origin    = transform.position;
    Vector2 direction = transform.right;            // AI 朝向

    RaycastHit2D hit = Physics2D.Raycast(origin, direction, detectDistance, playerMask);

    if (hit.collider != null)
    {
        Debug.Log($"发现 {hit.collider.name}，距离 {hit.distance:F2}");
    }
}

void OnDrawGizmosSelected()   // Scene 视图画线，调距离用
{
    Gizmos.color = Color.red;
    Gizmos.DrawLine(transform.position,
        transform.position + (Vector3)(transform.right * detectDistance));
}
```

### 查不到的四条排查清单（按概率排序）

| # | 条件 | 不满足的后果 |
| --- | --- | --- |
| 1 | 目标身上有 **`Collider2D`** | 完全检测不到（**光有 Rigidbody2D 不够**） |
| 2 | 目标的 **Layer 在 mask 里** | 完全检测不到 |
| 3 | `direction` 朝向正确 | 射线打反了 |
| 4 | `detectDistance` 够长 | 差一点到不了 |

→ 第 1、2 条占了「检测不到」原因的九成。

⚠️ **`transform.right` 不一定是角色前方**：不少 2D sprite 默认朝左，要用 `-transform.right`；或者用 `Vector2 facingDir` 字段（`-1` / `1`）记录朝向，比翻转 Transform 可靠。

### 坑：LayerMask 传了层索引而不是掩码

```csharp
Physics2D.Raycast(origin, dir, dist, 8);                        // ✗ 8 被当成 mask=8（第 4 层）
Physics2D.Raycast(origin, dir, dist, LayerMask.GetMask("Player"));  // ✓
Physics2D.Raycast(origin, dir, dist, 1 << playerLayerIndex);        // ✓
```

**这个坑不报错**——它检测的是完全不相干的层，表现为「射线一条命中都没有」，会让人误以为射线本身有问题。

### 先算成本：很多场景不该用射线

| 需求 | 该用什么 | 成本 |
| --- | --- | --- |
| 目标在不在**半径**内 | `Vector2.SqrMagnitude` 比较 | 几乎为零 |
| 目标在不在某个**形状**内 | `Physics2D.OverlapCircle` | 低 |
| 中间**有没有遮挡** | **`Physics2D.Raycast`** | 中 |
| 扇形视野 | 多条射线 / 角度判断 + 距离 | 高 |

→ **射线的真正价值是「视线检测」**：目标躲在墙后，范围上算在、但视线被挡住。只判断「离我多远」用距离比较就够，不必走物理查询。

### 2D 版本的 NonAlloc 与 ContactFilter2D

单条 `Physics2D.Raycast` 返回 `RaycastHit2D`（struct），**本身不产生 GC**，可以放心用。需要**多个**结果时才考虑零分配版本：

```csharp
RaycastHit2D[] results = new RaycastHit2D[8];        // 预分配，反复复用

// 写法一：直接传参数
int count = Physics2D.RaycastNonAlloc(origin, direction, results, detectDistance, playerMask);

// 写法二：ContactFilter2D（可精细控制是否检测 Trigger、法线角度等）
ContactFilter2D filter = new ContactFilter2D();
filter.useTriggers = false;                          // 不检测 Trigger 碰撞体
int count2 = Physics2D.Raycast(origin, direction, filter, results, detectDistance);
```

⚠️ **`Physics2D.queriesHitTriggers` 全局默认是 `true`**——目标挂的是 Trigger 碰撞体时默认也会被检测到。如果不希望这样，用 `ContactFilter2D` 单独关掉，比改全局设置安全。

## 相关

- [[XR射线交互与World Space Canvas]]
- [[UGUI事件接口与EventTrigger]]
