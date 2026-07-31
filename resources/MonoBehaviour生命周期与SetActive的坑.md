---
title: "MonoBehaviour 生命周期与 SetActive 的坑"
type: resource
tags: [unity, 生命周期, 面试]
created: "2026-07-31"
updated: "2026-07-31"
status: active
summary: "Awake/OnEnable/Start 生命周期；初始 inactive 对象不触发 Awake（首次激活才触发）；协程与 SetActive 的关系"
---

# MonoBehaviour 生命周期与 SetActive 的坑

## 标准生命周期

```
Awake（首次激活，仅一次）
  ↓
OnEnable（每次激活都执行）
  ↓
Start（激活后的第一帧 Update 前，仅一次）
  ↓
Update（每帧）
  ↓
OnDisable（每次禁用都执行）
  ↓
OnDestroy（销毁时执行）
```

## ⚠️ 初始 inactive 的坑（面试高频）

- **`Awake` 的触发条件是"第一次激活"，不是"场景加载"！**
- 场景里初始 `SetActive(false)` 的对象 → 加载时 **Awake 不执行** → 第一次 `SetActive(true)` 时 **Awake + OnEnable 一起触发**（顺序：先 Awake 后 OnEnable）
- **连环坑**：`Awake` 里缓存组件引用（`_rb = GetComponent<Rigidbody>()`），对象初始 inactive 时，激活前访问 `_rb` 就是 **null** → NRE！
- 实战教训：引用缓存尽量放 Awake；若对象可能初始 inactive，要么"使用前先激活"，要么把缓存逻辑挪到首次 OnEnable

## 相关细节

- 协程：GameObject 被 `SetActive(false)` 或销毁时，协程**会停止**；组件 `enabled = false` **不会**停止协程
- `Start` 在对象激活后的第一帧 Update 前执行；初始 inactive 则从激活后开始计时
- 对象池场景：状态重置必须写在 `OnEnable`（每次激活都触发）而不是 `Start`（只执行一次）

## 相关

- [[对象池实现]]
- [[对象池 OnEnable OnDisable 最佳实践]]
