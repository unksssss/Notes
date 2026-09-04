---
title: "Resources.Load 加载与 as 转型"
type: resource
tags: [unity, 资源管理, csharp, 项目实战]
created: "2026-08-18"
updated: "2026-08-18"
status: active
summary: "Resources.Load<T> 泛型 vs 非泛型+as 的机制、引用类型 as 无装箱误解、加载失败返回 null 不抛异常、判空习惯、卸载时机"
---

# Resources.Load 加载与 as 转型

## 两种写法

```csharp
// 写法一：泛型版本
GameObject go1 = Resources.Load<GameObject>("Prefabs/WaterGun");

// 写法二：非泛型 + as 转型
GameObject go2 = Resources.Load("Prefabs/WaterGun") as GameObject;
```

## 内部机制（面试重点）

- `Resources.Load<T>(path)` 底层实现就是**调用非泛型 Load 再内部转型**，两个写法本质一模一样，**性能无差别**
- 泛型版本优势：**类型安全（类型写错编译期报错）+ 代码可读性好**，推荐用泛型
- ⚠️ **不存在装箱**！`Resources.Load(path)` 返回的是 `UnityEngine.Object`——它**本身就是引用类型（class）**，`as GameObject` 只是**引用类型之间的向下转型（downcast）**，就是指针换个类型标签，零装箱零性能差

> 装箱（boxing）只发生在**值类型 → object**：如 `int → object`、`struct → object`（见 [[struct 装箱陷阱与值类型原理]]）。引用类型之间用 `as` 与装箱无关。

## 加载失败的表现

| 情况 | 表现 |
|---|---|
| 路径写错 / 资源不存在 | 返回 **null**，不抛异常 |
| `as` 转型失败（类型不匹配） | 返回 **null**，不抛异常 |
| 后续直接用 null 调方法 | **NullReferenceException**（这是真正炸的地方） |

**结论：两种写法失败都返回 null、都不报错，用之前必须判空！**

```csharp
var go = Resources.Load<GameObject>("Prefabs/WaterGun");
if (go == null)
{
    Debug.LogError("资源加载失败！检查路径");
    return;
}
```

## 补充：Resources 体系的硬伤

- 资源全部**打包进主包**，无法热更新（要热更 → [[Addressables资源生命周期]]）
- 单个资源**无法精确卸载**：`Resources.UnloadUnusedAssets()` 只卸载"未被引用"的资源，且成本高（别每帧调）
- Load 只是把资源从包内加载到内存，重复 Load 同一路径返回**同一份缓存**（不会重复加载）

## 一句话总结

> 泛型类型安全推荐用；`as` 是引用类型 downcast 不是装箱；加载失败返回 null 不抛异常；**用之前判空**。

## 相关

- [[UnityEngine.Object 判空与销毁机制]]
- [[struct 装箱陷阱与值类型原理]]
- [[Addressables资源生命周期]]
