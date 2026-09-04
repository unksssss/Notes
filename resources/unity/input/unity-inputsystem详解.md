---
title: "Unity InputSystem详解"
type: resource
tags: [unity, InputSystem, 输入系统]
created: "2026-04-29"
updated: "2026-04-29"
status: active
summary: "从面试题出发，由浅入深讲解 Unity Input System 的原理与应用"
source: ""
related: ["[[unity网络同步方案-状态同步vs帧同步]]"]
---

# Unity InputSystem 详解

## 1. 一句话解释

Input System 是 Unity 的新版输入系统，代替老旧的 `Input.GetAxis` / `Input.GetKeyDown`。它让你**用配置文件来声明按键映射**，而不是在代码里写死键位，手柄、键盘、触屏一套代码通吃。

## 2. 为什么需要它

**没有 Input System 的时候（旧版 `InputManager`）：**

```csharp
// 键位写死在代码里，改键位要改代码
if (Input.GetKeyDown(KeyCode.Space))
    player.Jump();

// 手柄和键盘要分别判断
float h = Input.GetAxis("Horizontal");       // 键盘
float h2 = Input.GetAxis("360_LeftStickX");  // 手柄
```

问题：

| 问题 | 说明 |
|------|------|
| 键位硬编码 | 策划想改键位→找程序员改代码→重新编译 |
| 多平台麻烦 | 手柄/键盘/触屏要写多套判断 |
| 没法自定义 | 玩家不能自由改键 |
| 不支持新设备 | 不支持触觉反馈、陀螺仪、多人控制器 |

**有了 Input System：**

策划在 `.inputactions` 配置文件里改键位，不碰代码。一套配置同时支持键盘、手柄、触屏、甚至 VR 控制器。

## 3. 基本原理

```
输入设备（键盘/手柄/触屏）
      ↓
Input System 运行时
      ↓
    配置文件 (.inputactions)
        ↓
    Action（例如 "Jump"）
        ↓
  你的代码监听 Action 事件
```

三层抽象：

```
设备层         →      中间层         →      逻辑层
Keyboard      →   "Jump" Action     →   player.Jump()
Gamepad       →   "Move" Action     →   player.Run()
Touchscreen   →   "Fire" Action     →   weapon.Shoot()
```

代码只关心 `"Jump"` 这个动作被触发了，**不关心你按的是空格、手柄 A 键还是触摸屏双击**。

## 4. 基础用法

### 4.1 安装

Window → Package Manager → 搜索 Input System → Install。
安装后 Unity 会提示启用新输入系统，点击 **Yes**。

### 4.2 创建输入配置文件

右键 → Create → Input Actions → 命名为 `PlayerInputActions`。

双击打开配置编辑器：

```
┌─────────────────────────────────┐
│ Action Maps    Actions          │
│ ┌──────────┐  ┌──────────────┐ │
│ │ Gameplay  │  │ Move  [Vector2]│ │
│ │    ↑       │  │ Jump  [Button] │ │
│ │    ↓       │  │ Fire   [Button]│ │
│ └──────────┘  └──────────────┘ │
│                                 │
│ Move 属性:                      │
│   Binding 1: WASD (键盘)       │
│   Binding 2: Left Stick (手柄) │
└─────────────────────────────────┘
```

### 4.3 在代码中使用

**方式一：直接在代码里绑定（适合新手）**

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    // 引用生成的 C# 包装类
    [SerializeField] private PlayerInputActions _input;

    void Awake()
    {
        _input = new PlayerInputActions();
        _input.Gameplay.Enable();
    }

    void Update()
    {
        // 读取值
        Vector2 move = _input.Gameplay.Move.ReadValue<Vector2>();
        transform.Translate(new Vector3(move.x, 0, move.y) * Time.deltaTime * 5f);
    }

    void OnEnable()  => _input?.Gameplay.Enable();
    void OnDisable() => _input?.Gameplay.Disable();
}
```

**方式二：事件回调（更推荐）**

```csharp
public class Player : MonoBehaviour
{
    private PlayerInputActions _input;

    void Awake()
    {
        _input = new PlayerInputActions();
    }

    void OnEnable()
    {
        _input.Gameplay.Enable();

        // 事件回调，不每帧检测
        _input.Gameplay.Jump.performed += OnJump;
        _input.Gameplay.Fire.started  += OnFireStart;
        _input.Gameplay.Fire.canceled += OnFireEnd;
    }

    void OnDisable()
    {
        _input.Gameplay.Jump.performed -= OnJump;
        _input.Gameplay.Fire.started  -= OnFireStart;
        _input.Gameplay.Fire.canceled -= OnFireEnd;
        _input.Gameplay.Disable();
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        // 按下时触发一次，不需要每帧检测
        rb.AddForce(Vector3.up * 10f, ForceMode.Impulse);
    }

