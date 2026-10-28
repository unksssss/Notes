---
title: Unity Input System 使用指南
type: resource
tags:
  - unity
  - 输入
  - XR
created: 2026-06-17
updated: 2026-10-28
status: active
summary: Unity 新版 Input System 完整使用指南：安装、创建 Action、绑定按键、三种读取方式；含 Value action 的 performed/canceled 触发语义与「回调 vs 轮询」取舍（实战踩坑：键盘松手不归零）。
source: ""
related: ["[[unity-inputsystem详解]]", "[[UGUI 事件系统与射线检测链路]]"]
---

# Unity Input System 使用指南

新版 Input System 替代了旧的 `Input.GetKey()`，核心概念：**Action → 绑定按键 → 代码读取 Action 值**。

---

## 安装

```
Window → Package Manager → Unity Registry → 搜 Input System → Install

弹出提示是否切换 → 点 Yes（重启编辑器）
```

---

## 三步走

### 第一步：创建 Input Action Asset

```
Project 右键 → Create → Input Actions
命名 "PlayerInputActions"（或 "XRInputActions"）

双击打开 → 看到编辑窗口：
  ├── Action Maps（分组，如 "Gameplay"、"UI"、"XR"）
  │   └── Actions（具体动作）
  │       └── Bindings（按键绑定）
```

### 第二步：添加 Action + 绑定按键

```
选中一个 Action Map → 点 + 号添加 Action:
  Action Type: Value（摇杆/轴）/ Button（按键）/ Pass Through（透传）

例如：
  Move → Action Type: Value, Control Type: Vector2
    └── Binding: Path → 右键 → 选 WASD 或 Left Stick

  Fire → Action Type: Button
    └── Binding: Path → 右键 → 选 Mouse Left Button 或 Trigger

  Look → Action Type: Value, Control Type: Vector2
    └── Binding: Path → Mouse Delta 或 Right Stick
```

### 第三步：生成 C# 类

```
选中 Input Action Asset → Inspector:
  ✅ Generate C# Class
  → Apply

会生成 PlayerInputActions.cs
```

---

## 代码读取输入

### 方式一：直接读（Update 里）

```cs
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private PlayerInputActions _input;

    void Awake()
    {
        _input = new PlayerInputActions();
    }

    void OnEnable()
    {
        _input.Gameplay.Enable();  // Gameplay 是 Action Map 名
    }

    void OnDisable()
    {
        _input.Gameplay.Disable();
    }

    void Update()
    {
        Vector2 move = _input.Gameplay.Move.ReadValue<Vector2>();
        transform.Translate(new Vector3(move.x, 0, move.y) * Time.deltaTime * 5f);

        if (_input.Gameplay.Fire.WasPressedThisFrame())
        {
            Debug.Log("Fire!");
        }
    }
}
```

### 方式二：事件回调

```cs
void OnEnable()
{
    _input.Gameplay.Fire.performed += OnFire;
    _input.Gameplay.Enable();
}

void OnDisable()
{
    _input.Gameplay.Fire.performed -= OnFire;
    _input.Gameplay.Disable();
}

void OnFire(InputAction.CallbackContext ctx)
{
    Debug.Log($"Fire! phase={ctx.phase} value={ctx.ReadValue<float>()}");
}
```

| phase | 时机 |
|---|---|
| `performed` | 按键完成（按下、阈值到达） |
| `started` | 按键开始 |
| `canceled` | 按键松开 |

### 方式三：Player Input 组件（最省事）

```
GameObject 挂 PlayerInput 组件:
  Actions: 拖 Input Action Asset
  Behavior: Send Messages / Invoke Unity Events / Invoke C Sharp Events

脚本里写方法名匹配：
  void OnMove(InputValue value) { ... }    // Action 名 = Move
  void OnFire(InputValue value) { ... }    // Action 名 = Fire
```

不需要手动 Enable/Disable，PlayerInput 自动管理。

---

## Action Type 说明

| Type | 用途 | 返回值 |
|---|---|---|
| **Value** | 持续读取值 | 摇杆 XY、鼠标位置 |
| **Button** | 按键按下/松开 | pressed、wasPressed、wasReleased |
| **Pass Through** | 原样穿透 | 不处理，直接传给下一级 |

---

## Value Action 的回调语义（最容易踩的坑）

上面「方式二：事件回调」的相位表是按 **Button** 写的。同一个 `performed`，落在 **Value** action 上语义完全不同：

