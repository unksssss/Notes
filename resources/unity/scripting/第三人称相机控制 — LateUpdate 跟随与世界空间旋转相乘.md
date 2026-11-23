---
title: "第三人称相机控制 — LateUpdate 跟随与世界空间旋转相乘"
type: resource
tags: [unity, 相机, input, 第三人称, scripting]
category: scripting
created: "2026-11-23"
updated: "2026-11-23"
status: active
summary: "第三人称相机的三层结构（PlayerCamera 承担 yaw / Main Camera 子节点承担 pitch）+ 四个真实坑：① `cameraTransform.rotation *= Quaternion.Euler(-dy,0,0)` 乘的是**世界 X 轴**，玩家 yaw 转开后上下看会变成翻滚（roll）而非俯仰 → 必须用 `localRotation` 或绕 `transform.right`；② 俯仰无 clamp，累计到 ±90° 画面倒转；③ 枢轴取 `transform.position`（脚底）导致视线贴地，要加头部偏移；④ 跟随写 `LateUpdate` 且要加阻尼，否则 CharacterController 贴地修正会传导成画面抖动。另含 Input System 的 Look 增量不该乘 `Time.deltaTime`、`Cursor.lockState` 必设、相机碰撞 SphereCast 收线。"
source: "Empty 2026-11-23 17:10 Player.cs 相机/输入重构"
related:
  - "[[Update-FixedUpdate-LateUpdate执行时机]]"
  - "[[Unity Input System 使用指南]]"
  - "[[CharacterController移动 — Move vs SimpleMove]]"
  - "[[Physics Raycast 与 NonAlloc]]"
---

# 第三人称相机控制 — LateUpdate 跟随与世界空间旋转相乘

## 一、先分清「谁转 yaw、谁转 pitch」

第三人称相机拆成**两个 Transform**，各管一个自由度，是这类控制器的通用骨架：

```
Player（CharacterController，只转 yaw）
└─ PlayerCamera（根，位置/朝向跟人 —— 承担 yaw）
   └─ Main Camera（子节点，只承担 pitch）
```

```csharp
// Player.cs
private Transform playerCamera;   // "PlayerCamera"
private Transform cameraTransform; // "PlayerCamera/Main Camera"

private void LateUpdate()
{
    playerCamera.position = transform.position;
    playerCamera.rotation = transform.rotation;
}

private void OnMouseMove(InputAction.CallbackContext ctx)
{
    float mouseX = ctx.ReadValue<Vector2>().x;
    float mouseY = ctx.ReadValue<Vector2>().y;
    transform.rotation *= Quaternion.Euler(0, mouseX * EulerSpeed, 0);          // yaw 给人
    cameraTransform.rotation *= Quaternion.Euler(-mouseY * EulerSpeed, 0, 0);  // pitch 给相机
}
```

分工是对的，但下面四行代码各自埋了一个坑。

---

## 二、坑一：`rotation *=` 乘的是世界轴，不是相机自己的右方向

`Transform.rotation` 是**世界空间**的四元数。`cameraTransform.rotation *= Quaternion.Euler(-dy, 0, 0)` 的语义是「在**世界 X 轴**上再转 -dy 度」。

玩家 yaw = 0 时，世界 X 轴恰好等于相机右方向，看不出问题；一旦玩家转身 90°，世界 X 轴变成相机的**前后轴**，此时鼠标上下移动：

- 期望：俯仰（抬头/低头）
- 实际：**画面翻滚（roll）**，同时把已经积累的 pitch 甩到别的轴上

这就是「转个身以后视角就歪了、越看越歪」的根因，很多人误以为是四元数精度问题。

**正解**——用局部空间相乘，或者显式绕相机自己的右轴：

```csharp
// 写法 A：局部空间（父节点只承担 yaw，子节点局部 X 恒等于相机右方向）
cameraTransform.localRotation *= Quaternion.Euler(-mouseY * EulerSpeed, 0, 0);

// 写法 B：显式绕自身右轴（世界空间但轴跟着相机走）
cameraTransform.rotation *= Quaternion.AngleAxis(-mouseY * EulerSpeed, cameraTransform.right);
```

> 为什么 A 一定成立：父节点 `playerCamera` 的旋转每帧被覆盖成**纯 yaw**（`transform.rotation` 里没有 pitch/roll），所以子节点 `localRotation` 的 X 轴 = 父的 right = 相机的 right，二者正交、永不耦合。

---

## 三、坑二：俯仰没有 clamp

欧拉角 pitch 累计到 ±90° 就翻到背面，超过 90° 画面上下颠倒，表现为「相机突然翻个个儿」。第三人称一般夹在 ±70°~±80°：

```csharp
private float pitch;

private void OnMouseMove(InputAction.CallbackContext ctx)
{
    Vector2 d = ctx.ReadValue<Vector2>();
    transform.rotation *= Quaternion.Euler(0, d.x * EulerSpeed, 0);
    pitch = Mathf.Clamp(pitch - d.y * EulerSpeed, -80f, 80f);
    cameraTransform.localRotation = Quaternion.Euler(pitch, 0, 0);   // 直接赋值，天然不累积漂移
}
```

