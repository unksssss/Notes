---
title: "UnityEngine.Object 判空与销毁机制"
type: resource
tags: [unity, 生命周期, 面试, 内存]
created: "2026-08-12"
updated: "2026-08-12"
status: active
summary: "Unity 对象双层结构：托管壳 + native 芯；Destroy 只销毁 native；== 重载使销毁即空；MissingReferenceException；假空对象"
---

# UnityEngine.Object 判空与销毁机制

（Day 20 知识点）

## 双层结构

一个 GameObject / 脚本组件由两部分组成：

```
C# 侧：托管对象（壳）→ 由 .NET GC 管理，在托管堆上
native 侧：C++ 对象（芯）→ Unity 引擎内部，不由 .NET GC 管
```

你的引用指向的是**托管壳**。

## Destroy 只销毁 native

```csharp
Destroy(go);   // 只销毁 native 部分！
```

- **native 芯**：立即标记销毁（帧末真正清理）
- **托管壳**：还躺在托管堆上，要等 **GC 回收**（时间不确定）

所以销毁后引用**本身根本不是 null**。

## == 被重载了

`UnityEngine.Object` 重载了 `==` / `!=`：比较时不看托管引用，而是检查背后对应的 **native 对象是否已销毁**——已销毁 → `== null` 返回 `true`。

## 由此产生的经典坑

1. **假空对象**：`myComp == null` 为 true，但它不是真 null——`ReferenceEquals(myComp, null)` 会返回 **false**（它绕过了重载）
2. 访问已销毁对象的字段/方法，抛 **`MissingReferenceException`**，不是 `NullReferenceException`
3. 项目里**判空统一用 `==`**，别在同一处混用 `==` / `??` / `?.`（`??` / `?.` 部分版本不走重载，行为会不一致）
4. `DestroyImmediate`：编辑器里即时销毁（运行时不要用）

## 为什么 Unity 要这么设计（面试加分点）

native 对象的销毁是**确定性**的（马上标记），而托管 GC 是**延迟**的（不知道啥时候来）。如果不重载 `==`，销毁后的引用会一直"看似有效"，一访问就报错；重载后，`== null` 能**立刻反映销毁状态**，让代码安全判断。

## 相关

- [[MonoBehaviour生命周期与SetActive的坑]]
- [[对象池 OnEnable OnDisable 最佳实践]]
