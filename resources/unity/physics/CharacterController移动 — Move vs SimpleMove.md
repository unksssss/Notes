---
title: "CharacterController移动 — Move vs SimpleMove"
type: resource
tags: [unity, 角色控制器, 移动, 实战坑]
created: "2026-08-17"
updated: "2026-11-17"
status: active
summary: "Move（位移/帧+手动重力）vs SimpleMove（速度/秒+自动重力）对比、斜坡 Slope Limit 与法线投影处理、选型建议。附「用 CC 手写巡逻」的四个坐标坑：Vector3.Distance 是三维度（巡逻点 y 被 spawn 高度冻结 → 到达判定永假 → 状态机卡死、固定方向直走）、Move(Vector3.down) 未乘 dt（参数是位移不是速度）、LookRotation 未去 y 分量（有高差就仰头）、朝向 Lerp 而位移用原始 Dir（转向期间侧滑）；并给出超时兜底与「先看状态机 → 再看坐标 → 最后看方向」的排查顺序。"
---

# CharacterController 移动 — Move vs SimpleMove

两个 API 长得像，但**参数语义、重力、返回值**全不一样。

## 核心对比表

| | `Move( motion )` | `SimpleMove( speed )` |
|---|---|---|
| **参数含义** | **位移向量**（米/帧）→ 必须自己乘 `Time.deltaTime` | **速度向量**（米/秒）→ 引擎内部自动乘 dt |
| **重力** | ❌ 不自动，手动：`velocity.y += Physics.gravity.y * Time.deltaTime` | ✅ 自动应用（传入的 y 分量会被忽略） |
| **返回值** | `CollisionFlags`（枚举：Sides/Above/Below，判断撞哪边） | `bool`（是否着地） |
| **碰撞/斜坡** | 完整碰撞响应，Slope Limit 生效 | 完整碰撞响应，Slope Limit 生效 |
| **适用场景** | 跳跃、自定义重力、精细控制（玩家角色） | 简单行走 NPC（自动走路） |

## 典型写法

```csharp
// Move：手动重力 + 乘 dt
public class PlayerMove : MonoBehaviour
{
    public CharacterController controller;
    public float speed = 5f;
    public float jumpHeight = 2f;
    Vector3 velocity;

    void Update()
    {
        // 1. 输入（水平方向）
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;

        // 2. 重力（手动！每帧累加）
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;              // 贴地小下压力，防止悬空抖动
        velocity.y += Physics.gravity.y * Time.deltaTime;

        // 3. 位移 = (水平速度 + 垂直速度) × dt
        controller.Move((move * speed + Vector3.up * velocity.y) * Time.deltaTime);

        // 4. CollisionFlags 可判断碰撞方向
        if ((controller.collisionFlags & CollisionFlags.Sides) != 0)
            Debug.Log("撞到侧面了");
    }
}

// SimpleMove：自动重力，只传水平速度，不用乘 dt
controller.SimpleMove(new Vector3(speedX, 0, speedZ)); // 返回 bool 着地
```

**一句话记**：`Move` = 位移 + 你管重力；`SimpleMove` = 速度 + 引擎管重力。

## 斜坡处理要点（老考题合集）

- **Slope Limit**（默认 45°）：超过该角度的斜坡会被当墙——`Move` 沿斜面方向会被阻挡。要爬更陡的坡就把 Slope Limit 调大，但超过 60° 物理就不真实了
- **isGrounded 的坑**：斜坡上 `Move` 位移是"水平位移+垂直下落"的组合，站在斜坡上要按**斜坡法线投影**移动方向，否则会滑向山脚
- SimpleMove 自动重力 + 返回着地 bool，适合对斜坡无精细需求的场景

## 选型建议

- **玩家角色**（要跳、要自定义重力、要判断撞墙）→ `Move`
- **NPC/自动行走**（只要会走、会着地判定）→ `SimpleMove`
- 两者都走 CharacterController 的碰撞，不会穿透

## 用 CC 手写巡逻：四个必踩的坐标坑

