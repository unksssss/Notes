---
title: "UnityEngine.Object 判空与销毁机制"
type: resource
tags: [unity, 生命周期, 面试, 内存]
created: "2026-08-12"
updated: "2026-09-03"
status: active
summary: "Unity 对象双层结构：托管壳 + native 芯；Destroy 只销毁 native；== 重载使销毁即空；MissingReferenceException；假空对象；为什么分两侧（历史/性能/生命周期主权）"
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

## 为什么整个架构要分成 C# 侧 + native 侧（Day 34 追问）

**一句话**：Unity 引擎本体是 C++，脚本是 C#——对象要同时活在两个内存规则不同的世界里，于是真实数据与生命周期归 native 芯，C# 只留薄壳做访问入口，InstanceID 桥接。分两侧是**历史 + 性能 + 生命周期主权**三重原因逼出来的：

1. **历史基因**：引擎 2005 年起就是 C++（物理 PhysX/渲染/动画全 native），C# 是后加的脚本指挥层——C# 全删引擎照样跑，C# 对象只能是映射到 C++ 对象上的壳
2. **性能**：热数据（Transform 矩阵/Mesh 顶点/贴图/刚体）放 C++ 自有内存，引擎热路径零托管开销、可池化自定义分配；放托管堆则每次访问跨语言边界 + 受 GC 牵制 → **壳很薄、芯很重**
3. **生命周期主权（最关键）**：对象生死若交 .NET GC——时机不可控（Boehm 延迟），且 C++ 还握着指针时被回收=崩溃。引擎拿走控制权：`Destroy` 确定性销毁 native 芯；GC 只回收壳；壳销毁后变"假空"（== 重载/MRE 全是这个设计的连锁反应）

**销毁语义三连**（谁没了 vs 何时）：

| 操作 | 谁没了 | 时机 |
|---|---|---|
| `go = null` | 只丢引用，谁都没销毁 | 立即（芯活着=潜在泄漏） |
| `Destroy(go)` | native 芯没了 | 确定性：立即标记、帧末清理 |
| GC 收壳 | 只剩壳被回收 | 延迟不确定 |

4. **序列化解耦（bonus）**：Inspector/场景/预制体的引用存的不是 C# 指针（重编译壳全重建），而是 InstanceID/GUID——重编译、热重载后靠它重新找到 native 芯

**比喻 🛋️**：壳=遥控器，芯=电视机。扔遥控器（GC）电视还在；砸电视（Destroy）遥控器还在但按了没反应（假空 + MRE）；`==` 重载是因为"遥控器在不在手上"不重要，"电视通不通电"才重要。

**面试金句**：> 引擎 C++ + 脚本 C# 两个世界内存规则不同 → 真实数据与生命周期归引擎（性能 + 确定性销毁），C# 留薄壳做入口，InstanceID 桥接；Destroy 销毁芯、GC 收壳、== 重载识别"芯没了壳还在"的假空。

## 相关

- [[MonoBehaviour生命周期与SetActive的坑]]
- [[对象池 OnEnable OnDisable 最佳实践]]
