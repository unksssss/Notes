---
title: "UGUI DrawCall 与性能优化"
type: resource
tags: [unity, UGUI, 性能优化, 渲染, DrawCall, unity面试]
created: "2026-10-08"
updated: "2026-10-08"
status: active
summary: "UGUI DrawCall 的生成机制与五条合批条件、破批十大元凶（Mask/TMP 材质/材质实例化/交错重叠）、Overdraw 与 DrawCall 的区别、Frame Debugger/Profiler 诊断链路与优化清单"
source: "Day 40 面试八问（Q8）"
related: ["UGUI 图集原理与合批", "UGUI多级UI性能优化与Canvas重建", "UGUI 事件系统与射线检测链路", "Unity 渲染批处理体系"]
---

# UGUI DrawCall 与性能优化

> 面试问：**UI 的性能优化怎么做？DrawCall 怎么降？**

> 与两篇兄弟笔记的分工：本文管 **DrawCall（渲染批次）**；[[UGUI多级UI性能优化与Canvas重建]] 管 **Canvas Rebuild（重建）**；[[UGUI 图集原理与合批]] 管 **图集与合批原理**。

## 一、DrawCall 是什么，UGUI 怎么产生的

- **DrawCall = CPU 通知 GPU「画一批东西」的一条命令**；中间切换渲染状态（材质/贴图/Shader 常量）就是一次 **SetPass Call**，通常与 DrawCall 同向变化。
- UGUI 的几何是**运行期由 `CanvasRenderer` 合出来的**：每个 `Graphic`（Image / Text / RawImage）生成顶点，Canvas 把它们**按材质 + 纹理 + 深度**排序分组，能合并的合成一个 Mesh 的多个 submesh → **一个 submesh 一个 DrawCall**。
- 所以 UI 的 DrawCall **不由 Hierarchy 层级决定，而由「排序后能否合并」决定**。

## 二、合批的五条条件（背下来）

一段 UI 能合成同一批次，必须同时满足：

1. **同一个 Material**（材质 ID 相同；`material` 被实例化过就会不同）
2. **同一张 Texture**（→ 图集：散图必然各占一批）
3. **深度连续**（渲染顺序相邻，中间没有别的批次的元素）
4. **不与其他批次元素视觉交错重叠**（A(tex1) 与 B(tex2) 重叠时，必须画成 A B A → 断成 3 批）
5. **同一个 Clip Rect / 不跨越 Mask 边界**（裁剪区域切换必然分段）

> 记忆口诀：**同材质、同贴图、深度连、不交错、同裁剪**。

## 三、破批十大元凶

| # | 元凶 | 说明 & 解法 |
|---|---|---|
| 1 | **散图未打图集** | 每张 Sprite 独立 texture → 每张一个批次。打成 Sprite Atlas |
| 2 | **`Mask` 组件** | 引入模板缓冲状态切换，必然断批 → 优先 **`RectMask2D`**（shader 裁剪，不切模板缓冲；但不同 clip rect 之间仍会分段） |
| 3 | **文字与图片交错** | TMP 有**自己的字体图集材质**，与 Sprite 图集不同 → 交错即断批。→ 文字尽量集中（独立 Canvas / 连续排列），或统一材质预设 |
| 4 | **材质实例化** | `image.material = new Material(...)`、或每帧 `material.SetColor(...)` 改颜色 → **`renderer.material` 每次访问都会克隆材质**（`sharedMaterial` 不会）。TMP 用 `color` 改色，别碰 `material` |
| 5 | **多 Canvas** | Canvas 之间不跨批；拆 Canvas 是双刃剑（独立重建 ✅ / 断批 ❌） |
| 6 | **`Shadow` / `Outline`** | 复制 Mesh，顶点成倍增加，且用独立材质 → 断批 + 顶点暴涨 |
| 7 | **Clip / 嵌套 Mask** | 每个 Mask 一个 clip rect，跨层元素分段 |
| 8 | **Z 深度交错** | Anchor/Rotation 导致元素深度穿插（视觉上前后关系与声明顺序不一致）→ 组内顺序被打乱 |
| 9 | **`RawImage` / 独立 Texture** | Runtime 下载的图、RenderTexture 不在任何图集里 → 独占批次 |
| 10 | **TMP 动态字体扩容** | SDF Atlas 新增字形会扩图 → 材质贴图变化 → 触发重建 + 断批（预生成字符集可缓解） |

## 四、⚠️ DrawCall ≠ Overdraw（高频混淆）

