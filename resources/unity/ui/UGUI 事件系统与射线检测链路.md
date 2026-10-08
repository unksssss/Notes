---
title: "UGUI 事件系统与射线检测链路"
type: resource
tags: [unity, UGUI, 事件系统, 射线检测, 性能优化, unity面试]
created: "2026-10-08"
updated: "2026-10-08"
status: active
summary: "UI 事件完整触发链路（Input→EventSystem→InputModule→Raycaster→ExecuteEvents 冒泡）、GraphicRaycaster 源码级射线流程、Raycast Target 过多的真实代价（含「不影响 DrawCall」辟谣）与七条优化手段"
source: "Day 40 面试八问（Q2 / Q3）"
related: ["UGUI事件接口与EventTrigger", "UGUI 拖拽实现与拖放检测", "UGUI DrawCall 与性能优化", "UGUI多级UI性能优化与Canvas重建"]
---

# UGUI 事件系统与射线检测链路

> 面试问：**UI 点击事件的完整触发链路是什么？** / **Raycast Target 太多会怎样？**

## 一、三个角色

| 角色 | 别名 | 职责 |
|---|---|---|
| `EventSystem` | 事件系统（全局唯一） | 每帧驱动 InputModule，持有 Raycaster 注册表，负责把「命中了谁」变成「发给谁」 |
| `BaseInputModule`（`StandaloneInputModule` / `InputSystemUIInputModule`） | 输入模块 | 把原始输入变成 `PointerEventData`，调 `RaycastAll`，再分发 enter/exit/down/up/click/drag |
| `BaseRaycaster`（`GraphicRaycaster` / `PhysicsRaycaster` / `Physics2DRaycaster`） | 射线投射器 | 真正回答"指针下面有什么"。UI 靠 Canvas 上的 `GraphicRaycaster`，3D 靠挂在相机上的 `PhysicsRaycaster` |

## 二、完整触发链路（鼠标点击为例）

```
Legacy Input / Input System
        ↓
EventSystem.Update()                        ← 每帧
        ↓   TickModules() + 选一个 ShouldActivateModule() 为真的模块
        ↓   m_CurrentInputModule.Process()
StandaloneInputModule.Process()
        ├─ SendUpdateEventToSelectedObject()          （导航/选中态）
        ├─ ProcessTouchEvents()                       （有触摸则处理触摸）
        └─ ProcessMouseEvent()                        （!ProcessTouchEvents && mousePresent）
                ↓
        GetMousePointerEventData()
                ↓
        eventSystem.RaycastAll(pointerData, m_RaycastResultCache)     ★ 核心
                ├─ 遍历 RaycasterManager.GetRaycasters()（所有已注册、IsActive 的 Raycaster）
                │     ├─ GraphicRaycaster.Raycast()   → UI
                │     ├─ PhysicsRaycaster.Raycast()   → 3D Collider
                │     └─ Physics2DRaycaster.Raycast() → 2D Collider
                └─ raycastResults.Sort(RaycastComparer)   → 排完序取第一个 = 命中目标
                ↓
        pointerEvent.pointerCurrentRaycast = FindFirstRaycast(...)
                ↓
        ┌── ProcessMove()    → HandlePointerExitAndEnter()  → OnPointerExit / OnPointerEnter
        ├── ProcessPress()   → OnPointerDown / OnPointerUp / OnPointerClick
        └── ProcessDrag()    → OnBeginDrag / OnDrag / OnEndDrag（释放帧还有个 OnDrop）
```

### 关键源码细节（面试加分项）

**① 排序规则**（`EventSystem.RaycastComparer`）——决定"谁在上层"：

```
相机 depth（Camera 模式）→ module 的 sortOrderPriority / renderOrderPriority
→ sortingLayer → sortingOrder → depth（同一 Canvas 内的层级深度）→ distance → index
```

**② `GraphicRaycaster.Raycast` 的过滤顺序**（这里的每一行都是一个性能点）：

