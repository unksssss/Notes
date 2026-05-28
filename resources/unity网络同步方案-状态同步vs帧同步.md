---
title: "Unity网络同步方案_状态同步vs帧同步"
type: resource
tags: [unity, 网络同步, 工业仿真]
created: "2026-04-29"
updated: "2026-04-29"
status: active
summary: "工业数字孪生场景中两种网络同步方案的对比与选型"
source: ""
related:
- "[[unity知识点-2026-04-29]]"
- "[[dots详解]]"
---

# Unity 网络同步方案：状态同步 vs 帧同步

## 1. 一句话解释

- **状态同步**：服务器（或权威端）每帧计算所有实体的最终状态（位置、旋转、速度），广播给所有客户端。客户端只管渲染。
- **帧同步（Lockstep）**：所有客户端接收相同的输入指令，各自独立做完全一致的逻辑计算，保证结果相同。

## 2. 为什么需要它

工业数字孪生/仿真中经常遇到这类需求：
- 多个客户端同时监控一台真实设备的运行状态（数据来自 PLC）
- 多个操作员协同操控一台虚拟机械臂
- 分布式产线仿真，多个工位需要看到同一套数据

没有好的同步方案，就会出现：
- 各客户端看到的数据不一致
- 操作不同步导致碰撞异常
- 带宽压力过大，延迟高

## 3. 基本原理

### 状态同步（State Synchronization）

```
服务器（权威端）
    │
    ├─ 每帧计算物理/逻辑
    ├─ 序列化所有实体状态
    ├─ 广播给所有客户端
    │
    ▼
客户端 A ──── 客户端 B ──── 客户端 C
    │             │             │
    ├─ 接收状态    ├─ 接收状态    ├─ 接收状态
    ├─ 插值渲染    ├─ 插值渲染    ├─ 插值渲染
    └─ 不参与计算  └─ 不参与计算  └─ 不参与计算
```

**核心特点：**
- 权威端负责所有逻辑
- 客户端只管"画"，不管"算"
- 带宽占用 ≈ O(实体数 × 状态属性数)
- 可通过变化阈值压缩（只传变化超过阈值的数据）

### 帧同步（Lockstep / Deterministic）

```
所有客户端接收相同输入
        │
        ├─ Input: {"id":"player1","type":"move","dir":"left"}
        ├─ Input: {"id":"player2","type":"rotate","angle":90}
        │
        ▼
客户端 A ──── 客户端 B
（确定性计算）  （确定性计算）
    │             │
    ├─ 物理模拟    ├─ 物理模拟
    ├─ 碰撞检测    ├─ 碰撞检测
    └─ 结果一致    └─ 结果一致
```

**核心特点：**
- 只传输输入指令，带宽极低
- 要求所有客户端确定性计算（相同的浮点运算、时间步长、随机数种子）
- 必须等待所有客户端的输入才能推进（延迟敏感）

## 3.5 帧同步深度解析：游戏开发视角

### 经典案例：格斗游戏

街霸、拳皇是帧同步的祖师爷。网络传输的只是纯输入指令：

```
帧 120：玩家1 → 轻拳
帧 121：玩家2 → 后跳
帧 122：玩家1 → 波动拳
```

每秒只传几个字节，两边各自跑一套完全一样的判定逻辑，结果天然一致。ArcSys 的 **GGPO（回滚式帧同步）** 至今是格斗游戏标配。

### 另一个经典：RTS 游戏

星际争霸、帝国时代也基于帧同步。400个单位同时移动，如果每个单位的位置都用状态同步去传，带宽直接爆炸。RTS 选帧同步的核心原因就是：**海量实体场景下，状态同步的带宽天花板打不住。**

星际 1 的逻辑帧率只有 8 帧/秒（125ms 一帧），所有玩家卡到这个节拍上同步推进，慢但稳。

### 帧同步的核心难题

**① 确定性（Determinism）**

同样的输入，在不同机器上必须得出完全一样的结果：

```csharp
// ❌ 不能用这些
float rand = Random.value;        // 随机种子不同
float dt = Time.deltaTime;        // 帧率不同导致步长不同
Physics.Raycast(...);             // 物理引擎结果跨平台不一致
Mathf.Sin(angle);                 // 不同CPU的Sin结果有细微差异

// ✅ 必须自己实现确定性数学库
public class DeterministicRandom
{
    private ulong seed;
    public float Next()
    {
        seed = seed * 1103515245 + 12345;
        return (seed >> 16) & 0x7FFF;
    }
}
```

