---
title: "行为树工程实现 — 黑板、RUNNING 中断与节点生命周期"
type: resource
tags: [unity, AI, 行为树, 架构, unity面试]
created: "2026-10-22"
updated: "2026-10-22"
status: active
summary: "手写行为树的工程层三个核心：黑板（共享上下文）的查找链与三态陷阱、Sequence/Selector 对 RUNNING 的中断语义、节点与宿主（Transform / NavMeshAgent）的依赖注入。附 2026-10-22 UnityGroupWorking 实战 10 个 bug 的逐个根因与修复代码：GetData 提前 return 导致父链查找成死代码、SetData 用 Add 抛重复键、parent.parent 硬编码层级、LayerMask 恒为 0、Sequence 遇 RUNNING 继续遍历、using UnityEditor 混进运行时程序集、Attach 私有导致树建不起来、TaskPatrol transform 未注入 + 索引不递增 + 绕过 NavMeshAgent。"
source: "2026-10-22 UnityGroupWorking 行为树实战代码 review"
related: ["[[巡逻保安AI — FSM与行为树的机制差异与选型]]", "[[有限状态机FSM — 手写实现与状态切换边界]]", "[[Physics Raycast 与 NonAlloc]]", "[[Unity 类继承体系 Object-Component-Behaviour-MonoBehaviour]]"]
---

# 行为树工程实现 — 黑板、RUNNING 中断与节点生命周期

## 一、核心认知

**写一个行为树框架，代码量其实只有三件事：**

| 部件 | 职责 | 一句话 |
| --- | --- | --- |
| **节点（Node）** | 返回 `SUCCESS` / `FAILURE` / `RUNNING` | 只回答「这件事做完了没有」 |
| **黑板（Blackboard）** | 跨节点共享数据（目标、位置、仇恨值） | 节点之间**不许直接互相引用** |
| **遍历器（Tree）** | 每帧从根 tick 一次 | 树本身无状态，状态全在节点与黑板里 |

理论层（FSM 与 BT 的选型、Reactive Selector 为何能打断）见 📎 [[巡逻保安AI — FSM与行为树的机制差异与选型]]。**本篇只讲「真写起来会踩什么」**——全部来自 2026-10-22 `UnityGroupWorking` 里那套手写 BT 的实际代码。

---

## 二、黑板：查找链与三态陷阱

### 2.1 为什么必须共享

`CheckPlayerInFOVRange` 发现玩家后，`CheckPlayerInAttackRange` 和 `TaskChase` 都要用同一个「玩家」引用。三条路：

- ❌ 节点之间互相持有引用 → 树一改就断
- ❌ 每个节点自己 `FindWithTag("Player")` → 每帧 N 次查找
- ✅ **挂在共同祖先上，子节点沿父链向上查** —— 这就是黑板的最小实现

### 2.2 原始代码：`GetData` 的提前 return 把整段父链查找变成死代码

```cs
// Assets/Scripts/BT/Node.cs（原始）
public object GetData(string key)
{
    object value = null;

    if (!dataContext.TryGetValue(key, out value)) return value;   // ★ 这里
    //沿父级向上查找
    Node node = parent;
    while (node != null)
    {
        value = node.GetData(key);
        if (value != null) return value;
        node = node.parent;
    }
    return null;
}
```

`TryGetValue` 返回 `false`（自己身上没有）时**直接 return null**，后面 8 行父链查找永远执行不到。**条件写反了一个 `!`，整个黑板机制就废了。**

**后果链条**（比单个 bug 更值得记）：

1. `CheckPlayerInAttackRange.GetData("player")` 恒为 `null` → 恒返回 `FAILURE` → **攻击分支永远不触发**
2. `CheckPlayerInFOVRange.GetData("player")` 也恒为 `null` → 每帧都重新 `OverlapSphere` → 每帧都调 `SetData`
3. 于是撞上第二个 bug：`SetData` 用 `Dictionary.Add`，第二次同 key → **`ArgumentException: An item with the same key has already been added`**

