---
title: "CharacterController移动 — Move vs SimpleMove"
type: resource
tags: [unity, 角色控制器, 移动, 实战坑]
created: "2026-08-17"
updated: "2026-08-17"
status: active
summary: "Move（位移/帧+手动重力）vs SimpleMove（速度/秒+自动重力）对比、斜坡 Slope Limit 与法线投影处理、选型建议"
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

## 关联

- [[Update-FixedUpdate-LateUpdate执行时机]]（Move 放 Update，物理固定步长不适用）
- [[VR物理冲击力 — AddExplosionForce vs 自定义定向力]]
