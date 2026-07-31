---
title: "Addressables 资源生命周期"
type: resource
tags: [unity, 资源管理, 性能优化, 面试]
created: "2026-07-31"
updated: "2026-07-31"
status: active
summary: "Addressables 引用计数机制：Load/Release 成对、计数累加、只 Load 不 Release = 内存泄漏；与 Resources 对比"
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

## 相关

- [[对象池实现]]
