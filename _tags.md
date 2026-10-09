---
title: Tags Index
type: index
updated: 2026-10-08
---

# Tags Index

### DOTS
- [[dots详解]] — Unity DOTS (ECS + Jobs + Burst) 完整讲解

### ECS
- [[dots详解]] — Unity DOTS (ECS + Jobs + Burst) 完整讲解

### InputSystem
- [[unity-inputsystem详解]] — 从面试题出发，由浅入深讲解 Unity Input System 的原理与应用

### Profiler
- [[profiler自定义采样]] — Unity Profiler 自定义采样标记定位性能瓶颈

### ScriptableObject
- [[scriptableobject数据驱动设计]] — 从面试题出发，由浅入深讲解 ScriptableObject 的原理与应用

### URP
- [[urp移动优化]] — URP 渲染管线在移动端的优化配置与技巧

### daily
- [[2026-04-28]] — (no summary)

### unity
- [[dots详解]] — Unity DOTS (ECS + Jobs + Burst) 完整讲解
- [[Unity 序列化机制]] — Unity 序列化器范围/限制、6.6 原生 Dictionary 序列化
- [[单例模式与静态类选型]] — 要对象用单例、只要函数用静态类；静态类做不到的四件事
- [[UI-逻辑-数据分层与事件驱动]] — UI/逻辑/数据三层 + 事件驱动（TaskStateManager 范式）
- [[profiler自定义采样]] — Unity Profiler 自定义采样标记定位性能瓶颈
- [[scriptableobject数据驱动设计]] — 从面试题出发，由浅入深讲解 ScriptableObject 的原理与应用
- [[unity-inputsystem详解]] — 从面试题出发，由浅入深讲解 Unity Input System 的原理与应用
- [[unity知识点-2026-04-28]] — 今日学习：DOTS + 协程/UniTask + 对象池 + URP优化 + Profiler采样
- [[unity知识点-2026-04-29]] — 今日知识点：工业仿真网络同步方案 — 状态同步 vs 帧同步
- [[unity网络同步方案-状态同步vs帧同步]] — 工业数字孪生场景中两种网络同步方案的对比与选型
- [[urp移动优化]] — URP 渲染管线在移动端的优化配置与技巧
- [[协程原理与unitask]] — Unity 协程工作原理、局限性与 UniTask 替代方案
- [[对象池实现]] — Unity 对象池原理与 IObjectPool<T> 实现（踩坑修正版）
- [[ToggleGroup底层机制]] — ToggleGroup allowSwitchOff 行为与底层调用链
- [[Addressables资源生命周期]] — Addressables 引用计数机制与资源生命周期
- [[Obi Rope粒子约束体系]] — Obi Rope 粒子约束架构、Blueprint 局部坐标系与整体平移
- [[MonoBehaviour生命周期与SetActive的坑]] — 生命周期与初始 inactive 陷阱
- [[IL2CPP 编译原理与陷阱]] — IL2CPP 编译流水线、泛型处理、代码裁剪、反射限制
- [[Unity 渲染批处理体系]] — 静态批处理 vs GPU Instancing vs SRP Batcher
- [[struct 装箱陷阱与值类型原理]] — 值类型装箱拆箱机制与 GC 压力
- [[Boehm GC 保守式垃圾回收原理]] — 保守式 GC 标记-清除、增量 GC
- [[对象池 OnEnable OnDisable 最佳实践]] — 对象池激活/回收钩子最佳实践
- [[Animator参数与性能优化]] — 参数四类型、Trigger vs Bool、StringToHash
- [[XR射线交互与World Space Canvas]] — 三件套配置与射线命中 UGUI
- [[UGUI事件接口与EventTrigger]] — IPointerMoveHandler 不生效四大原因
- [[Update-FixedUpdate-LateUpdate执行时机]] — 三 Update 分工与两套时钟解耦
- [[UGUI布局系统与强制刷新]] — LayoutGroup 动态加子项不刷新的解法
- [[DOTween与协程选型]] — 协程管流程、DOTween 管补间、可中断可控
- [[TMP Text 零分配更新]] — TMP SetText 零分配更新文本
- [[Physics Raycast 与 NonAlloc]] — LayerMask 过滤、NonAlloc 零分配射线检测
- [[Unity线程模型 — 子线程为什么不能碰Transform]] — 渲染帧首快照与数据竞争、帧内数据静止
- [[Unity 类继承体系 Object-Component-Behaviour-MonoBehaviour]] — System.Object → UnityEngine.Object 四大分支；Component 两条腿（Behaviour vs 直接继承）；12 道面试追问

### 线程安全
- [[Unity线程模型 — 子线程为什么不能碰Transform]] — 渲染帧首快照与数据竞争、帧内数据静止

