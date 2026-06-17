---
title: "XR Grab Interactable 完整参考手册"
type: resource
tags: [unity, VR, XR Interaction Toolkit, XR Grab Interactable]
created: "2026-06-17"
updated: "2026-06-17"
status: active
summary: "XR Grab Interactable 每个属性的作用、取值范围、使用场景和VR消防项目实际配置。"
source: ""
related: [["XR Interaction Toolkit 配置指南"]]
---

# XR Grab Interactable 完整参考手册

`XR Grab Interactable` 让 VR 手柄可以抓住物体并移动、旋转、投掷。抓取从手靠近→抓住→跟着走→松手→飞出，全程由以下属性控制。

---

## 一、Movement（移动控制）

这一组决定了"抓住后物体怎么跟着手走"。

### Movement Type

物体的跟随算法，直接决定手感。

| 值 | 原理 | 手感 |
|---|---|---|
| **Velocity Tracking** | 每帧算手的速度，给物体施加对应的速度 | 有惯性、真实、最推荐 ✅ |
| **Kinematic** | 直接把物体移到手的位置，不走物理 | 贴紧手、无延迟、无惯性 |
| **Instantaneous** | 抓瞬间跳到手上，之后不再跟随 | 瞬移后不动，适合"捡起不跟随" |

> Velocity Tracking：松手时保留速度，可以扔出去；Kinematic：松手即停，死活不飞。想要扔灭火器 → Velocity Tracking；想要精确放置 → Kinematic。

### Smooth Position / Smooth Rotation

勾上后物体跟随带平滑过渡，不会丝~一帧跳变。

- **勾上** → 手抖时物体不抖，手感柔。几乎所有物体都该开 ✅
- **不勾** → 物体和手完全同步，精度高但可能抖动。只有需要精准对齐的小零件关掉。

### Velocity Scale

手移动的速度按倍率传给物体。

- `1`（默认）：手移动 1m/s → 物体也移动 1m/s
- `>1`：物体移动比手快，显得更"轻"，扔得更远
- `<1`：物体移动比手慢，显得更"重"，扔不远

> 灭火器设 0.8 → 拿着有厚重感；小工具设 1.5 → 轻盈称手。

### Throw On Detach

松手时是否保留惯性飞出去。

- **勾上**：松手物体沿当时速度继续飞（像真扔东西） ✅
- **不勾**：松手物体立即停住（像放在桌上）

> 灭火器勾上 → 可以扔给队友；精密仪器不勾 → 放下就停稳。

### Throw Smoothing

松手那一帧的速度平滑系数。`0` = 取松手瞬间速度（可能猝停），`1` = 用前几帧加权平均（平滑但延迟感）。

- 小物体设 0.3~0.5，扔出去自然
- 重物体设 0.7~1，松手不突兀

### Tighten Position / Tighten Rotation

抓得"紧不紧"。值越大，物体跟手越死，越不来回晃。

- `1`：死死贴手，完全同步。会震动。
- `0.5`：有点松，手甩动时物体有一个小延迟跟随
- `0`：完全松垮，像用橡皮筋拽着

> 灭火器设 0.7~0.8，稳稳跟着。手术刀设 1，绝对精准。

---

## 二、Track（约束控制）

决定物体在哪些维度上跟着手走。

### Track Position

- **勾** → 物体跟手移动（灭火器、工具）
- **不勾** → 物体不位移，只旋转（门、把手、阀门）

### Track Rotation

- **勾** → 物体跟手旋转（灭火器、手电筒）
- **不勾** → 物体不转（滑杆、开关）

### Track Scale

- 几乎永远不勾。V R 里不让玩家捏大捏小物体。

---

## 三、Attach Transform（抓取锚点）

决定"手抓到物体上的哪个点"。

### Attach Transform

拖一个子空物体到此字段。抓取时手会直接移到这个子物体的位置，而不是碰到物体的那个随机点。

**为什么重要：**

```
没设 Attach Transform：
  → 手碰到灭火器底部 → 抓着底部提起来 → 灭火器歪了
  → 松手 → 喷口朝地

设了 Attach Transform：
  → 手柄自动对齐到把手位置 → 灭火器方向正确
  → 喷口朝前 → 可以立刻喷
```

> 灭火器上建一个 Empty "GripPoint"，放在把手位置，拖到此处。

---

## 四、Retain（维持控制）

### Retain Transform Parent

物体被抓时，保持在原来的父级关系下。

- **勾上** → 门被抓时还是门框的子物体，不会脱离铰链
- **不勾** → 门被抓时脱离门框，可以拿着门到处走

> 门/抽屉/阀门这类有机械约束的物体，一定要勾上。

---

## 五、Interactable Events（事件系统）

物体在不同交互阶段触发的事件，挂你的业务逻辑。

### On Select Entered

**触发时机：** 手碰到物体并按下 Grip → 物体被抓起会瞬间。

```cs
// 灭火器被抓
public void OnGrab()
{
    fireExAni.Play("Grab");
    Debug.Log("灭火器已装备");
}
```

### On Select Exited

**触发时机：** 松手，物体从手上脱离。

```cs
// 松手
public void OnRelease()
{
    StopSpray(); // 防止松手还在喷
}
```

### On Hover Entered / On Hover Exited

**触发时机：** 手靠近（还没抓）/ 手移开。

```cs
// 靠近 → 高亮
public void OnHover()
{
    highlight.SetActive(true);
}
public void OnHoverExit()
{
    highlight.SetActive(false);
}
```

### On Activated / On Deactivated ⭐ 最重要的

**触发时机：** 抓住状态下 + 扣扳机（Trigger）。

> 灭火器喷射的核心！

```cs
// 抓住灭火器 → 扣扳机 → 喷射
public void OnActivated(ActivateEventArgs args)
{
    customParticalExtinguish.StartSpray();
    fireSmoke.Play();
}

public void OnDeactivated(DeactivateEventArgs args)
{
    customParticalExtinguish.StopSpray();
    fireSmoke.Stop();
}
```

---

## 六、Interaction Manager（交互管理）

### Interaction Manager

场景里唯一的 `XR Interaction Manager` 组件，拖进去即可。

### Select Mode

| 值 | 效果 |
|---|---|
| `Single` | 一次只有一只手能抓。灭火器不能两只手同时拿 ✅ |
| `Multiple` | 可以两只手同时抓。适合双手操作的物体（步枪、长棍） |

### Interaction Layer Mask

限制什么层的手能抓这个物体。配合分离的 Interactable 层使用。

---

## 七、消防项目配置速查

| 物体 | Movement | Track Pos | Track Rot | Attach Transform | Throw | Select Mode | 事件 |
|---|---|---|---|---|---|---|---|
| 灭火器 | Velocity 0.8 | ✅ | ✅ | GripPoint | ✅ | Single | Activated→喷射 |
| 门把手 | Kinematic | ❌ | ✅ | HandlePoint | ❌ | Single | Select→OpenDoor() |
| 验电器 | Velocity 1 | ✅ | ✅ | Tip | ❌ | Single | Activated→检测 |
| 干沙子桶 | Velocity 0.5 | ✅ | ❌（不倒） | Center | ❌ | Single | Activated→撒沙 |
| 水枪 | Velocity 1 | ✅ | ✅ | NozzlePoint | ❌ | Multiple | Activated→喷水 |

