---
title: resources index
type: index
updated: 2026-08-06
---

# Resources

| File | Summary |
|------|---------|
| [[unity网络同步方案-状态同步vs帧同步]] | Unity网络同步方案_状态同步vs帧同步 |
| [[unity-inputsystem详解]] | Unity InputSystem详解 |
| [[scriptableobject数据驱动设计]] | ScriptableObject数据驱动设计 |
| [[unity知识点-2026-04-29]] | Unity知识点 2026-04-29 |
| [[profiler自定义采样]] | Profiler自定义采样 |
| [[urp移动优化]] | URP移动优化 |
| [[对象池实现]] | 对象池实现 |
| [[IL2CPP 编译原理与陷阱]] | IL2CPP 编译流水线、泛型处理、代码裁剪、反射限制 |
| [[Unity 渲染批处理体系]] | 静态批处理 vs GPU Instancing vs SRP Batcher |
| [[csharp/struct 装箱陷阱与值类型原理]] | 值类型装箱拆箱机制、GC 压力、避免方法 |
| [[ToggleGroup底层机制]] | ToggleGroup allowSwitchOff 行为与底层调用链（早退拦截 + NotifyToggleOn） |
| [[哈希表冲突解决与Dictionary底层]] | 链地址法/开放地址法、墓碑标记、C# Dictionary 扩容机制 |
| [[DFS与BFS遍历算法]] | DFS/BFS 对比：最短路径（BFS）、环检测（DFS），均 O(V+E) |
| [[Addressables资源生命周期]] | Addressables 引用计数：Load/Release 成对、只 Load 不 Release = 泄漏 |
| [[链表反转]] | 三指针迭代（O(n)/O(1)）与递归（O(n)/O(n)）两种写法 |
| [[Obi Rope粒子约束体系]] | Obi Rope 粒子约束架构、初始爆炸根因、Blueprint 局部坐标系与整体平移 |
| [[MonoBehaviour生命周期与SetActive的坑]] | 初始 inactive 不触发 Awake、协程与 SetActive 关系 |
| [[排序算法-快排归并堆排]] | 快排/归并/堆排复杂度对比：稳定性、最坏退化、空间占用 |
| [[KMP字符串匹配]] | 主串指针不回退、next 前缀表、O(n+m)、面试高频追问 |
| [[Animator参数与性能优化]] | 参数四类型、Trigger vs Bool、StringToHash 预计算 int、HasParameter |
| [[XR射线交互与World Space Canvas]] | TrackedDeviceGraphicRaycaster + Event Camera + XRRayInteractor 三件套 |
| [[UGUI事件接口与EventTrigger]] | IPointerMoveHandler 不生效四大原因、Raycast Target、验证技巧 |
| [[递归与迭代转换]] | 尾递归转循环、一般递归用栈/队列显式保存状态、二叉树前序迭代模板 |
| [[动态规划入门]] | 最优子结构/重叠子问题、三板斧套路、爬楼梯变体转移方程 |
| [[DOTween与协程选型]] | 协程管流程、DOTween 管补间；可中断/可控是选 DOTween 的核心理由 |
| [[Update-FixedUpdate-LateUpdate执行时机]] | FixedUpdate 固定步长跑物理、Update 跑逻辑、LateUpdate 相机跟随；两套时钟解耦 |
| [[UGUI布局系统与强制刷新]] | LayoutGroup 动态加子项不刷新的解法：ForceRebuildLayoutImmediate/ForceUpdateCanvases、GetWorldCorners |
| [[UGUI布局系统与强制刷新]] | LayoutGroup 动态加子项不刷新的解法：ForceRebuildLayoutImmediate/ForceUpdateCanvases、GetWorldCorners；ScrollRect 三种 MovementType |
| [[TMP Text 零分配更新]] | TMP SetText 格式化重载/SetCharArray 零分配更新文本，避开 string 拼接 GC |
| [[二分查找]] | 二分查找原理、最少/最多比较次数（⌈log₂n⌉）、O(log n)、C# BinarySearch |
| [[Physics Raycast 与 NonAlloc]] | Raycast 性能：LayerMask 过滤、同步返回；NonAlloc 预分配数组零分配（VR 首选） |
| [[贪心算法入门]] | 贪心=局部最优、找零钱翻车案例、贪心选择性质、与 DP 的分界 |
| [[dots详解]] | DOTS详解 |
| [[unity知识点-2026-04-28]] | Unity知识点 2026-04-28 |
| [[UGUI多级UI性能优化与Canvas重建]] | 多级UI性能优化：Canvas Rebuild 根因、动静分离拆 Canvas、浅平化层级、四层优化框架 |
