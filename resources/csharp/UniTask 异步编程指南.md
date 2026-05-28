---
tags:
  - unity
  - csharp
  - async
  - unitask
  - 异步编程
created: 2026-05-21
source:
  - https://www.cnblogs.com/clnchanpin/p/19685090
  - https://www.cnblogs.com/Firepad-magic/p/18229337
---

# UniTask 异步编程指南

## 什么是 UniTask

**UniTask** 是由日本开发商 **Cysharp**（也是 `ZeroFormatter`、`MessagePack` 的开发者）为 Unity 深度定制的高性能异步任务库。它以 `struct` 替代 `class`，实现**零 GC 分配**，是 Unity 官方 `Task` 和协程的最佳替代方案。

### 核心优势

| 痛点 | UniTask 解决方案 |
|------|----------------|
| 协程无法返回值 | 异步任务直接 `return` |
| 难以取消 / 超时 | 内置 `CancellationToken`，支持超时 |
| GC 开销高 | `struct` 实现，避免迭代器 GC |
| 嵌套混乱 | `async/await` 让代码线性化 |
| 主线程回调管理 | `SwitchToMainThread()` 可靠切换 |

---

## 安装

通过 UPM 添加 Git URL：

```
https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask
```

或通过 OpenUPM：

```bash
openupm add com.cysharp.unitask
```

命名空间：`using Cysharp.Threading.Tasks;`

---

## API 速查

| API                               | 用途         | 示例                                     |
| --------------------------------- | ---------- | -------------------------------------- |
| `UniTask.Delay(ms)`               | 延迟执行       | `await UniTask.Delay(1000);`           |
| `.ToUniTask()`                    | 转换原生异步操作   | `asyncOp.ToUniTask(cancellationToken)` |
| `UniTask.Run(Action)`             | 后台线程执行     | 耗时计算用，需切回主线程                           |
| `GetCancellationTokenOnDestroy()` | 物体销毁时自动取消  | 写在 `MonoBehaviour` 中                   |
| `CancellationTokenSource`         | 手动取消       | `cts.Cancel()`                         |
| `UniTask.WhenAll`                 | 并行等待所有任务   | 批量资源加载                                 |
| `UniTask.WhenAny`                 | 最快结果返回     | 多节点请求取最快                               |
| `UniTask.Yield()`                 | 等待下一帧      | 替代 `yield return null`                 |
| `UniTask.SwitchToMainThread()`    | 切回主线程      | 后台任务后更新 UI                             |
| `.Forget()`                       | 忘记 / 触发式异步 | 避免未 await 的编译警告                        |
| `UniTask.Void` / `UniTaskVoid`    | 无返回值的异步方法  | `async UniTaskVoid Start()`            |

---

## 实战案例

### 1. 异步资源加载（支持超时取消）

```csharp
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class AsyncResourceLoader : MonoBehaviour
{
    [SerializeField] private Image targetImage;
    [SerializeField] private string spritePath = "Sprites/Icon";

    private async void Start()
    {
        var cancellationToken = this.GetCancellationTokenOnDestroy();
        try
        {
            Sprite loadSprite = await Resources.LoadAsync<Sprite>(spritePath)
                .ToUniTask(cancellationToken: cancellationToken, timeoutMilliseconds: 5000);
            if (loadSprite != null && targetImage != null)
                targetImage.sprite = loadSprite;
        }
        catch (System.OperationCanceledException)
        {
            Debug.LogWarning("图片加载被取消（物体销毁或超时）");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"图片加载失败：{e.Message}");
        }
    }
}
```

### 2. 定时 / 延迟任务（替代 Invoke / 协程延迟）

```csharp
public class TimerTaskDemo : MonoBehaviour
{
    private CancellationTokenSource cts;

    private async void Start()
    {
        cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        // 延迟 2 秒
        await UniTask.Delay(2000, cancellationToken: cancellationToken);
        ShowTip();

        // 每 3 秒刷新
        while (!cancellationToken.IsCancellationRequested)
        {
            await UniTask.Delay(3000, cancellationToken: cancellationToken);
            // 更新 UI 逻辑
        }
    }

    public void CancelAllTasks()
    {
        cts?.Cancel();
        cts?.Dispose();
    }

    private void OnDestroy() => CancelAllTasks();
}
```

### 3. 多任务并行（WhenAll）

```csharp
async void Start()
{
    var ct = this.GetCancellationTokenOnDestroy();

    (Sprite sprite, AudioClip audio, GameObject prefab) = await UniTask.WhenAll(
        Resources.LoadAsync<Sprite>(spritePath).ToUniTask(ct),
        Resources.LoadAsync<AudioClip>(audioPath).ToUniTask(ct),
        Resources.LoadAsync<GameObject>(prefabPath).ToUniTask(ct)
    );

    Instantiate(prefab, transform);
}
```

### 4. 角色动作系统（Forget 启动）

```csharp
public class CharacterController : MonoBehaviour
{
    private void Start()
    {
        WalkAsync().Forget();
        AttackAsync().Forget();
    }

    private async UniTask WalkAsync()
    {
        while (true)
        {
            PlayAnimation("Walk");
            await UniTask.Delay(2000);
        }
    }

    private async UniTask AttackAsync()
    {
        PlayAnimation("Attack");
        await UniTask.Delay(1000);
        DealDamage(10);
    }
}
```

### 5. Socket 心跳机制（后台线程 + 主线程切换）

```csharp
public class SocketHeartbeat : MonoBehaviour
{
    private CancellationTokenSource cts;

    async void Start()
    {
        cts = new CancellationTokenSource();

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                await UniTask.SwitchToMainThread(); // 切回主线程发数据
                // 发送心跳包...
                await UniTask.Delay(1000, cancellationToken: cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log("心跳停止");
        }
    }

    void OnDestroy() => cts?.Cancel();
}
```

