---
title: "协程原理与UniTask"
type: resource
tags: [unity, 协程, 异步, UniTask, async]
created: "2026-04-28"
updated: "2026-05-20"
status: active
summary: "Unity 协程工作原理、IL 层状态机、局限性、UniTask 零 GC 异步方案深度解析"
source: ""
related: ["[[unity知识点-2026-04-28]]", "[[unity知识点-2026-04-29]]", "[[DOTS详解]]", "[[Profiler自定义采样]]"]
---

# 协程原理与 UniTask

## 一、协程（Coroutine）工作原理

### 1.1 接口层面：IEnumerator 的本质

协程的根基是 `IEnumerator` 接口：

```csharp
public interface IEnumerator
{
    object Current { get; }
    bool MoveNext();
    void Reset();  // 几乎不用
}
```

当你写 `yield return`，C# 编译器把它编译成一个**状态机类**：

```csharp
// 编译器生成的代码（简化）
IEnumerator Countdown()
{
    return new <Countdown>d__0(0);  // 状态机实例
}

class <Countdown>d__0 : IEnumerator
{
    int __state;
    object __current;

    bool MoveNext()
    {
        switch (__state)
        {
            case 0:  // 初始
                __state = -1;
                count = 5;
                goto case 1;
            case 1:  // while 入口
                if (count <= 0) return false;
                Debug.Log(count);
                __current = new WaitForSeconds(1f);
                __state = 2;
                return true;  // 暂停，下次从 state=2 恢复
            case 2:  // 恢复点
                __state = -1;
                count--;
                goto case 1;
        }
        return false;
    }
}
```

> 每个 `yield return` 处状态自增 + `return true` 挂起；Unity 下次调用 `MoveNext()` 时继续。

### 1.2 Unity 调度器：CoroutineManager 的工作原理

Unity 引擎内部有一个 `CoroutineManager`（C++ 实现），分**时间驱动**和**帧事件驱动**两类调度：

| yield 指令 | 恢复时机 | 调度器类型 |
|---|---|---|
| `null` / `0` | 下一帧的 Update 之后 | 帧尾统一处理 |
| `WaitForEndOfFrame` | 渲染完成后，摄像机和 GUI 渲染之前 | WaitForEndOfFrame 回调 |
| `WaitForFixedUpdate` | 固定时间步物理更新后 | FixedUpdate 回调 |
| `WaitForSeconds` | 指定时间后（Time.time 累计） | 计时器列表，每帧检查到期 |
| `WaitUntil` / `WaitWhile` | 每帧检查谓词 | 帧尾遍历等待队列 |
| `WaitForSecondsRealtime` | 不受 Time.timeScale 影响，用 Time.realtimeSinceStartup | 独立计时 |

**调度流程**（伪代码）：

```csharp
void CoroutineManager.Update()
{
    // 1. 处理 WaitForSeconds 到期
    foreach (var wait in timeBasedWaits)
        if (wait.Expired())
            wait.Owner.MoveNext();

    // 2. 处理 WaitUntil/WaitWhile
    foreach (var wait in predicateWaits)
        if (wait.Predicate())
            wait.Owner.MoveNext();

    // 3. 处理 null/nextFrame（帧尾统一调度）
    foreach (var coroutine in pendingResume)
        coroutine.MoveNext();
}
```

### 1.3 协程嵌套

```csharp
IEnumerator Outer()
{
    Debug.Log("Outer start");
    yield return StartCoroutine(Inner());  // 嵌套等待
    Debug.Log("Outer end");
}

IEnumerator Inner()
{
    yield return new WaitForSeconds(1f);
    Debug.Log("Inner done");
}
```

嵌套实现原理：Unity 把 `StartCoroutine(Inner)` 返回的 Coroutine 当作一个特殊的 yield instruction，内部维护父子链表——`Inner` 完成时恢复 `Outer`。

---