    private void OnFireStart(InputAction.CallbackContext ctx)
    {
        isFiring = true;  // 按住时持续开火
    }

    private void OnFireEnd(InputAction.CallbackContext ctx)
    {
        isFiring = false;
    }
}
```

### 4.4 三种事件阶段

```
按键周期：  started → performed → canceled

按一下：   started + performed（同步触发）
按住：     started → performed（持续触发）
松开：     canceled
```

## 5. 进阶用法

### 5.1 多套 Action Map 切换

```csharp
// 不同游戏状态使用不同的按键映射
_input.Gameplay.Disable();  // 游戏时关闭移动输入
_input.UI.Enable();         // 打开 UI 输入（菜单、对话等）
```

典型场景：
```
Gameplay  → 奔跑、跳跃、射击
UI        → 菜单导航、确认、取消
Vehicle   → 加速、转向、刹车（驾驶时替代 Gameplay）
```

### 5.2 玩家自定义键位

```csharp
// 获取当前按键绑定
string currentKey = _input.Gameplay.Jump.GetBindingDisplayString();

// 重新绑定（让玩家按下新键）
_input.Gameplay.Jump.PerformInteractiveRebinding()
    .WithCancelingThrough("<Keyboard>/escape")  // ESC 取消
    .OnComplete(callback => {
        Debug.Log($"新键位: {_input.Gameplay.Jump.GetBindingDisplayString()}");
        callback.Dispose();
    })
    .Start();
```

### 5.3 Input Action Assets 分离

把 `.inputactions` 做成可复用的资源，不同场景用不同配置：

```
PlayerInputActions（基础操作）
BossFightInputActions（Boss 战额外按键）
VehicleInputActions（驾驶操作）
```

### 5.4 振动和触觉反馈

```csharp
// 手柄振动
Gamepad.current?.SetMotorSpeeds(0.5f, 0.8f); // 左马达、右马达

// 停止振动
Invoke(nameof(StopVibration), 0.5f);

void StopVibration()
{
    Gamepad.current?.SetMotorSpeeds(0f, 0f);
}
```

## 6. 面试题答案

### Q：新旧输入系统有什么区别？为什么推荐用 Input System？

**回答思路：**

> **一句话**：旧版 `InputManager` 把键位写死在代码里，新版 Input System 用配置文件声明映射关系。
>
> **核心优势：**
> 1. **键位与代码分离** — 策划在 `.inputactions` 文件里改键，不动代码
> 2. **多平台统一** — 一套 Action（如 "Jump"）同时绑定键盘空格、手柄 A 键、触屏点击
> 3. **事件驱动** — `performed`/`started`/`canceled` 三阶段回调，不用每帧检测
> 4. **玩家自定义键位** — `PerformInteractiveRebinding()` 内置支持
> 5. **设备无关** — 代码只关心 "Jump" 动作是否触发，不关心具体是哪个设备

### Q：三种事件阶段（started/performed/canceled）的区别？

> **started**：按键刚按下（一次）
> **performed**：按键生效（按一下就触发一次，按住持续触发）
> **canceled**：按键松开
>
> 通常 `performed` 最常用，`started` 和 `canceled` 在需要按住的场景（蓄力、开火）使用。

### Q：如何支持玩家自定义键位？

> 使用 `PerformInteractiveRebinding()` 方法：
> 1. 调用该方法进入绑定模式
> 2. 等待玩家按下新键
> 3. 绑定结果自动保存到 `.inputactions` 文件
> 4. 支持 ESC 取消、超时回收、按键冲突检测

## 7. 踩坑记录

- **忘 Enable 了**：创建 `PlayerInputActions` 后要手动 `Enable()`，否则收不到输入。可以在 `OnEnable()` 里统一启用
- **同一 Action 重复绑定**：多脚本同时监听同一个 Action 没问题，但注意在 `OnDisable()` 里取消订阅，防止场景切换时报错
- **生成的 C# 类文件名不对**：`.inputactions` 文件的 Inspector 里要勾选 **Generate C# Class**，生成的类名由 **C# Class Name** 字段决定
- **旧版 Input 和 Input System 冲突**：在 Project Settings → Player → Active Input Handling 里选 **Input System Package (New)** 或 **Both**
- **连点器检测**：`started` 和 `performed` 在快速连按时可能同步触发，如果只需要"按一下"的效果，只用 `performed`

## 相关笔记

- [[ScriptableObject数据驱动设计]] — Input Actions 配置和 ScriptableObject 都是数据驱动思路
- [[Profiler自定义采样]] — 用 Profiler 检测输入延迟
