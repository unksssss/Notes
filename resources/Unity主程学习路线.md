---
title: "Unity主程学习路线"
type: resource
tags: [unity, 职业规划, 学习路线, 长期]
created: "2026-06-04"
updated: "2026-06-04"
status: active
summary: "Unity程序从一年经验到主程的完整学习路线，分五个阶段：基础→引擎深入→架构→专项突破→主程能力。"
source: ""
related: []
---

# Unity 主程学习路线

基于一年基础（VR消防项目、StepManager架构、OAVA粒子、自定义灭火系统）的进阶路径。

---

## 第一阶段：基础站稳 ✅ 已完成

```
C# 基础        → 类/接口/事件/delegate/LINQ/泛型
Unity 生命周期  → MonoBehaviour/Update/FixedUpdate/协程
UI 系统        → UGUI + DOTween
粒子系统       → ParticleSystem + OAVA + VFX Graph 入门
设计模式       → 单例/事件驱动/数据驱动(ScriptableObject)
```

---

## 第二阶段：引擎深入 👈 当前位置

### 必学

- **ScriptableObject 深度**：Editor 脚本、自定义 Inspector、数据管线
- **协程 / async-await / UniTask**：取代 Invoke 和嵌套协程，配合 DOTween 做复杂时序
- **Shader 入门**：ShaderLab 基础语法、URP Shader Graph（可视化搭节点）、顶点着色器 vs 片段着色器
- **性能优化**：Profiler（CPU/GPU/Memory）、Draw Call / Batching / SRP Batcher、Object Pooling、GC 优化（装箱、字符串、闭包）

### 可选

- Addressables — 资源热更新、分包加载
- Input System — 新版输入系统
- Timeline — 导演级时序控制

---

## 第三阶段：架构能力

- **架构模式**：MVC / MVP / MVVM 在 Unity 里的落地、ECS (DOTS) 入门、依赖注入（Zenject / VContainer）
- **编辑器工具**：Custom Editor / PropertyDrawer、EditorWindow、Gizmos / Handles
- **项目架构设计**：模块化（Assembly Definition）、配置表管线（Excel → ScriptableObject → 运行时）、资源管线（导入设置自动化）

---

## 第四阶段：专项突破

- **渲染**：URP 全套（Renderer Feature / Render Pass / Decal）、Shader 进阶（自定义光照、后处理）、VFX Graph 深度
- **网络**：Netcode for GameObjects / Mirror、状态同步 vs 帧同步、服务器权威校验
- **构建 & CI/CD**：Jenkins / GitHub Actions、AssetBundle / Addressables 热更新、平台适配（VR：Quest / Pico / HTC）

---

## 第五阶段：主程能力

- **技术决策**：技术选型（什么项目用什么方案）、架构评审、性能预算（每帧多少ms给什么系统）
- **团队协作**：Git Flow / Code Review 规范、编码规范、技术文档 + 知识库（Obsidian 就是起点）
- **美术/策划沟通**：能看懂美术诉求 → 翻译成技术方案、能评估策划需求工时、能说"这个做不了"的技术理由

---

## 推荐资源

| 方向 | 资源 |
|---|---|
| C# 深度 | 《C# in Depth》— 委托/LINQ/async 章节 |
| Unity 架构 | 《Game Programming Patterns》— 免费在线版 |
| Shader | [Catlike Coding](https://catlikecoding.com/unity/tutorials/) — 从零开始 |
| 性能 | Unity 官方 Profiler 文档 + Frame Debugger |
| 编辑器 | Freya Holmér 的 YouTube（Shapes 作者） |

---