| Action Type | `performed` 何时触发 | `canceled` 何时触发 |
| --- | --- | --- |
| **Button** | 按下（越过 press point） | 松开 |
| **Value** | 值**从 `default` 变为非 default** 时，**只触发一次** | 值**回到 default** 时触发一次 |
| **PassThrough** | 值每帧变化都触发 | 值回到 default |

关键在第二行：Value action 在**非零区间内改值不会再触发 `performed`**。WASD 从 A 换到 D、从 W 加按 A（斜向），都只是「非零 → 另一个非零」，回调不会再来一次。

### 典型故障：只订 performed，松手后值不归零

```cs
// ❌ 只订一半相位
input.Player.Move.performed += OnKeyboardMove;

void OnKeyboardMove(InputAction.CallbackContext ctx)
{
    moveDirection = ctx.ReadValue<Vector2>();   // 松手后再也没人来改这个字段
}
```

表现：按下能走、换向失灵、**松手后角色继续朝最后方向滑**。`moveDirection` 永远停在最后一次非零值上。

```cs
// ✅ 补上 canceled
input.Player.Move.performed += OnKeyboardMove;
input.Player.Move.canceled  += OnKeyboardMove;

void OnKeyboardMove(InputAction.CallbackContext ctx)
{
    // canceled 相位下 ReadValue 会返回 default，但显式写更不容易被后人改坏
    moveDirection = ctx.canceled ? Vector2.zero : ctx.ReadValue<Vector2>();
}
```

### 更推荐：Value action 直接轮询

```cs
void Update()
{
    Vector2 move = input.Player.Move.ReadValue<Vector2>();   // 每帧现读，天然归零
    ...
}
```

轮询不需要关心相位语义：松手那一帧读到的就是 `(0,0)`，换向那一帧读到新值，不存在「漏触发」。**心智模型：Button 用回调（关心边沿），Value 用轮询（关心当前状态）。**

> 补充：`initialStateCheck: true` 的 Value action 在 action 被 Enable 时就会立刻发一次 `performed`（如果当前已有非零值），这也是为什么「只订 performed」在编辑器里偶尔看起来是对的——按住键再进 Play，第一次能对上。

### 相关坑：多输入源共用一个方向字段

键盘和虚拟摇杆同时写 `moveDirection` 时，常见写法是加一个 `isJoyStickControl` 标志。标志的**置位条件**必须是「值是否为零」，而不是「谁调用了回调」：

```cs
// ❌ 摇杆松手（value 为零）也会进摇杆模式，之后键盘分支全被跳过
void OnJoyStick(Vector2 value) { isJoyStickControl = true; ... }

// ✅ 松手即退出摇杆模式
void OnJoyStick(Vector2 value) { isJoyStickControl = value != Vector2.zero; ... }
```

否则会形成死锁：置 `true` 在前、复位在后，而复位所在的分支本身又被 `true` 挡住 → 第一次按键无效。

📎 相关：[[UGUI 事件系统与射线检测链路]]

---

## Control Type（期望的输入类型）

| Control Type | 适用绑定 |
|---|---|
| Vector2 | WASD、摇杆、鼠标移动 |
| Axis | 单轴（扳机、滚轮） |
| Button | 按键、鼠标点击 |
| Stick | 手柄摇杆 |
| Dpad | 十字键 |

---

## 常见绑定速查

| 操作 | Action Type | Control Type | 绑什么 |
|---|---|---|---|
| 移动 | Value | Vector2 | WASD + 左摇杆 |
| 视角 | Value | Vector2 | Mouse Delta + 右摇杆 |
| 跳跃 | Button | — | Space + 手柄A |
| 扳机 | Value | Axis | Trigger（轴值 0~1） |
| 抓取 | Button | — | Grip |
| 交互 | Button | — | Right Trigger |

---

## VR / XR 场景

Input System 配合 XR Interaction Toolkit 时，**不需要手写 Input Action 代码**——XRIT Starter Assets 自带完整的 Input Action Asset。

需要做的只是：

```
XR Origin → XR Interaction Manager → 拖 XRIT Starter Assets 的 Input Action Asset
手柄 → XR Controller → 自动绑定
```

---

## 旧 Input vs 新 Input

| 旧 (`Input.GetKey`) | 新 (Input System) |
|---|---|
| 硬编码按键 | Input Action Asset 可视化配置 |
| 不支持热插拔手柄 | 自动检测新设备 |
| 每个平台写一遍 | 一套 Action 多平台 |
| `Input.GetAxis` | `ReadValue<Vector2>()` |

---

