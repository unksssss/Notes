---
title: "DOTween详解"
type: resource
tags: [unity, DOTween, 动画, 补间动画]
created: "2026-05-29"
updated: "2026-05-29"
status: active
summary: "Unity DOTween 动画补间库完整指南：基本用法、回调系统、Sequence序列、生命週期控制、常用模式与性能注意事项"
source: ""
related: []
---

# DOTween 详解

DOTween 是 Unity 最常用的动画补间库，用链式 API 让 Transform、UI、材质等属性的动画变得极其简洁。

## 安装

Unity Package Manager → Add package from git URL：
```
https://github.com/Demigiant/dotween.git
```

安装后在菜单栏 **Tools → Demigiant → DOTween Utility Panel** → **Setup DOTween**。

## 基本用法

所有动画都从 `transform.DOMoveX()`、`material.DOColor()` 这类扩展方法开始，链式调用配置。

### Transform

```cs
// 移动
transform.DOMove(new Vector3(5, 0, 0), 1f);       // 世界坐标
transform.DOLocalMove(new Vector3(5, 0, 0), 1f);  // 本地坐标
transform.DOMoveX(5f, 1f);                         // 单轴
transform.DOMoveY(5f, 1f);
transform.DOMoveZ(5f, 1f);

// 旋转
transform.DORotate(new Vector3(0, 90, 0), 1f);
transform.DOLocalRotate(new Vector3(0, 90, 0), 1f);
transform.DOLookAt(target.position, 1f);           // 看向目标

// 缩放
transform.DOScale(Vector3.one * 2f, 1f);
transform.DOScaleX(2f, 1f);
transform.DOShakeScale(0.5f, 0.5f);                // 抖动缩放

// 跳动 / 路径
transform.DOJump(new Vector3(5, 0, 0), 3f, 1, 1f);  // 抛物线跳跃
transform.DOPath(points, 3f);                        // 沿路径移动

// Punch（弹回）和 Shake（震动）
transform.DOPunchPosition(new Vector3(0, 1, 0), 0.5f, 10, 1f);
transform.DOShakePosition(0.5f, 1f, 20, 90f);
```

### UI（需要 `using DG.Tweening;`）

```cs
// RectTransform
rectTransform.DOAnchorPos(new Vector2(100, 0), 0.5f);
rectTransform.DOSizeDelta(new Vector2(200, 100), 0.3f);

// CanvasGroup（淡入淡出最常用）
canvasGroup.DOFade(0f, 0.5f);   // 淡出
canvasGroup.DOFade(1f, 0.5f);   // 淡入

// Image / Text / SpriteRenderer
image.DOColor(Color.red, 0.5f);
image.DOFade(0f, 0.5f);         // 修改 alpha
text.DOText("Hello", 1f);       // 打字机效果
text.DOColor(Color.blue, 0.5f);
```

### 材质

```cs
material.DOColor(Color.red, 1f);
material.DOFade(0.5f, 1f);                   // 修改 alpha
material.DOFloat(1f, "_Glossiness", 0.5f);   // 修改 shader 属性
material.DOVector(new Vector4(1, 0, 0, 1), "_TintColor", 0.5f);
material.DOOffset(new Vector2(1, 0), "_MainTex", 1f); // UV 偏移
```

### Camera

```cs
camera.DOOrthoSize(10f, 1f);             // 正交相机的 size
camera.DOFieldOfView(90f, 1f);           // 透视相机的 FOV
camera.DOColor(Color.red, 1f);           // 背景色
```

## 缓动曲线 `.SetEase()`

```cs
transform.DOMoveX(5f, 1f).SetEase(Ease.OutQuad);
```

### 常用曲线速查