> 出处：2026-11-17 `Empty` 项目敌人 AI。用 `CharacterController` + 固定 `targetDir` 写巡逻/追击时，这四个坑会互相掩盖，按下面的顺序排查。

### 坑一：`Vector3.Distance` 是三维度，平面判定必须拍平

最隐蔽的一个。生成点带高度（`+ Vector3.up * 5f` 从空中落下）时，第一帧算出的巡逻点会把 **y 一起冻结在空中高度**：

```cs
// ❌ 巡逻点 y 被 position.y 污染
targetPos = transform.position + targetDir * R;

// ✅ 压到水平面
private Vector3 SelfPos => new(transform.position.x, 0, transform.position.z);
targetPos = SelfPos + targetDir * R;
targetPos = new Vector3(targetPos.x, 0, targetPos.z);
if (Vector3.Distance(SelfPos, targetPos) < 1f) isPatrolling = true;
```

落地后 `Distance` 里恒定夹着 ~4m 的 Δy → **永远 ≥ 阈值** → 到达判定永假 → `isPatrolling` 不回 `true` → `targetDir` 永不刷新 → 每帧 `Move(targetDir)` 变成**固定方向直走**。

现象是「敌人一直朝一个方向走到地图边缘」，而代码里根本没有任何「追」的逻辑在跑 —— 排查时容易一头扎进 AI 决策层，实际是坐标层。

> 判据：凡是「平面寻路 / 平面巡逻 / 平面射程」的距离判定，先把 y 拍平再比。地形有高差时这个 bug 会变成「有时能到、有时永远到不了」，最难复现。

### 坑二：`Move(Vector3.down)` 没乘 `Time.deltaTime`

```cs
// ❌ 每帧固定下移 1 米，帧率越高掉得越快
if (!characterController.isGrounded) characterController.Move(Vector3.down);

// ✅
characterController.Move(Vector3.down * gravity * Time.deltaTime);
```

`Move` 的参数是**位移（米/帧）**不是速度（米/秒）—— 这是本 API 最常见的误用，60fps 和 144fps 下重力表现完全不同。

### 坑三：`LookRotation(Dir)` 没去 y 分量 → 有高差就仰头

```cs
// ❌ Dir 含 y，敌人会对着地面/天空"看"
transform.rotation = Quaternion.LookRotation(Dir);

// ✅ 只取水平朝向
Vector3 flat = Dir; flat.y = 0f;
if (flat.sqrMagnitude > 1e-4f)
    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flat), 0.1f);
```

### 坑四：朝向用 `Lerp` 后的 forward，位移却用原始 `Dir` → 侧滑

```cs
// ❌ 转向期间朝向还没转过去，位移已经朝新方向走了 → 横着平移
transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(Dir), 0.1f);
characterController.Move(Dir * speed * Time.deltaTime);

// ✅ 位移跟着朝向走，才是「先转向再前进」
characterController.Move(transform.forward * speed * Time.deltaTime);
```

### 附：状态机卡死的通用兜底

上面四个坑任意一个都能让「到达判定」永假。与其逐个排查，不如先加超时兜底，把「永久卡死」降级成「偶尔发呆」：

```cs
patrolTimer += Time.deltaTime;
if (patrolTimer > 6f) { isPatrolling = true; patrolTimer = 0f; }   // 强制重选点
```

**排查顺序建议**：先确认「状态机在不在跑」（加日志看 `isPatrolling` 是否翻转）→ 再看「坐标对不对」（打 `targetPos` 与 `SelfPos`）→ 最后才看「方向反没反」。方向 bug 会被前两类完全掩盖：判断和移动用的是同一个向量时，逻辑自洽，只是整体反了。

## 关联

- [[Update-FixedUpdate-LateUpdate执行时机]]（Move 放 Update，物理固定步长不适用）
- [[NavMeshAgent 移动控制 — SetDestination 与 transform 直写的冲突]]（手写巡逻卡死后，该不该换寻路的完整判断链）
- [[VR物理冲击力 — AddExplosionForce vs 自定义定向力]]