### unity面试
- [[YooAssets 热更新流程 — HybridCLR 实战]] — 热更实战链路：资源差异 + 代码差异两条线
- [[委托与事件]] — 委托/事件机制高频题：event 是 delegate 字段的封装，外部只能 +=/-=
- [[字体与文本合批]] — 「字体能合批吗」高频题：字体不决定合批
- [[两个链表的第一个公共结点]] — 剑指 Offer 52，双指针路程相等
- [[链表中环的入口结点]] — 剑指 Offer 23，快慢指针 + 数学推导
- [[unity知识点-2026-04-28]] — 今日学习：DOTS + 协程/UniTask + 对象池 + URP优化 + Profiler采样
- [[unity知识点-2026-04-29]] — 今日知识点：工业仿真网络同步方案 — 状态同步 vs 帧同步
- [[Update-FixedUpdate-LateUpdate执行时机]] — 三 Update 分工与两套时钟解耦
- [[UGUI Canvas 渲染模式与屏幕适配]] — Canvas 三模式 + Canvas Scaler + 刘海屏 SafeArea（面试八问 Q1/Q4/Q5）
- [[UGUI 事件系统与射线检测链路]] — 事件触发链路 + GraphicRaycaster 源码流程 + Raycast Target 代价（面试八问 Q2/Q3）
- [[UGUI 拖拽实现与拖放检测]] — 背包拖拽骨架 + 拖拽中碰撞检测失效八条根因（面试八问 Q6/Q7）
- [[UGUI DrawCall 与性能优化]] — 合批五条件 + 破批十大元凶 + Overdraw + 诊断工具链（面试八问 Q8）

### 协程
- [[协程原理与unitask]] — Unity 协程工作原理、局限性与 UniTask 替代方案

### 对象池
- [[对象池实现]] — Unity 对象池原理与 IObjectPool<T> 实现（踩坑修正版）

### 工业仿真
- [[unity网络同步方案-状态同步vs帧同步]] — 工业数字孪生场景中两种网络同步方案的对比与选型

### 异步
- [[协程原理与unitask]] — Unity 协程工作原理、局限性与 UniTask 替代方案

### 性能优化
- [[profiler自定义采样]] — Unity Profiler 自定义采样标记定位性能瓶颈
- [[对象池实现]] — Unity 对象池原理与 IObjectPool<T> 实现（踩坑修正版）
- [[Addressables资源生命周期]] — Addressables 引用计数机制与资源生命周期
- [[TMP Text 零分配更新]] — TMP SetText 零分配更新文本
- [[Physics Raycast 与 NonAlloc]] — LayerMask 过滤、NonAlloc 零分配射线检测
- [[UGUI 图集原理与合批]] — 图集共享纹理免切换、Mask vs RectMask2D、动静分离
- [[UGUI多级UI性能优化与Canvas重建]] — Canvas Rebuild 根因、动静分离、浅平化、面试框架
- [[UGUI 事件系统与射线检测链路]] — Raycast Target 过多的真实代价与七条优化
- [[UGUI DrawCall 与性能优化]] — 合批五条件、破批十大元凶、DrawCall vs Overdraw、诊断工具链
- [[字体与文本合批]] — 字体不决定合批；Legacy Text vs TMP 差异、fallback 拆 submesh、文本与 Image 交错破批

### 算法
- [[哈希表冲突解决与Dictionary底层]] — 链地址法/开放地址法、C# Dictionary 扩容机制
- [[DFS与BFS遍历算法]] — DFS/BFS 对比：最短路径、环检测，均 O(V+E)
- [[链表反转]] — 三指针迭代与递归两种写法
- [[排序算法-快排归并堆排]] — 快排/归并/堆排复杂度对比：稳定性、最坏退化、空间占用
- [[KMP字符串匹配]] — 主串指针不回退、next 前缀表、O(n+m)
- [[递归与迭代转换]] — 尾递归转循环、一般递归用栈/队列显式保存状态
- [[动态规划入门]] — 最优子结构/重叠子问题、三板斧套路、爬楼梯变体
- [[二分查找]] — 原理、比较次数推导、O(log n)、C# BinarySearch
- [[贪心算法入门]] — 局部最优、找零钱翻车案例、贪心 vs DP 分界
- [[A星寻路算法]] — A* = Dijkstra + 启发式 f=g+h、可采纳性保证最优
- [[两个链表的第一个公共结点]] — 单链表相交必 Y 形；长度差对齐 / 双指针走完自己走对方 / 哈希表（剑指 52）
- [[链表中环的入口结点]] — 快慢指针找相遇点 + 推导 a=(n-1)L+c，fast 回 head 同速再遇即入口（剑指 23）

### 寻路
- [[A星寻路算法]] — A* = Dijkstra + 启发式 f=g+h、可采纳性保证最优

