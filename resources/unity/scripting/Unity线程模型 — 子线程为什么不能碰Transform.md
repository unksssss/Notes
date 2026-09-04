---
title: "Unity线程模型 — 子线程为什么不能碰Transform"
type: resource
tags: [unity, 线程安全, 渲染, 真实面经]
created: "2026-09-04"
updated: "2026-09-04"
status: active
summary: "Unity API 线程不安全的底层真相：渲染循环每帧首把 Transform 矩阵快照打包给 GPU，子线程写入与渲染线程读取同一内存=数据竞争 → 偶发错位/坐标乱飞；本质是渲染管线要求帧内数据绝对静止；正确姿势：Job 算纯数据、主线程统一提交"
related:
  - "[[Update-FixedUpdate-LateUpdate执行时机]]"
  - "[[dots详解]]"
  - "[[协程原理与unitask]]"
---

# Unity 线程模型 — 子线程为什么不能碰 Transform

来源：真实面经（CSDN《Unity 面试题背后的引擎设计原理与工程实践》），Day 35 先讲后答 ✅

## 一句话真相

Unity 的渲染循环在**每一帧开始时**，会把场景里 GameObject 的 **Transform 矩阵快照**打包成 GPU 可读的 Buffer，交给 GPU 并行渲染。如果子线程此时去写 `transform.position`，而渲染线程正在读同一块内存——这就是**数据竞争**。

结果：不直接崩溃、不报错，而是**偶发**的模型错位、坐标乱飞，且无法稳定复现——这种 Bug 比 Crash 更难排查。

## "线程不安全"的本质

- 不是"引擎做不到线程安全"，而是**渲染管线要求帧内数据绝对静止**：CPU 写矩阵的同时 GPU 正在读它渲染
- 引擎文档笼统说"Unity API 不能在子线程调用"，掩盖了上面的本质矛盾——面试时点破这层，答案直接高一档

## 正确姿势

1. 子线程 / Job 里**只算纯数据**（不碰任何 Unity API 的数学运算），结果写入 `NativeArray`
2. 回主线程（Update）**批量赋值** transform
3. 官方例外通道：`IJobParallelForTransform`（TransformAccess 特殊读写通道）——也不是"随便裸改"，是受控的批量接口

## 关联

- [[dots详解]] — Job System / Burst 并行计算体系
- [[Update-FixedUpdate-LateUpdate执行时机]] — 主线程执行模型
- [[协程原理与unitask]] — 协程本质仍是主线程，不存在跨线程问题
