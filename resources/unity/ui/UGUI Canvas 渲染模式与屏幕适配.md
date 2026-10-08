---
title: "UGUI Canvas 渲染模式与屏幕适配"
type: resource
tags: [unity, UGUI, Canvas, 屏幕适配, unity面试]
created: "2026-10-08"
updated: "2026-10-08"
status: active
summary: "Canvas 三种渲染模式（Overlay / Screen Space-Camera / World Space）的渲染时机与遮挡差异、Canvas Scaler 三种缩放模式与 Match 公式、刘海屏 SafeArea 适配完整代码与坑"
source: "Day 40 面试八问（Q1 / Q4 / Q5）"
related: ["UGUI事件接口与EventTrigger", "UGUI DrawCall 与性能优化", "UGUI多级UI性能优化与Canvas重建"]
---

# UGUI Canvas 渲染模式与屏幕适配

> 回答三个高频面试问：① Canvas 的渲染模式有几种？② Canvas Scaler 的缩放模式？③ 刘海屏怎么处理？

## 一、Canvas 的三种 Render Mode

| | Screen Space - Overlay | Screen Space - Camera | World Space |
|---|---|---|---|
| 渲染时机 | 所有相机渲染完后，**最后叠在最上层** | 在指定相机渲染流程中，距相机 `Plane Distance` 处的平面上 | 和普通 3D 物体一样，参与场景渲染 |
| 依赖相机 | ❌ 不需要（Event Camera 留空） | ✅ 必须指定 Render Camera | ✅ 需要 Event Camera 才能交互 |
| 能否被 3D 遮挡 | ❌ **永远最前**，不可能被遮挡 | ✅ 可以（在 Canvas 与相机之间放 3D 物体即可） | ✅ 完全按深度关系 |
| 尺寸驱动 | 自动 = 屏幕分辨率（像素） | 相机视锥在 `Plane Distance` 处的截面 | 手动控制 RectTransform 的 position/rotation/**scale** |
| 受相机 FOV / 透视影响 | ❌ | ✅ | ✅ |
| 受后处理影响 | ❌（在 URP 后处理之后叠加） | ✅ | ✅ |

**选型经验**

- **Overlay**：绝大多数 2D / 手游 UI。省事、不受相机影响，但**无法被 3D 物体遮挡**，也不吃后处理效果。
- **Screen Space - Camera**：需要「UI 被场景物体切开/遮挡」的表现（如角色走到 UI 前），或需要 UI 参与后处理（泛光/景深影响 UI）。
- **World Space**：UI 是场景的一部分——世界空间血条、悬浮名牌、交互面板。此时 `RectTransform.sizeDelta` **就是世界单位**，通常用 `scale` 缩放到合适大小（惯例：1 unit ≈ 100 px，即 `localScale = 0.01` 时尺寸刚好按像素理解）。

**排序规则**

- Overlay 与 Camera 模式Canvas 之间靠 `Sorting Layer` + `Order in Layer` 排序；
- Camera 模式还受**相机的 depth** 影响（相机 depth 小的先渲染）；
- 同一个 Canvas 内部靠 Hierarchy 顺序（后渲染的在上）。

**⚠️ 两个易错点**

1. **`Plane Distance` 必须大于相机近裁面**，否则 UI 被裁掉（画面直接消失，不是变模糊）。
2. **Overlay 的 `Event Camera` 必须是 null**；Camera / World Space 模式不设 Event Camera → **UI 完全点不动**（没有 Raycaster 能提供相机）。这是"换渲染模式后 UI 失效"的第一大原因。

```cs
// 运行时切换模式时，别忘了一起改 Event Camera
var canvas = GetComponent<Canvas>();
canvas.renderMode = RenderMode.ScreenSpaceCamera;
canvas.worldCamera = Camera.main;                       // ★ 交互必需
canvas.planeDistance = 10f;                             // 且要 > 相机 nearClipPlane
```

## 二、Canvas Scaler 的三种 UI Scale Mode

### 1. Constant Pixel Size（恒定像素）

UI 元素**像素尺寸固定**，不做任何缩放。`Scale Factor` 手动指定。
→ 后果：高分屏上 UI 显得小、低分屏上显得大。**只适合固定分辨率的 PC 工具类界面**。

### 2. Scale With Screen Size（随屏幕大小缩放）★ 最常用

按 `Reference Resolution` 等比缩放，让 UI 在各分辨率下**占屏幕比例稳定**。

核心公式（Unity 源码 `CanvasScaler.HandleScaleWithScreenSize`）：

```cs
float logWidth  = Mathf.Log(screenSize.x / referenceResolution.x, 2f);
float logHeight = Mathf.Log(screenSize.y / referenceResolution.y, 2f);
float logWeightedAverage = Mathf.Lerp(logWidth, logHeight, matchWidthOrHeight);   // ★ Match 就是这里
scaleFactor = Mathf.Pow(2f, logWeightedAverage);
```

- `Match Width Or Height = 0` → **只保证宽度**可见（`scaleFactor = 屏宽/参考宽`）
- `Match Width Or Height = 1` → **只保证高度**可见
- `Match = 0.5` → 宽度高度各占一半权重（**大多数项目的默认值**）

**Match 怎么选**：

| 场景 | 建议 |
|---|---|
| 竖屏手游、UI 横向内容多（需保证宽度完整可见） | `0`（Match Width） |
| 横屏 / PC、UI 纵向分层（顶栏 + 内容 + 底栏） | `1`（Match Height） |
| 不确定 / 通用 | `0.5` |

> 🔑 **为什么 Match 的选择很重要**：极端宽高比（如 21:9 或 4:3）下，未匹配的那一维会**多出或裁掉**空间 → 用 anchor + 布局去适配对应的边缘，或者干脆给个宽容的布局。

`Screen Match Mode` 的另外两个选项（只在非 Match 场景用）：
- **Expand**：始终扩大 Canvas，保证参考分辨率**完全可见**（可能多出留白）；
- **Shrink**：始终缩小 Canvas，保证内容**完全装进屏幕**（可能出现裁切内容）。

### 3. Constant Physical Size（恒定物理尺寸）

按 DPI 换算，保证 UI 在**不同设备上物理尺寸（英寸）一致**。
主要给需要「真实尺寸」的场景用（AR 量尺、医疗/工业标定界面）。日常 UI 基本不用。

### 常用配置

```
UI Scale Mode           = Scale With Screen Size
Reference Resolution    = 1920 x 1080
Screen Match Mode       = Match Width Or Height
Match                   = 0.5
Reference Pixels Per Unit = 100
```

```cs
// 运行时读缩放系数：屏幕像素 → Canvas 局部坐标的换算就靠它
float sf = canvas.scaleFactor;      // 由 CanvasScaler 维护

// 屏幕点 → 某 RectTransform 的局部坐标（拖拽/定位必备，见 [[UGUI 拖拽实现与拖放检测]]）
RectTransformUtility.ScreenPointToLocalPointInRectangle(
    rect, screenPos, eventCamera: null /* Overlay 传 null */, out var localPos);