**② 网络延迟 = 游戏卡顿**

帧同步必须等所有玩家的输入才能推进下一帧。一个人延迟 200ms，所有人等他。

三种主流应对方案：

| 方案 | 做法 | 代表游戏 |
|------|------|---------|
| **Lockstep（经典）** | 死等所有输入，没到就不动 | 帝国时代（局域网） |
| **Time Warp / 乐观锁** | 不等，预测推进，收到旧输入后回滚重算 | 早期 RTS |
| **GGPO / 回滚** | 预测 + 网络层回滚，用户感知不到延迟 | 街霸5、罪恶装备 |

**③ 断线处理**

一个人掉线游戏直接卡死。常见策略：
- 超时机制：超过 N 帧收不到输入，用"空操作"或"复读最后一帧"填充
- 重放加入：新玩家需要快进运行整个历史输入才能加入对局

### Unity 帧同步骨架

```csharp
public class LockstepManager : MonoBehaviour
{
    public const int TICK_RATE = 20;                // 20 逻辑帧/秒
    public const float TICK_INTERVAL = 1f / TICK_RATE;
    private int currentTick;
    private float timer;
    private Dictionary<int, List<PlayerInput>> inputBuffer = new();

    void FixedUpdate()
    {
        timer += Time.fixedDeltaTime;
        while (timer >= TICK_INTERVAL)
        {
            timer -= TICK_INTERVAL;
            // 检查所有玩家的当前帧输入是否到齐
            if (HasAllInputs(currentTick))
            {
                SimulateTick(currentTick, inputBuffer[currentTick]);
                currentTick++;
            }
            else
            {
                // 输入没到齐，不能推进 —— 这就是"卡"的来源
                Debug.Log("Waiting for inputs...");
                break;
            }
        }
    }

    void SimulateTick(int tick, List<PlayerInput> inputs)
    {
        foreach (var input in inputs)
        {
            // ✅ 必须纯数值计算，不依赖 Unity 非确定性 API
            entityPositions[input.playerId] += input.moveDir * TICK_INTERVAL;
        }
    }
}
```

### 什么样的游戏适合/不适合帧同步

| 适合 | 不适合 |
|------|--------|
| RTS（一堆单位，带宽敏感） | FPS（射击手感依赖即时反馈） |
| 格斗（判定精确到帧） | MOBA（300ms 延迟回滚不如状态同步平滑） |
| 赛车（确定性物理） | 开放世界（状态太多，确定性难保证） |
| 回合制 | 物理复杂的休闲游戏 |

> **注：** MOBA 类（LOL、DOTA）早期也是帧同步，后来全面转向状态同步 + 插值，因为玩家对延迟的容忍度极低且网络环境复杂。

### 工业仿真 vs 游戏场景的差异

| 维度 | 游戏（格斗/RTS） | 工业仿真 |
|------|-----------------|---------|
| 同步强度 | 必须完全一致 | 允许微小差异 |
| 延迟代价 | 卡一帧就想砸键盘 | PLC 几十毫秒波动无所谓 |
| 实体数量 | 几百个单位 | 几台设备 |
| 物理复杂度 | 碰撞/刚体/流体 | 预定轨迹运动 |

工业仿真用状态同步就足够了。帧同步是游戏领域为了应对**海量实体、精确判定、超低带宽**被逼出来的方案。

## 4. 基础用法

### 状态同步简单示例（Mirror 网络库）

```csharp
// 服务器端：每帧广播状态
public class SyncState : NetworkBehaviour
{
    [SyncVar] public Vector3 position;
    [SyncVar] public Quaternion rotation;
    [SyncVar] public float temperature; // PLC 温度数据

    void Update()
    {
        if (isServer)
        {
            // 从 PLC 读取数据，同步给客户端
            temperature = ReadFromPLC();
        }
    }
}

// 客户端：插值显示
void Update()
{
    if (isClient)
    {
        transform.position = Vector3.Lerp(
            transform.position, position, Time.deltaTime * 10f
        );
    }
}
```

### 帧同步基础结构

