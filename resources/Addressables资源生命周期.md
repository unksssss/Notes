---
title: "Addressables 资源生命周期"
type: resource
tags: [unity, 资源管理, 性能优化, 面试]
created: "2026-07-31"
updated: "2026-08-18"
status: active
summary: "Addressables 引用计数机制：Load/Release 成对、计数累加、只 Load 不 Release = 内存泄漏；LoadAssetAsync/Release 与 InstantiateAsync/ReleaseInstance 配对规则与错误后果；与 Resources 对比"
---

# Addressables 资源生命周期

## 为什么用 Addressables（对比 Resources）

| | Resources.Load | Addressables |
|---|---|---|
| 资源打包 | 全部打进主包 | 可分 AssetBundle 按需下载 |
| 启动加载 | 启动慢（全解析） | 按需加载 |
| 单个资源卸载 | ❌ 无法卸载（资源本体常驻内存） | ✅ 引用计数归零自动卸载 |
| 远程热更 | ❌ | ✅ 可打远程包 |

## 核心三件套

- **Addressable Asset**：勾选 Addressable 标记的资源，有唯一字符串地址（key）
- **AsyncOperationHandle**：`LoadAssetAsync<T>()` 返回的异步句柄，一切记账靠它
- **引用计数**：每次 Load **+1**，每次 Release **-1**

## 生命周期流程

```
LoadAssetAsync(key)   →  引用计数 +1 → 资源进内存
       │
       ▼
   ...游戏中使用中...
       │
       ▼
Release(handle)       →  引用计数 -1 → 计数归 0 → 资源被卸载 ✅
```

## 黄金规则（面试必答）

1. **Load 和 Release 必须成对出现**，像借钱还钱
2. **只 Load 不 Release = 内存泄漏**——资源永远不会卸载
3. **Load 3 次 → 计数 3 → 必须 Release 3 次**，少一次都卸不掉
4. **必须通过 handle 释放**：`Addressables.Release(handle)`，不能只丢引用
5. 实例化用 `InstantiateAsync` + `ReleaseInstance` 配对
6. 释放前检查 `handle.IsValid()`，避免重复释放报错
7. `LoadSceneAsync` 加载的场景用 `UnloadSceneAsync` 卸载，同样计数配对

## 释放 API 配对（Day 25 加深）

| 加载方式 | 释放 API | 内部行为 |
|---|---|---|
| `LoadAssetAsync<T>(key)` | `Addressables.Release(handle)` | 只减引用计数（-1） |
| `InstantiateAsync(key)` | `Addressables.ReleaseInstance(instance)` | **销毁实例 + 减引用计数**一步到位 |

> `InstantiateAsync` = `LoadAssetAsync`（计数+1）+ 实例化 GameObject，所以比方式一多一层"实例管理"。

**配对错误的后果**：

| 错误写法 | 后果 |
|---|---|
| InstantiateAsync 的实例却用 `Release(handle)` | 计数减了但**实例没销毁** → GameObject 残留场景，引用关系混乱 |
| 手动 `Destroy` 实例却忘了 `ReleaseInstance` | 实例没了但**计数不减** → 资源永远驻留内存 → **内存泄漏** |
| 销毁实例后又调 `ReleaseInstance` | 重复释放，计数错乱，可能报错或提前卸载导致其他实例 Missing |

> **核心**：引用计数**归零才真正卸载**。配对错误 = 计数永远不归零 = 资源泄漏！

## 相关

- [[对象池实现]]
- [[Resources.Load 加载与 as 转型]]
