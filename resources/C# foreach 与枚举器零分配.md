---
title: "C# foreach 与枚举器零分配"
type: resource
tags: [csharp, 性能, GC, 面试]
created: "2026-08-12"
updated: "2026-08-12"
status: active
summary: "foreach 的 GC 真相：数组 for 展开零分配、List<T> struct 枚举器不走接口零分配、接口接收枚举器即装箱、yield 状态机分配"
---

# C# foreach 与枚举器零分配

（Day 20 知识点）

## 编译器展开的本质

`foreach` 是语法糖，被展开成固定模式：拿枚举器 → `while(MoveNext())` → 取 `Current`。**"拿枚举器"的方式**决定了会不会产生堆分配。

## 三种情况对照表

| 遍历对象 | 编译器行为 | 堆分配 |
|---|---|---|
| `int[]` 数组 | 直接展开成 **for 索引访问**，根本没有枚举器 | ❌ 零分配 |
| `List<T>`（泛型） | **模式匹配**直接调用 `List<T>.Enumerator`（struct）的方法，全程栈上 | ❌ 零分配 |
| 非泛型 `ArrayList` / `Hashtable` | 元素是 `object` 引用，值类型在放入时**早已装箱** | ✅ 大量分配 |

## 装箱元凶：接口引用

`List<T>.Enumerator` 虽然是 struct，但如果**显式赋给接口类型**就会装箱：

```csharp
foreach (var x in list) { }          // ✅ 零分配（struct 枚举器 + 编译器不走接口）
IEnumerator<int> e = list.GetEnumerator();  // ❌ struct 装箱成接口引用 → 堆分配！
```

普通 `foreach` 的模式匹配是**直接按具体类型调用**，不经过 `IEnumerator` 接口，所以不装箱。

## 真正会分配的场景（实战高频坑）

1. 显式用 `IEnumerator` / `IEnumerator<T>` 接口接收枚举器 → 装箱
2. 遍历非泛型集合（`ArrayList` 里的值类型早就装箱了）
3. 遍历**自定义迭代器方法**（含 `yield return` 的函数）→ 编译器生成**状态机类**，每调用一次就 `new` 一个对象
4. 历史问题：旧 Unity（旧 Mono）的数组 foreach 有额外开销，新版已无

## 记忆口诀

> **struct 枚举器 + 编译器不走接口 = 零分配**

Update 里每帧 `foreach (var item in list)` 遍历 `List<T>` 本身零分配，放心用；别手动碰接口类型枚举器就行。

## 相关

- [[csharp/struct 装箱陷阱与值类型原理]]
- [[Boehm GC 保守式垃圾回收原理]]
- [[TMP Text 零分配更新]]