## 二、协程的五个硬伤（深入 + 代码实证）

### 2.1 在主线程运行

协程只是一个时间片分割器（time-slicer），**无法将工作卸载到其他线程**：

```csharp
IEnumerator HeavyComputation()
{
    // ❌ 下面的运算仍然在主线程进行，Time.deltaTime 依然飙升
    for (int i = 0; i < 100_000_000; i++)
        Mathf.Sqrt(i);
    yield return null; // 只是推迟执行，并没卸载到其他线程
}
```

> 需要真正的多线程 → Job System、async/await + Task.Run、UniTask + ThreadPool。

### 2.2 GC 分配

```csharp
// ❌ 每个 yield 都 new 一个对象
yield return new WaitForSeconds(1f);   // 16 bytes
yield return new WaitForEndOfFrame();  // 12 bytes
yield return null;                     // 0 bytes（特殊优化）
```

**GC 影响**：每秒 60 帧的协程，仅 WaitForSeconds 就产生 ~960 bytes/s，一分钟 ~56 KB。对于大量协程（弹幕、特效）GC 累积不可忽略。

**缓解方案**：
```csharp
// 缓存 WaitForSeconds 实例（但 WaitForSeconds 内部不缓存，Unity 内部有优化？）
// Unity 2020+ 确实有 WaitForSeconds 实例池，但仅限于字面量 1f — 不能依赖
private static readonly WaitForSeconds _oneSec = new WaitForSeconds(1f);
yield return _oneSec;
```

> **Best practice**：高频协程改用 UniTask。

### 2.3 无法返回值

```csharp
// 协程不能 return，只能用回调
IEnumerator FetchData(Action<string> callback)
{
    yield return new WaitForSeconds(1f);
    callback?.Invoke("data");
}

// 嵌套取值要传 callback，回调地狱
```

### 2.4 异常断裂

```csharp
IEnumerator Risky()
{
    yield return null;
    try
    {
        yield return null;  // ❌ try 里的 yield，异常跳出后无法被外层 catch
    }
    catch (Exception e)
    {
        Debug.Log(e);       // 这个 catch 永远抓不到 yield 内抛出的异常
    }
}
```

协程内部的异常本质上是 `MoveNext()` 抛出的。外层包 `try` 只能抓到**当前 MoveNext() 调用**内的异常，跨 yield 后栈帧已经切了。

**真实场景**：网络请求过程中断网 → yield 后的代码抛出异常 → Unity 控制台打出 `Exception`，但 `try-catch` 抓不到。

### 2.5 取消困难

```csharp
Coroutine c = StartCoroutine(MyCoroutine());
StopCoroutine(c);  // 必须持有 Coroutine 引用

// 全局停止所有协程
StopAllCoroutines();  // 粗粒度，会停掉所有
```

无法做到**选择性取消**或**超时自动取消**（除非在 yield 后面手动检查条件）。

---

## 三、async/await 原理（前置知识）

### 3.1 C# 状态机生成

`async` 关键字和协程一样，也是 **编译器生成状态机**：

```csharp
async Task<string> FetchDataAsync()
{
    var a = await Step1();
    var b = await Step2(a);
    return b;
}

// 编译器生成：AsyncStateMachine 结构体
struct <FetchDataAsync>d__0 : IAsyncStateMachine
{
    int __state;
    TaskAwaiter<string> __awaiter;
    string __result;

    void MoveNext()
    {
        try
        {
            switch (__state)
            {
                case 0: // await Step1()
                    __awaiter = Step1().GetAwaiter();
                    if (__awaiter.IsCompleted) goto case 1;
                    __state = 1;
                    // 注册 continuation：MoveNext 完成后回调
                    __builder.AwaitUnsafeOnCompleted(ref __awaiter, ref this);
                    return;
                case 1: // Step1 完成，取结果
                    var a = __awaiter.GetResult();
                    // ...await Step2(a)
            }
        }
        catch (Exception e) { __builder.SetException(e); }
    }
}
```

