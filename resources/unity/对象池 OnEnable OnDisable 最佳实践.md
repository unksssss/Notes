---
title: "对象池 OnEnable OnDisable 最佳实践"
type: resource
tags: [unity, gc, memory, objectpool, pattern]
created: "2026-07-21"
updated: "2026-07-21"
status: active
summary: "利用 OnEnable/OnDisable 配合对象池实现自动重置，避免手动重置状态的耦合问题"
---

# 对象池 OnEnable / OnDisable 最佳实践

## 问题场景

对象池的核心逻辑：
```
从池取对象 → 使用 → 归还对象
             ↓
      状态需要重置！
```

当对象有很多运行时状态（位置、速度、材质、动画等）时，手动逐字段重置会产生严重的**耦合问题**：

```cs
// ❌ 问题写法：归还时手动重置每个状态
public class Bullet : MonoBehaviour
{
    private Vector3 _velocity;
    private float _lifetime;
    private Material _material;
    private MeshRenderer _renderer;

    // 回归池时手动重置...
    public void Reset()
    {
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        _velocity = Vector3.zero;
        _lifetime = 0;
        _material.color = Color.white;
        _renderer.enabled = true;
        // ...一旦新增字段就很容易漏掉
    }
}
```

**耦合问题：**
1. 忘记新增字段 → 状态残留
2. 多人协作时不知道哪些字段要重置
3. 与对象池逻辑直接绑定，不能复用到其他地方

## 最佳实践：利用 OnEnable / OnDisable 生命周期

### 核心思路

Unity 的组件生命周期提供了天然的"初始化钩子"：

| 生命周期 | 触发时机 | 用于对象池 |
|:---------|:---------|:-----------|
| `OnEnable()` | 每次 `SetActive(true)` 时触发 | ✅ 初始化/重置状态 |
| `OnDisable()` | 每次 `SetActive(false)` 时触发 | ✅ 清理运行时状态 |
| `Awake()` | **仅一次**，实例化时 | ✅ 只执行一次的配置 |

```cs
// ✅ 最佳实践：对象本身负责自己状态的初始化和清理
public class Bullet : MonoBehaviour
{
    [Header("初始值（Inspector 可配）")]
    [SerializeField] private Color _defaultColor = Color.white;
    
    private Rigidbody _rb;
    private MeshRenderer _renderer;
    private float _spawnTime;

    private void Awake()
    {
        // 只做一次：缓存组件引用
        _rb = GetComponent<Rigidbody>();
        _renderer = GetComponent<MeshRenderer>();
    }

    private void OnEnable()
    {
        // ✅ 自动重置：每次从池取出时触发
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _renderer.material.color = _defaultColor;
        _spawnTime = Time.time;
    }

    private void OnDisable()
    {
        // ✅ 自动清理：每次归池时触发
        // 停止所有协程、取消 Tween、清理临时状态
        StopAllCoroutines();
    }

    private void Update()
    {
        // 飞行逻辑...
        if (Time.time - _spawnTime > _lifetime)
        {
            // 归池
            gameObject.SetActive(false);  
            // ↑ 自动触发 OnDisable
        }
    }
}
```

### 对象池类本身只需要管理激活状态

```cs
public class SimpleObjectPool<T> where T : MonoBehaviour
{
    private readonly Stack<T> _pool = new();
    private readonly T _prefab;

    public SimpleObjectPool(T prefab, int preloadCount = 0)
    {
        _prefab = prefab;
        for (int i = 0; i < preloadCount; i++)
        {
            var obj = CreateNew();
            obj.gameObject.SetActive(false);
            _pool.Push(obj);
        }
    }

    public T Get()
    {
        T obj;
        if (_pool.Count > 0)
        {
            obj = _pool.Pop();
        }
        else
        {
            obj = CreateNew();  // 池满扩容
        }
        obj.gameObject.SetActive(true);  // → 自动触发 OnEnable
        return obj;
    }

    public void Return(T obj)
    {
        obj.gameObject.SetActive(false);  // → 自动触发 OnDisable
        _pool.Push(obj);
    }

    private T CreateNew()
    {
        var obj = Object.Instantiate(_prefab);
        // 放在 DontDestroyOnLoad 场景中，规避场景切换销毁
        Object.DontDestroyOnLoad(obj.gameObject);
        return obj;
    }
}
```

### 为什么不推荐手动 Reset 方法

```cs
// ⚠️ 两种常见的错误写法：

// 错误1：池类调对象的重置方法（耦合）
public class BulletPool
{
    public void Return(Bullet bullet)
    {
        bullet.Reset();          // ← 池类需要知道 Bullet 有哪些状态
        bullet.gameObject.SetActive(false);
    }
}

// 错误2：在 Awake/Start 中重置状态（只执行一次，不会在归还时触发）
private void Awake()
{
    transform.position = Vector3.zero;  // ❌ 只跑一次
}
```

### 完整模板

```cs
/// <summary>
/// 对象池基类 — 使用 OnEnable/OnDisable 自动管理状态
/// </summary>
public abstract class PooledBehaviour : MonoBehaviour
{
    /// <summary>
    /// 子类在此方法中做初始化（每次从池取出时调用）
    /// </summary>
    protected abstract void OnPoolEnable();

    /// <summary>
    /// 子类在此方法中做清理（每次归池时调用）
    /// </summary>
    protected abstract void OnPoolDisable();

    // 模板方法模式：封装 OnEnable/OnDisable
    private void OnEnable()
    {
        OnPoolEnable();
    }

    private void OnDisable()
    {
        OnPoolDisable();
    }
}

// 使用示例
public class Effect : PooledBehaviour
{
    private ParticleSystem _ps;

    private void Awake()
    {
        _ps = GetComponent<ParticleSystem>();
    }

    protected override void OnPoolEnable()
    {
        _ps.Play();
    }

    protected override void OnPoolDisable()
    {
        _ps.Stop();
        _ps.Clear();  // 保证粒子轨迹清除
    }
}
```

## 要点总结

| 原则 | 说明 |
|:-----|:------|
| **谁的状态谁负责** | 每个对象自己管理重置逻辑，不要让池类来重置 |
| **利用生命周期** | `OnEnable` = 初始化，`OnDisable` = 清理，`Awake` = 一次性配置 |
| **只控制激活** | 池类只需要调用 `SetActive(true/false)`，不要手动调 Reset |
| **DontDestroyOnLoad** | 池化对象放 DontDestroyOnLoad 场景防止场景切换时销毁 |
| **池满扩容** | 取对象时空池就 `Instantiate`，不需要预分配所有对象 |

## 参考

- Unity Manual: Order of Execution for Event Functions
- Unity Best Practice: Object Pooling