### 设计模式
- [[单例模式与静态类选型]] — 要对象用单例、只要函数用静态类；静态类做不到的四件事
- [[UI-逻辑-数据分层与事件驱动]] — UI/逻辑/数据三层 + 事件驱动（TaskStateManager 范式）

### 序列化
- [[Unity 序列化机制]] — Unity 序列化器范围/限制、6.6 原生 Dictionary 序列化

### 数据结构
- [[两个链表的第一个公共结点]] — 单链表相交必 Y 形；双指针走完自己走对方（剑指 52）
- [[链表中环的入口结点]] — 快慢指针 + a=(n-1)L+c 推导（剑指 23）
- [[哈希表冲突解决与Dictionary底层]] — 链地址法/开放地址法、C# Dictionary 扩容机制
- [[链表反转]] — 三指针迭代与递归两种写法
- [[排序算法-快排归并堆排]] — 快排/归并/堆排复杂度对比
- [[递归与迭代转换]] — 尾递归转循环、一般递归用栈/队列显式保存状态
- [[动态规划入门]] — 最优子结构/重叠子问题、三板斧套路
- [[二分查找]] — 原理、比较次数推导、O(log n)
- [[贪心算法入门]] — 局部最优、找零钱翻车案例

### 物理
- [[Physics Raycast 与 NonAlloc]] — LayerMask 过滤、NonAlloc 零分配射线检测
- [[Rigidbody 睡眠与 Trigger Collider]] — 睡眠机制省模拟、传送不唤醒坑、Trigger vs Collider

### 数据驱动
- [[scriptableobject数据驱动设计]] — 从面试题出发，由浅入深讲解 ScriptableObject 的原理与应用

### 每日学习
- [[unity知识点-2026-04-28]] — 今日学习：DOTS + 协程/UniTask + 对象池 + URP优化 + Profiler采样
- [[unity知识点-2026-04-29]] — 今日知识点：工业仿真网络同步方案 — 状态同步 vs 帧同步

### 渲染
- [[urp移动优化]] — URP 渲染管线在移动端的优化配置与技巧
- [[UGUI 图集原理与合批]] — 图集共享纹理免切换、Mask vs RectMask2D、动静分离
- [[UGUI DrawCall 与性能优化]] — UGUI 合批五条件、破批十大元凶、Overdraw 与诊断
- [[Unity 渲染批处理体系]] — SRP Batcher / GPU Instancing / 静态动态合批

### 网络同步
- [[unity网络同步方案-状态同步vs帧同步]] — 工业数字孪生场景中两种网络同步方案的对比与选型

### 输入系统
- [[unity-inputsystem详解]] — 从面试题出发，由浅入深讲解 Unity Input System 的原理与应用

### 动画
- [[DOTween与协程选型]] — 协程管流程、DOTween 管补间，可中断可控是核心差异

### 生命周期
- [[MonoBehaviour生命周期与SetActive的坑]] — 初始 inactive 不触发 Awake
- [[Update-FixedUpdate-LateUpdate执行时机]] — 三 Update 分工与两套时钟解耦
- [[Unity 类继承体系 Object-Component-Behaviour-MonoBehaviour]] — Object→Component→Behaviour/MonoBehaviour 全景；BehaviourManager 派发与 enabled/isActiveAndEnabled 区别

### UGUI
- [[UGUI Canvas 渲染模式与屏幕适配]] — Canvas 三渲染模式 + Canvas Scaler 三模式 + 刘海屏 SafeArea
- [[UGUI 事件系统与射线检测链路]] — 事件触发链路、GraphicRaycaster 射线流程、Raycast Target 代价
- [[UGUI 拖拽实现与拖放检测]] — 拖拽四接口骨架、落点判定三方案、碰撞检测失效八条根因
- [[UGUI DrawCall 与性能优化]] — 合批五条件、破批十大元凶、Overdraw、诊断工具链
- [[UGUI多级UI性能优化与Canvas重建]] — Canvas Rebuild 根因与四层优化
- [[UGUI事件接口与EventTrigger]] — IPointerMoveHandler 不生效四大原因
- [[UGUI布局系统与强制刷新]] — LayoutGroup 动态加子项不刷新的解法
- [[TMP Text 零分配更新]] — TMP SetText 零分配更新文本
- [[UGUI 图集原理与合批]] — 图集共享纹理免切换、Mask vs RectMask2D、动静分离

### Canvas
- [[UGUI Canvas 渲染模式与屏幕适配]] — Overlay / Screen Space-Camera / World Space 渲染时机与 Event Camera 坑
- [[UGUI多级UI性能优化与Canvas重建]] — Canvas Rebuild 机制与动静分离拆 Canvas

### 屏幕适配
- [[UGUI Canvas 渲染模式与屏幕适配]] — Canvas Scaler 三模式、Match 公式、刘海屏 SafeArea 适配

