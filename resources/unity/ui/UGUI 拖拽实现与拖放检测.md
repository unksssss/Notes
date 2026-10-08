---
title: "UGUI 拖拽实现与拖放检测"
type: resource
tags: [unity, UGUI, 拖拽, 事件系统, 物理检测, unity面试]
created: "2026-10-08"
updated: "2026-10-08"
status: active
summary: "背包道具拖拽四接口实现骨架（含幽灵 CanvasGroup 处理与坐标转换）、拖放目标探测三方案，以及「拖拽过程中碰撞检测失效」的八条根因与解法（UI 无 Collider / Kinematic-vs-Static / ghost 挡射线）"
source: "Day 40 面试八问（Q6 / Q7）"
related: ["UGUI 事件系统与射线检测链路", "UGUI事件接口与EventTrigger", "Physics Raycast 与 NonAlloc", "DOTween与协程选型"]
---

# UGUI 拖拽实现与拖放检测

> 面试问：**背包的拖拽是怎么实现的？** / **UI 拖拽过程中两个 GameObject 碰撞检测失效怎么处理？**

## 一、拖拽的接口族

| 接口 | 回调 | 触发时机 |
|---|---|---|
| `IInitializePotentialDragHandler` | `OnInitializePotentialDrag` | 按下后、位移超过阈值**之前**。用来调 `e.useDragThreshold = false` 取消阈值 |
| `IBeginDragHandler` | `OnBeginDrag` | 位移超过 `EventSystem.pixelDragThreshold`（默认 10px）的第一帧 |
| `IDragHandler` | `OnDrag` | 拖拽中每帧 |
| `IEndDragHandler` | `OnEndDrag` | 松手帧（**在 `OnDrop` 之后**） |
| `IDropHandler` | `OnDrop` | 松手帧，发给**抬起那一刻射线命中的对象**（不是拖拽对象） |

**释放帧的精确顺序**（`StandaloneInputModule.ProcessMousePress` 释放分支）：

```
OnPointerUp → OnDrop(发给 currentOverGo，ExecuteHierarchy 冒泡) → OnEndDrag(发给 pointerDrag) → OnPointerClick
```

> ⚠️ 记住：**`OnDrop` 的目标来自「抬起帧的 `pointerCurrentRaycast.gameObject`」**。这一条是下面 Q7 全部问题的钥匙——谁挡住了射线，事件就发给谁。

## 二、标准实现流程（背包道具 → 场景）

**五步**：① 按下 → 记录原位/原父节点；② 开始拖 → 生成幽灵（或脱层拖原物）+ **幽灵关掉射线**；③ 拖动 → 换算坐标跟手 + **主动做物理探测**高亮可放置目标；④ 松手 → 判定落点；⑤ 成功放置 or 回弹。

```cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 背包道具拖拽 → 拖到场景放置。
/// 挂在背包格子里的 Item 上（需要 Graphic 且 Raycast Target = true）。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class BackpackDragItem : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IInitializePotentialDragHandler
{
    [Header("引用")]
    [SerializeField] private RectTransform ghost;          // 拖拽幽灵（可空 = 直接拖原物体）
    [SerializeField] private CanvasGroup   ghostGroup;     // 挂幽灵上：alpha + blocksRaycasts
    [SerializeField] private Canvas        rootCanvas;
    [SerializeField] private float         maxRayDistance = 50f;
    [SerializeField] private LayerMask     dropLayer;      // ★ 只检测「可放置」层，别用 Everything

    private RectTransform _self;
    private Vector2       _originAnchoredPos;
    private Transform     _originParent;
    private Camera        _eventCam;                       // Overlay 模式下为 null
    private readonly RaycastHit[] _hitBuffer = new RaycastHit[8];   // 复用 → 零 GC

    private void Awake()
    {
        _self = (RectTransform)transform;
        if (rootCanvas == null) rootCanvas = GetComponentInParent<Canvas>();
    }

    // 取消 10px 拖拽阈值（想「按下即拖」时实现它；想保留阈值就删掉这个接口）
    public void OnInitializePotentialDrag(PointerEventData e) => e.useDragThreshold = false;

    // ---------- ① 开始拖 ----------
    public void OnBeginDrag(PointerEventData e)
    {
        _originParent      = _self.parent;
        _originAnchoredPos = _self.anchoredPosition;
        _eventCam          = GetEventCamera(e);

        if (ghost != null)
        {
            ghost.gameObject.SetActive(true);
            ghost.SetAsLastSibling();                      // 拖拽中要盖住其他兄弟节点
            if (ghostGroup != null)
            {
                ghostGroup.alpha = 0.7f;
                ghostGroup.blocksRaycasts = false;         // ★★ Q7 核心：幽灵不参与射线
            }
        }
        else if (_originParent.TryGetComponent(out LayoutGroup _))
        {
            // 直接拖原物体：必须先脱离 LayoutGroup，否则每帧被布局系统拽回去
            _self.SetParent(rootCanvas.transform, worldPositionStays: true);
            _self.SetAsLastSibling();
        }
    }

    // ---------- ② 拖动中 ----------
    public void OnDrag(PointerEventData e)
    {
        var target = ghost != null ? ghost : _self;

        // Overlay 模式 eventCamera 传 null；Camera / World 模式必须传 eventCamera，否则坐标全错
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)target.parent, e.position, _eventCam, out var local))
            target.anchoredPosition = local;

        ProbeSceneTarget(e.position);                      // ★ 物理侧要自己查（见 Q7）
    }

    // ---------- ③ 松手 ----------
    public void OnEndDrag(PointerEventData e)
    {
        if (ghostGroup != null) ghostGroup.blocksRaycasts = true;
        if (ghost != null) ghost.gameObject.SetActive(false);

        var hit = ProbeSceneTarget(e.position);
        bool overUI = EventSystem.current.IsPointerOverGameObject(e.pointerId);

        if (hit.HasValue && !overUI) PlaceToScene(hit.Value);
        else                         ReturnToOrigin();

        _eventCam = null;
    }

    // ---------- 工具 ----------

    private Camera GetEventCamera(PointerEventData e)
    {
        if (e.pressEventCamera != null) return e.pressEventCamera;                 // Camera / World 模式
        return rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main;
    }

    private RaycastHit? ProbeSceneTarget(Vector2 screenPos)
    {
        var cam = _eventCam != null ? _eventCam : Camera.main;
        if (cam == null) return null;

        Ray ray = cam.ScreenPointToRay(screenPos);
        int count = Physics.RaycastNonAlloc(ray, _hitBuffer, maxRayDistance, dropLayer);   // 零分配

        RaycastHit? nearest = null;
        float minDist = float.MaxValue;
        for (int i = 0; i < count; i++)                    // RaycastNonAlloc 不排序 → 自己取最近
            if (_hitBuffer[i].distance < minDist) { minDist = _hitBuffer[i].distance; nearest = _hitBuffer[i]; }
        return nearest;
    }

    private void PlaceToScene(RaycastHit hit)
    {
        Debug.Log($"[背包] 放置到 {hit.collider.name} @ {hit.point}");
        ReturnToOrigin();                                  // 业务：实例化场景物 / 通知管理器
    }

    private void ReturnToOrigin()
    {
        if (ghost != null) return;                         // 有幽灵时原格子道具没动过
        _self.SetParent(_originParent, worldPositionStays: false);
        _self.anchoredPosition = _originAnchoredPos;
    }
}
```

### 落点判定的三种方案

| 方案 | 用法 | 适用 |
|---|---|---|
| **`IDropHandler`** | 目标对象自己实现 `OnDrop` 处理业务 | 目标数量少、结构固定（装备栏格位） |
| **`EventSystem.RaycastAll` 手动** | 自己拿全部命中结果并过滤 | ghost 关射线后、或需要「多个候选里挑一个」（要排除自己） |
| **`RectTransformUtility.RectangleContainsScreenPoint`** | 纯几何判断点是否在目标 Rect 内 | 完全不想依赖 Raycast Target / 想给目标一个宽容命中区 |

```cs
private static readonly List<RaycastResult> s_Buffer = new List<RaycastResult>(16);

private static GameObject FindDropTarget(PointerEventData e, GameObject self)
{
    s_Buffer.Clear();
    EventSystem.current.RaycastAll(e, s_Buffer);            // 结果已按前后排序
    foreach (var r in s_Buffer)
        if (r.gameObject != self && r.gameObject != e.pointerDrag) return r.gameObject;
    return null;
}
```

### 坐标转换三兄弟（必背）

```cs
// 屏幕点 → 指定 RectTransform 的局部坐标（跟手定位用；Overlay 的 eventCamera 传 null）
RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screenPos, cam, out Vector2 local);

// 屏幕点 → 世界坐标（World Space Canvas / 场景定位用）
RectTransformUtility.ScreenPointToWorldPointInRectangle(rect, screenPos, cam, out Vector3 world);

// 世界坐标 → 屏幕坐标
Vector2 sp = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
```

> 🔴 **最高频 bug**：把 `e.position`（屏幕坐标）直接赋给 `rectTransform.position` 或 `anchoredPosition` → 分辨率/Canvas 缩放一变位置就乱飞。**必须走 `RectTransformUtility`**。

## 三、Q7：拖拽过程中碰撞检测失效

### 先分清是哪一层"碰撞检测"

| 你在用 | 期望 | 实际 |
|---|---|---|
| 物理事件 `OnTriggerEnter` / `OnCollisionEnter` | 拖到目标上就触发 | ❌ **拖的是 UI，UI 没有 Collider/Rigidbody，物理引擎根本不知道它"碰"了谁** |
| UGUI 事件 `OnPointerEnter` / `OnDrop` | 拖到目标 UI 上就触发 | ⚠️ 可能被**拖拽幽灵挡住射线** |
| `Physics.Raycast` 手动查询 | 拖到 3D 目标上命中 | ✅ 只要自己发这条射线就正常（UI 不会挡 `Physics.Raycast`） |

### 八条根因与解法

**R1（UI 层，最常见）—— 拖拽幽灵挡住了射线**

