---
title: "MonoBehaviour 生命周期与 SetActive 的坑"
type: resource
tags: [unity, 生命周期, 面试]
created: "2026-07-31"
updated: "2026-10-17"
status: active
summary: "Awake/OnEnable/Start 生命周期；**执行顺序是分阶段的**（所有 Awake → 所有 OnEnable → 所有 Start，这是 Start 的依赖保证）；**active 与 enabled 两个开关的分界**（Awake 只看 active，enabled 从 OnEnable 起才生效）；三种激活情况实测对照（enabled=false / 初始 inactive / 再次激活）；Instantiate 同步执行 Awake；对象池重置为何必须放 OnEnable；OnDisable vs OnDestroy 触发时机对比"
---

# MonoBehaviour 生命周期与 SetActive 的坑

## 标准生命周期

```
Awake（首次激活，仅一次）
  ↓
OnEnable（每次激活都执行）
  ↓
Start（激活后的第一帧 Update 前，仅一次）
  ↓
Update（每帧）
  ↓
OnDisable（每次禁用都执行）
  ↓
OnDestroy（销毁时执行）
```

## 执行顺序：分阶段，不是逐对象

场景加载时**不是**「A.Awake → A.Start → B.Awake → B.Start」，而是**先跑完所有对象的同一阶段**：

```
① 所有对象的 Awake
② 所有对象的 OnEnable
③ 所有对象的 Start
④ 第一帧 Update
```

→ 这就是 **Start 的依赖保证**：它执行时，**所有对象的 Awake 都已完成**。

据此分工：

- 只做自身初始化（缓存组件引用、初始化字段）→ **放 Awake**
- 要访问其他对象 / 单例 → **放 Start**（放 Awake 时对方可能还没初始化，拿到 null）

⚠️ **例外**：运行时 `Instantiate` 出来的对象，Awake 会**同步插入**在实例化那一刻——可能发生在其他对象的 Start 跑完之后。所以「所有 Awake 在 Start 之前」**只对同一批场景加载的对象成立**。

## 两个开关：active 与 enabled

「不执行」有两个完全不同的原因，混淆是这题最大的失分点：

| 开关 | 所属层级 | 关掉后 |
| --- | --- | --- |
| `gameObject.SetActive(false)` | **GameObject** | 整个对象"不存在"，所有组件都不工作 |
| `enabled = false` | **Component**（Behaviour） | 对象还在，只是这个组件不工作 |

### 分界线：Awake 只看 active，不看 enabled

```
Awake      → 只看 activeInHierarchy
──────────────────────────────────────  ← enabled 从这里开始生效
OnEnable   → activeInHierarchy && enabled
Start      → activeInHierarchy && enabled
Update     → activeInHierarchy && enabled
```

判断「组件此刻会不会跑」的标准公式：

```csharp
Behaviour.isActiveAndEnabled == gameObject.activeInHierarchy && enabled
```

**但 Awake 不受这个公式约束**——它只看 `activeInHierarchy`。这就是上面那条分界线的本质。

### 三种情况实测对照（2026-10-17 逐条验证）

| 情况 | Awake | OnEnable | Start |
| --- | --- | --- | --- |
| 正常激活 | ✅ | ✅ | ✅ |
| **`enabled = false`**（对象 active） | ✅ **照跑** | ❌ | ❌ |
| **初始 inactive**（对象未激活） | ❌ | ❌ | ❌ |
| 首次 `SetActive(true)` 后 | ✅ 补 | ✅ | ✅ 补 |
| **再次激活**（激活过 → false → true） | ❌ **不再** | ✅ | ❌ **不再** |

两条实测结论：

1. **`enabled = false`**：运行场景时 Awake 照常执行，OnEnable / Start 不执行；手动把 `enabled` 设为 true 后，**OnEnable 和 Start 会依次补上**——Start 不是"永远不执行"，而是**延后到启用那一刻**
2. **初始 inactive**：Awake 完全不执行；手动 `SetActive(true)` 后 **Awake → OnEnable → Start 依次执行**——Awake 是"补课"，不是"丢失"

⚠️ 注意：对象 inactive 时，`enabled` 是 true 还是 false **都无所谓**——第一道门（active）就关了，`enabled` 根本不参与判断。两个条件是 **AND**，但卡的**位置不同**。

