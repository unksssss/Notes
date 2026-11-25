---
title: "Unity InputSystem详解"
type: resource
tags: [unity, InputSystem, 输入系统]
created: "2026-04-29"
updated: "2026-11-25"
status: active
summary: "从面试题出发，由浅入深讲解 Unity Input System 的原理与应用；第 8 节补入重开局泄漏实战：生成的 `InputSystem_Actions` 不是 `UnityEngine.Object`，`LoadScene` 不销毁它 → ActionMap 仍 enabled、委托指向已销毁实例 → `MissingReferenceException`，正解是 `Awake` 只 new、`Bind/UnBind` 在 `OnEnable`/`OnDisable` 配对、`OnDestroy` 里 `Disable + Dispose + 置 null`。"
source: "2026-11-25 Empty 项目 ReGame 实战补强"
related: ["[[unity网络同步方案-状态同步vs帧同步]]", "[[Unity 消息方法的虚方法隐藏 — override 与 new]]"]
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

## 8. 重开局泄漏：`InputSystem_Actions` 不是 UnityEngine.Object（2026-11-25 实战）

**现象**：打完一局点「再来一局」，`SceneManager.LoadScene` 重载场景后，**鼠标一动就抛 `MissingReferenceException`**，日志指向旧 `Player` 的 `transform` / `Animator`。

**根因**：`.inputactions` 生成的 C# 类（`InputSystem_Actions`）是**普通 C# 对象，不是 `UnityEngine.Object`**。

| | `MonoBehaviour` / `ScriptableObject` | 生成的 `InputSystem_Actions` |
| --- | --- | --- |
| 是否 `UnityEngine.Object` | 是 | **否** |
| 场景卸载时 | 被 Unity 销毁 | **原封不动**（GC 才管它） |
| 销毁后判空 | `== null` 成立（重载了 `==`） | 只能靠 `Dispose()` |

于是 `LoadScene` 之后：旧实例还活着 → 它的 `Player` ActionMap 仍然 **enabled** → 委托仍然指向**已销毁的 `Player` 实例方法** → 新场景又 `new` 一份并 `Enable()` → **两份 ActionMap 同时 enabled**，一次鼠标移动两份回调都跑，旧的那份访问已销毁对象就炸。

> 关键认知：**`Enabled` 状态和委托订阅是「活着的托管对象上的状态」，不归场景管**。任何 `Action`/`event`/`InputAction`/`CancellationTokenSource` 字段都是同一个雷区。

**正确骨架**：

```cs
public class Player : RoleBase
{
    private InputSystem_Actions input;

    private void Awake()
    {
        input = new InputSystem_Actions();   // 只 new，不 Enable
    }

    protected override void OnEnable()
    {
        base.OnEnable();      // RoleBase 里的 Health 订阅
        BindInput();
    }

    protected override void OnDisable()
    {
        UnBindInput();
        base.OnDisable();
    }

    private void OnDestroy()
    {
        if (input == null) return;
        UnBindInput();
        input.Dispose();      // 释放非托管资源 + 清空绑定
        input = null;
    }

    private void BindInput()
    {
        if (input == null) return;
        // 幂等：先 -= 再 +=，防 OnEnable 多次触发导致重复订阅
        input.Player.Move.performed   -= OnMove;
        input.Player.Move.performed   += OnMove;
        input.Player.Attack.performed -= OnAttack;
        input.Player.Attack.performed += OnAttack;
        input.Player.Enable();
    }

    private void UnBindInput()
    {
        if (input == null) return;
        input.Player.Disable();   // 先停派发，再退订
        input.Player.Move.performed   -= OnMove;
        input.Player.Attack.performed -= OnAttack;
    }
}
```

**四个容易写错的点**：

1. **`Dispose()` 之后不能再用** —— `input` 必须置 `null`，下次 `OnEnable` 前要重新 `new`（本项目是「一个场景一个 Player」，所以放 `OnDestroy` 即可）。
2. **顺序是「先 `Disable` 再 `-=`」** —— 反过来在退订过程中仍可能有回调进来。
3. **订阅写在 `OnEnable` 而不是 `Start`** —— `Start` 一辈子只跑一次，`OnDisable` 退订后就再也绑不回来了（对象池/`SetActive` 复用场景直接失效）。
4. **`callback` 里不要做重活** —— `performed` 是输入线程同步进来的，只做「记下意图」，真正逻辑放 `Update`。

**同类隐患清单**（都是「非 UnityEngine.Object + 订阅」）：

- `joyStick.Motion = OnJoyStick`（普通 `Action` 字段）—— 场景卸载不会自动解绑，`OnDisable` 里要 `-=`；
- `public static event Action OnFire` —— 生命周期跟**程序集**不跟场景，静态事件会持有已销毁实例 → 强引用泄漏 + `MissingReferenceException`；
- 订阅方自己不退订时，静态事件是最难查的一种（`-=` 忘了写，谁也发现不了）。

**自检**：重开局/切场景后，看 `InputDebugger` 里同一 ActionMap 是不是有**两份以上** enabled 实例；或在回调里打 `GetInstanceID()`，看是否出现旧 ID。

## 相关笔记

- [[scriptableobject数据驱动设计]] — Input Actions 配置和 ScriptableObject 都是数据驱动思路
- [[profiler自定义采样]] — 用 Profiler 检测输入延迟
- [[Unity 消息方法的虚方法隐藏 — override 与 new]] — `Player.Awake` 隐藏基类 `Awake` 导致初始化静默丢失
- [[UnityEngine.Object 判空与销毁机制]] — 为什么 `InputSystem_Actions` 的 `== null` 永远不成立