```cs
// 伪代码，对应 GraphicRaycaster.Raycast 主干
var canvasGraphics = GraphicRegistry.GetGraphicsForCanvas(canvas);  // ★ 拿到本 Canvas 下所有注册的 Graphic
for (int i = 0; i < canvasGraphics.Count; i++)
{
    var graphic = canvasGraphics[i];
    if (graphic.depth == -1 || !graphic.raycastTarget || graphic.canvasRenderer.cull)
        continue;                                                   // ★ raycastTarget 在这里被一刀砍掉
    if (!RectTransformUtility.RectangleContainsScreenPoint(
            graphic.rectTransform, pointerPosition, eventCamera, graphic.raycastPadding))
        continue;                                                   // ② 矩形包含判断（含 raycastPadding 扩展）
    if (eventCamera != null && eventCamera.WorldToScreenPoint(...).z < 0)
        continue;                                                   // ③ 图形在相机背面
    if (!graphic.Raycast(pointerPosition, eventCamera))
        continue;                                                   // ④ 往上遍历父链跑 ICanvasRaycastFilter（Mask / RectMask2D）
    // ⑤ 打包 RaycastResult（含 depth / sortingLayer / sortingOrder / distance）
}
```

> 💡 `Graphic.Raycast` 内部会**从自身沿父链向上遍历**，对每个 `ICanvasRaycastFilter`（`Mask`、`RectMask2D` 都实现了它）问一句"这个点在我裁剪范围内吗"。这是 Mask 区域较深时的隐形开销来源。

**③ 事件是「冒泡」的**——`ExecuteEvents` 的两个 API 别搞混：

| API | 行为 | 用在哪 |
|---|---|---|
| `ExecuteEvents.Execute<T>(go, data, functor)` | **只发给 `go` 自己**，不向上找 | 已确定处理者后（如 click、drag） |
| `ExecuteEvents.ExecuteHierarchy<T>(go, data, functor)` | 从 `go` 开始**沿父节点向上**找第一个实现 `T` 的组件并执行 | 按下（pointerDown）、抬起后的 drop |
| `ExecuteEvents.GetEventHandler<T>(go)` | 同上，但**只返回**那个组件不执行 | 记录 `pointerPress` / `pointerDrag` |

→ 所以**子物体没实现 `IPointerClickHandler`，父物体实现了，点子物体父物体也会响应**。

**④ 点击（Click）的双条件**：

```cs
// ProcessMousePress 的释放分支（简化）
var pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);
if (pointerEvent.pointerPress == pointerUpHandler && pointerEvent.eligibleForClick)
    ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerClickHandler);
```

必须**按下与抬起命中的是同一个处理者**才发 `OnPointerClick` → 这就是"按下后拖出去再松手，按钮不响应点击"的原因。

**⑤ 拖拽的三个阈值/时机**（`ProcessDrag`）：

```cs
if (!pointerEvent.dragging && ShouldStartDrag(pointerEvent.pressPosition, pointerEvent.position,
        eventSystem.pixelDragThreshold, pointerEvent.useDragThreshold))
{
    ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.beginDragHandler);
    pointerEvent.dragging = true;
}
```

- 位移超过 `EventSystem.pixelDragThreshold`（默认 10 px）才发 `OnBeginDrag`；
- 实现 `IInitializePotentialDragHandler` 可把 `pointerEvent.useDragThreshold = false` 来**取消阈值**（想按下即拖时用）；
- `pointerDrag` 在**按下帧**就被记录：`pointerEvent.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo)`。

**⑥ 释放帧的顺序（超高频考点）**：

```
OnPointerUp →（若 dragging）OnDrop 发给 currentOverGo（ExecuteHierarchy 冒泡）→ OnEndDrag 发给 pointerDrag → OnPointerClick（条件满足时）
```

**`OnDrop` 是发给「抬起那一帧射线命中的对象」**——这一点直接决定了 [[UGUI 拖拽实现与拖放检测]] 里"拖到目标上没反应"的根因。

**⑦ 输入模块选型**：用新 Input System 时把 `StandaloneInputModule` 换成 `InputSystemUIInputModule`（挂在同一个 EventSystem 上，替换掉旧模块，否则两个模块抢输入）。

## 三、Raycast Target 太多会怎样？

### 真实代价（都在 CPU 主线程）

1. **每帧都在跑，不只是点击时**：`StandaloneInputModule.Process()` 每帧执行 → `ProcessMouseEvent()` → `RaycastAll` **每帧**全量重算。鼠标不动也照跑。
2. **遍历规模 = 该 Canvas 下 Graphic 总数**：`GraphicRegistry.GetGraphicsForCanvas(canvas)` 返回全部注册 Graphic（`OnEnable` 注册、`OnDisable` 注销，所以 inactive 的不参与），逐个做 `Raycast`。
3. **单次判断不便宜**：`RectangleContainsScreenPoint` 要做 RectTransform 的矩阵逆变换；`Graphic.Raycast` 还要沿父链跑 `ICanvasRaycastFilter`（Mask/RectMask2D 裁剪）。
4. **结果排序**：命中结果要 Sort（O(n log n)，比较器已缓存，无额外 GC）。
5. **多点触控成倍**：`ProcessTouchEvents` 每个触点一轮 `RaycastAll`。