⚠️ **调试教训**：这三个 bug（`GetData` 写反 / `SetData` 用 Add / `LayerMask` 恒为 0）**互相掩盖**——因为 mask 是 0，`OverlapSphere` 永远返回空数组，`SetData` 根本没被调用，所以异常不出现；异常不出现，就不会有人去查 `GetData`。**修好一个才会暴露下一个**，这种「连环雷」只能靠一次性通读整条链路，不能靠报错驱动。

### 2.3 修复：迭代查父链 + 索引器写入

```cs
private readonly Dictionary<string, object> dataContext = new();

// ✅ 索引器：key 存在则覆盖，不存在则新增（语义 = 「设置」）
public void SetData(string key, object value) => dataContext[key] = value;

// ✅ 迭代查父链，不递归
public object GetData(string key)
{
    if (dataContext.TryGetValue(key, out var v)) return v;

    for (var node = parent; node != null; node = node.parent)
        if (node.dataContext.TryGetValue(key, out v)) return v;

    return null;
}

// ✅ 清除也要沿父链（配合「目标丢失」时清黑板）
public bool ClearData(string key)
{
    if (dataContext.Remove(key)) return true;

    for (var node = parent; node != null; node = node.parent)
        if (node.dataContext.Remove(key)) return true;

    return false;
}
```

> `node.dataContext` 虽然声明为 `private`，但 C# 的 `private` 是**类型级**而非**实例级**——同类型的另一个实例可以访问。所以父链查找不需要把字段改成 `internal`。

### 2.4 三个工程细节

**① 递归写法是 O(depth²)**

原代码 `value = node.GetData(key)` 在循环里又递归了一次，每层都把上面的祖先重新扫一遍。虽然行为树深度通常 ≤ 5 无所谓，但既然已经手写了 `for` 循环，就别在循环体里再递归——**改成直接访问 `node.dataContext` 就退化成 O(depth)**。

**② 「值为 null」与「key 不存在」不可区分**

`GetData` 用 `null` 当「没找到」的哨兵，意味着**不能往黑板里存 `null`**。要存可空值（比如「当前目标 = 无」）有两个解法：

```cs
// 方案 A：拆成两个 API，语义清晰
public bool TryGetData(string key, out object value) { /* 沿父链查找 */ }

// 方案 B：哨兵对象
public static readonly object NullSentinel = new();
```

**③ `Dictionary<string, object>` + `as` 强转的类型安全代价**

`object` 存 `Transform` 是引用类型，**不装箱**；但如果存 `int` / `float` / `struct` 就会装箱（见 📎 [[struct 装箱陷阱与值类型原理]]）。更麻烦的是取出来必须 `as Transform`，写错类型静默返回 `null`，运行到 `playerTransform.position` 才 NRE。

工业级做法是泛型黑板：

```cs
public class Blackboard
{
    private readonly Dictionary<Type, object> map = new();
    public void Set<T>(T value) => map[typeof(T)] = value;
    public bool TryGet<T>(out T value)
    {
        if (map.TryGetValue(typeof(T), out var o)) { value = (T)o; return true; }
        value = default; return false;
    }
}
```

用**类型当 key**，编译期就能挡住「存 Transform 取 Rigidbody」这类错，也不用再拼字符串。

### 2.5 `parent.parent.SetData` —— 硬编码层级

```cs
// Assets/Scripts/BT/CheckPlayerInFOVRange.cs:26（原始）
parent.parent.SetData("player", colliders[0].transform);
```

`FOV` 是 `Selector` 的子节点，`Selector` 是 `root` 的子节点，所以 `parent.parent` 恰好是 root。**但只要在 FOV 外面再包一层 `Sequence`（比如「先巡逻再察觉」），深度就变成 3，`parent.parent` 指向的是中间那个 Sequence —— 数据挂错地方，兄弟分支读不到，而且不报错。**