| Ease 类型 | 效果 | 适用场景 |
|---|---|---|
| `Ease.Linear` | 匀速 | 很少用，太机械 |
| `Ease.InQuad` | 慢→快 | 元素离开屏幕 |
| `Ease.OutQuad` | 快→慢 | **最常用**，UI 出现、物体移动到达 |
| `Ease.InOutQuad` | 慢→快→慢 | 来回运动、呼吸效果 |
| `Ease.InBack` | 先倒退再加速 | 弹射效果 |
| `Ease.OutBack` | 过冲再回弹 | UI 弹窗出现 |
| `Ease.OutBounce` | 弹跳减速 | 掉落落地 |
| `Ease.OutElastic` | 弹性衰减 | 橡皮筋效果 |
| `Ease.Flash` | 闪烁 | 警告提示 |

### 自定义曲线

```cs
AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
transform.DOMoveX(5f, 1f).SetEase(curve);
```

## 回调系统

### 生命周期回调

```cs
transform.DOMoveX(5f, 1f)
    .OnStart(()     => Debug.Log("动画开始"))
    .OnUpdate(()    => Debug.Log("每帧调用"))
    .OnComplete(()  => Debug.Log("正常播完"))
    .OnKill(()      => Debug.Log("动画结束（完成或被 Kill 都触发）"))
    .OnStepComplete(() => Debug.Log("循环的每次迭代完成"))
    .OnPause(()     => Debug.Log("动画暂停"))
    .OnPlay(()      => Debug.Log("动画恢复"));
```

### 回调执行顺序

```
OnStart → OnUpdate(每帧) → [OnStepComplete × N次] → OnComplete → OnKill
```

### OnComplete vs OnKill

| | OnComplete | OnKill |
|---|---|---|
| 正常播完 | ✅ 触发 | ✅ 触发 |
| tween.Kill() | ❌ 不触发 | ✅ 触发 |
| 默认 autoKill 完成 | ✅ | ✅ |
| 对象被 Destroy | ❌ | ✅ |
| 场景切换 | ❌ | ❌（都不会触发） |

> **最佳实践：** 需要"动画结束后做某事"时，优先用 `OnComplete`。需要"无论什么原因结束都清理状态"时，用 `OnKill`。

## Sequence 序列动画

Sequence 可以编排多个动画的播放顺序：

```cs
// 方式一：DOTween.Sequence()
Sequence sq = DOTween.Sequence();
sq.Append(transform.DOMoveX(3f, 1f));       // 接在上一动画后面
sq.Append(transform.DORotate(new Vector3(0, 90, 0), 0.5f));
sq.Join(transform.DOScaleY(2f, 0.5f));       // 和上一动画同时播放
sq.AppendInterval(0.5f);                      // 等待 0.5 秒
sq.AppendCallback(() => Debug.Log("序列结束"));

// 方式二：链式创建（推荐，更简洁）
Sequence sq = DOTween.Sequence()
    .Append(transform.DOMoveX(3f, 1f))
    .Append(transform.DORotate(new Vector3(0, 90, 0), 0.5f))
    .AppendCallback(() => Debug.Log("完了"))
    .SetLoops(-1, LoopType.Yoyo);            // 无限来回循环
```

### Sequence 操作方法

| 方法 | 效果 |
|---|---|
| `Append(Tween)` | 追加到序列末尾，按顺序播放 |
| `Join(Tween)` | 和上一个 Append 的动画**同时**开始 |
| `Prepend(Tween)` | 插入到序列最前面 |
| `Insert(float atPosition, Tween)` | 在指定时间点插入 |
| `AppendInterval(float)` | 等待 N 秒 |
| `AppendCallback(Action)` | 插入回调 |

### 完整示例：UI 弹窗出现动画

```cs
Sequence ShowPopup(RectTransform panel, CanvasGroup cg)
{
    panel.localScale = Vector3.zero;
    cg.alpha = 0f;

    return DOTween.Sequence()
        .Append(panel.DOScale(1f, 0.3f).SetEase(Ease.OutBack))
        .Join(cg.DOFade(1f, 0.3f))
        .OnComplete(() => Debug.Log("弹窗显示完成"));
}
```

## 循环 `.SetLoops()`