**症状**：Profiler 里 `EventSystem.Update` → `StandaloneInputModule.Process` → `GraphicRaycaster.Raycast` 占比偏高，移动端掉帧；大列表/复杂界面尤其明显。

### ⛔ 辟谣：Raycast Target 不影响 DrawCall

很多资料说"Raycast Target 越多 DrawCall 越多"——**错的**。Raycast Target 是**逻辑层**（命中检测）的开关，DrawCall 是**渲染层**（`CanvasRenderer` 的 Mesh 合批）的事，两者互不影响。关掉 Raycast Target 不会让画面少一个 DrawCall，只会让 `GraphicRaycaster` 少判一个矩形。→ 渲染层的优化看 [[UGUI DrawCall 与性能优化]]。

### 七条优化手段

| # | 手段 | 说明 |
|---|---|---|
| 1 | **纯展示元素关掉 Raycast Target** | Text、装饰 Image、背景图一律关。收益最大、最无脑（注意：关了就收不到 enter/exit，别误关要交互的） |
| 2 | **用一块透明 Image 当"点击板"** | 大面板里想整块可点 → 顶层放一个透明 Image 承接，而不是给每个子元素开 |
| 3 | **`CanvasGroup.blocksRaycasts = false`** | 整棵子树一次性退出射线（不影响 alpha / interactable）。隐藏面板、拖拽幽灵的标准做法 |
| 4 | **按交互性拆 Canvas** | `GraphicRaycaster` 只遍历**自己 Canvas** 的 Graphic → 把不交互的静态 UI 挪到另一个（可不挂 Raycaster 的）Canvas，遍历量直接砍掉一大块 |
| 5 | **`Image.raycastPadding`** | 缩小或扩展命中区域，避免为了命中去加更大的透明图 |
| 6 | **`Image.alphaHitTestMinimumThreshold` / 自定义 `ICanvasRaycastFilter`** | 像素级命中（不规则按钮），但 alpha 判定要读纹理、很贵，慎用 |
| 7 | **长列表只让可见项参与** | 滚动列表用 `RectMask2D`（会设 `canvasRenderer.cull`，被裁掉的 Graphic 直接被 Raycaster `continue` 跳过）+ 对象池回收屏外项 |

**其他相关开关**

- `GraphicRaycaster.ignoreReversedGraphics`：忽略背对相机的图形（默认开）；
- `GraphicRaycaster.blockingObjects`：`None` / `TwoD` / `ThreeD` / `All` —— 设为 2D/3D 时，**2D/3D 碰撞体会挡住 UI 事件**（UI 收不到点击）。这是「UI 点了没反应」的隐藏元凶之一；
- `Canvas.overrideSorting`：会让 `Graphic.Raycast` 停止向上遍历（父链裁剪判定中断）。

## 四、定位问题的三件套

```cs
// ① 探针：这个屏幕点下面到底是谁
var data = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
var results = new List<RaycastResult>();
EventSystem.current.RaycastAll(data, results);          // results[0] 就是会收到事件的那个
if (results.Count > 0) Debug.Log($"命中: {results[0].gameObject.name}, depth={results[0].depth}");

// ② 常用判空：指针是否在 UI 上（-1 = 鼠标；触摸用 touchId）
bool overUI = EventSystem.current.IsPointerOverGameObject();
bool overUIByTouch = EventSystem.current.IsPointerOverGameObject(touchId);
```

③ Editor 里运行时选中 **EventSystem**，Inspector 底部能看到当前 `pointerEnter` / `pointerPress` / `raycast results`，比打日志快得多。

## 相关

- [[UGUI事件接口与EventTrigger]] — 接口写法 vs EventTrigger、`IPointerMoveHandler` 不生效四大原因
- [[UGUI 拖拽实现与拖放检测]] — 拖拽四接口、`OnDrop` 时序、拖拽中目标探测失效的根因
- [[UGUI DrawCall 与性能优化]] — 渲染侧（合批 / Overdraw）
- [[UGUI多级UI性能优化与Canvas重建]] — Canvas Rebuild 侧
- [[Physics Raycast 与 NonAlloc]] — `PhysicsRaycaster` 与 `RaycastNonAlloc` 的配合
- [[UGUI Canvas 渲染模式与屏幕适配]] — Event Camera 没设 → 事件全失效