三种解法，按推荐度排：

```cs
// ✅ 1. 沿父链一路挂到根（数据放最稳的地方）
public void SetDataOnRoot(string key, object value)
{
    var n = this;
    while (n.parent != null) n = n.parent;
    n.SetData(key, value);
}

// ✅ 2. 构造时注入共享黑板（最干净，也是主流框架做法）
public CheckPlayerInFOVRange(Transform owner, Blackboard bb) { ... }

// ⚠️ 3. 保留 SetData 的「就近写」语义，但用 ClearData 的沿父链语义配对
//     —— 写入和查找的语义必须对称，否则一定出错
```

**判据：凡是出现 `parent.parent`、`GetComponentInParent` 之类的「按深度取祖先」，都是在赌树的结构不变。树是会变的。**

---

## 三、RUNNING 的中断语义：Sequence 与 Selector 不对称

### 3.1 原始代码

```cs
// Assets/Scripts/BT/Sequence.cs（原始）
public override NodeState Evaluate()
{
    bool isAnyChildRunning = false;
    foreach (var node in children)
    {
        switch (node.Evaluate())
        {
            case NodeState.RUNNING:
                isAnyChildRunning = true;
                break;          // ★ 只 break 出 switch，循环继续
            case NodeState.SUCCESS:
                continue;
            case NodeState.FAILURE:
                state = NodeState.FAILURE;
                return state;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    state = isAnyChildRunning ? NodeState.RUNNING : NodeState.SUCCESS;
    return state;
}
```

对比 `Selector`（正确）：遇 `RUNNING` / `SUCCESS` 都**直接 `return`**。

### 3.2 为什么 Sequence 这样写是错的

`Sequence` 的语义是「**依次**执行，前一个没做完不许做下一个」。原代码遇 `RUNNING` 只 `break` 出 `switch`，`foreach` 继续跑 —— **后面的兄弟节点照样被 tick**。

具体到这个项目：`Sequence[CheckPlayerInAttackRange, TaskAttack]`。第一帧玩家进入 3 米，`TaskAttack` 开始播攻击动画返回 `RUNNING`；下一帧 `CheckPlayerInAttackRange` 返回 `RUNNING`（假设它在做「转向玩家」这种多帧动作），**但 `TaskAttack` 还是被 tick 了一次** → 攻击动画被重复触发 / 冷却被重复扣。

> 反直觉的地方：`isAnyChildRunning` 这个变量名暗示作者知道「有子节点在跑」，但把「有子节点在跑」当成了「**继续跑其它兄弟**」的理由，而正确语义是「**到此为止**」。

### 3.3 修复

```cs
public override NodeState Evaluate()
{
    foreach (var node in children)
    {
        switch (node.Evaluate())
        {
            case NodeState.RUNNING:
                state = NodeState.RUNNING;
                return state;          // ✅ 立刻中断，后面的兄弟不 tick
            case NodeState.FAILURE:
                state = NodeState.FAILURE;
                return state;
            case NodeState.SUCCESS:
                continue;              // ✅ 继续下一个
        }
    }
    state = NodeState.SUCCESS;         // 全部成功才算成功
    return state;
}
```

### 3.4 顺带：`Selector` 里的不可达代码

```cs
case NodeState.RUNNING:
    state = NodeState.RUNNING;
    return state;
    break;              // ⚠️ CS0162 不可达代码（warning，不是 error，容易被无视）
```

`return` 后的 `break` 是死代码。它不致命，但**它会让人误以为这里需要 `break`，从而在下一次重构时把 `return` 改成 `break`** —— 也就是 3.2 那个 bug 的来源。删掉。

### 3.5 记忆式 vs 响应式：中断语义的另一半

| 类型 | 每 tick 行为 | 适用 |
| --- | --- | --- |
| **记忆式 Sequence** | 记住上次停在哪个子节点，从那里继续 | 「开门 → 走进去」这种不能被打断的流程 |
| **响应式 Sequence** | 每帧从第一个子节点重跑，条件节点放最前面 | 「玩家在范围内 → 追击」，条件一变立刻退出 |

