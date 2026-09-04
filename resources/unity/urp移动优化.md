---
title: URP移动优化
type: resource
tags:
  - unity
  - URP
  - 渲染
created: 2026-04-28
updated: 2026-04-28
status: active
summary: URP 渲染管线在移动端的优化配置与技巧
source: ""
related:
  - "[[unity知识点-2026-04-28]]"
  - "[[unity知识点-2026-04-29]]"
  - "[[scriptableobject数据驱动设计]]"
---

# URP 移动优化

## URP 是什么？

URP（Universal Render Pipeline）是 Unity 基于 Scriptable Render Pipeline 的预构建管线，专为跨平台设计。

## 移动端核心优势

| 特性           | 说明                  |
| ------------ | ------------------- |
| 单通道前向渲染      | 减少 Pass 次数          |
| SRP Batcher  | 大幅降低 Draw Call      |
| Shader Graph | 可视化编辑，自动生成兼容 Shader |
| LOD 适配       | 跨平台自动降级             |

## 运行时动态优化

```csharp
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MobileOptimizer : MonoBehaviour
{
    void Start()
    {
        var urp = GraphicsSettings.currentRenderPipeline
                  as UniversalRenderPipelineAsset;
        if (urp != null)
        {
            urp.msaa = MSAASamples.None;        // 关闭抗锯齿
            urp.shadowDistance = 30f;            // 降低阴影距离
            urp.shadowCascadeCount = 1;          // 减少级联
            urp.renderScale = 0.8f;              // 降分辨率渲染
        }
    }
}
```

## 注意事项

- 高端设备可开启 MSAA x2，低端设备必须关
- Render Objects Feature 可以做自定义后处理
- URP 的 Complexity 工具可以分析场景渲染开销

## 相关笔记

- [[对象池实现]] — 结合对象池减少实例化开销
- [[Profiler自定义采样]] — 用 Profiler 检查渲染瓶颈