| | DrawCall | Overdraw |
|---|---|---|
| 层面 | **CPU** 提交命令的次数 | **GPU** 同一像素被重复着色的次数 |
| 症状 | CPU 主线程高、帧率受限于提交量 | GPU 带宽/填充率吃满，移动端发热掉电 |
| 元凶 | 破批（材质/贴图切换） | 全屏半透明遮罩叠层、大面积透明 UI、粒子 |
| 优化 | 合批（图集/统一材质/减少 Mask） | 减少全屏遮罩层数、遮挡剔除、减少半透明面积 |

**验证**：Scene View 的 **Overdraw** 调试视图（亮的区域就是过度绘制区）；Game View Stats 的 **Batches** 看批次。

> 经验值：移动端**单个界面 UI DrawCall 控制在 20~40** 以内较健康，> 100 需优化；全屏等效 Overdraw 层数尽量 **< 3**。

## 五、诊断工具链

| 工具 | 看什么 |
|---|---|
| **Game View Stats** | `Batches` / `SetPass Calls` / Tris / Verts —— 最快的横向对比 |
| **Frame Debugger** ★ | 逐批次展开，直接告诉你 **Why this draw call can't be batched**（`Objects have different materials` / `A different clip rect` 等） |
| **Profiler → Rendering / UI** | `Canvas.SendWillRenderCanvases`（重建）、`Canvas.RenderOverlays`、`EventSystem.Update`（射线） |
| **Profiler → UI 模块 / UI Toolkit Debugger** | 单个 Canvas 的 batch 数与 rebuild 次数分布 |
| **Scene View Overdraw 模式** | 过度绘制热区 |

```cs
// 运行时统计：快速定位"到底几个 Canvas / 几个 Mask / 几份材质"
var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
var masks    = FindObjectsByType<Mask>(FindObjectsInactive.Include, FindObjectsSortMode.None);
Debug.Log($"Canvas={canvases.Length}, Mask={masks.Length}");
```

## 六、优化清单（按收益排序）

**① 资源层（收益最大）**

- 同一界面/同一图集的 Sprite 打成 **Sprite Atlas**，一个界面尽量只用 1~2 张图集；
- **`Mask` → `RectMask2D`**；去掉 `Shadow` / `Outline`（用美术出图替代）；
- 字体图集预先烘焙常用字符集（避免运行时扩容）。

**② 结构层**

- **动静分离**：静态背景 / 动态列表 / 高频刷新区分到不同 Canvas（不必太碎，2~3 个）；
- 减少元素视觉交错，同材质的排在一起；
- 滚动列表用**对象池 + 只渲染可见项**（📎 [[对象池实现]]）。

**③ 代码层**

- 不要实例化材质：改色走 `Graphic.color` / `TMP_Text.color`，组件用 `sharedMaterial` 读；
- 文本更新用 `TMP.SetText` 零分配重载（📎 [[TMP Text 零分配更新]]）；
- 避免每帧改 RectTransform / 每帧 `GetComponent`；
- 用 `CanvasGroup` 控制显隐而不是 `SetActive`（减少重建，见 Rebuild 笔记）。

**④ 无效优化（要辟谣的）**

- **关 `Raycast Target` 不会减少 DrawCall**——它是逻辑层命中开关，属于 [[UGUI 事件系统与射线检测链路]] 的范畴；
- `RectTransform` 嵌套深度**不影响 DrawCall**（只影响重建范围）。

## 七、面试回答框架

1. **定位**：UI 性能 = **CPU 侧（DrawCall 合批 + Canvas Rebuild + 事件射线）** 与 **GPU 侧（Overdraw）** 两侧，先说是哪一侧的问题，再给手段；
2. **DrawCall**：合批五条件 → 破批元凶（散图、Mask、文字图片交错、材质实例化）；
3. **Rebuild**：动静分离拆 Canvas、浅平化层级、CanvasGroup 替代 SetActive；
4. **Overdraw**：减少全屏半透明叠层；
5. **验证**：Frame Debugger 看断批原因 + Profiler 看 `SendWillRenderCanvases` + Overdraw 视图，**给优化前后数字**（这部分最加分）。

**加分项**：主动区分 `Mask` vs `RectMask2D`、指出"关 Raycast Target 不降 DrawCall"、说明"拆 Canvas 独立重建但断批"的权衡。

## 相关

- [[UGUI 图集原理与合批]] — 图集共享纹理、Mask vs RectMask2D、按界面分组
- [[UGUI多级UI性能优化与Canvas重建]] — Canvas Rebuild 根因与四层优化
- [[Unity 渲染批处理体系]] — SRP Batcher / GPU Instancing / 静态动态合批（3D 侧）
- [[UGUI 事件系统与射线检测链路]] — Raycast Target 的真实代价
- [[TMP Text 零分配更新]] — 文本更新的 GC 与重建成本
- [[对象池实现]] — 滚动列表 / 长列表