⚠️ **顺序即优先级**：响应式 Sequence 里，条件节点放**后面**意味着「前面的动作跑完才发现条件不成立了」——已经白跑了一帧（甚至一整个动画）。放**最前**才是真正的守卫。

---

## 四、节点生命周期：Node 不是 MonoBehaviour

### 4.1 依赖必须由构造注入

`Node` 是纯 C# 类（`public class Node`），**拿不到 `transform`、`gameObject`、`GetComponent`**。所以宿主对象必须显式传进来：

```cs
// ✅ 正确：构造注入
public class CheckPlayerInAttackRange : Node
{
    private readonly Transform owner;      // 不叫 transform，避免和 Unity 的 API 撞名产生误解
    private readonly float range;

    public CheckPlayerInAttackRange(Transform owner, float range = 3f)
    {
        this.owner = owner;
        this.range  = range;
    }

    public override NodeState Evaluate()
    {
        if (!(GetData("player") is Transform player))
        {
            state = NodeState.FAILURE;
            return state;
        }

        // 比 Distance 省一次 sqrt
        float sqr = (player.position - owner.position).sqrMagnitude;
        state = sqr <= range * range ? NodeState.SUCCESS : NodeState.FAILURE;
        return state;
    }
}
```

### 4.2 `TaskPatrol` 三连

```cs
// Assets/Scripts/BT/TaskPatrol.cs（原始）
private Transform transform;        // ★ 从未赋值
private int currentWaypointIndex;   // ★ 从未递增

public TaskPatrol(Transform[] _waypoints) { waypoints = _waypoints; }   // 只接 waypoints

public override NodeState Evaluate()
{
    Transform wp = waypoints[currentWaypointIndex];
    if (Vector3.Distance(wp.position, transform.position) < 0.01f)   // ★ NRE
    {
        transform.position = wp.position;                            // ★ 绕过 NavMeshAgent
        ...
    }
    else
    {
        transform.position = Vector3.MoveTowards(transform.position, wp.position, 3f * Time.deltaTime);
    }
    state = NodeState.RUNNING;   // ★ 恒 RUNNING，永不 SUCCESS
    return state;
}
```

| 问题 | 根因 | 后果 |
| --- | --- | --- |
| `transform` 为 null | 构造函数只接了 `waypoints`，字段没赋值；`Node` 不是 MonoBehaviour 也没有 `transform` 属性 | 第一帧 `NullReferenceException` |
| `currentWaypointIndex` 不递增 | 到达后只置 `isWaiting`，没有 `++` | 永远在 A 点原地打转，B/C 点永不访问 |
| 直接写 `transform.position` | 绕过 `NavMeshAgent` | 穿墙、脱离 NavMesh、`SetDestination` 状态与真实位置不一致；斜坡/障碍上直接瞬移 |
| 恒 `RUNNING` | 巡逻本来就该一直跑，但节点返回值决定父节点行为 | 放在 `Selector` 首位 → 永远短路，追击/攻击分支**饿死** |

### 4.3 修复：交给 NavMeshAgent + 状态枚举 + 让位

```cs
public class TaskPatrol : Node
{
    private readonly NavMeshAgent agent;
    private readonly Transform[] waypoints;
    private readonly float waitTime = 1f;
    private int index;
    private float waitCount;
    private bool isWaiting;

    public TaskPatrol(NavMeshAgent agent, Transform[] waypoints)
    {
        this.agent     = agent;
        this.waypoints = waypoints;
    }

    public override NodeState Evaluate()
    {
        // ✅ 有目标时主动让位，让 Selector 左边的追击/攻击分支接管
        if (GetData("player") != null) { isWaiting = false; return NodeState.FAILURE; }

        if (isWaiting)
        {
            waitCount += Time.deltaTime;
            if (waitCount < waitTime) return NodeState.RUNNING;
            isWaiting = false;
            index = (index + 1) % waypoints.Length;    // ✅ 递增 + 环形取模
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.05f)
        {
            waitCount = 0f;
            isWaiting = true;
            return NodeState.RUNNING;
        }

        agent.SetDestination(waypoints[index].position);   // ✅ 交给寻路，不碰 position
        return NodeState.RUNNING;
    }
}
```

