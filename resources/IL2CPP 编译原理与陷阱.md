---
title: "IL2CPP 编译原理与陷阱"
type: resource
tags: [unity, il2cpp, build, aot, reflection]
created: "2026-07-28"
updated: "2026-07-28"
status: active
summary: "IL2CPP 编译流水线、泛型处理、代码裁剪、反射限制、Mono 对比"
---

# IL2CPP 编译原理与陷阱

## 什么是 IL2CPP

IL2CPP（Intermediate Language To C++）是 Unity 的 AOT（Ahead-of-Time）编译后端。

```
C# 源码 → IL（中间语言）→ C++ 代码 → 原生二进制
```

## 编译流程

```
Step 1: C# 编译器 (csc/mcs)
  └─> .NET 中间语言 (IL / CIL)     ← 所有 C# 代码先转成 IL

Step 2: IL2CPP 工具链
  └─> C++ 源代码                    ← IL2CPP 将 IL 翻译成 C++

Step 3: 平台 C++ 编译器 (clang/GCC/MSVC)
  └─> 原生机器码 (.so/.dll/.dylib)  ← 最终产物
```

## IL2CPP vs Mono 对比

| 维度 | Mono | IL2CPP |
|------|------|--------|
| 编译方式 | JIT（部分 AOT） | 纯 AOT |
| 编译时间 | 快 | 慢（多了 C++ 编译） |
| 运行时性能 | 解释执行 | 原生二进制，通常更快 |
| 包体大小 | 依赖 Mono runtime（~10MB） | 可剥离未用代码，包体通常更小 |
| 泛型支持 | 完整（JIT 生成特化） | 代码分裂/共享机制 |
| 反射能力 | 完整 | 受限（需要 link.xml） |
| 平台要求 | 合 Android/Editor 开发 | iOS/主机/WebGL 必须用 |
| 代码安全 | 易反编译（IL 层） | 难反编译（原生二进制） |

## 泛型处理（代码分裂/共享）

IL2CPP 不支持 JIT，因此不能在运行时为泛型生成新特化版本。它使用两种策略：

### 引用类型泛型参数 — 共享

```csharp
List<string> 和 List<object> 共享同一份 C++ 实现
```

### 值类型泛型参数 — 特化

```csharp
List<int> 和 List<float> 各自生成独立的 C++ 实现
```

**陷阱**：IL2CPP 在编译时只能生成"它能看到的"泛型特化。如果一个泛型类型只在运行时通过反射加载，它可能没有对应的 C++ 实现 → **MissingMethodException**。

## 代码裁剪（Managed Stripping）

IL2CPP 搭配 Unity 的代码裁剪系统，会移除未使用的 IL 代码来减小包体。

| Level | 行为 | 风险 |
|-------|------|------|
| Disabled | 不裁剪 | 无风险，包体大 |
| Low | 移除未使用的类和方法 | 低风险 |
| Medium | 激进移除，包括部分编辑器代码 | 中等风险 |
| High | 最激进，可能移除反射依赖的类型 | 高风险 |

**陷阱**：高裁剪级别下，通过 `Activator.CreateInstance`、`Assembly.GetType` 等反射方式创建的类型会被视为"未使用"而被移除。

## 反射限制（最重要陷阱）

AOT 编译下动态创建类型的操作会失败：

| 操作 | Mono | IL2CPP |
|------|------|--------|
| `new GameObject("name")` | ✅ | ✅ |
| `Activator.CreateInstance(Type)` | ✅ | ❌ 需要 link.xml |
| `Assembly.Load(byte[])` | ✅ | ❌ 不支持 |
| `typeof(T).GetMethod("Foo")` | ✅ | ✅（类型可识别） |
| `typeof(T).MakeGenericType(args)` | ✅ | ❌ 运行时泛型构造受限 |
| `FindObjectsByType` | ✅ | ✅ |

### 解决方案：link.xml

```xml
<linker>
  <!-- 保留整个程序集 -->
  <assembly fullname="Assembly-CSharp" preserve="all"/>

  <!-- 保留特定类型 -->
  <assembly fullname="UnityEngine">
    <type fullname="UnityEngine.Advertisements" preserve="all"/>
  </assembly>

  <!-- 保留特定命名空间 -->
  <assembly fullname="Newtonsoft.Json">
    <namespace fullname="Newtonsoft.Json" preserve="all"/>
  </assembly>
</linker>
```

### 备选方案

```csharp
// 方式1：用 PreserveAttribute 标记
using UnityEngine.Scripting;

public class MyType
{
    [Preserve]
    public void MethodUsedByReflection() { }  // IL2CPP 会保留
}

// 方式2：用 iphone 预处理（仅 iOS）
[System.Diagnostics.Conditional("UNITY_IPHONE")]
class PreserveAttribute : System.Attribute { }
```

## IL2CPP 性能特征

### 优点
- **运行时性能更好**：原生二进制 vs 解释执行，数值密集型计算明显更快
- **包体更小**：代码裁剪 + 不需要 Mono runtime
- **跨平台一致**：所有平台行为一致（不会像 Mono 一样 JIT 行为差异）
- **安全性高**：反编译难度大幅提升

### 缺点
- **编译极慢**：多了一道 C++ 编译步骤，大型项目首次编译可能 ~30 分钟
- **Debug 困难**：报错栈跟踪是 C++ 级别的，需要 IL2CPP Call Stack 映射
- **开发迭代慢**：改一行代码 → 整个 IL2CPP 重新编译，反馈周期长
- **动态特性受限**：反射、动态代码生成、运行时泛型都受限

## 最佳实践

1. **开发阶段用 Mono**，发布阶段切 IL2CPP
2. **提前配置 link.xml**，避免发布后出现 MissingMethodException
3. **避免运行时 `Assembly.Load` / `Activator.CreateInstance`**，改用工厂模式 + Preserve
4. **使用 `[Preserve]` 标记反射入口**，特别是序列化回调、Json 解析、IoC 容器
5. **泛型不要在运行时动态构造**（`MakeGenericType`），编译时能确定的就定死
6. **IL2CPP 的 `System.Reflection.Emit` 不可用**，需要动态代码生成用 Emit 的请考虑替代方案
7. **代码裁剪从 Low 开始**，逐步提高到 Medium 和 High，每次 Build 测试

## 参考

- [IL2CPP Overview - Unity Manual](https://docs.unity3d.com/Manual/IL2CPP.html)
- [Managed Stripping - Unity Manual](https://docs.unity3d.com/ManagedStripping)
- [link.xml - Unity Manual](https://docs.unity3d.com/Manual/ManagedStrippingLinkXml)
