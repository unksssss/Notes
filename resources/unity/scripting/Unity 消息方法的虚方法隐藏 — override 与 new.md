---
title: "Unity 消息方法的虚方法隐藏 — override 与 new"
type: resource
tags: [unity, csharp, scripting, 继承, 生命周期]
category: scripting
created: "2026-11-25"
updated: "2026-11-25"
status: active
summary: "Unity 消息方法（Awake/OnEnable/Start/Update…）不参与 C# 的虚调用链，引擎是按**方法名**反射派发的：`private void Awake()` 会**隐藏**基类 `protected virtual Awake`，基类那段代码静默不执行（CS0114 警告常被忽略）。三个真实现场：`Player.Awake` 隐藏 `RoleBase.Awake`（`health` 兜底失效）、`Player.Update` 隐藏 `RoleBase.Update`（基类逻辑将来塞进去就死）、以及为什么不能用 `Awake` 做「子类先于基类」的初始化。正解统一是 `protected override` + `base.`，且注意 `Update` 这类每帧消息在 `override` 后**必须显式调用 `base.Update()`**，否则同样丢逻辑。"
source: "Empty 2026-11-25 14:5x / 15:19 RoleBase.cs + Player.cs"
related:
  - "[[Unity 类继承体系 Object-Component-Behaviour-MonoBehaviour]]"
  - "[[MonoBehaviour生命周期与SetActive的坑]]"
  - "[[UnityEngine.Object 判空与销毁机制]]"
  - "[[伤害系统设计 — 接口驱动与职责分层]]"
---

# Unity 消息方法的虚方法隐藏 — override 与 new

## 一、问题从哪来

`RoleBase` 里写了这么一段，想把「没有 Health 就补一个」做成基类统一兜底：

```cs
public class RoleBase : MonoBehaviour
{
    internal Health health;

    protected virtual void Awake()
    {
        if (health == null) health = gameObject.GetOrAddComponent<Health>();
    }
}
```

子类 `Player` 也写了 `Awake`：

```cs
public class Player : RoleBase
{
    private InputSystem_Actions input;

    private void Awake()          // ← 没有 override
    {
        input = new InputSystem_Actions();
        input.Player.Enable();
    }
}
```

结果：**`RoleBase.Awake` 一次都没执行**，`health` 全程为 null，直到别的地方补组件才「看起来正常」。

---

## 二、根因：Unity 消息不是虚调用

### 2.1 引擎怎么找消息方法

Unity 的 `Awake`/`OnEnable`/`Start`/`Update`/`OnDestroy` 这些**不是** `MonoBehaviour` 的虚方法，而是引擎在 `ScriptingRuntime` 里**按字符串名字**从类的方法表里查的：

```text
行为树上的每个 MonoBehaviour 实例
  → 取它的 C# 类型
  → 在该类型的方法表里按名字找 "Awake"
  → 找到【最派生的那一个】直接调用
```

所以派发规则不是 C# 的虚调用（沿 vtable 走），而是「**按名字取最派生实现，然后只调那一个**」。这带来两个后果：

1. **基类的同名方法不会被自动调用** —— 不是「子类覆盖了虚方法」，而是「子类的方法把基类的同签名方法遮住了」。
2. **不需要 `virtual`/`override` 也能「生效」** —— 所以 `private void Awake()` 在 Unity 里看起来「能用」，编译器只给一条 `CS0114` 警告（「隐藏了继承成员，若有意隐藏请用 new 关键字」），极易被当成噪音忽略。

### 2.2 隐藏（hiding）与覆盖（overriding）的区别

| | `override` | 隐藏（`new` 或不写） |
| --- | --- | --- |
| 派发方式 | 虚表，运行时按实际类型 | 编译期按**静态类型** |
| 基类方法 | 被子类**替换**，可通过 `base.` 调用 | 仍在，但**通过基类静态类型**才能调到 |
| 编译器 | 需要基类 `virtual`/`abstract` | 不要求基类 `virtual`；若基类有同名成员则报 `CS0114` 警告 |
| Unity 消息 | 正常（引擎调到最派生那个） | **基类那段永远不执行** |

关键差别就在最后一行：Unity 引擎永远只调「最派生的那一个」，所以只要子类方法**没有 `override`**，基类的消息方法就是死代码。

### 2.3 一个反直觉的细节

如果外部代码**显式以基类静态类型调用**，隐藏的方法还是能调到的：

```cs
RoleBase r = player;
r.Awake();     // 编译不过：Awake 是 protected；即便改成 public，这里调到的是 RoleBase.Awake
```

也就是说，隐藏并没有「删掉」基类方法，它只是**从 Unity 的派发路径上消失了**。这解释了为什么这类 bug 特别难查：IDE 里 `Go to Definition` 点 `Awake` 能跳到基类，代码看起来「有」，运行时就是不跑。

---

## 三、三个真实现场

### 现场 1：`Player.Awake` 隐藏 `RoleBase.Awake` → `health` 兜底失效

