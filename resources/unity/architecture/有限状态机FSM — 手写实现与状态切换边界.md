---
title: "有限状态机FSM — 手写实现与状态切换边界"
type: resource
tags: [unity, 架构, 状态机, AI, 设计模式]
created: "2026-10-21"
updated: "2026-10-22"
status: active
summary: "手写 FSM 三层骨架（IState 接口 / 状态基类持 owner / 状态机 Dictionary 注册 + 幂等切换），Exit→换ID→Enter 的顺序理由、非 MonoBehaviour 状态机开不了协程的后果、状态切换边界五要素，以及怪物 AI 实战里的 6 个工程坑"
source: "E:\\unityproject\\UnityGroupWorking\\UnityGroupWorking-main\\Assets\\Scripts\\FSM"
related: ["[[Animator参数与性能优化]]", "[[scriptableobject数据驱动设计]]", "[[单例模式与静态类选型]]", "[[MonoBehaviour生命周期与SetActive的坑]]"]
---

# 有限状态机FSM — 手写实现与状态切换边界

## 一、核心认知

状态机解决的是**「同一时刻只能处于一种行为模式」**的问题。三个要素缺一不可：

| 要素 | 内容 | 落点 |
| --- | --- | --- |
| 状态集合 | Idle / Walk / Attack / Enraged / Dead | `enum State` |
| 转移条件 | 距离、血量、冷却、动画事件 | 各状态 `OnUpdate()` |
| 生命周期钩子 | 进入一次性、持续每帧、退出一一次性 | `OnEnterState / OnUpdate / OnExitState` |

**关键分界：逻辑状态机 ≠ Animator 状态机。**

- Animator 的 `AnimatorStateMachine` 管的是**表现层**（动画片段怎么过渡、混合树怎么加权），它不知道游戏规则。
- 逻辑 FSM 管的是**行为层**（该不该追、该不该打、能不能被打断），然后**把结果写成 Animator 参数**（`isRunning` / `isAttacking`）由 Animator 自己去播。
- 反过来做——让 Animator 的 `Has Exit Time` 或动画事件去决定游戏逻辑——就会出现「动画播完了怪还在追」「受击动画被走路打断但逻辑没切」这类错位。**逻辑永远是主，动画是仆。**

---

## 二、三层骨架（2026-10-21 实战代码）

### 1. 接口层：只有三个钩子

```cs
public interface IState
{
    void OnEnterState();  // 状态开始，只执行一次：开动画、开导航、播音效
    void OnUpdate();      // 状态持续，每帧：判断条件、算转移
    void OnExitState();   // 状态结束，只执行一次：关动画、复位标志
}

public enum State { Default, Idle, Attack, Walk, Enraged, Dead }
```

接口里**不要塞 `owner` 参数**。状态对象在构造时就绑定了宿主，每帧再传一遍是多余的参数搬运。

### 2. 基类层：持宿主 + 强制三钩子

```cs
public abstract class MonsterStateBase : IState
{
    protected MonsterCTRL owner;                       // 状态持有宿主，反向调用其属性
    protected MonsterStateBase(MonsterCTRL _owner) => owner = _owner;

    public abstract void OnEnterState();
    public abstract void OnUpdate();
    public abstract void OnExitState();
}
```

`protected` 而不是 `public`：状态只服务于自己的宿主，外部不该摸到 `owner`。

### 3. 状态机层：注册表 + 幂等切换

```cs
public class MonsterStateMachine
{
    private readonly Dictionary<State, IState> states = new Dictionary<State, IState>();
    private IState currentState;
    private State currentStateId;
    public State CurrenStateID => currentStateId;

    public void RegisterState(State state, IState newState) => states[state] = newState;

    public void ChangeState(State newStateID)
    {
        if (currentStateId == newStateID) return;              // ① 幂等早退
        if (!states.TryGetValue(newStateID, out var newState)) // ② 缺失状态不崩
        {
            Debug.LogError($"State not found: {newStateID}");
            return;
        }
        currentState?.OnExitState();                           // ③ 先退出
        currentStateId = newStateID;                           // ④ 再换 ID
        currentState = newState;                               // ⑤ 再换引用
        currentState.OnEnterState();                           // ⑥ 最后进入
    }

    public void Update() => currentState?.OnUpdate();
}
```

