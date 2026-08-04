---
title: "DOTween 与协程的选型"
type: resource
tags: [unity, 项目实战, 动画, 面试]
created: "2026-08-04"
updated: "2026-08-04"
status: active
summary: "协程（状态机/流程控制）vs DOTween（时间轴补间/可中断可控），选择口诀：'动'用 DOTween、'流程'用协程，可混用"
---

# DOTween 与协程的选型

## 本质区别

| 维度 | 协程（IEnumerator + yield） | DOTween |
|------|------|------|
| 本质 | **状态机**，`yield` 把函数拆段，每帧恢复 | **时间轴补间系统**，Tween 对象自驱动 |
| 时间缩放 | 受 `Time.timeScale = 0` 影响（`WaitForSeconds` 卡住，除非 `WaitForSecondsRealtime`） | 可按 `SetUpdate(UpdateType.UnscaledTime)` 忽略 |
| 控制能力 | **不能单独暂停/恢复/倒放**一个协程 | `.Pause()/.Play()/.PlayBackwards()/.TogglePause()` 全支持 |
| 缓动曲线 | 自己写插值公式，丑 | 现成 `SetEase(Ease.OutBack)` 一大把 |
| 流程编排 | 线性直白，适合"先 A，等 2s，再 B" | `.Append().Join()` 可以排队但不如协程直观 |

## 选择口诀

> **"动"用 DOTween（位移/缩放/颜色/数值的补间），"流程"用协程（时序控制、等待事件）**

## 典型场景：VR 开门反向

需求：按 Trigger 门缓动旋转 90°，**动画过程中再按可以反向关回去**。

→ **必须 DOTween**：Tween 引用存下来，再按 `PlayBackwards()` 一行搞定。
→ 协程做这个要自己记录当前旋转进度、写反向插值、处理反转瞬间的角度边界，非常痛苦。

**核心考点**：选中 DOTween 的理由**首答"可中断/可控/反向"**，而不是只说"代码简洁、性能好"。

## 混用技巧

- 协程里 `yield return tween.WaitForCompletion()` 等补间完成
- DOTween 的 `.OnComplete()` 回调里启动协程
- 注意：`transform.DOKill()` 清理残留 Tween，防止目标被"幽灵 Tween"锁定

## 相关

- [[协程原理与unitask]]
- [[dots详解]]