```cs
transform.DOMoveX(5f, 1f)
    .SetLoops(3, LoopType.Restart);  // 播放 3 次（共 3 次）

transform.DOMoveX(5f, 1f)
    .SetLoops(-1, LoopType.Yoyo);    // 无限来回
```

### LoopType

| LoopType | 效果 |
|---|---|
| `Restart` | 每次循环从头开始 |
| `Yoyo` | 来回播放（正→反→正→反） |
| `Incremental` | 每次循环累加上一次的目标值 |

## 其他常用设置

```cs
transform.DOMoveX(5f, 2f)
    .SetDelay(0.5f)               // 延迟 0.5 秒开始
    .SetRelative()                // 相对移动（当前位置 +5）
    .SetSpeedBased()              // duration 变成速度（秒/单位），而非总时长
    .SetEase(Ease.OutQuad)
    .SetLoops(2, LoopType.Yoyo)
    .SetId("myTween")             // 给动画起名，方便后用 DOTween.Kill("myTween")
    .SetAutoKill(true)            // 完成后自动销毁 tween（默认 true）
    .SetUpdate(true)              // 忽略 Time.timeScale（不受暂停影响）
    .OnComplete(() => { });
```

### SetUpdate 详解

```cs
// 不受 Time.timeScale=0 影响（常用于暂停菜单中的动画）
transform.DOMoveX(5f, 1f).SetUpdate(true);

// 更精细的控制
.SetUpdate(UpdateType.Normal)   // 受 timeScale 影响（默认）
.SetUpdate(UpdateType.Late)     // 在 LateUpdate 中更新
.SetUpdate(UpdateType.Fixed)    // 在 FixedUpdate 中更新
.SetUpdate(UpdateType.Manual)   // 手动驱动（需调用 DOTween.ManualUpdate()）
```

## Tween 的生命周期控制

```cs
Tween tween = transform.DOMoveX(5f, 2f);

tween.Pause();           // 暂停
tween.Play();            // 恢复
tween.Restart();         // 从头开始
tween.Complete();        // 直接跳到终点并完成
tween.Complete(true);    // 跳到终点 + 触发 OnComplete 回调
tween.Flip();            // 反转方向
tween.Goto(0.5f);        // 跳到播放进度的 50%
tween.Goto(1f, true);    // 跳到终点 + 触发回调
tween.Kill();            // 立即终止（不触发 OnComplete，但触发 OnKill）
tween.Kill(true);        // 立即终止 + 立即跳到终点
tween.Rewind();          // 回到起点
tween.Rewind(true);      // 回到起点 + 触发回调
```

### 全局控制

```cs
DOTween.PauseAll();                     // 暂停所有动画
DOTween.Pause("myTween");               // 暂停指定 ID 的动画
DOTween.PlayAll();                      // 恢复所有
DOTween.Kill("myTween");                // 终止指定动画
DOTween.KillAll();                      // 终止所有（场景切换前常用）
DOTween.KillAll(true);                  // 终止所有 + 跳到终点（对象会停在最终状态）
DOTween.CompleteAll();                  // 立即完成所有动画

// 查询
DOTween.PlayingTweens();               // 正在播放的动画数量
DOTween.PausedTweens();                // 暂停的动画数量
```

## From() — 从目标值到当前值

```cs
// 普通: 当前位置 → 目标位置
transform.DOMoveX(5f, 1f);

// From: 从目标位置 → 弹回当前位置
transform.DOMoveX(5f, 1f).From();                 // 从 (5, y, z) 跳到当前位置
transform.DOMoveX(5f, 1f).From(true);             // From(true) = 相对偏移
```

`From(true)` 的含义：

```cs
// 假设初始位置是 (2, 0, 0)
transform.DOMoveX(5f, 1f).From();
// → 先瞬移到 (5, 0, 0)，再动画移到 (2, 0, 0)

transform.DOMoveX(5f, 1f).From(true);
// → 先瞬移到 (2+5=7, 0, 0)，再动画移到 (2, 0, 0)
```

## 常用模式

### 模式一：淡入淡出