**为什么顺序必须是 Exit → 换 ID → Enter，不能颠倒？**

- `OnExitState()` 里常常要读「我原本是哪个状态」来决定清理什么（比如 Walk 退出才关 `isRunning`）。若先换 ID，Exit 里读到的已是新状态 → 清理错对象。
- `OnEnterState()` 里若读 `CurrenStateID` 做自检，必须先换成新 ID，否则读到旧值。
- 一句话：**Exit 看旧值，Enter 看新值，所以换 ID 必须卡在两者中间。**

**`currentState?.OnUpdate()` 的切换时机语义**：在 `OnUpdate()` 内部调 `ChangeState` 时，本次调用栈不会执行新状态的 `OnUpdate`，新状态从**下一帧**才跑起来。所以切换后的那一帧是「空窗」——如果新状态的进入依赖本帧的位移/检测，要放在 `OnEnterState` 里做，别指望下一帧补。

---

## 三、状态切换边界五要素（面试常问）

| # | 要素 | 本项目的落点 | 缺失后果 |
| --- | --- | --- | --- |
| 1 | 切换条件由谁判定 | 各状态 `OnUpdate` 自判（分散式） | 判定散落各处，改一处漏三处 |
| 2 | 不可切规则 | **缺失**：Dead 没实现，攻击中可被 Idle 抢切 | 死亡后还能走、还能被打断 |
| 3 | 优先级 | **缺失**：同帧「进攻击范围」和「超追击距离」同时成立时按 if/else 顺序取胜 | 结果依赖代码行序，重构即变行为 |
| 4 | Enter/Exit 清理配对 | Walk 进 `isStopped=false` / 出 `isStopped=true`，动画参数成对置位 | 单向置位 → 状态残留 |
| 5 | 非法切换处理 | `TryGetValue` 失败 `LogError` 后 return，不崩 | 直接索引器抛 `KeyNotFoundException` |

第 2、3 条是最容易漏的。**集中式判定**（由状态机统一按优先级表裁决）比分散式更抗重构，代价是状态机要知道业务条件。

---

## 四、实战中的 6 个工程坑

### 坑 1：每帧 `SetDestination` 会重复触发内部寻路

```cs
// ❌ 每帧无条件重设
public override void OnUpdate()
{
    owner.Agent.SetDestination(owner.Target.position);
    ...
}
```

`SetDestination` 不是「设个目标值」那么便宜：它会请求 NavMesh 路径（Corridor 计算 + 可能的 A*），目标几乎没动也照样重算。单只怪看不出来，同帧几十只就是主线程尖峰。

```cs
// ✅ 目标位移超过阈值才重设
private Vector3 lastTargetPos;
private const float RepathThreshold = 0.5f;

public override void OnUpdate()
{
    var targetPos = owner.Target.position;
    if ((targetPos - lastTargetPos).sqrMagnitude > RepathThreshold * RepathThreshold)
    {
        owner.Agent.SetDestination(targetPos);
        lastTargetPos = targetPos;
    }
    // 用 sqrMagnitude 比较，省一次开方
}
```

另外 `isStopped = true` 只停移动**不取消路径**，`velocity` 也不会立刻归零（有惯性），要瞬停得同时 `agent.ResetPath()`。

### 坑 2：`OnUpdate` 空实现 + 冷却字段只读不写 = 攻击无冷却

```cs
// ❌ 进入就置 isAttacking=true，OnUpdate 空，靠动画事件 EndAttack 回 Idle
public override void OnUpdate() { }
```

`attackCoolDown` 从配置读进来了，但**全项目没有任何地方写回 `canAttack`**。结果：动画事件一触发就回 Idle，Idle 下一帧又满足距离 → 再进 Attack，形成「贴脸连击」。

