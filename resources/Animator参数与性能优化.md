---
title: "Animator 参数类型与性能优化"
type: resource
tags: [unity, 动画, 性能, 项目实战]
created: "2026-08-03"
updated: "2026-08-03"
status: active
summary: "Animator 参数四类型、Trigger vs Bool 区别、字符串参数哈希查找的性能坑、StringToHash 预计算 int 优化、HasParameter 防静默失效"
---

# Animator 参数类型与性能优化

## 参数四种类型

| 类型 | 用途 | 特点 |
|---|---|---|
| **Float** | 速度、混合权重等连续值 | 可平滑过渡 |
| **Int** | 离散状态（ID、计数） | 整数值 |
| **Bool** | 开关状态 | 布尔值 |
| **Trigger** | 一次性触发（出拳、闪避） | **自动重置**，不用手动清 |

> **Trigger vs Bool 的灵魂区别**：Trigger 触发一次后**自动恢复 false**（动画状态机消费掉），Bool 必须代码手动改——用错会导致"只触发一次"或"卡在触发态"的诡异 bug！

## ⚠️ 性能坑：字符串参数名 = 每帧哈希查找

```csharp
// ❌ 错误示范（每帧调用）
animator.SetFloat("moveSpeed", speed);   // 内部字符串 → 哈希 → 查表
animator.SetBool("isRunning", running);

// ✅ 正确做法：预计算参数 ID（int）
private static readonly int MoveSpeed = Animator.StringToHash("moveSpeed");
private static readonly int IsRunning = Animator.StringToHash("isRunning");

animator.SetFloat(MoveSpeed, speed);     // int 直接定位，零查找
animator.SetBool(IsRunning, running);
```

**原理**：Animator 参数底层用哈希表/数组存储，传字符串每帧要做哈希计算 + 查表；传 int 参数 ID 直接索引。

**关于 GC 的精确说法**：
- 字符串**字面量**（写死在代码里）会被 C# intern 驻留，**不会每帧分配**
- 真正的每帧开销是**重复的哈希计算 + 哈希表查找**
- 但若参数名是**动态拼接**的（如 `"speed" + index`），会产生字符串分配 → 实打实的 GC 压力

## 两个隐藏小坑

1. **参数改名后代码静默失效**：状态机里参数名改过，代码字符串写错**编译不报错、运行时静默失效** → 用 `HasParameter()` 检查或 int 常量集中管理可提前发现
2. **参数别滥用**：参数数量多了，Animator 内存和同步开销都会涨

## 面试一句话版本

> 参数名写进 `static readonly int` 常量，永远不要每帧传字符串。

## 相关

- [[协程原理与unitask]]
- [[MonoBehaviour生命周期与SetActive的坑]]
