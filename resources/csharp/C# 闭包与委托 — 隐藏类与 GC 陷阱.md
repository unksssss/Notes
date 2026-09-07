---
title: "C# 闭包与委托 — 隐藏类与 GC 陷阱"
type: resource
tags: [csharp, 闭包, 委托, GC, 隐藏类, 真实面经]
created: "2026-09-07"
updated: "2026-09-07"
status: active
summary: "闭包的本质：编译器为捕获外部变量的匿名方法生成隐藏类（c__DisplayClassX），捕获变量搬成字段、方法体变实例方法；堆对象存活期=委托引用存活期 → 存成静态/事件订阅会把捕获对象（含 this）拴到程序结束=泄漏；每帧 new 捕获型 lambda = 持续 GC Alloc"
related:
  - "[[Boehm GC 保守式垃圾回收原理]]"
  - "[[C# foreach 与枚举器零分配]]"
  - "[[UnityEngine.Object 判空与销毁机制]]"
---

# C# 闭包与委托 — 隐藏类与 GC 陷阱

来源：掘金面经解析《C# 委托、事件与闭包》——"闭包 + IL"是区分只会写业务 vs 能深入引擎的试金石。

## 闭包是什么

```csharp
void SpawnEnemies(float interval) {
    int count = 0;                        // 被 lambda 捕获的局部变量
    System.Action spawnLoop = delegate {
        count++;
        Debug.Log($"spawn {count}, interval={interval}");
    };
    someEvent += spawnLoop;               // 存成字段/事件订阅
}
```

匿名方法访问了外部局部变量（`count`、参数 `interval`）——方法执行完这些变量本该消失，却要"活"着给闭包用。

## 编译器怎么解：隐藏类

1. C# 编译器生成一个**隐藏类**（反编译里叫 `c__DisplayClass0_0` 之类，类名由"方法名+序号"构成）；
2. 被捕获的局部变量被**搬成这个类的字段**（捕获列表 = 所有被读写的外部局部变量 + this 引用）；
3. 匿名方法体变成该隐藏类的**实例方法**，原方法里 `new` 这个类、把变量拷进字段；
4. 从此变量活在**堆上的隐藏类对象**里，不再在栈上。

## 与 GC 的关系（核心考点）

- 隐藏类对象是**堆对象**，存活期 = 引用它的委托的存活期；
- **闭包泄漏**：存成**静态字段 / 事件订阅 / 长命类字段** → 捕获的整包变量（大数组、甚至 `this` 指向的 MonoBehaviour）跟着活到委托解绑/程序结束——经典泄漏：单例事件 `+=` 了捕获 this 的 lambda，场景卸载对象也回收不了；
- **每帧 GC Alloc**：Update/高频循环里每帧 new 捕获型 lambda → 每帧 new 隐藏类对象。**不捕获变量**的 lambda 会被编译器缓存成静态委托（零分配），**一捕获就躲不掉**。

## 面试答题框架

"lambda 访问外部变量时，编译器生成隐藏类把捕获变量搬成字段、方法变实例方法，形成堆上闭包对象；它的存活期跟随委托引用——存成静态/事件订阅时被捕获对象被长期引用 = 泄漏；高频 new 捕获型 lambda = 持续 GC Alloc。判断标准：捕获对象（尤其 this）被长命委托间接引用 = 泄漏。"

## 自查清单

- [ ] 能用 ILSpy/Rider IL Viewer 说出 ldfld/stfld 与隐藏类
- [ ] 知道捕获 int?/枚举可能触发意外装箱
- [ ] 反注册（-= / OnDestroy 解绑）能救事件订阅泄漏