```cs
// ✅ 状态机不是 MonoBehaviour，开不了协程 → 用时间戳累加
private float lastAttackTime = -999f;

public override void OnUpdate()
{
    if (Time.time - lastAttackTime >= owner.AttackCoolDown)
        owner.TryAttack();          // 内部判定朝向 + 半径
}
```

### 坑 3：状态机不是 MonoBehaviour，用不了协程

`MonsterStateMachine` 是纯 C# 类（`class` 而非 `MonoBehaviour`），拿不到 `StartCoroutine`。所以任何「等 X 秒」的需求只有两条路：

1. **时间戳 + `Time.time`**（推荐，零分配、可暂停可加速）
2. 让 `owner`（MonoBehaviour）代跑协程，状态里存 `Coroutine` 句柄并在 `OnExitState` 里 `StopCoroutine`

第 2 条要特别小心：状态被打断时协程没停 → 旧状态的延时逻辑在新状态里生效，是典型「幽灵回调」。

### 坑 4：距离判定只算半径不算朝向 → 背后也能打中

```cs
// ❌ 球形判定，怪背对玩家照样造成伤害
if (Vector3.Distance(player.position, transform.position) <= attackHitRadius)
    playerHealth.TakeDamage(attackDamage);
```

```cs
// ✅ 扇形判定：距离 + 夹角
Vector3 toTarget = player.position - transform.position;
if (toTarget.sqrMagnitude > attackHitRadius * attackHitRadius) return;

float cosHalf = Mathf.Cos(attackAngle * 0.5f * Mathf.Deg2Rad);
if (Vector3.Dot(transform.forward, toTarget.normalized) < cosHalf) return;  // 在身后

playerHealth.TakeDamage(attackDamage);
```

`Dot` 与 `cos` 比较即可，**不要 `Vector3.Angle` 再比**——`Angle` 内部要开方+反三角，比点乘贵一个量级。

### 坑 5：配置用 `int` 存距离

`MonsterConfig` 六个字段全是 `public int`（HP / chaseDistance / attackDistance / attackCoolDown / attackDamage / attackHitRadius）。距离类字段用 `int` 的后果：

- `2.9f <= 3` 判为「进范围」，策划填 3 以为是 3 米整；
- 与 `Vector3.Distance`（float）比较时每帧隐式转换；
- 想填 0.5 米的小怪直接填不进去。

**距离/时间/倍率一律 `float`，只有 HP、伤害、数量用 `int`。**

### 坑 6：`GameRoot` 单例没有 `DontDestroyOnLoad`，懒加载 getter 是「伪 null」

```cs
// ❌ 只赋值不防重、不持久化
private void Awake() { if (Instance == null) Instance = this; }

// ❌ 组件不存在时，每次访问都 GetComponent 一次
public PlayerHealth PlayerHealth => playerHealth ??= Player.GetComponent<PlayerHealth>();
```

- 切场景后旧 `GameRoot` 被销毁，`Instance` 变成**伪 null**（Unity 重载 `==`，对象壳还在、原生侧已销毁）→ 访问成员抛 `MissingReferenceException`。
- `playerHealth == null` 判断对「组件不存在」永远成立 → 每次访问都走一次 `GetComponent`（native 调用 + 类型查找），且 Unity 的 `==` 重载本身有额外开销（实例比较 + `IsNativeObjectAlive`）。

```cs
// ✅ 标准单例 + Awake 里一次性校验
private void Awake()
{
    if (Instance != null && Instance != this) { Destroy(gameObject); return; }
    Instance = this;
    DontDestroyOnLoad(gameObject);

    playerHealth = Player.GetComponent<PlayerHealth>();
    if (playerHealth == null) Debug.LogError("[GameRoot] Player 上缺 PlayerHealth 组件", this);
}
```

> ⚠️ Unity 6.6 起新工程默认 `Enter Play Mode` 为 **Reload Scene only**（关闭 domain reload）→ `static Instance` 会跨 Play 残留。`Awake` 里的「非本实例就销毁」防重分支正好能兜住这种陈旧引用，但更稳的是加 `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` 主动清空静态字段。