### 事件系统
- [[UGUI 事件系统与射线检测链路]] — EventSystem / InputModule / Raycaster 三角色与 ExecuteEvents 冒泡
- [[UGUI 拖拽实现与拖放检测]] — 拖拽四接口与释放帧 OnDrop→OnEndDrag 时序
- [[委托与事件]] — C# event 的订阅/退订机制与 UnityEvent 取舍

### 射线检测
- [[UGUI 事件系统与射线检测链路]] — GraphicRaycaster 源码级过滤流程与排序规则
- [[Physics Raycast 与 NonAlloc]] — LayerMask 过滤、NonAlloc 零分配射线检测

### 拖拽
- [[UGUI 拖拽实现与拖放检测]] — 背包拖拽四接口骨架与拖放探测三方案

### 物理检测
- [[UGUI 拖拽实现与拖放检测]] — UI 拖拽无 Collider、Kinematic vs Static 不触发、手动 Query 兜底
- [[Rigidbody 睡眠与 Trigger Collider]] — 睡眠机制省模拟、传送不唤醒坑、Trigger vs Collider

### DrawCall
- [[UGUI DrawCall 与性能优化]] — UGUI 合批五条件与破批十大元凶
- [[字体与文本合批]] — 字体/文本侧的合批条件与 fallback 拆 submesh
- [[Unity 渲染批处理体系]] — SRP Batcher / GPU Instancing / 静态动态合批（3D 侧）

### 资源管理
- [[AssetBundle 生命周期与卸载语义]] — Unload(false/true)、依赖顺序、粉红材质排查

### 热更新
- [[热更新 HybridCLR — AOT 泛型元数据补全]] — 程序集剥离、清单 MD5 下发、AOT 泛型元数据补全
- [[YooAssets 热更新流程 — HybridCLR 实战]] — YooAssets 三档运行模式 + 八步热更链路 + Assembly.Load 动态加载

### 闭包
- [[C# 闭包与委托 — 隐藏类与 GC 陷阱]] — 隐藏类搬变量、存活期=委托引用、闭包泄漏
- [[委托与事件]] — delegate 是函数指针+target；event 是 delegate 字段的封装（外部只能 +=/-=）

### 架构
- [[UI-逻辑-数据分层与事件驱动]] — UI/逻辑/数据三层 + 事件驱动（TaskStateManager 范式）
- [[Unity 类继承体系 Object-Component-Behaviour-MonoBehaviour]] — Object→Component→Behaviour/MonoBehaviour 体系全景；Component 两条腿（Behaviour vs 直接继承）；3D 与 2D 物理设计不对称

### 类层级
- [[Unity 类继承体系 Object-Component-Behaviour-MonoBehaviour]] — UnityEngine.Object 四大分支 + Component 两条腿（走 Behaviour 与直接继承）完整体系；12 道面试追问

### scripting
- [[IL2CPP 编译原理与陷阱]] — IL2CPP 编译流水线、泛型处理、代码裁剪、反射限制、Mono 对比
- [[协程原理与unitask]] — Unity 协程工作原理、IL 层状态机、yield 指令恢复时机、UniTask 替代
- [[Unity 序列化机制]] — 序列化器范围与限制、Dictionary 三绕路、6.6 原生 Dictionary
- [[Unity 类继承体系 Object-Component-Behaviour-MonoBehaviour]] — 类继承体系全景、BehaviourManager 派发、Component 两条腿

## Stats

| Tag | Count |
|-----|-------|
| unity | 35 |
| 算法 | 12 |
| 性能优化 | 10 |
| unity面试 | 12 |
| 数据结构 | 9 |
| 每日学习 | 2 |
| DOTS | 1 |
| ECS | 1 |
| Profiler | 1 |
| ScriptableObject | 1 |
| 数据驱动 | 1 |
| InputSystem | 1 |
| 输入系统 | 1 |
| 网络同步 | 1 |
| 工业仿真 | 1 |
| URP | 1 |
| 渲染 | 4 |
| 协程 | 1 |
| 异步 | 1 |
| 对象池 | 1 |
| 动画 | 1 |
| 生命周期 | 3 |
| UGUI | 9 |
| 物理 | 2 |
| daily | 1 |
| 线程安全 | 1 |
| 资源管理 | 1 |
| 热更新 | 1 |
| 闭包 | 2 |
| 寻路 | 1 |
| 序列化 | 1 |
| 设计模式 | 2 |
| 架构 | 2 |
| 类层级 | 1 |
| scripting | 4 |
| Canvas | 2 |
| 屏幕适配 | 1 |
| 事件系统 | 2 |
| 射线检测 | 2 |
| 拖拽 | 1 |
| 物理检测 | 2 |
| DrawCall | 3 |
