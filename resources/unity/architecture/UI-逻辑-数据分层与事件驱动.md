---
title: "UI-逻辑-数据分层与事件驱动"
type: resource
tags: [架构, 设计模式, unity, 面试]
created: "2026-09-08"
updated: "2026-11-03"
status: active
summary: "UI/逻辑/数据三层职责划分；状态变更走事件、UI 订阅刷新（TaskStateManager 实战范式）；对比脚本直接互相调用；优点（高内聚低耦合/谁改状态不知谁在听/纯逻辑可单测）+ 缺点（事件流难追踪/简单面板过度设计）；实战两个静默失效点：out 标志位在循环里被覆盖导致事件不发、订阅不退订（真实面经：零跑『了解 MVC 吗』）"
---

# UI-逻辑-数据分层与事件驱动

> 来源：牛客零跑 U3D 面经（"了解 MVC 吗？"）+ CSDN 中高级面经（"你项目里用什么架构？为什么选它？"）——Day 37 面经，灯皇以 TaskStateManager 实战作答

## 三层职责划分（实战版 MVC）

| 层 | 职责 | 注意 |
|---|---|---|
| **UI 层** | 只管开关面板、状态显示、接收输入 | 不做业务判断 |
| **逻辑层** | 判断规则、驱动状态变更、操作 UI | 状态变更**走事件**，不直接 new UI |
| **数据层** | 保存 UI 需要显示的数据（常与逻辑层合并） | 数据是"唯一真相源" |

## 事件驱动核心机制（TaskStateManager 范式）

- **状态变更方不直接调用 UI**，只发事件/改状态
- **UI 订阅事件刷新**：谁改状态、不用知道谁在听（发布-订阅解耦）
- 纯逻辑类与表现分离 → 逻辑层**可脱离 Unity 对象单测**

```csharp
// 逻辑层：状态变更只发事件
stateManager.SetState(...);   // 内部触发 event

// UI 层：订阅刷新
taskPanel.OnStateChanged += RefreshUI;
```

## 为什么选它（对比"脚本互相直接调用"）

1. **高内聚、低耦合**：每个脚本职责单一，三者不互相 new
2. **解耦**：状态变更方不知道监听者存在 → 加新 UI 不改逻辑代码
3. **可测试性**：纯逻辑类不依赖引擎对象可单测
4. **状态唯一来源**：数据层是真相源，不会出现"各处各存一份状态互相打架"

## 缺点与边界（面试加分：能说缺点才是真懂）

1. **事件流难追踪**：断点地狱——不知道谁触发了状态、谁在响应（隐式控制流）
2. **过度设计风险**：简单面板强上三层 + 事件总线 = 杀鸡用牛刀（YAGNI）
3. **数据/逻辑合层后**职责边界要靠自律，容易悄悄长胖

## 实战：事件「不发」的两个静默失效点

分层本身不难，难的是**事件该发的时候没发**——不报错、不崩，只是 UI 不刷新。两个真实案例（`Empty` 武器拾取系统，2026-11-03）。

### 坑 1：`out` 标志位在循环里被互相覆盖 ★最隐蔽

需求：玩家周围 2 米内的武器列表变化时，发一次事件通知 UI。

```cs
// ✗ 错
bool isAddChanged = false;
for (int i = 0; i < AllWeaponItems.Count; i++)
{
    if (Vector3.Distance(...) < 2f)
        AddAreaWeaponItems(AllWeaponItems[i], out isAddChanged);   // 每轮覆盖
}
if (isAddChanged) OnAreaWeaponChanged?.Invoke(AreaWeaponItems);
```

```cs
private void AddAreaWeaponItems(WeaponItem item, out bool isChanged)
{
    if (!AreaWeaponItems.Contains(item)) { isChanged = true; AreaWeaponItems.Add(item); return; }
    isChanged = false;
}
```

**根因**：`out` 的语义是「每次调用都必须写一个值」，不是「只在变化时赋值」。循环 N 次，最终值 = **最后一次调用**的结果。

- 第 1 把枪进范围（`true`）→ 第 2 把本来就在列表里（`false`）→ 最终 `false` → 事件不发
- 只有「遍历顺序里最后一把恰好是新进入的」才碰巧正确 → **时序相关 bug**，本地测不出来，线上偶发

**改法**：

```cs
// ✓ 返回 bool，调用方聚合
private bool AddAreaWeaponItems(WeaponItem item)
{
    if (AreaWeaponItems.Contains(item)) return false;
    AreaWeaponItems.Add(item);
    return true;
}

isAddChanged |= AddAreaWeaponItems(AllAreaWeaponItems[i]);
```

或者干脆**去掉标志位**，变化了就直接发（列表量级小的时候，多发几次比藏 bug 划算）。

> 通用规律：**「有没有变化」是聚合量，不能用 `out` 在循环里传递。** 同样的形状会出现在伤害飘字、buff 列表、任务追踪里。

### 坑 2：订阅了不退订

```cs
private void Start() => FightingManager.Instance.OnAreaWeaponChanged += OnNewChange;
// 没有 OnDestroy / OnDisable 里的 -=
```

场景重载后 `Instance` 换人、旧 UI 对象已被销毁，但委托链还挂着 → 下次事件触发时访问已销毁的 `Transform`，`MissingReferenceException`；同时旧对象被静态事件「钉住」，GC 收不掉。

**规矩**：`+=` 和 `-=` 必须成对出现在同一屏代码里，写订阅的同时就把退订写上。更稳的做法是 📎 [[事件总线实现 — 订阅令牌与发布期快照]] 里的令牌（`IDisposable`）模式，让生命周期自己管。

---

## 经验法则

> **复杂度与"谁在看状态"的数量成正比**：单面板直接调用即可；多 UI 订阅同一状态源 → 事件驱动；项目小别硬上分层架构。