用「存一个 `pitch` 标量、每帧直接赋值」比「`*= ` 累积」更稳：既 clamp 得住，也不会因为浮点累积产生缓慢 roll 漂移。

---

## 四、坑三：枢轴取在脚底

`playerCamera.position = transform.position` 里，`transform.position` 是人物的**根节点 pivot**，低模角色通常是**脚底**（y = 0）。相机枢轴贴地 → 视线低、贴地穿模、抬头时人挡镜头。

```csharp
[SerializeField] private Vector3 pivotOffset = new Vector3(0, 1.6f, 0);  // 或取 head bone
playerCamera.position = transform.position + transform.rotation * pivotOffset;
```

---

## 五、坑四：跟随要放 LateUpdate，且要阻尼

**为什么必须是 LateUpdate**：角色位移在 `Update` 里（`characterController.Move`）。若相机也在 `Update` 里跟，两个脚本的 `Update` 顺序不确定 → 相机可能读到**上一帧**的位置，画面抖一帧；`LateUpdate` 保证在所有 `Update` 之后，读到的永远是本帧最终位置。详见 📎 [[Update-FixedUpdate-LateUpdate执行时机]]。

**为什么还要阻尼**：`CharacterController.Move` 会做贴地/斜坡修正，位置里有小台阶；硬赋值会把台阶一比一传给相机。加 `SmoothDamp` 抹掉高频抖动：

```csharp
private Vector3 camVel;

private void LateUpdate()
{
    Vector3 target = transform.position + transform.rotation * pivotOffset;
    playerCamera.position = Vector3.SmoothDamp(playerCamera.position, target, ref camVel, 0.05f);
    playerCamera.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);  // 只跟 yaw，别跟 pitch
}
```

注意最后一行：`playerCamera.rotation` **只跟 yaw**。若照抄 `transform.rotation` 而 Player 因为地形/斜坡带了 pitch，会把俯仰二次叠加到相机上。

---

## 六、鼠标输入的量纲

| 来源 | 含义 | 要不要乘 `Time.deltaTime` |
| --- | --- | --- |
| Input System `Look`（`ReadValue<Vector2>()`） | **本帧鼠标位移增量** | ❌ 不要。乘了会「帧率越高转得越慢」 |
| 旧 Input Manager `Input.GetAxis("Mouse X")` | 已乘 dt 的伪增量（约 `delta * 0.1`） | ❌ 不要（内部已处理） |

所以从旧输入系统迁到 Input System 时，**灵敏度系数不能照抄**——旧系统的 `GetAxis` 已经缩放过一次，直接搬 `mouseX * 0.1f` 会变得极慢或极快。

另外两件必做的小事：

```csharp
Cursor.lockState = CursorLockMode.Locked;   // 否则鼠标移出窗口就转不动
Cursor.visible = false;                     // 与 lockState 配套
```

`CursorLockMode.Locked` 下鼠标仍产生 delta（只是不移动光标），这是第一人称/第三人称视角的标准做法；`Confined` 会把光标卡在窗口内、到边就不产生 delta，手感是「转到边就停」。

---

## 七、相机碰撞（第三人称必备）

第三人称相机在枢轴**后方**，贴墙时一定会穿模。标准做法是从枢轴向相机方向打 `SphereCast`，命中就把相机拉到命中点前方：

```csharp
private void ResolveCameraCollision(Vector3 pivot, Vector3 desiredPos, float radius = 0.2f)
{
    Vector3 dir = desiredPos - pivot;
    float dist = dir.magnitude;
    if (Physics.SphereCast(pivot, radius, dir.normalized, out var hit, dist, ~LayerMask.GetMask("Player")))
        cameraTransform.position = pivot + dir.normalized * Mathf.Max(hit.distance - 0.05f, 0.3f);
    else
        cameraTransform.position = desiredPos;
}
```

用 `SphereCast` 而不是 `Raycast`：射线只保证「中心点不穿」，镜头边缘照样会切进墙里。详见 📎 [[Physics Raycast 与 NonAlloc]]。

---

## 八、必须记住的点

1. `Transform.rotation` 是世界空间，`localRotation` 是父空间——**乘哪个决定了绕哪根轴转**。
2. 两级 Transform 各管一个自由度，是本类控制器的骨架；pitch 一定要 clamp。
3. 相机跟人放 `LateUpdate`（读本帧最终位置），且只跟 yaw。
4. Input System 的鼠标增量不乘 dt；旧输入系统的 `GetAxis` 也不乘 dt，但两者量纲不同。
5. 第三人称的相机位置是「算出来的」不是「直接赋值的」——`SmoothDamp` + `SphereCast` 两件事都得做。

## 相关

- [[Update-FixedUpdate-LateUpdate执行时机]] —— 为什么跟随必须在 LateUpdate，以及脚本执行顺序的坑
- [[Unity Input System 使用指南]] —— `InputAction.CallbackContext`、performed/canceled 与 `ReadValue`
- [[CharacterController移动 — Move vs SimpleMove]] —— 角色位移产生的台阶从哪来
- [[Physics Raycast 与 NonAlloc]] —— SphereCast 与 LayerMask 的用法