### 首次激活 vs 再次激活

| 阶段 | Awake | OnEnable | Start |
| --- | --- | --- | --- |
| 首次激活 | ✅ | ✅ | ✅ |
| `SetActive(false)` | — | OnDisable | — |
| 再次激活 | ❌ | ✅ | ❌ |

**Awake 和 Start 整个生命周期只跑一次**，再次激活只补 OnEnable。

→ **这就是对象池必须把重置逻辑写在 `OnEnable` 的原因**（不是约定俗成，是被回调机制逼出来的）：

- 放 `OnEnable`：每次取出都重置 ✅
- 放 `Awake` / `Start`：只有第一次生效，第二次取出就是**脏数据** ❌

而这个坑特别隐蔽——**第一次测试完全正常，第二次才开始出错**，表现为"数据越来越乱"，不像崩溃那么显眼。

## ⚠️ 初始 inactive 的坑（面试高频）

- **`Awake` 的触发条件是"第一次激活"，不是"场景加载"！**（机制见上文「两个开关」与「首次激活 vs 再次激活」）
- **连环坑**：`Awake` 里缓存组件引用（`_rb = GetComponent<Rigidbody>()`），对象初始 inactive 时，激活前访问 `_rb` 就是 **null** → NRE！
- 实战教训：引用缓存尽量放 Awake；若对象可能初始 inactive，要么"使用前先激活"，要么把缓存逻辑挪到首次 OnEnable

## ⚠️ OnDisable vs OnDestroy（Day 17 失分点）

| 场景 | OnDisable | OnDestroy |
|---|---|---|
| `SetActive(false)` | ✅ 触发 | ❌ 不触发 |
| `Destroy(物体)` | ✅ 触发（**先**） | ✅ 触发（后） |
| 场景卸载 | ✅ 触发 | ✅ 触发 |
| 组件 `enabled = false` | ✅ 触发 | ❌ 不触发 |

- **销毁流程 = 组件先失活 → 再销毁**：`Destroy` 时 OnDisable 先执行，然后才 OnDestroy（常见误区："销毁时只有 OnDestroy" ❌）
- **记忆口诀**：OnDisable = "失活就喊"，OnDestroy = "销毁才喊"
- **实战**：取消订阅、释放引用等清理逻辑放 **OnDisable** 更稳——隐藏（SetActive(false)）和销毁（Destroy）都能兜住；只放 OnDestroy 的话，对象只是被隐藏时清理逻辑不会执行

## 相关细节

- 协程：GameObject 被 `SetActive(false)` 或销毁时，协程**会停止**；组件 `enabled = false` **不会**停止协程
- `Start` 在对象激活后的第一帧 Update 前执行；初始 inactive 则从激活后开始计时
- 对象池场景：状态重置必须写在 `OnEnable`（每次激活都触发）而不是 `Start`（只执行一次）——**机制见上文「首次激活 vs 再次激活」**
- **`Instantiate` 是同步执行 Awake 的**：`var go = Instantiate(prefab);` 这一行返回前，`go` 的 Awake 已经跑完了。所以「先 Instantiate 再传参」的写法，Awake 里永远读到默认值——**这是初始化顺序 bug 的典型来源**

## OnApplicationFocus vs OnApplicationPause（Day 19，VR/移动端切后台）

| 回调 | 触发时机 | 频率 |
|---|---|---|
| `OnApplicationPause(bool)` | 仅在**暂停状态改变**时（true=切后台，false=切回） | 低频，状态真正改变才触发 |
| `OnApplicationFocus(bool)` | **每次焦点变化**都触发（弹窗/来电/摘头显） | 高频，比 Pause 频繁 |

**调用顺序（口诀：切出 Focus 先、切回 Pause 先）**：
- 切后台：`OnApplicationFocus(false)` → `OnApplicationPause(true)`
- 切回前台：`OnApplicationPause(false)` → `OnApplicationFocus(true)`

**实战**：暂停音频/存档等逻辑放 `OnApplicationPause`（状态真正改变才调用一次，不会重复执行）；UI 焦点类响应放 `OnApplicationFocus`。

## 相关

- [[对象池实现]]
- [[对象池 OnEnable OnDisable 最佳实践]]