> `agent.remainingDistance` 在 `pathPending == true` 时**返回值无意义**（可能是 0 或无穷大），所以必须先判 `pathPending`。这是 NavMeshAgent 最经典的坑之一。

---

## 五、程序集边界：`using UnityEditor` 混进运行时脚本

```cs
// Assets/Scripts/BT/Node.cs:2（原始）
using UnityEditor.Rendering.Toon;
// Assets/Scripts/BT/Tree.cs:7（原始）
using UnityEditor;
```

**这两个脚本挂在场景里、参与运行时逻辑，属于 `Assembly-CSharp`（运行时程序集）。**

### 根因

`UnityEditor.dll` 及其下所有命名空间（`UnityEditor.*`）**只在 Editor 里存在，不会被打进 Player**。Editor 里一切正常（因为 DLL 在），一旦 Build Player：

```
error CS0234: The type or namespace name 'Rendering' does not exist in the namespace 'UnityEditor'
```

或者更隐蔽的：编译过了，但 IL2CPP 阶段报 `UnityEditor` 引用无法解析。

### 两种修法

```cs
// ✅ 方案 A：确认是误加的 using，直接删（本例就是这种——两行都没用到任何类型）
//    Unity 的 IDE 自动补全（尤其是 Rider/VS 的「智能 using」）最爱干这事

// ✅ 方案 B：确实需要编辑器 API（比如画 Gizmo、读 AssetDatabase）
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Tree : MonoBehaviour
{
#if UNITY_EDITOR
    private void OnDrawGizmos() { /* 用 UnityEditor.Handles 画调试线 */ }
#endif
}
```

⚠️ **方案 B 只适用于「用 Editor API 做调试可视化」**。如果业务逻辑真的依赖 `AssetDatabase`，那这段代码必须搬到 `Assets/Editor/` 目录或 `asmdef` 里勾 `Editor` 平台 —— **`#if` 只能隔离编译，隔离不了架构错误**。

---

## 六、`Attach` 私有 → 树根本建不起来

```cs
// Assets/Scripts/BT/Node.cs（原始）
protected List<Node> children = new List<Node>();
private void Attach(Node node) { node.parent = this; children.Add(node); }   // ★ private

// Sequence / Selector 都只有无参构造
public Selector() : base() { }
```

`Attach` 是 `private`，`children` 是 `protected`，`Sequence` / `Selector` **只有无参构造**。于是外部（`Tree.SetUpTree()`）**没有任何合法途径给组合节点挂子节点** —— 这棵树在 API 层面就建不起来。

即使把 `Attach` 改成 `public`，还得有人手动 `selector.Attach(new TaskPatrol(...))` 一串，非常啰嗦。

```cs
// ✅ 用 params 构造 + public Attach 一次解决
public Node() { }
public Node(params Node[] children)
{
    foreach (var c in children) Attach(c);
}

public void Attach(Node node)
{
    node.parent = this;
    children.Add(node);
}
```

调用端就变成声明式的：

```cs
protected override Node SetUpTree()
{
    var patrol = new TaskPatrol(agent, waypoints);
    var chase  = new Sequence(new CheckPlayerInFOVRange(transform, 10f), new TaskChase(agent));
    var attack = new Sequence(new CheckPlayerInAttackRange(transform, 3f), new TaskAttack(animator));

    // 顺序即优先级：能打就打，打不到就追，都没有才巡逻
    return new Selector(attack, chase, patrol);
}
```