> **关键区别**：async 状态机是 **struct**（值类型），协程状态机是 **class**（引用类型）。

### 3.2 SynchronizationContext 与 Unity 的 PlayerLoop

`await` 后恢复线程的规则依赖 `SynchronizationContext.Current`：

- **WinForms/WPF**：UI SynchronizationContext → 回到 UI 线程
- **ASP.NET Core**：无 SynchronizationContext → 回到线程池
- **Unity**：`UnitySynchronizationContext` → 回到主线程

**Unity 的 PlayerLoop**：

```
PlayerLoop
├── Initialization
├── EarlyUpdate
├── FixedUpdate              ← WaitForFixedUpdate
├── PreUpdate
├── Update                   ← MonoBehaviour.Update()
│   └── UnitySynchronizationContext.Post(actions)  ← async continuation 在此处理
├── PreLateUpdate
├── PostLateUpdate
│   └── WaitForEndOfFrame
└── ...rendering...
```

每次 `Update` 阶段，Unity 会处理 `UnitySynchronizationContext` 中堆积的所有 continuation（委托），所以 `await` 后的代码跑在 **Update 生命周期**内。

---

## 四、UniTask 深入解析

### 4.1 零 GC 根基：UniTask 是结构体

```csharp
// UniTask 本质（大幅简化）
[AsyncMethodBuilder(typeof(AsyncUniTaskMethodBuilder))]
public readonly struct UniTask : IEquatable<UniTask>
{
    internal readonly IAwaiter awaiter;  // 持有完成的 awaiter（可池化）
    internal readonly bool isCompleted;  // 同步完成时直接执行
}

public readonly struct UniTask<T> : IEquatable<UniTask<T>>
{
    internal readonly IAwaiter<T> awaiter;
    internal readonly T result;         // 同步完成时直接带值
    internal readonly bool isCompleted;
}
```

- **`Task` 是 class** → 每次 `await` 分配堆内存
- **`UniTask` 是 struct** → 栈分配，零 GC
- **`ValueTask`** 也是 struct，但 UniTask 进一步去掉了对 `IValueTaskSource` 的接口调用开销

### 4.2 UniTask.Delay 的实现原理

```csharp
public static UniTask Delay(int millisecondsDelay, ...)
{
    // 不是开 Timer 线程！是注册到 PlayerLoop 的 Timing 回调
    var tcs = autoResets ? GetPooled() : new UniTaskCompletionSource();

    // 插入 PlayerLoop 计时器列表
    PlayerLoopHelper.AddAction(
        PlayerLoopTiming.Update,
        new DelayPromise(millisecondsDelay, tcs)
    );

    return tcs.Task;
}
```

内部：一个 `DelayPromise` 每帧检查时间是否到期，到期时调用 `tcs.TrySetResult()` → `MoveNext()` 恢复执行。

**与 Task.Delay 的对比**：

| | Task.Delay | UniTask.Delay |
|---|---|---|
| 定时器方式 | OS 线程池 Timer | PlayerLoop 帧轮询 |
| 精度 | 高（~15ms Windows 时钟） | 依赖帧率（60fps ~16ms） |
| 切换上下文 | 回到线程池再切回主线程 | 直接在主线程恢复 |
| GC | 每次分配 Timer 对象 | 零 GC（池化） |

### 4.3 CancellationToken 集成

```csharp
// 方式 1：GameObject 销毁时自动取消
CancellationToken ct = this.GetCancellationTokenOnDestroy();

await SomeTask(ct);

// 方式 2：手动超时
using var cts = new CancellationTokenSource();
cts.CancelAfterSlim(TimeSpan.FromSeconds(5));
try
{
    await LongTask(cts.Token);
}
catch (OperationCanceledException)
{
    Debug.Log("超时取消");
}

// 方式 3：按 MonoBehaviour 生命周期取消
CancellationToken ct = this.GetCancellationTokenOnDestroy();

// 方式 4：父 CancellationToken 链接
using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, userCts.Token);
await LongTask(linkedCts.Token);
```