---

## 五、FSM vs 行为树 vs GOAP

| 维度 | FSM | 行为树（BT） | GOAP |
| --- | --- | --- | --- |
| 心智模型 | 当前状态 + 转移 | 从根节点按优先级/序列往下走 | 目标 + 前提条件 + 动作代价，运行时规划 |
| 适合 | 状态少（<10）、转移明确的角色 | 层级化行为、需要频繁插入新分支 | 目标组合爆炸、需要涌现式行为 |
| 扩展成本 | 状态数平方级转移，容易变「意大利面」 | 加节点不改旧树 | 加动作即可，但要调代价函数 |
| 调试难度 | 低（打印当前状态就够） | 中（要可视化树执行路径） | 高（规划结果不直观） |
| 典型坑 | 状态爆炸、转移遗漏 | 每帧遍历开销、黑板数据竞争 | 规划抖动、代价难调 |

**一句话选型**：状态 ≤10 用 FSM；有「优先级打断 + 可复用子树」用 BT；要「怪物自己想办法」用 GOAP。工业界常见组合是 **FSM 管顶层（Idle/Combat/Dead）+ BT 管 Combat 内部**。

> 行为树的节点语义（Selector / Sequence / Condition、`Running` 是记忆而非状态、打断来自 Reactive Selector）与「巡逻保安该选哪个」的完整推演，见 [[巡逻保安AI — FSM与行为树的机制差异与选型]]。

---

## 六、复杂度与开销

| 操作 | 复杂度 | 备注 |
| --- | --- | --- |
| `RegisterState` | O(1) 均摊 | 启动时一次性，可忽略 |
| `ChangeState` | O(1) | 哈希查找；状态对象**复用**，不 new |
| `Update` | O(1) | 一次虚调用 `OnUpdate` |
| 内存 | O(状态数) | 每只怪一套状态对象，N 只怪 = N×状态数 |

**枚举键不装箱**：`Dictionary<State, IState>` 里 `EqualityComparer<State>.Default` 对枚举类型会特化成 `EnumEqualityComparer<State>`，内部按底层整型比较，不走 `ValueType.Equals` 的反射路径，所以**没有装箱**。

但「不装箱」不等于「最快」：枚举值连续时 `switch` 会被编译成跳转表，比哈希查找快；状态数少且固定时 `switch` 分派更优。选 `Dictionary` 的理由是**支持运行时注册与热更扩展**，不是为了性能。

---

## 七、面试延伸

1. **「你的怪物 AI 为什么用手写状态机而不是 Animator？」** —— 表现与逻辑分离；Animator 无法表达「冷却、仇恨、打断优先级」，也无法在服务器/无渲染环境跑。
2. **「状态爆炸了怎么办？」** —— 分层状态机（HFSM，父状态持有子状态机，子状态共享父状态的进入/退出）或把「移动/攻击」拆成正交的并行状态机（移动一个、行为一个）。
3. **「怎么实现状态打断？」** —— 状态表加优先级字段，`ChangeState` 里 `if (newPriority < currentPriority && !force) return;`，配合 `force` 参数处理死亡这类绝对打断。
4. **「切换瞬间的动画怎么不抖？」** —— 逻辑切完只写参数，过渡时长交给 Animator 的 `Transition Duration`；不要用 `CrossFade` 和参数双管齐下。

---

## 相关

- [[Animator参数与性能优化]] —— 逻辑状态机把结果写成 Animator 参数（`StringToHash` 预计算），以及过渡进行中 `GetCurrentAnimatorStateInfo` 返回哪个状态
- [[scriptableobject数据驱动设计]] —— `MonsterConfig : ScriptableObject` 把六项数值外置，运行时改数值不用改代码
- [[单例模式与静态类选型]] —— `GameRoot.Instance` 这类全局访问点的取舍
- [[MonoBehaviour生命周期与SetActive的坑]] —— `Awake` 读配置、`Start` 取 `GameRoot.Instance` 的执行顺序依赖