> **顺序即优先级**：`Selector` 从左到右，第一个不返回 `FAILURE` 的胜出。所以「攻击」必须排最左。反过来写（`patrol` 在最左且恒 `RUNNING`）就是 4.2 里的「饿死」问题。

---

## 七、`Tree` 基类：方法隐藏

```cs
// Assets/Scripts/BT/Tree.cs（原始）
protected void Start()  { root = SetUpTree(); }
private   void Update() { if (root != null) root.Evaluate(); }
```

| 问题 | 说明 |
| --- | --- |
| `Start()` 是 `protected` 非 `virtual` | Unity 的消息派发**不看访问修饰符**，`protected Start` 能被调到。但子类一旦也写 `void Start()` → **隐藏**（CS0108），base 的 `SetUpTree()` 不再执行 → `root` 永远是 `null` → 树完全不动，**且没有任何报错** |
| `Update()` 是 `private` | 同上。子类写 `void Update()` 会隐藏它，base 里的 `root.Evaluate()` 不再被调用 |
| `SetUpTree()` 返回 `null` | 没有判空，`Update` 里判了 `root != null` 所以不崩，但**静默失效** |

```cs
// ✅ 修复：显式 virtual，子类用 override 而不是重新声明
protected virtual void Start()  { root = SetUpTree(); }
protected virtual void Update() { root?.Evaluate(); }

protected abstract Node SetUpTree();
```

⚠️ **通用规律**：MonoBehaviour 的消息方法（`Awake` / `Start` / `Update` / `OnEnable`…）**由 Unity 反射调用，访问修饰符无所谓；但「子类重新声明」会隐藏基类版本，且不报错**。所以框架基类里的消息方法必须 `virtual`，子类必须 `override`。详见 📎 [[MonoBehaviour生命周期与SetActive的坑]]。

---

## 八、`LayerMask` 恒为 0

```cs
// CheckPlayerInFOVRange.cs / CheckPlayerInAttackRange.cs（原始）
private static LayerMask playerLayerMask;   // 从未赋值
```

`LayerMask` 是 `struct`，默认值 `0` → `Physics.OverlapSphere(pos, 10, 0)` 的 mask 为 0 表示**一个层都不检测** → 恒返回空数组 → **永远发现不了玩家**。

而且：
- `private static` **不会被序列化**（Unity 只序列化 `public` 或 `[SerializeField] private` 的**实例**字段）
- `Node` 不是 `MonoBehaviour`，**根本没有 Inspector 可挂**

```cs
// ✅ 构造注入，并在 Tree（MonoBehaviour）里用 LayerMask.GetMask 或 [SerializeField] 配
public CheckPlayerInFOVRange(Transform owner, float radius, LayerMask mask) { ... }

// Tree 侧：
[SerializeField] private LayerMask playerLayer = 1 << 8;   // 或 LayerMask.GetMask("Player")
```

⚠️ `LayerMask.GetMask("Player")` 在 `Awake` 之前调用返回 0（层名表还没初始化），**必须在 `Awake`/`Start` 之后调**，或者直接用位运算 `1 << LayerMask.NameToLayer("Player")`。见 📎 [[Physics Raycast 与 NonAlloc]]。

### 顺带：`OverlapSphere` 每帧分配

`Physics.OverlapSphere` 每次调用都 `new Collider[]` → 每帧 GC 压力。感知节点通常 5~10Hz 跑（不需要每帧），真要每帧就用 `OverlapSphereNonAlloc`：

```cs
private readonly Collider[] hits = new Collider[8];
int count = Physics.OverlapSphereNonAlloc(owner.position, radius, hits, mask);
```

---

## 九、十个 bug 汇总表