跟随指针的 ghost 在指针正下方且 `Raycast Target = true` → `GraphicRaycaster` 命中的最上层就是 ghost → `OnDrop` 发给 ghost 而不是下面真正的目标。

```cs
// ✅ 幽灵挂 CanvasGroup，开始拖就把射线关掉
ghostGroup.blocksRaycasts = false;      // 不影响 alpha / interactable，只是不参与射线
// 等价土办法：ghost 上所有 Image/Text 的 raycastTarget = false
```

**R2（物理层，最常见）—— UI 拖拽不产生物理事件**

UGUI 拖拽走的是 EventSystem（逻辑层），**不产生任何物理碰撞**。想让"拖到场景物体上"有反应，必须**主动查询**：

```cs
int n = Physics.RaycastNonAlloc(cam.ScreenPointToRay(screenPos), buffer, dist, dropLayer);
// 或用 OverlapSphere / OverlapBox 做体积检测（拖拽有面积时更稳）
```

**R3 —— Kinematic Rigidbody 与 Static Collider 之间不产生碰撞/触发事件**

物理引擎的配对规则：`Kinematic Rigidbody` ↔ `Static Collider` **不会**触发 `OnTriggerEnter`/`OnCollisionEnter`。所以"给被拖物体加了 `isKinematic = true` 的 Rigidbody 想让它检测碰撞" → 依然没反应。

```cs
// ✅ 让被拖物体真正参与物理
rb.isKinematic = false;
rb.useGravity  = false;
rb.constraints = RigidbodyConstraints.FreezeRotation;
// 移动用 MovePosition（走物理步进）
rb.MovePosition(targetPos);
```

**R4 —— Trigger 事件需要至少一方有 Rigidbody**

两个都是 Trigger 且都没有 Rigidbody → 不生成事件。3D 物理必守规则。

**R5 —— `GraphicRaycaster.blockingObjects` 反向干扰**

设为 `TwoD` / `ThreeD` / `All` 时，**碰撞体会挡住 UI 事件** → 表现是"UI 突然点不动了/拖放目标收不到事件"。

```
Canvas → GraphicRaycaster → Blocking Objects: None（默认，除非确实需要遮挡）
```

**R6 —— 3D 物体用 `OnMouseXXX` 与 EventSystem 混用**

`OnMouseDrag` 期间鼠标被**独占**，其他对象的 `OnMouseXXX` 不再触发。→ 统一改走 `PhysicsRaycaster` + `IPointerXXXHandler`，别混用两套输入机制。

**R7 —— LayoutGroup 把拖拽物拽回去**

父容器有 `LayoutGroup` → 每帧重排，手动改 `anchoredPosition` 无效。
→ 开始拖时把元素 reparent 到 Canvas 顶层（`SetParent(canvas.transform, true)`），或临时 `LayoutElement.ignoreLayout = true`。

**R8 —— 子物体抢了 ScrollRect 的拖拽**

item 上实现 `IDragHandler` 后，滚动列表滑不动（事件被子物体接走）。
→ `OnBeginDrag` 里判断主方向：竖直滚动列表 + 水平拖动 → `scrollRect.enabled = false`，拖完再还原；或让 item 实现 `IScrollHandler` 转发。

### 兜底方案：手动补发事件

Ghost 关射线后，若还想让下层 UI 收到 `OnPointerEnter` 之类（比如高亮预览），可以自己 Raycast + Execute：

```cs
EventSystem.current.RaycastAll(e, s_Buffer);
foreach (var r in s_Buffer)
{
    if (r.gameObject == e.pointerDrag) continue;                  // 跳开自己
    ExecuteEvents.Execute(r.gameObject, e, ExecuteEvents.pointerEnterHandler);   // 手动补发
    // 也可以 ExecuteHierarchy 让父物体兜住
}
```

### 四步排查法

1. 用 [[UGUI 事件系统与射线检测链路]] 里的探针代码打印 `RaycastAll` 结果 → **先确认「谁收走了事件」**（十有八九是 ghost）；
2. Profiler / Frame Debugger 看是不是 `OnTrigger` 压根没产生 → 指向 R2/R3/R4；
3. 单独拖一个空物体测物理配对（是否有一方带 Rigidbody、是否 Kinematic）；
4. 检查 Canvas 的 `Blocking Objects`、Layer 碰撞矩阵（`Physics` 设置里层与层的勾选）。

## 相关

- [[UGUI 事件系统与射线检测链路]] — 完整链路、`OnDrop` 时序、`RaycastAll` 探针
- [[UGUI事件接口与EventTrigger]] — 接口 vs EventTrigger、`OnPointerMove` 不生效原因
- [[Physics Raycast 与 NonAlloc]] — `RaycastNonAlloc` 零分配写法、LayerMask 过滤
- [[UGUI Canvas 渲染模式与屏幕适配]] — `pressEventCamera` 在三种模式下的差异
- [[DOTween与协程选型]] — 回弹动画（`DOAnchorPos`）
- [[UI-逻辑-数据分层与事件驱动]] — 拖放结果如何回灌数据层