### 4.4 async void 的危害与 UniTaskVoid

```csharp
// ❌ async void 异常会崩溃
async void FireAndForget()
{
    await UniTask.Delay(100);
    throw new Exception("模拟异常");  // Unity 崩溃
}

// ✅ UniTaskVoid：异常会被 UniTaskScheduler 捕获并派发
async UniTaskVoid SafeFireAndForget()
{
    try
    {
        await UniTask.Delay(100);
        throw new Exception("模拟异常");
    }
    catch (Exception e)
    {
        Debug.LogError(e);  // 安全处理
    }
}
```

> 所有 `async void` 都应该替换为 `async UniTaskVoid`。

### 4.5 Completable 与 Delegate

UniTask 提供了 `UniTaskCompletionSource` 实现"手动完成"模式：

```csharp
// 非 async 方法也能返回 UniTask
UniTask<T> WaitForEvent()
{
    var utcs = UniTaskCompletionSource<T>.Create();
    
    void Handler(T result)
    {
        SomeEvent -= Handler;
        utcs.TrySetResult(result);
    }
    
    SomeEvent += Handler;
    return utcs.Task;
}
```

### 4.6 线程切换

```csharp
async UniTask RunOnThreadPool()
{
    // 切换到线程池
    await UniTask.SwitchToThreadPool();
    var result = ComputeHeavy();  // 子线程运行

    // 切回主线程
    await UniTask.SwitchToMainThread();
    ApplyResult(result);  // 主线程安全
}

// 指定 PlayerLoop Timing
await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
await UniTask.Yield(PlayerLoopTiming.EndOfFrame);
```

### 4.7 组合 API

```csharp
// 等待所有完成
await UniTask.WhenAll(
    FetchA(),
    FetchB(),
    FetchC()
);

// 任一完成
var (completed, _) = await UniTask.WhenAny(
    PrimaryFetch(),
    Timeout(TimeSpan.FromSeconds(3))  // 超时兜底
);

// 顺序执行
await UniTask.WhenAll(
    Enumerable.Range(0, 10).Select(i => ProcessItem(i))
);

// 延迟执行
await UniTask.DelayFrame(3);          // 等 3 帧
await UniTask.NextFrame();            // 下一帧
await UniTask.WaitUntil(() => flag);  // 等价 WaitUntil
```

### 4.8 生命周期绑定

```csharp
public class MyBehaviour : MonoBehaviour
{
    CancellationToken _ct;

    void Awake()
    {
        // 推荐方式：绑定到 GameObject 销毁
        _ct = this.GetCancellationTokenOnDestroy();
    }

    async UniTaskVoid Start()
    {
        // 用 using 自动管理池化资源的生命周期
        await using var asset = await LoadAssetAsync(path);
    }

    async UniTask LoopTask(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await UniTask.Delay(1000, cancellationToken: ct);
        }
        Debug.Log("Loop 已安全退出");
    }
}
```

### 4.9 调试：UniTaskTracker

在 Unity 菜单栏打开 **Window → UniTask → Tracker**，可以看到：

- 当前存活的 UniTask 数量
- 每个任务的堆栈帧（创建位置）
- 已完成/待处理/已取消的统计

> 对于排查 **UniTask 泄漏**（创建后没人 await）非常有帮助。

---

## 五、协程 → UniTask 迁移实战

### 5.1 对应关系表