| # | 位置 | 症状 | 根因 | 修复 |
| --- | --- | --- | --- | --- |
| 1 | `Node.GetData` | 攻击分支永不触发 | `if (!TryGetValue) return value;` 提前返回，父链查找是死代码 | 命中才返回，否则迭代查父链 |
| 2 | `Node.SetData` | 同 key 二次写抛 `ArgumentException` | 用了 `Dictionary.Add` | 改用索引器 `dict[key] = value` |
| 3 | `Node.GetData` | 无 | 循环里递归调用 → O(depth²) | 循环内直接查 `node.dataContext` |
| 4 | `FOV:26` | 树一改就断 | `parent.parent.SetData` 硬编码深度 | 注入共享黑板 / 挂到根 |
| 5 | `Sequence` | 后续 Action 被误执行 | `RUNNING` 只 `break` switch，继续遍历兄弟 | 立刻 `return RUNNING` |
| 6 | `Selector` | CS0162 警告 | `return` 后跟 `break` | 删 `break` |
| 7 | `Node.cs:2` / `Tree.cs:7` | Build Player 编译失败 | 运行时脚本 `using UnityEditor.*` | 删 using / `#if UNITY_EDITOR` / 搬 Editor 目录 |
| 8 | `Node.Attach` | 树建不起来 | `Attach` 私有 + 组合节点只有无参构造 | `public Attach` + `params` 构造 |
| 9 | `Tree` | 树静默不动 | 子类重声明 `Start`/`Update` 隐藏基类版本 | 基类 `virtual`、子类 `override` |
| 10 | `TaskPatrol` | NRE / 原地打转 / 穿墙 | `transform` 未注入、索引不递增、绕过 NavMeshAgent | 注入 `NavMeshAgent` + 环形索引 + `SetDestination` |
| 11 | 感知节点 | 永远发现不了玩家 | `static LayerMask` 从未赋值 = 0 | 构造注入 + `[SerializeField]` 配层 |

---

## 十、面试延伸

**Q：手写行为树最难的地方是什么？**
不是节点类型（Selector/Sequence 各 10 行），是**三件工程问题**：① 黑板的数据归属与查找语义（写和读必须对称）；② `RUNNING` 的中断语义（组合节点要记住「停在谁身上」，且要不要重跑前置条件）；③ 节点与宿主的依赖注入（Node 是纯 C# 类，Transform/Agent/Animator 全靠外部喂）。

**Q：为什么工业级行为树都要可视化编辑器？**
因为树的执行路径是**运行时数据**，纯代码调试只能靠 `Debug.Log` 打返回值，一棵 30 节点的树一次 tick 打 30 行日志。编辑器能高亮当前执行路径 + 显示黑板快照，这是「能调试」和「不能调试」的差别。

**Q：行为树 vs 状态机，什么时候必须换？**
行为数量到 8~10 个、且需要**正交组合**（边走边打、受击打断攻击）时。4 个状态以内用 FSM 更快、零框架代码。详见 📎 [[巡逻保安AI — FSM与行为树的机制差异与选型]]。

**Q：`RUNNING` 到底是不是状态？**
是「记忆」不是「状态」。`SUCCESS`/`FAILURE` 当帧结束，只有 `RUNNING` 要求「下次从我这继续」——所以行为树本质是**一次带记忆的深度优先遍历**。

---

## 相关

- 📎 [[巡逻保安AI — FSM与行为树的机制差异与选型]] —— 理论层：FSM 与 BT 六维差异、Reactive Selector 为何能打断、什么时候该换
- 📎 [[有限状态机FSM — 手写实现与状态切换边界]] —— 对照实现：FSM 三层骨架 + 切换边界五要素
- 📎 [[Physics Raycast 与 NonAlloc]] —— `OverlapSphere` 分配问题、LayerMask 传参坑、`ContactFilter2D`
- 📎 [[MonoBehaviour生命周期与SetActive的坑]] —— MonoBehaviour 消息方法的反射调用规则（方法隐藏为何静默失效）
- 📎 [[Unity 类继承体系 Object-Component-Behaviour-MonoBehaviour]] —— 为什么 `Node` 拿不到 `transform`
- 📎 [[struct 装箱陷阱与值类型原理]] —— `Dictionary<string, object>` 存值类型的装箱代价