```cs
public void FadeIn(CanvasGroup cg)
{
    cg.DOFade(1f, 0.3f).From(0f).SetEase(Ease.OutQuad);
}

public void FadeOut(CanvasGroup cg)
{
    cg.DOFade(0f, 0.3f).OnComplete(() => cg.gameObject.SetActive(false));
}
```

### 模式二：弹窗弹出 + 淡入

```cs
public void ShowPanel(RectTransform panel)
{
    panel.localScale = Vector3.zero;
    panel.gameObject.SetActive(true);

    DOTween.Sequence()
        .Append(panel.DOScale(1f, 0.3f).SetEase(Ease.OutBack))
        .Join(panel.GetComponent<CanvasGroup>().DOFade(1f, 0.3f));
}
```

### 模式三：列表项逐个出现

```cs
public void ShowListItems(List<RectTransform> items)
{
    Sequence sq = DOTween.Sequence();

    foreach (var item in items)
    {
        item.localScale = Vector3.zero;
        sq.Append(item.DOScale(1f, 0.15f).SetEase(Ease.OutBack));
    }
}
```

### 模式四：无限呼吸效果

```cs
transform.DOScale(1.1f, 1f)
    .SetEase(Ease.InOutSine)
    .SetLoops(-1, LoopType.Yoyo);
```

### 模式五：闪烁提示

```cs
image.DOColor(Color.red, 0.5f)
    .SetLoops(6, LoopType.Yoyo)
    .SetEase(Ease.Flash);
```

### 模式六：物体掉落落地

```cs
transform.position = spawnPoint + Vector3.up * 3f;
transform.DOJump(targetPosition, 1.5f, 1, 0.5f)
    .SetEase(Ease.OutBounce);
```

### 模式七：打字机效果

```cs
text.DOText("欢迎进入配电房消防演练", 2f)
    .SetEase(Ease.Linear);

// 带富文本的打字机
text.DOText("请选择<color=red>正确的</color>装备", 2f)
    .SetEase(Ease.Linear);
```

## 性能与内存

### Tween 的自动回收

默认 `SetAutoKill(true)`：动画完成后 tween 对象自动回收到池中。**无需手动 Kill**，DOTween 内部管理。

### 手动 Kill 的场景

```cs
// 场景切换前
void OnDestroy()
{
    transform.DOKill();  // 杀死对象上所有动画
    DOTween.KillAll();   // 或全局清除（Loading 场景常用）
}
```

### 注意事项

- **对象销毁前 Kill**：如果动画对象可能被 Destroy，在 `OnDestroy` 中 `transform.DOKill()`
- **场景切换**：DOTween 不会自动清除，切换场景前调用 `DOTween.KillAll()`
- **Lambda 闭包陷阱**：回调中引用的变量在动画结束后才释放，避免在循环中创建带外部引用的 tween
- **`DOTween.Init()`** 不需要手动调用，首次创建 tween 时自动初始化

## 常用命名空间

```cs
using DG.Tweening;             // 所有 Tween/Sequence/Ease
using DG.Tweening.Core;        // TweenCallback 等核心类型
using DG.Tweening.Plugins;     // 自定义插件开发时用到
```

## 速查卡片

```
┌─────────────────────────────────────────────┐
│  动画       →   transform.DOMoveX(5f, 1f)  │
│  缓动       →   .SetEase(Ease.OutQuad)     │
│  延迟       →   .SetDelay(0.5f)            │
│  循环       →   .SetLoops(-1, LoopType.Yoyo)│
│  回调       →   .OnComplete(() => {})      │
│  序列       →   Sequence.Append().Join()   │
│  暂停       →   tween.Pause()              │
│  终止       →   tween.Kill()               │
│  全局       →   DOTween.KillAll()          │
│  暂停无关   →   .SetUpdate(true)           │
│  相对       →   .SetRelative()             │
│  UI 淡入    →   canvasGroup.DOFade(1f, 0.3f)│
└─────────────────────────────────────────────┘
```