```csharp
// 输入帧数据结构
[Serializable]
public struct InputFrame
{
    public int tick;
    public int playerId;
    public float horizontal;
    public float vertical;
    public bool action;
}

// 确定性逻辑更新（所有客户端必须一致）
public class DeterministicSimulation
{
    public void Tick(InputFrame[] inputs, float fixedDeltaTime)
    {
        // ⚠️ 不能使用 UnityEngine.Random
        // ⚠️ 不能使用 Physics.Raycast（结果可能不一致）
        // ⚠️ 不能依赖 Time.deltaTime
        // ✅ 使用固定时间步长 FixedUpdate
        foreach (var input in inputs)
        {
            // 确定性物理/逻辑更新
        }
    }
}
```

## 5. 进阶用法（工业仿真场景）

### 场景 A：PLC 数据监控（10+ 客户端）

推荐：**状态同步**

```csharp
// 优化：只传变化超过阈值的数据，降低带宽
public struct PLCState
{
    public float jointAngle1;
    public float jointAngle2;
    public float motorTemp;
}

public class PLCSync
{
    private PLCState lastState;
    private const float ANGLE_THRESHOLD = 0.1f;  // 角度变化阈值
    private const float TEMP_THRESHOLD = 0.5f;    // 温度变化阈值

    public PLCState GetDelta(PLCState current)
    {
        PLCState delta = current;
        // 未变化的字段标记为 float.NaN，接收端跳过更新
        if (Mathf.Abs(current.jointAngle1 - lastState.jointAngle1) < ANGLE_THRESHOLD)
            delta.jointAngle1 = float.NaN;
        // ...
        return delta;
    }
}
```

### 场景 B：协同操控虚拟机械臂（2 客户端 + 物理交互）

推荐：**权威状态同步 + 本地预测**（混合方案）

```
服务器（权威端）
    ├─ 运行物理模拟（Unity Physics / DOTS Physics）
    ├─ 接收客户端 A、B 的操作指令
    ├─ 每帧将物理结果广播
    ▼
客户端 A ──────────── 客户端 B
    ├─ 渲染服务器状态    ├─ 渲染服务器状态
    ├─ 本地轻量级预测    ├─ 本地轻量级预测
    └─ 收到权威状态后    └─ 收到权威状态后
       纠偏平滑           纠偏平滑
```

**为什么不建议纯帧同步：**
- 网络抖动时，帧同步会卡住等待输入
- 工业设备对实时性要求极高，卡顿可能触发安全风险
- 浮点确定性在跨平台（Windows + Linux 工控机）极难保证

## 6. 面试题答案

**题目：** 在分布式工业仿真系统中（多个客户端共同监控或操控），请对比状态同步和帧同步的优缺点，为以下两个场景选择方案并说明理由：
1. 10个客户端监控PLC设备数据，仅可视化
2. 2个客户端协同操控虚拟机械臂

**答案要点：**

| 维度 | 状态同步 | 帧同步 |
|------|---------|--------|
| 带宽 | 大（传完整状态） | 极小（只传输入） |
| 实现难度 | 简单 | 极高 |
| 确定性要求 | 不需要 | 必须严格保证 |
| 延迟容忍度 | 高（可插值） | 低（必须等） |
| 服务器压力 | 大（每帧计算） | 小（只转发输入） |
| 物理一致性 | 弱（各客户端可不同） | 强（完全一致） |

- **场景A（监控）** → 状态同步。不参与逻辑，只需展示，带宽可通过变化阈值压缩。
- **场景B（协同操控）** → 权威状态同步 + 本地预测。工业场景对可靠性要求高于一致性，帧同步的卡顿风险不可接受。

## 7. 踩坑记录

- Unity Physics 结果在不同平台不完全一致，不要依赖同一帧的物理计算结果做帧同步
- `Time.timeScale` 在不同客户端可能不同，帧同步必须使用独立计时器
- Mirror 的 `[SyncVar]` 不能同步 `Quaternion` 用 `Vector4` 替代或自定义序列化
- 工业仿真中 PLC 数据的采样频率通常 < 100Hz，比游戏同步简单得多，不要过度设计

## 相关笔记

- [[dots详解]] — DOTS Physics 可用于服务器端物理模拟
- [[unity知识点-2026-04-29]]
