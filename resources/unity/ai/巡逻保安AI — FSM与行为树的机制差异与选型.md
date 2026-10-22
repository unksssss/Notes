---
title: "巡逻保安AI — FSM与行为树的机制差异与选型"
type: resource
tags: [unity, AI, 状态机, 行为树, unity面试]
created: "2026-10-22"
updated: "2026-10-22"
status: active
summary: "FSM 与行为树的六维差异（心智模型 / 执行方式 / 扩展成本 / 复用 / 打断 / 调试）；三个易混机制（Running 是记忆不是状态、打断来自 Reactive Selector 而非行为树本身、FSM 的病根是状态间两两耦合）；巡逻保安选 FSM 的完整实现（4 状态 + 感知降频 + Enter/Exit 配对 + 幂等切换）；换成行为树的四个信号与混用实践。"
source: "面试八股整理（2026-10-22）"
related: ["[[有限状态机FSM — 手写实现与状态切换边界]]", "[[Physics Raycast 与 NonAlloc]]"]
---

# 巡逻保安AI — FSM与行为树的机制差异与选型

## 一、核心认知

**FSM 和 BT 不是「低级/高级」的关系，是两种组织思路：**

- **FSM**：回答「我**现在处于**什么状态」——任何时刻只有一个状态在跑，状态之间靠转移边跳
- **BT**：回答「我要**达成**什么目标」——每次 tick 从根节点往下遍历，靠节点返回值决定走哪条分支

一句话：**FSM 是横向的图，BT 是纵向的树。**

---

## 二、六维差异

| 维度 | FSM | 行为树（BT） |
| --- | --- | --- |
| 心智模型 | 我现在处于什么状态 | 我要达成什么目标 |
| 执行方式 | 只跑当前那一个状态 | 每次 tick 从根往下遍历 |
| 扩展成本 | 加状态要补 N 条转移边，最坏 N² 量级 | 加行为只挂一个叶子 |
| 复用 | 靠子状态机 / 继承，别扭 | 子树天然可复用，整组节点可搬 |
| 优先级与打断 | 要自己写优先级表，容易沦为 if-else 行序 | 天然有序：Selector 从左到右，前面成功就不看后面 |
| 调试 | 打印一个枚举值就够 | 必须可视化，否则看不出执行到哪个分支 |

### 三个容易讲错的地方

**① 行为树的 `Running` 不是状态，是「记忆」**

节点返回 `Success` / `Failure` / `Running`。前两个当帧结束，只有 `Running` 表示「没做完，下次从我这继续」。

所以 BT 本质是**一次带记忆的深度优先遍历**——必须记住上次停在哪个节点，否则每帧从根重跑会把进度清空（比如「走 3 步」会一直重走第 1 步）。

**② 「行为树能灵活打断」这句话不完整**

打断能力取决于组合节点的类型：

| 节点类型 | 每 tick 是否重判条件 | 能否打断 |
| --- | --- | --- |
| **Reactive Selector**（反应式选择器） | 是，每次都从头评估所有子节点 | 能，玩家一出现立刻切走 |
| **普通 Sequence** | 否，某子节点 Running 后锁定在它身上 | 不能，正在开门时看见玩家也得开完 |

→ **打断是 Selector 逐帧重判带来的，不是行为树自带的**。

**③ FSM 的病根不是「状态多」，是「状态之间两两耦合」**

4 个状态时它极其清晰。到 12 个状态、还要表达「边走边开枪」「受击打断攻击」这种**正交行为**时，转移边会缠成一团。

补救方案有两个：

- **分层状态机（HFSM）**：父状态持有一套子状态机，共享进入/退出
- **并行状态机**：移动一个（Idle/Walk/Run）、行为一个（Idle/Attack/Hit），互不干扰

---

## 三、选题：巡逻保安用 FSM

### 3.1 需求结构

四个状态，几乎是一条单向链（见对话中的转移图）：

```
巡逻 --看见--> 警戒 --确认 1.5s--> 追击 --丢失 3s / 超距--> 返回 --到达原位--> 巡逻
```

### 3.2 为什么不选行为树

| 判断项 | 实际情况 |
| --- | --- |
| 状态数 | 4 个 |
| 转移关系 | 几乎线性的链，无交叉 |
| 是否需要组合行为 | 不需要（没有「边巡逻边检查门」） |
| 谁维护 AI | 自己，不是策划 |
| 框架成本 | BT 起步 200 行（节点基类 + Selector/Sequence + Blackboard + tick 调度）+ 编辑器；FSM 0 行 |

→ **这个规模下行为树是纯负债：为挂一幅画盖一座美术馆。**

### 3.3 完整实现

