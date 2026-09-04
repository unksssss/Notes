---
title: "UGUI 事件接口与 EventTrigger"
type: resource
tags: [unity, ui, 项目实战]
created: "2026-08-03"
updated: "2026-08-06"
status: active
summary: "UGUI 事件两种写法（接口 vs EventTrigger）、IPointerMoveHandler 不生效的四大原因、验证技巧；C# event vs UnityEvent（序列化与 Inspector 配置、性能差异）"
---

# UGUI 事件接口与 EventTrigger

## 两种写法

**方式一：实现事件接口**
```csharp
public class HoverDetector : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler
{
    public void OnPointerEnter(PointerEventData e) { /* 进入 */ }
    public void OnPointerMove(PointerEventData e) { /* 移动 */ }
}
```
事件系统通过接口检测，把射线命中的物体上的对应回调发过去。

**方式二：EventTrigger 组件**
不用写接口，Inspector 可视化添加 `Pointer Enter`、`Pointer Move` 等条目，拖回调函数绑定，适合简单场景/策划配表。

## ⚠️ IPointerMoveHandler 不生效的四大原因

按出现频率排：

1. **物体上没有可被射线命中的 Graphic** —— 事件射线（GraphicRaycaster）只会命中**勾选了 `Raycast Target`** 的 Image/Text。只挂脚本没 Image，或 Image 的 Raycast Target 被取消 → 事件穿透，回调永不触发！🕳️
2. **Canvas 上没有 GraphicRaycaster** —— 整个画布的 UI 射线交互全靠它，缺了全废
3. **被其他 UI 遮挡** —— 事件命中"最上层"物体，下层收不到（除非上层关闭 Raycast Target）
4. **物体被 Disable / 不在 active 的 Canvas 下**

> 冷门坑：Image 有 Raycast Target，但**父物体 Image 挡住**了它（子物体渲染在后）。UI 命中遵循"从上到下"的层级顺序，不只是视觉遮挡关系！

## ⚠️ C# event vs UnityEvent（Day 17 补充）

**C# `event`（如 `public event Action OnXxx;`）**：
- 本质是**被封装起来的委托字段**：只向外部暴露 `+=` / `-=` 订阅接口
- **Invoke 只能在声明它的类内部**触发，外部不能调用、不能用 `=` 覆盖清空订阅列表
- 委托是**多播**的，一个 event 可挂无数订阅者
- **不参与 Unity 序列化** → Inspector 看不到、拖不了配置

**UnityEvent**（Button.onClick 就是它）：
- 可序列化 → **Inspector 直接拖拽配置回调**，存进 prefab/场景
- 内部用 **PersistentCall（持久化调用列表）** 记录「目标对象 + 方法名 + 参数」序列化保存
- ⚠️ **性能比原生 event 慢**（内部反射查找 + 调用开销，有 GC 分配）→ 低频 UI 事件（按钮点击）用它没问题；**Update 里每帧触发的事件别用 UnityEvent**，用 C# event / Action

| | C# event | UnityEvent |
|---|---|---|
| Inspector 配置 | ❌ 不行 | ✅ 拖拽配置 |
| Invoke 性能 | 快 | 慢（反射） |
| 序列化 | ❌ | ✅ |
| 适用 | 代码内订阅、高频 | UI 拖配置、低频 |

## 验证技巧

1. `EventSystem.current.IsPointerOverGameObject()` 或回调里 Debug.Log，看事件到底发给谁
2. Editor 选中 EventSystem，运行时看 Inspector 的 `currentInputModule` 指针悬停物体
3. 查目标物体 Inspector 的 **Raycast Target** 勾选状态

## 相关

- [[XR射线交互与World Space Canvas]]
- [[ToggleGroup底层机制]]