- **症状**：`RoleBase.OnEnable` 里 `health == null` 的判断永远成立（因为基类 `Awake` 没跑），玩家靠 `GetOrAddComponent` 在 `OnEnable` 里现场补。
- **为什么当时没炸**：`GetOrAddComponent` 恰好也在 `OnEnable` 里兜了一遍，等于「基类 Awake 想做的事，OnEnable 又做了一次」。
- **为什么不能用 Awake 做这件事**：即便改成 `override`，`Awake` 的时机是「**对象被实例化/激活时**」，而 `Player` 的 `Health` 需要 `FightingManager.Start` 里才知道 `maxHealth`（要读配表）。`Awake` 早于配表加载完成 → 补出来的组件也 `Init` 不了。**结论：不是 `override` 就能解决，得换时机**（`OnEnable` 兜底 + 配表就绪后 `Init`）。

### 现场 2：`Player.Update` 隐藏 `RoleBase.Update` → 现在是空的，将来不是

```cs
// RoleBase
protected virtual void Update() { }

// Player
private void Update() { /* 移动逻辑 */ }   // CS0114
```

今天行为正确，纯粹因为 `RoleBase.Update` 是空的。一旦哪天往基类 `Update` 里加「受击硬直计时」「状态机 tick」，**子类静默不执行**，而且是「昨天还好好的」那种 bug。

> 同类隐藏的高发名单：`Awake` / `Start` / `OnEnable` / `OnDisable` / `OnDestroy` / `Update` / `LateUpdate` / `FixedUpdate` / `OnTriggerEnter` / `OnCollisionEnter`。基类只要声明了 `protected virtual` 的这几个名字，子类就必须 `override`。

### 现场 3：为什么 `Awake` 不能做「基类先跑、子类后跑」

有人以为 `override` + `base.Awake()` 就能保证「基类逻辑先于子类逻辑」：

```cs
protected override void Awake()
{
    base.Awake();          // 基类先
    input = new InputSystem_Actions();   // 子类后
}
```

顺序确实对了，但**时机仍然太早**：`Awake` 阶段同场景的其他对象可能还没 `Awake`，`FindAnyObjectByType<Player>()` 之类的查找拿不到人；配表、单例也都还没就绪。真正的「基类先、子类后、且依赖可用」的时机是 `Start`（Unity 保证所有 `Awake`/`OnEnable` 先于任何 `Start`）。

---

## 四、正确写法

### 4.1 基类声明 virtual，子类 override + base

```cs
public class RoleBase : MonoBehaviour
{
    protected virtual void Start()
    {
        // 基类初始化：CharacterController / Animator / AudioSource
    }
}

public class Player : RoleBase
{
    protected override void Start()
    {
        base.Start();          // ← 忘了这行，基类初始化全丢
        // 子类初始化
    }
}
```

`Start` 是**一次性**初始化，`base.Start()` 忘写只是漏初始化，比较容易发现（NRE）。`Update` 是**每帧**的，`base.Update()` 忘写就是「基类每帧逻辑永远不跑」，更难发现。

### 4.2 每帧消息的「模板方法」写法

如果基类确实需要每帧做事，而子类也需要，用「非虚的消息方法 + 虚的钩子」最稳：

```cs
public class RoleBase : MonoBehaviour
{
    private void Update()          // 唯一的消息入口，子类不许再写 Update
    {
        OnUpdate();
    }

    protected virtual void OnUpdate() { }   // 子类 override 这个
}
```

这样即使子类忘了 `base.Update()`，基类的每帧逻辑也一定跑（因为它不在虚链上）。代价是多一层调用、以及团队必须约定「不许直接写 `Update`」。

### 4.3 顺手消掉 CS0114

- 基类有 `protected virtual X` → 子类写 `protected override X`。
- 基类**没有** `virtual` 但子类想同名 → 写 `new`，明确表达「我就是要遮住它」。
- 两者都不是，只是重名 → 改名。**CS0114 不要当噪音**，它是免费的设计提示。

---

## 五、必须记住的点

1. Unity 消息方法**按名字派发到最派生实现**，不是 C# 虚调用 —— 基类的同名方法不会自动执行。
2. `private void Awake()` 隐藏 `protected virtual Awake` 时**编译通过**，只有 `CS0114` 警告，基类逻辑静默死掉。
3. `override` 解决的是「**能不能调到**」，不解决「**时机对不对**」—— 现场 1 就是 `override` 也救不了的例子（配表未就绪）。
4. `Update`/`LateUpdate`/`FixedUpdate` 这类每帧消息，`override` 后**必须显式 `base.X()`**；想让基类逻辑无条件执行，用「非虚消息 + 虚钩子」。
5. 基类想统一兜底（补组件、绑事件），优先放 `OnEnable` 而不是 `Awake` —— `OnEnable` 可重入、且 `SetActive` 复用时还能再兜一次。

## 相关

- [[Unity 类继承体系 Object-Component-Behaviour-MonoBehaviour]] —— `Behaviour.enabled` 与消息派发的层级关系
- [[MonoBehaviour生命周期与SetActive的坑]] —— Awake/OnEnable/Start 的分阶段执行顺序，为什么 Start 能依赖 Awake 的结果
- [[UnityEngine.Object 判空与销毁机制]] —— 基类里 `== null` 判空为什么对「已销毁」对象也成立
- [[伤害系统设计 — 接口驱动与职责分层]] —— `RoleBase.OnEnable` 里 Health 兜底与订阅幂等的完整上下文
