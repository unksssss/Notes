---
title: "Unity Input System 使用指南"
type: resource
tags: [unity, Input System, 输入, XR]
created: "2026-06-17"
updated: "2026-06-17"
status: active
summary: "Unity 新版 Input System 完整使用指南，从安装、创建Action、绑定按键到代码读取输入。"
source: ""
related: [["XR Interaction Toolkit 配置指南"]]
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

