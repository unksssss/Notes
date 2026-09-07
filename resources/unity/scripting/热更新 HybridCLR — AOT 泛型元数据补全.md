---
title: "热更新 HybridCLR — AOT 泛型元数据补全"
type: resource
tags: [unity, 热更新, HybridCLR, IL2CPP, AOT, 真实面经]
created: "2026-09-07"
updated: "2026-09-07"
status: active
summary: "HybridCLR + AssetBundle 热更整体流程：程序集剥离（AOT 主包 vs HotUpdate.dll）→ 清单 MD5 版本比对下发 → AOT 泛型裁剪根因（泛型=按需实例化，主包没见过的组合机器码里没有）→ DHE 元数据补全；附 AOT vs JIT 精讲"
related:
  - "[[IL2CPP 编译原理与陷阱]]"
  - "[[Addressables资源生命周期]]"
---

# 热更新 HybridCLR — AOT 泛型元数据补全

来源：CSDN 面经《unity几道面试题》原题"hybrid+assetbundle 热更整体流程讲一下"。

## 热更的意义

不发新包（不重新提审/下载整包）就能修改**玩法逻辑代码**与资源。纯换贴图/配表用资源热更即可；改 C# 逻辑必须**代码热更**。

## HybridCLR 整体流程（四步）

1. **程序集剥离**：主包只留引擎 + 少量 AOT 程序集；业务逻辑全编译进 `HotUpdate.dll`，**不参与主包 IL2CPP AOT** 编译；
2. **打包下发**：DLL 与资源一起打进 AssetBundle，并生成**清单文件**（每个 AB 的 MD5 / 大小 / 版本号）；
3. **启动更新**：启动时对比本地清单 vs CDN 清单 → 只下载差异项 → HybridCLR 加载器把新 DLL 交给解释器/补充元数据执行；
4. **AOT 泛型补全（最大坑）**：生成 **AOT 泛型元数据补全文件（DHE / Metadata Expansion）** 随包携带，运行时注册给 IL2CPP。

## 为什么必须补 AOT 泛型元数据

- **泛型 = 模板，按需实例化**：`List<int>` 与 `List<string>` 是两份不同代码；
- **JIT** 可以"用到了现编"；**AOT（IL2CPP）** 只能在编译期把**主包代码里实际出现过**的泛型组合编成机器码——没出现过的，机器码里**根本不存在**；
- 热更 DLL 若 new 了主包没用过的泛型（如 `List<我的热更类>`）→ 运行时 IL2CPP 找不到现成机器码 → **直接崩溃**；
- DHE 元数据补全 = 把主包没用过的泛型元数据也带一份，运行时"补零件"注册，热更代码才能正常 new。

> 💡 C++ 类比：泛型裁剪 ≈ **模板实例化**——`.cpp` 里没实例化的模板组合，链接产物里就没有。

## AOT vs JIT 精讲（热更地基）

| | 编译时机 | 产物 | Unity 落地 |
|---|---|---|---|
| JIT | 运行时边跑边译 | IL + 运行时翻译 | Mono（编辑器等） |
| AOT | 打包时全量译完 | 原生机器码 | IL2CPP（iOS/Android/主机） |

- **类比**：JIT = 带菜谱上门现学现炒的大厨；AOT = 中央厨房预制菜，加热即食不带菜谱。
- **为什么 iOS 必须 AOT**：苹果 App Store 禁止 App **运行时动态生成可执行代码**（JIT 被视为动态注入）→ iOS 包必须纯机器码。
- **AOT 的两个著名代价**：① 反射受限（结构信息被裁剪 → link.xml / [Preserve] 保命）；② 泛型裁剪（本篇主角）。

## 面试答题框架

① 程序集怎么分（主包 AOT 壳 + HotUpdate.dll 热更）→ ② 资源/代码如何下发（AB + 清单 MD5/版本比对、差异下载）→ ③ 为什么 HybridCLR 要 AOT 泛型元数据补全（AOT 只编见过组合 + 热更代码崩 + DHE 兜底）。能主动把 AOT/JIT 差异讲出来是加分项。