---

## Forget() — 触发后不管（Fire & Forget）

### 为什么需要 Forget

当一个 `async UniTask` 方法被调用但不 `await` 时，编译器会报警告。`.Forget()` 告诉编译器"我知道没 await，异常我负责处理"：

```csharp
private async UniTask LoadDataAsync()
{
    await UniTask.Delay(1000);
    Debug.Log("加载完成");
}

private void Start()
{
    LoadDataAsync();          // ⚠️ 编译警告
    LoadDataAsync().Forget(); // ✅ 无警告，自动捕获异常
}
```

### Forget vs async void vs UniTaskVoid

| 方式 | 是否可 await | 异常处理 | 推荐场景 |
|------|:------------:|:--------:|----------|
| `.Forget()` | ✅ 可 await | ✅ Editor 中打印异常到 Console | **推荐** |
| `async void` | ❌ 不能 | ❌ 异常会崩溃进程 | 别用 |
| `UniTaskVoid` | ❌ 不能 | ⚠️ 默认静默丢失 | 仅用于 Event 回调 |

### 最佳实践

```csharp
public void OnClick()
{
    PlayAttackAnimationAsync().Forget(); // ✅ 点按钮触发动画，不等结果
}

private async UniTask PlayAttackAnimationAsync()
{
    await UniTask.Delay(500);
    // 播放动画...
}
```

### 📌 记住

- **需要等结果的 → `await`**
- **不需要等结果的 → `.Forget()`**
- Release build 下异常默认吃掉，可通过 `UniTaskScheduler.UnobservedTaskException` 自定义

---

## CancellationToken 取消特定任务

### 思路

每个独立任务持有自己的 `CancellationTokenSource`，调用 `Cancel()` 只取消该任务。配合 `CancelAndNew` 模式，取消后还能重新启动。

### 多个 cts 管理独立任务

```csharp
public class MultiTaskManager : MonoBehaviour
{
    private CancellationTokenSource cts_attack;
    private CancellationTokenSource cts_move;
    private CancellationTokenSource cts_buff;

    private void Start()
    {
        cts_attack = new CancellationTokenSource();
        cts_move   = new CancellationTokenSource();
        cts_buff   = new CancellationTokenSource();

        AttackLoopAsync(cts_attack.Token).Forget();
        MoveLoopAsync(cts_move.Token).Forget();
        BuffLoopAsync(cts_buff.Token).Forget();
    }

    // 取消指定任务
    public void StopAttack() => CancelAndNew(ref cts_attack);
    public void StopMove()   => CancelAndNew(ref cts_move);
    public void StopBuff()   => CancelAndNew(ref cts_buff);

    private async UniTask AttackLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            Debug.Log("攻击！");
            await UniTask.Delay(1000, cancellationToken: token);
        }
        Debug.Log("攻击任务已取消");
    }

    private async UniTask MoveLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            Debug.Log("移动中...");
            await UniTask.Delay(500, cancellationToken: token);
        }
    }

    private async UniTask BuffLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            Debug.Log("Buff生效中");
            await UniTask.Delay(2000, cancellationToken: token);
        }
    }

    // 取消旧的 + 创建新的 cts（支持重新启动）
    private void CancelAndNew(ref CancellationTokenSource cts)
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = new CancellationTokenSource();
    }

    private void OnDestroy()
    {
        cts_attack?.Cancel(); cts_attack?.Dispose();
        cts_move?.Cancel();   cts_move?.Dispose();
        cts_buff?.Cancel();   cts_buff?.Dispose();
    }
}
```

### 场景速查

| 场景 | 做法 |
|------|------|
| 一个任务一个取消令牌 | 每个任务各自持有自己的 `CancellationTokenSource` |
| 取消指定任务 | `cts.Cancel()` → token 检查点抛出 `OperationCanceledException` |
| 取消后重新启动 | `CancelAndNew(ref cts)`：先取消旧的，再 `new` 一个 |
| 一个 cts 管多个子任务 | 多个任务共享同一个 token，`Cancel()` 全部结束 |
| 物体销毁自动取消 | `this.GetCancellationTokenOnDestroy()` 替代手动 cts |

### 扩展方法技巧

```csharp
public static class CancellationTokenExtensions
{
    public static CancellationTokenSource Renew(this CancellationTokenSource cts)
    {
        cts?.Cancel();
        cts?.Dispose();
        return new CancellationTokenSource();
    }
}

// 使用
cts = cts.Renew();
```

---

## 避坑指南

1. **命名空间不能漏**：`using Cysharp.Threading.Tasks;`
2. **主线程限制**：`UniTask.Run` 后台线程中不能操作 `GameObject` / UI，必须用 `SwitchToMainThread()` 切回来
3. **总是加取消逻辑**：使用 `GetCancellationTokenOnDestroy()` 或 `CancellationTokenSource`，防止物体销毁后任务残留导致内存泄漏
4. **版本适配**：Unity 2020+ 建议 UniTask 2.0+，检查 GitHub 兼容性说明
5. **适度使用**：简单的延迟（无返回值、无需取消）仍可用原生协程，复杂场景才用 UniTask

---

## 参考链接

- [GitHub - Cysharp/UniTask](https://github.com/Cysharp/UniTask)
- [UniTask 中文文档](https://www.lfzxb.top/unitask_reademe_cn/)
- [知乎 - UniTask 中文使用指南](https://zhuanlan.zhihu.com/p/572670728)
