---
title: "AssetBundle 生命周期与卸载语义"
type: resource
tags: [unity, AssetBundle, 资源管理, 内存, 真实面经]
created: "2026-09-07"
updated: "2026-09-07"
status: active
summary: "AssetBundle.Unload(false) 只卸包头/元数据、保留已加载资源实例；Unload(true) 连资源实例全销毁（场景在用 → 粉红 Missing）；依赖加载先父后子、卸载先子后父；Addressables 引用计数 = 这套底层的人肉记账自动化"
related:
  - "[[Addressables资源生命周期]]"
  - "[[Resources.Load 加载与 as 转型]]"
---

# AssetBundle 生命周期与卸载语义

来源：CSDN 面经《Unity 面试官视角：10 大高频考点》——AB 卸载两档位是资源管理方向必问。

## 两个卸载档位

| API | 卸载什么 | 已 LoadAsset 的实例 | 适用场景 |
|---|---|---|---|
| `Unload(false)` | AB 包头/**清单元数据**（目录） | **保留**，照常能用 | 场景还在用资源时 |
| `Unload(true)` | 包头 + **内存中的资源实例一并销毁** | 全灭 → Missing/粉红 | 整块内容确定不用（切场景/卸模块） |

**铁律**："false 保实例、true 全销毁；**用着别 true**"。场景正在显示某 AB 的资源却 Unload(true)（哪怕无依赖问题）= 粉红头号来源。

## 依赖陷阱（粉红材质排查方向）

- **加载顺序**：Bundle B 引用 Bundle A 的材质 → 必须**先 Load(A) 再 Load(B)**，B 才能拿到正确引用；
- **卸载顺序**：**先卸子（B）再卸父（A）**——先 Unload(A) 则 B 的资源还在、引用的 A 资源却没了 → 粉红；
- **使用期误卸**：无依赖问题，但正在用的 AB 被 true 卸载 → 粉红。

口诀："**加载父先、卸载子先、用着别 true**"。

## 与 Addressables 的关系

Addressables 的引用计数（Load/Release 成对）本质就是**包装这套 AB 卸载逻辑**：Release 让计数 -1，**计数归零才真正触发底层 AB 卸载**——省掉手动 AB 时代"谁该卸、什么时候卸"的人肉记账。但理解底层 Unload(false/true) 语义仍是排查 Addressables 泄漏/粉红问题的基础。

## 面试答题框架

① 先讲两档位语义与"为什么 false 后资源还能用"（实例还活着）→ ② 展开依赖：加载先父后子、卸载先子后父 → ③ 粉红材质排查三方向（依赖顺序 / 使用期误卸 / 依赖缺失加载）→ ④ 可衔接 Addressables 引用计数（加分）。
