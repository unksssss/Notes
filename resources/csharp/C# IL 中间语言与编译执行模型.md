---
title: "C# IL 中间语言与编译执行模型"
type: resource
tags: [csharp, il, cil, 编译, 运行时, 面试]
created: "2026-09-08"
updated: "2026-09-08"
status: active
summary: "IL（CIL/MSIL）= .NET 通用中间语言：所有 .NET 语言编译成同一 IL 再交给 JIT/AOT；两段论（csc 一次编译产物与平台无关）；栈机模型（ldarg 压栈/add 弹栈运算/ret 返回）；元数据支撑反射；box=newobj 等指令与装箱/闭包隐藏类/协程状态机在 IL 层可见；Unity 四条消费路线 Mono/IL2CPP/CoreCLR/HybridCLR"
related:
  - "[[IL2CPP 编译原理与陷阱]]"
  - "[[热更新 HybridCLR — AOT 泛型元数据补全]]"
  - "[[C# 闭包与委托 — 隐藏类与 GC 陷阱]]"
  - "[[struct 装箱陷阱与值类型原理]]"
  - "[[协程原理与unitask]]"
---

# C# IL 中间语言与编译执行模型

## 一句话定义

**IL（Intermediate Language）**：C# 编译产物不是机器码，而是"半成品字节码"。官方名 **CIL（Common Intermediate Language）**，历史名 MSIL。所有 .NET 语言（C#/VB/F#）编译成同一种 IL，再由运行时/编译器在执行前翻译成机器码。

## 两段论模型

```
C# 源码 →[csc / Roslyn 编译（与平台无关）]→ IL 程序集（.dll/.exe）
         IL 程序集 →[运行期 JIT 边跑边译]→ 机器码（Mono / CoreCLR）
         IL 程序集 →[打包期 AOT 全量翻译]→ 机器码（IL2CPP: IL → C++ → 原生）
```

- 编译一次、产物全球统一，适配平台推迟到第二阶段；
- 程序集 = IL 指令 + **元数据**（类型/方法/字段表，反射的基础）+ 资源清单。

## 为什么存在 IL

1. **跨语言**：C#/VB/F# 互通互调（同一套 IL）；
2. **跨平台**：一份 .dll 到哪个平台由运行时/AOT 负责翻译；
3. **元数据自描述**：反射（typeof/GetMethod）靠类型清单；
4. **延迟决策**：JIT vs AOT 可到最后一刻定（CoreCLR 迁移、HybridCLR 热更的根基）。

## 栈机模型

IL 不碰寄存器，一切通过**操作数栈（evaluation stack）**：

```csharp
public static int Add(int a, int b) => a + b;
```

```cil
.method public hidevirt static int32 Add(int32 a, int32 b) cil managed
{
  ldarg.0   // a 压栈
  ldarg.1   // b 压栈
  add       // 弹出 b、a → 压入 a+b
  ret       // 弹出结果返回
}
```

## 常见指令 ↔ 已学知识点对照

| 指令 | 含义 | 关联知识点 |
|---|---|---|
| ldarg/ldloc/stloc | 压参/局部变量、存回槽 | 普通变量 |
| **box** | 值类型装箱 | 📎 struct 装箱（GC Alloc 源头） |
| newobj | 堆上 new 对象 | GC 分配 |
| call/callvirt | 方法调用 | 虚方法走 callvirt |
| brfalse/br | 跳转 | **协程状态机** = yield 翻译成跳转标签 |
| 隐藏类 + ldfld | lambda 捕获 | 📎 闭包 c__DisplayClass 即 IL 产物 |

## Unity 四条消费路线（2026）

- **Mono（JIT）**：边跑边译 → 编辑器/部分平台；
- **IL2CPP（AOT）**：IL → C++ → 机器码；编译期决策导致**泛型裁剪/反射受限**（需 link.xml/[Preserve]）；
- **CoreCLR**：Unity 6.5~6.8 迁移中的官方 .NET 运行时（见 IL2CPP 篇四阶段表）；
- **HybridCLR（热更）**：IL2CPP 设备上补 IL 解释器/元数据 → **热更本质是在 IL 层做文章**。

## 记忆锚点

> IL = .NET 世界的"通用交换格式"（类比 .obj/.wav）：所有语言先翻译成它，所有平台从它出发。
> "能看 IL 的程序员"能直接看出 GC Alloc/装箱/闭包泄漏——面经考 IL 层面的原因。