```csharp
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class GuardAI : MonoBehaviour
{
    public enum State { Patrol, Alert, Chase, Return }

    [Header("巡逻")]
    [SerializeField] Transform[] waypoints;
    [SerializeField] float patrolSpeed = 1.5f;
    [SerializeField] float arriveThreshold = 0.3f;

    [Header("感知")]
    [SerializeField] Transform player;
    [SerializeField] float viewDistance = 8f;
    [SerializeField] float viewAngle = 120f;      // 全角
    [SerializeField] float loseDistance = 14f;    // 离出生点超过这个距离就放弃
    [SerializeField] float confirmTime = 1.5f;    // 警戒多久转追击
    [SerializeField] float loseSightTime = 3f;    // 视线丢失多久放弃
    [SerializeField] float eyeHeight = 1.6f;
    [SerializeField] float senseInterval = 0.1f;  // 感知 10Hz
    [SerializeField] LayerMask obstacleMask;

    [Header("移动")]
    [SerializeField] float chaseSpeed = 4f;

    NavMeshAgent agent;
    State state = State.Patrol;
    int wpIndex;
    float stateTimer;
    float loseSightTimer;
    Vector3 homePos;
    Vector3 lastSeenPos;

    bool canSee;          // 本帧缓存的感知结果
    float senseTimer;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        homePos = transform.position;
    }

    void Start() => EnterState(State.Patrol);

    void Update()
    {
        // ① 感知降频：射线检测不必每帧跑
        senseTimer += Time.deltaTime;
        if (senseTimer >= senseInterval)
        {
            senseTimer = 0f;
            canSee = CanSeePlayer();
        }

        stateTimer += Time.deltaTime;

        // ② 状态分发：只有一个分支执行
        switch (state)
        {
            case State.Patrol: TickPatrol(); break;
            case State.Alert:  TickAlert();  break;
            case State.Chase:  TickChase();  break;
            case State.Return: TickReturn(); break;
        }
    }

    void TickPatrol()
    {
        if (canSee) { ChangeState(State.Alert); return; }

        if (agent.remainingDistance <= arriveThreshold && !agent.pathPending)
        {
            wpIndex = (wpIndex + 1) % waypoints.Length;
            agent.SetDestination(waypoints[wpIndex].position);
        }
    }

    void TickAlert()
    {
        FaceTarget(player.position);

        if (canSee)
        {
            if (stateTimer >= confirmTime) ChangeState(State.Chase);
        }
        else
        {
            ChangeState(State.Return);   // 只是路过，回去继续巡逻
        }
    }

    void TickChase()
    {
        if (canSee)
        {
            loseSightTimer = 0f;
            lastSeenPos = player.position;
            agent.SetDestination(lastSeenPos);
        }
        else
        {
            loseSightTimer += Time.deltaTime;
            if (loseSightTimer >= loseSightTime) ChangeState(State.Return);
        }

        // 追太远就放弃，防止追出关卡
        if ((player.position - homePos).sqrMagnitude > loseDistance * loseDistance)
            ChangeState(State.Return);
    }

    void TickReturn()
    {
        if (canSee) { ChangeState(State.Alert); return; }

        if (agent.remainingDistance <= arriveThreshold && !agent.pathPending)
            ChangeState(State.Patrol);
    }

    void ChangeState(State next)
    {
        if (state == next) return;      // 幂等：防止同帧反复 Enter/Exit

        ExitState(state);
        state = next;
        stateTimer = 0f;
        EnterState(next);
    }

    void EnterState(State s)
    {
        switch (s)
        {
            case State.Patrol:
                agent.isStopped = false;
                agent.speed = patrolSpeed;
                agent.SetDestination(waypoints[wpIndex].position);
                break;

            case State.Alert:
                agent.isStopped = true;     // 停下来观察
                break;

            case State.Chase:
                agent.isStopped = false;
                agent.speed = chaseSpeed;
                loseSightTimer = 0f;
                break;

            case State.Return:
                agent.isStopped = false;
                agent.speed = patrolSpeed;
                agent.SetDestination(homePos);
                break;
        }
    }

    void ExitState(State s)
    {
        // 本需求没有必须清理的项，但入口必须留着
        // 一旦加入动画参数 / 音效 / 特效，都要在这里成对复位
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 to = player.position - transform.position;

        // 距离：sqrMagnitude 省一次开方
        if (to.sqrMagnitude > viewDistance * viewDistance) return false;

        Vector3 dir = to.normalized;

        // 视野角：点乘与 cos(半角) 比较，不要用 Vector3.Angle
        if (Vector3.Dot(transform.forward, dir) < Mathf.Cos(viewAngle * 0.5f * Mathf.Deg2Rad))
            return false;

        // 遮挡：射线命中障碍物就看不见
        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        if (Physics.Raycast(eye, dir, to.magnitude, obstacleMask))
            return false;

        return true;
    }

    void FaceTarget(Vector3 target)
    {
        Vector3 dir = target - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        transform.rotation = Quaternion.Slerp(
            transform.rotation, Quaternion.LookRotation(dir), 8f * Time.deltaTime);
    }
}
```