```

## 三、刘海屏 / 挖孔屏 / 圆角屏适配

**根因**：屏幕四角是圆角、顶部有刘海/挖孔、底部有 Home Indicator → 安全区（Safe Area）以外的内容会被**物理遮挡**，且这些区域内的触摸也会被系统拦截。

**方案**：读 `Screen.safeArea`（返回 `Rect`，**像素单位、左下角为原点**），把它转换成归一化 anchor 赋给内容根节点。

```cs
using UnityEngine;

/// <summary>
/// 挂在 UI 内容根节点上（全屏拉伸、anchor 0~1），自动收缩到屏幕安全区。
/// 需要 Canvas Scaler = Scale With Screen Size 才能保证归一化 anchor 语义正确。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform _rt;
    private Rect _lastSafeArea;
    private ScreenOrientation _lastOrientation;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        Apply();
    }

    private void Update()
    {
        // 刘海屏在横竖屏切换后 safeArea 会变，必须持续检测（比较 Rect 是值类型比较，无 GC）
        if (Screen.safeArea != _lastSafeArea || Screen.orientation != _lastOrientation)
            Apply();
    }

    private void Apply()
    {
        _lastSafeArea    = Screen.safeArea;
        _lastOrientation = Screen.orientation;

        Vector2 min = _lastSafeArea.position;
        Vector2 max = _lastSafeArea.position + _lastSafeArea.size;

        // 像素 → 归一化（除以屏幕尺寸）
        min.x /= Screen.width;   min.y /= Screen.height;
        max.x /= Screen.width;   max.y /= Screen.height;

        _rt.anchorMin = min;
        _rt.anchorMax = max;

        // anchor 改了要清 offset，否则残留的 left/right/top/bottom 会把节点推歪
        _rt.offsetMin = Vector2.zero;
        _rt.offsetMax = Vector2.zero;
    }
}
```

**踩坑清单**

1. **背景层与内容层要分开**：背景（纯色/大图）**不加** SafeArea，保持全屏铺满（否则刘海两边露黑边）；只给**内容层**挂 SafeAreaFitter。
2. **Editor 里 `Screen.safeArea` 永远等于全屏**，看不出效果 → 用 **Device Simulator**（Package Manager 装 `Device Simulator`）或 Game View 选带刘海的机型预览。
3. **横竖屏切换 / 分屏 / 折叠屏**都会改变 safeArea → 必须用 `Update` 或 `OnRectTransformDimensionsChange` 监听，只算一次是常见 bug。
4. `Screen.safeArea` 是**像素值**，别直接赋给 `sizeDelta`（那会得到一个巨大的方块）—— 一定要除以 `Screen.width/height` 归一化后再给 anchor。
5. 顶部状态栏 / 底部 Home 条除了 safeArea，还可以配合 `Screen.cutouts`（挖孔区域数组）做更精细的避让，但绝大多数项目 safeArea 就够了。

## 相关

- [[UGUI事件接口与EventTrigger]] — 事件接口与 Raycast Target（渲染模式改了 Event Camera 没设 → 事件全失效）
- [[UGUI 拖拽实现与拖放检测]] — `pressEventCamera` 在三种模式下的差异
- [[UGUI DrawCall 与性能优化]] — Overlay 与 Camera 模式的渲染开销差异
- [[UGUI多级UI性能优化与Canvas重建]] — 结构层优化（动静分离拆 Canvas）
- [[XR射线交互与World Space Canvas]] — World Space Canvas 的射线交互三件套（历史项目，XR 语境）