| 协程写法 | UniTask 写法 |
|---|---|
| `yield return null` | `await UniTask.Yield()` |
| `yield return new WaitForSeconds(1f)` | `await UniTask.Delay(1000)` |
| `yield return new WaitForEndOfFrame()` | `await UniTask.Yield(PlayerLoopTiming.PostLateUpdate)` |
| `yield return new WaitForFixedUpdate()` | `await UniTask.Yield(PlayerLoopTiming.FixedUpdate)` |
| `yield return new WaitUntil(() => flag)` | `await UniTask.WaitUntil(() => flag)` |
| `yield return new WaitWhile(() => flag)` | `await UniTask.WaitWhile(() => flag)` |
| `StopCoroutine(c)` | `cts.Cancel()` |
| `StartCoroutine(Child())` | `await Child()` |
| `callback(data)` | `return data` |
| `yield return WWW/UnityWebRequest` | `await request.SendWebRequest()` |

### 5.2 典型迁移模式

**协程 → UniTask：等待动画播放**

```csharp
// 旧：协程
IEnumerator PlayAnimationAndWait(Animator anim, string state)
{
    anim.Play(state);
    yield return new WaitForSeconds(anim.GetCurrentAnimatorStateInfo(0).length);
    Debug.Log("动画结束");
}

// 新：UniTask
async UniTask PlayAnimationAndWait(Animator anim, string state, CancellationToken ct = default)
{
    anim.Play(state);
    await UniTask.Delay(
        (int)(anim.GetCurrentAnimatorStateInfo(0).length * 1000),
        cancellationToken: ct
    );
    Debug.Log("动画结束");
}
```

**协程 → UniTask：连续请求**

```csharp
// 旧：协程 + 回调
IEnumerator FetchChain(Action<string> onDone)
{
    var a = "";
    StartCoroutine(FetchA(result => a = result));
    yield return null;  // 粗糙的等待…
    var b = "";
    StartCoroutine(FetchB(a, result => b = result));
    yield return null;
    onDone?.Invoke(b);
}

// 新：UniTask + 返回值
async UniTask<string> FetchChain()
{
    var a = await FetchA();
    var b = await FetchB(a);
    return b;  // 直接返回值
}
```

### 5.3 迁移注意事项

1. **不要混用**：协程中 `await` UniTask 或者 UniTask 中 `yield return` 都会出问题
2. **MonoBehaviour 生命周期**：UniTask 不会因为 `SetActive(false)` 自动停止，必须挂 `CancellationToken`
3. **性能敏感路径**：UniTask 零 GC 的优势在高频调用（Update 的每帧 async 方法）时最明显
4. **渐进迁移**：老的协程 Manager 可以用 `UniTaskCompletionSource` 桥接

---

## 六、选择决策树

```
需要异步操作？
├─ 真的需要多线程？
│   ├─ CPU 密集计算         → Job System / Burst / 子线程
│   └─ 只是"等一段时间"    → 单线程异步足够
│
├─ 需要返回值？
│   ├─ 多次嵌套返回         → UniTask（必须）
│   └─ 一次性回调           → 协程 + Action 回调（勉强能用）
│
├─ 需要取消/超时？
│   ├─ 是                   → UniTask（CancellationToken）
│   └─ 否                   → 可以选协程，但推荐 UniTask
│
├─ 需要在子线程计算后回主线程？
│   └─ UniTask.SwitchToMainThread
│
└─ 现有代码全是协程，不想大改？
    └─ 缓存 WaitForSeconds + 回调模式，新模块用 UniTask 逐步替代
```

> **推荐**：新项目、新模块默认用 UniTask。旧协程代码在重构触及时逐步迁移，不要一次性全量替换。

---

## 七、推荐阅读

- [[unity知识点-2026-04-28]] — Unity 日常知识点
- [[unity知识点-2026-04-29]] — 进阶话题
- [[DOTS详解]] — Jobs System 实现真正的多线程并行
- [[Profiler自定义采样]] — 检测协程/UniTask 的性能开销
- Cysharp/UniTask GitHub: https://github.com/Cysharp/UniTask
- Microsoft Docs: Async state machines — 编译器生成的状态机详细说明
