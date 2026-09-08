---
title: "Unity 序列化机制"
type: resource
tags: [序列化, unity, 面试]
created: "2026-09-08"
updated: "2026-09-08"
status: active
summary: "Unity 序列化三问：哪些类型能被序列化/为什么 Dictionary 要绕 wrapper/私有字段如何序列化；Unity 6.6 官方新特性：Inspector 原生 Dictionary 序列化（两列 key-value + 编译期校验，无需自写 wrapper）"
---

# Unity 序列化机制

> 来源：Unity 6.6（6000.6.0f1，09-02 发布）官方新特性 + 序列化基础整理（Day 37 官方新特性，首学）

## Unity 序列化基础

Unity 序列化器（SerializedObject/Inspector）的能力范围：

- **可序列化**：`[Serializable]` 标记的自定义类/结构体、基本类型、`UnityEngine.Object` 引用（存 GUID 而非数据）、数组、`List<T>`（T 可序列化时）
- **不直接支持**：Dictionary、接口字段、泛型容器（旧版）、只读属性、static 字段、`const`/`readonly`
- **私有字段**：加 `[SerializeField]` 即可序列化（Unity 不看访问级别，看字段本身）
- **属性（Property）不序列化**：只有字段进序列化器
- 序列化 ≠ JsonUtility/Json.NET：Unity 序列化走 **YAML**（场景/预制体/asset 文件），JsonUtility 是运行时导出 JSON 的快照工具，两者机制不同

### 旧版 Dictionary 的三条绕路方案

1. 自写 `[Serializable] public class StringIntDict : Dictionary<string,int> {}` 再包一层（仍有限制）
2. 拆成两个平行 List（keyList + valueList）运行时同步——最常用的人肉方案
3. 用第三方（Odin）自定义 Inspector 绘制

## Unity 6.6 原生 Dictionary 序列化（官方新特性）

Unity 6.6（6000.6.0f1）起：

- **Inspector 直接序列化 Dictionary**，无需 wrapper 类、无需平行 List、无需 Odin
- 显示为**两列 key-value 布局**（可增删改）
- 带**编译期校验规则**：不支持的签名（如非序列化 key 类型）直接编译期报错，而非运行时才炸
- 写配置表/数据驱动的痛点大幅缓解（[[scriptableobject数据驱动设计]] 配置流的好搭档）

## 面试表达框架

1. Unity 序列化器只看**字段**，`[Serializable]` + `[SerializeField]` 双标记覆盖自定义类与私有字段
2. Dictionary 老痛点：非序列化容器 → wrapper/平行 List/Odin 三绕路
3. Unity 6.6 原生支持 Dictionary 序列化 + 编译期校验（新特性加分点）