### 3.4 四个不能省的设计点

1. **切换统一走 `ChangeState`，不在状态里直接赋 `state = xxx`。**
   Enter 里改了全局状态（`agent.speed` / `isStopped` / 动画参数），Exit 必须复位。这个成对关系只有集中在一处才管得住。典型事故：追击里把 speed 设成 4，回巡逻忘了还原，保安以跑步速度散步。

2. **`if (state == next) return;` 是必需的幂等判断。**
   否则同帧可能 Enter/Exit 来回几次，动画和音效重复触发。

3. **感知降频到 10Hz。**
   单次 `Raycast` + 点乘不贵，但 20 个保安 × 60 帧 = 每秒 1200 次射线。**感知精度需求远低于渲染**，10Hz 完全够。

4. **追击的退出条件有两个。**
   只有「看不见」的话，玩家跑出地图也照样追。加一条「离出生点超过 `loseDistance` 就放弃」，是关卡设计里的硬需求。

### 3.5 等价的树结构（对照用）

```
Root (Selector)
├─ Sequence 追击
│   ├─ Condition 看见玩家?
│   ├─ Action    更新目标点
│   └─ Action    移动到目标点
├─ Sequence 警戒
│   ├─ Condition 刚发现玩家?
│   └─ Action    原地观察 1.5s
└─ Sequence 巡逻
    ├─ Action 选下一个路点
    └─ Action 移动到路点
```

功能一模一样，但要多写：`BTNode` 基类、`Selector`、`Sequence`、`Condition/Task` 包装、`Blackboard`、递归 tick 调度、可视化编辑器。

---

## 四、什么时候该换成行为树

四个信号，出现任意一个就该换：

1. **行为超过 8~10 个**，开始出现「同一时刻做两件事」（边巡逻边检查门锁）
2. **需要跨角色复用**：保安 / 平民 / 敌人共用「逃跑」「呼救」子树
3. **策划要自己改 AI**，不能每次找程序改代码
4. **多人并行加行为**，FSM 的转移表天天冲突

### 工业界的真实做法是混用

> 行为树管高层决策（该追击还是该巡逻），叶子节点内部的具体流程（开门的位移 → 动画 → 锁解除）还是一个小 FSM。

**BT 擅长「选」，FSM 擅长「执行一段有明确阶段的流程」**，两者不是替代关系。

---

## 五、面试答题模板

> "我选 FSM。这个需求只有 4 个状态，而且转移几乎是一条单向链——巡逻、警戒、追击、返回，没有交叉，也没有组合行为。
>
> 用枚举加 switch 实现，零框架代码；用行为树就得先写节点基类、Selector、Sequence、Blackboard 和 tick 调度，两百行起步，功能一点没多，只是多了扩展性——而这个规模用不上扩展性。
>
> 但我知道什么情况该换：行为超过十个、出现正交组合需求、或者策划要自己在编辑器里改 AI 的时候，行为树的成本和收益就倒过来了。真实项目里常见的是混用——行为树做高层选择，叶子节点内部还是小状态机。"

**主动说出换型条件，比只答「FSM 简单」高一个档次**——它证明你是按规模选方案，不是按「哪个更高级」选。

---

## 六、面试延伸

1. **「状态爆炸了怎么办？」** → HFSM（父状态持子状态机）或拆成并行的移动状态机 + 行为状态机
2. **「行为树怎么实现打断？」** → Reactive Selector 每 tick 重判全部子节点条件；普通 Sequence 会锁定 Running 节点，不打断
3. **「行为树为什么不每帧从根重跑？」** → 会丢失 Running 节点的进度；tick 频率与渲染解耦时（比如 10Hz）更容易暴露
4. **「GOAP 与它们的关系？」** → FSM/BT 是「人把逻辑写死」，GOAP 是「人写动作与目标，运行时规划路径」；适合目标组合爆炸的场景，但代价是行为不可预测、难调

---

## 相关

- [[有限状态机FSM — 手写实现与状态切换边界]] —— FSM 的三层骨架（IState / 状态基类 / 状态机注册表）、Exit→换ID→Enter 的顺序理由、状态切换边界五要素，本篇的选型结论建立在它的实现细节上
- [[Physics Raycast 与 NonAlloc]] —— 本篇 `CanSeePlayer` 里的遮挡检测，以及 NonAlloc 在多点检测时的用法
