---
title: Tags Index
type: index
updated: 2026-10-24
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
- [[平面反射 — 镜像相机、斜投影裁剪与反射贴图采样]] — Built-in 平面反射五步链路与十坑
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
- [[MonoBehaviour生命周期与SetActive的坑]] — 执行顺序分阶段；active 与 enabled 两个开关的分界；三种激活情况实测
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
- [[TCP 与 UDP 的区别与游戏选型]] — TCP vs UDP 十维度对比、队头阻塞、粘包归属、自研可靠 UDP
- [[可靠 UDP 与 KCP 原理]] — 四个机制用户态详解 + RTT/RTO 术语速查与 RTO 自适应计算；KCP 取舍；选择性可靠
- [[有限状态机FSM — 手写实现与状态切换边界]] — 手写 FSM 三层骨架、切换边界五要素、FSM vs 行为树 vs GOAP

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
- [[TCP 与 UDP 的区别与游戏选型]] — 队头阻塞为何让游戏弃用 TCP；粘包是 TCP 独有问题
- [[可靠 UDP 与 KCP 原理]] — 「UDP 怎么保证可靠」高频题：四个机制逐个拆解 + RTO 怎么算（Karn / Jacobson 公式 / 退避）+ KCP 快在哪 + 选择性可靠

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
- [[排序算法-快排归并堆排]] — 快排/归并/堆排对比 + 快排四写法（Hoare 中间 pivot 天然抗退化、不必随机化；递归边界用 `j` 不用 `i`；Lomuto 有序 16k 77ms vs Hoare 1ms；三路处理全相同 0ms；迭代版栈深恒 O(log n)）
- [[KMP字符串匹配]] — 主串指针不回退、next 前缀表、O(n+m)
- [[递归与迭代转换]] — 尾递归转循环、一般递归用栈/队列显式保存状态
- [[动态规划入门]] — 最优子结构/重叠子问题、三板斧套路、爬楼梯变体
- [[滑动窗口的最大值]] — 双端单调队列存下标；队尾弹掉 <= 新元素的、队首弹掉过期的；O(n)/O(k)（剑指 59-I）
- [[数位加减凑倍数]] — 第 i 位 ±1 等价于整体 ±10ⁱ，进位借位不用特判；2^k 枚举 n+Σδᵢ·10ⁱ 找 p 的正倍数
- [[ACM模式输入输出]] — 三层结构（IO 壳/算法/输出）；五种输入形态；cin>> 后接 getline 会读空串导致整组错位
- [[字符串变形]] — 单词反序 + 大小写反转；★ 初版 `i > 0` 漏 s[0]；最优是整体反转 + 逐词反转 O(1) 空间（BM83）
- [[最长公共前缀]] — 纵向扫描一列一列比；★ `j >= size() ||` 越界判断必须放 `||` 左边（短路保护）；排序比首尾 O(n·m·log n)（LC 14）
- [[验证IP地址]] — IPv4 四关 / IPv6 三关；★ 并联验证（isIPv4 不行试 isIPv6）而非先猜类型；split 必须补最后一段（BM85）
- [[二分查找]] — 原理、比较次数推导、O(log n)、C# BinarySearch
- [[贪心算法入门]] — 局部最优、找零钱翻车案例、贪心 vs DP 分界
- [[A星寻路算法]] — A* = Dijkstra + 启发式 f=g+h、可采纳性保证最优
- [[两个链表的第一个公共结点]] — 单链表相交必 Y 形；长度差对齐 / 双指针走完自己走对方 / 哈希表（剑指 52）
- [[链表中环的入口结点]] — 快慢指针找相遇点 + 推导 a=(n-1)L+c，fast 回 head 同速再遇即入口（剑指 23）
- [[二叉搜索树BST]] — 中序=升序；插入/删除三情况（双子用中序后继）；递归每层 return 且调用处接返回值
- [[二叉平衡树AVL]] — 严格平衡 |bf| ≤ 1；四型旋转 LL/RR/LR/RL；插入至多 1 次旋 vs 删除最坏 O(log n) 次；树高 ≤ 1.44·log₂n
- [[红黑树]] — 五性质 + 黑高推导；插入三 case（看叔节点）；2-3-4 树同构；AVL vs RBT 取舍
- [[二叉搜索树转双向链表]] — 中序 = 升序 → 遍历顺便接指针；`prev` 必须 `TreeNode*&` 引用传参（剑指 36）
- [[对称的二叉树]] — 镜像递归：交叉比较 `a->left` vs `b->right`；中序回文 ≠ 对称（剑指 28）
- [[合并二叉树]] — 同侧配对 + 重叠相加 + 不重叠整棵接管；base case 要 return 对方节点（LC 617）
- [[二叉树的镜像]] — 每个节点 swap(left, right) 换指针；单参骨架、交换 val 是错的（剑指 27）
- [[验证二叉搜索树]] — 上下界区间递归 / 中序严格升序；父子比较会误判（剑指 98）
- [[完全二叉树]] — 层序靠左连续 + 数组连续存储；堆是完全二叉树但不是 BST
- [[用vector实现栈]] — 尾部三件套 push_back/back/pop_back；**pop_back 返回 void**，必须 back() 再 pop_back()；摊还 O(1)
- [[判断完全二叉树]] — BFS 空节点标记（nullptr 必须入队）/ 层序编号 maxIdx==n（LC 958）
- [[平衡二叉树]] — 家族概念：给 BST 加高度约束；谱系 AVL→红黑树→Treap→Splay；维持靠旋转
- [[判断平衡二叉树]] — 后序 + -1 哨兵 + 双短路 O(n)；两坑：只判根 / 用 ==（剑指 55-2）
- [[二叉搜索树的最近公共祖先]] — 分叉即停 O(h)/O(1)；路径法取最深公共节点可泛化到 LC236（剑指 68-I）
- [[二叉树的最近公共祖先]] — 后序自底向上，左右都非空即 LCA；O(n)/O(h)（剑指 68-II）
- [[二叉树的序列化与反序列化]] — 文本+逗号+`'\0'`；节点必须 new；BFS 版 / 递归前序版（剑指 37）
- [[重建二叉树]] — 前序第一个是根 + 中序切分；`leftSize = k - il`；前序+后序不行（剑指 7）
- [[二叉树的右视图]] — 每层最右；BFS 取每层末个（先存 sz）/ DFS 根-右-左 + 深度首现 O(h) / 不建树版（BM41）
- [[栈的输出序列种数]] — 卡特兰数 C(2n,n)/(n+1)；n=3 时唯一不可能的排列是 312；公式法 long long 只到 n=30（中间量先溢出）

### 寻路
- [[A星寻路算法]] — A* = Dijkstra + 启发式 f=g+h、可采纳性保证最优
- [[NavMeshAgent 移动控制 — SetDestination 与 transform 直写的冲突]] — agent 是位置的唯一所有者；只 SetDestination 不写 transform；pathPending 与到达判定；LookAt 与 updateRotation 只能活一个；Warp 合法瞬移

### 设计模式
- [[单例模式与静态类选型]] — 要对象用单例、只要函数用静态类；静态类做不到的四件事
- [[UI-逻辑-数据分层与事件驱动]] — UI/逻辑/数据三层 + 事件驱动（TaskStateManager 范式）
- [[有限状态机FSM — 手写实现与状态切换边界]] — 手写 FSM 三层骨架、切换边界五要素、FSM vs 行为树 vs GOAP

### 状态机
- [[有限状态机FSM — 手写实现与状态切换边界]] — 手写 FSM 三层骨架、切换边界五要素、FSM vs 行为树 vs GOAP

### AI
- [[有限状态机FSM — 手写实现与状态切换边界]] — 手写 FSM 三层骨架、切换边界五要素、FSM vs 行为树 vs GOAP
- [[巡逻保安AI — FSM与行为树的机制差异与选型]] — **FSM 与行为树六维差异**；Running 是记忆不是状态、打断来自 Reactive Selector、FSM 病根是状态两两耦合；巡逻保安选 FSM 的完整实现；换 BT 的四个信号 + 混用实践
- [[行为树工程实现 — 黑板、RUNNING 中断与节点生命周期]] — 手写 BT 工程三件套：黑板查找链与三态陷阱（含字符串 key 零保护）、Sequence 遇 RUNNING 必须立刻 return、持续型动作的让位条件、节点依赖注入与 OnEnter/OnExit；实战 16 个 bug 根因与修复

### 行为树
- [[巡逻保安AI — FSM与行为树的机制差异与选型]] — Selector / Sequence / Condition 的语义与打断能力；BT 与 FSM 的选型判断（状态数 / 组合需求 / 谁维护 / 框架成本）
- [[行为树工程实现 — 黑板、RUNNING 中断与节点生命周期]] — 黑板（GetData 沿父链 / SetData 用索引器 / 三态问题 / 常量类收口）、Sequence 与 Selector 对 RUNNING 的不对称处理、顺序即优先级、恒 RUNNING 饿死兄弟分支、Attach 可见性与 params 构造、节点生命周期与依赖注入

### 架构
- [[有限状态机FSM — 手写实现与状态切换边界]] — 手写 FSM 三层骨架、切换边界五要素、FSM vs 行为树 vs GOAP
- [[巡逻保安AI — FSM与行为树的机制差异与选型]] — 「按规模选范式」：4 状态用 FSM（零框架代码），行为涨到 8~10 个且有组合需求才换 BT
- [[行为树工程实现 — 黑板、RUNNING 中断与节点生命周期]] — 框架基类设计：消息方法必须 virtual（子类重声明会静默隐藏）、运行时程序集不得引用 UnityEditor

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
- [[滑动窗口的最大值]] — 单调队列是 deque 的两端玩法（pop_front + pop_back 同时用）；存下标才能按位置淘汰
- [[数位加减凑倍数]] — 比特位枚举（mask 的 bit i 对应 10ⁱ），天然无回溯副作用
- [[二分查找]] — 原理、比较次数推导、O(log n)
- [[贪心算法入门]] — 局部最优、找零钱翻车案例
- [[二叉搜索树BST]] — 中序=升序；递归插入/查找/删除；顺序插入退化为链 → 需平衡树
- [[二叉平衡树AVL]] — 自平衡 BST：平衡因子 + 旋转维持平衡（BST → AVL → 红黑树 主线中段）
- [[红黑树]] — 自平衡 BST；C++ map/set 底层；Java HashMap 链表 ≥8 树化
- [[二叉搜索树转双向链表]] — BST 中序串联成双向链表；left 指前驱 / right 指后继（剑指 36）
- [[对称的二叉树]] — 递归判左右子树互为镜像；交叉配对 + 三类终止条件（剑指 28）
- [[合并二叉树]] — 两树递归合并：同侧配对、值相加、空侧整棵接管（LC 617）
- [[二叉树的镜像]] — 逐节点交换左右孩子指针；结构与遍历顺序无关（剑指 27）
- [[验证二叉搜索树]] — BST 约束是子树级；区间收紧 + 中序升序两种判法（剑指 98）
- [[完全二叉树]] — 形状约束（靠左连续）；1-based 左 2i / 右 2i+1 / 父 i/2
- [[用vector实现栈]] — size 与 capacity 是两个账本；扩容整体搬家 → 旧引用失效；std::stack 默认 deque 因为不搬家
- [[判断完全二叉树]] — 层序 BFS 找「断层」；空之后不能再有非空（LC 958）
- [[平衡二叉树]] — `bf = 左高 − 右高`（非节点数差）；严格/弱/随机/均摊四档
- [[判断平衡二叉树]] — `abs(hl−hr) <= 1`，差 1 也合法；必须递归到每个节点
- [[二叉搜索树的最近公共祖先]] — BST 有序性预判方向，无需存路径；都小走左 / 都大走右 / 否则即答案
- [[二叉树的最近公共祖先]] — 无有序性 → 只能后序汇合；返回值是「命中节点或 LCA」混合语义
- [[二叉树的序列化与反序列化]] — char 装不下 int 须 to_string；空节点用 # 占位才能唯一还原
- [[重建二叉树]] — 序列反推结构；哈希表「值→中序下标」把 O(n²) 降到 O(n)
- [[二叉树的右视图]] — DFS「根-右-左」时同深度首现即最右；`depth == res.size()` 的语义是「该深度尚未记录」
- [[栈的输出序列种数]] — LIFO 的直接推论：能由栈产生的排列不含 312 模式；种数 = 卡特兰数

### 物理
- [[Physics Raycast 与 NonAlloc]] — LayerMask 过滤、NonAlloc 零分配射线检测；2D 版（Physics2D）与 3D 的 API 差异、射线的成本判断
- [[Rigidbody 睡眠与 Trigger Collider]] — 睡眠机制省模拟、传送不唤醒坑、Trigger vs Collider
- [[NavMeshAgent 移动控制 — SetDestination 与 transform 直写的冲突]] — updatePosition/updateRotation 双写冲突、pathPending 与 remainingDistance、stoppingDistance 到达判定、enabled=false vs isStopped、Warp 瞬移、十坑清单

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
- [[URP 卡通描边 — 法线外扩与 ZOffset]] — 两 Pass 描边：法线外扩 + Cull Front、屏幕恒定线宽补偿、ZOffset 治脸部脏描边
- [[平面反射 — 镜像相机、斜投影裁剪与反射贴图采样]] — 镜像相机 + 斜投影裁剪 + GL.invertCulling + 屏幕空间采样；三方案边界与十坑

### 平面反射
- [[平面反射 — 镜像相机、斜投影裁剪与反射贴图采样]] — 镜像矩阵、`CalculateObliqueMatrix`、绕序翻转、RT/镜像相机生命周期

### 网络
- [[TCP 与 UDP 的区别与游戏选型]] — 面向连接/可靠/字节流 vs 无连接/不可靠/数据报；游戏选型与自研可靠 UDP
- [[可靠 UDP 与 KCP 原理]] — 可靠 UDP 的四个机制、KCP 与 TCP 差异、可靠与不可靠双通道分流
- [[Socket 通讯与心跳重连机制]] — `Accept` 返回新 socket、半开连接与 KeepAlive 不够用的理由、心跳参数与 NAT 约束、重连退避与抖动

### 心跳与重连
- [[Socket 通讯与心跳重连机制]] — 为什么需要应用层心跳、Ping-Pong 设计、间隔与超时的取值依据、重连 ≠ 重新 Connect

### 网络同步
- [[unity网络同步方案-状态同步vs帧同步]] — 工业数字孪生场景中两种网络同步方案的对比与选型

### 输入系统
- [[unity-inputsystem详解]] — 从面试题出发，由浅入深讲解 Unity Input System 的原理与应用

### 动画
- [[DOTween与协程选型]] — 协程管流程、DOTween 管补间，可中断可控是核心差异

### 生命周期
- [[MonoBehaviour生命周期与SetActive的坑]] — 执行顺序分阶段（Start 的依赖保证）；active 与 enabled 两开关分界；首次 vs 再次激活；Instantiate 同步跑 Awake
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
- [[YooAssets 热更新流程 — HybridCLR 实战]] — YooAssets 三档运行模式 + 八步热更链路 + Assembly.Load 动态加载 + YooAsset 3.0.6 官方示例首次打包实证（打包配置项语义 / 补丁 FSM 八节点及各节点实现细节 / HostPlayMode 两个独立坑：BundledCopyOption 与 CDN 版本错位）

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

### 二叉树
- [[二叉搜索树BST]] — 中序=升序；插入/删除三情况；递归每层 return
- [[二叉平衡树AVL]] — 平衡因子 / 最小失衡子树 / 四型旋转；插入 1 次 vs 删除 O(log n) 次
- [[红黑树]] — 五性质、插入三 case、2-3-4 树同构、AVL 对比
- [[二叉搜索树转双向链表]] — 中序遍历顺便接左右指针；prev 引用传参 + 后序位置改 left
- [[对称的二叉树]] — 结构镜像递归（交叉比较）；终止条件顺序 都空/一空/值不等
- [[合并二叉树]] — 同侧配对（`a->left` 配 `b->left`）；与对称二叉树互为对照
- [[二叉树的右视图]] — BFS 取每层最后（先存 sz）/ DFS 根-右-左 + `depth==size` 首现；顺序反了是左视图
- [[二叉树的镜像]] — 单参逐节点 swap；与对称二叉树的「双参成对」骨架正好相反
- [[验证二叉搜索树]] — 上下界 `(low, high)` 下传；或中序 `prev` 引用校验递增
- [[完全二叉树]] — 与 BST 正交：管形状不管值；判定用 BFS（空节点也入队）
- [[判断完全二叉树]] — 唯一故意让 nullptr 入队的 BFS；判空再入队会恒返回 true
- [[平衡二叉树]] — 第三个正交维度（高度）；与完全二叉树（形状）、BST（值序）互相独立
- [[判断平衡二叉树]] — 全树性质题；与《验证二叉搜索树》同构（都易只看根）
- [[二叉搜索树的最近公共祖先]] — 自顶向下单向下降；对比 LC236 普通二叉树需后序自底向上
- [[二叉树的最近公共祖先]] — 后序 DFS 的典型应用；与 BST 版构成「自顶向下 vs 自底向上」对照
- [[二叉树的序列化与反序列化]] — BFS 层序版 + DFS 前序版；两种遍历范式的应用对照
- [[重建二叉树]] — 分治递归：前序给根、中序给切分；右子树起点 `+1` 跳过根

### cpp
- [[二叉搜索树BST]] — 递归 insert/search/remove，中序升序验证
- [[二叉平衡树AVL]] — AVL.cpp 手搓（rotateLeft/Right + 四型判定；remove 待重建）
- [[红黑树]] — RBT.cpp 手搓（rotateLeft/Right + fixInsert 待完成）
- [[二叉搜索树转双向链表]] — 手搓中序递归串联；`TreeNode*&` 引用传参的核心坑
- [[对称的二叉树]] — 手搓镜像递归 `isMirror(a,b)`；交叉比较写反就变成判「两树相同」
- [[合并二叉树]] — 手搓双参递归 `mergeTrees(a,b)`；返回值必须接住、原地改入参的副作用
- [[二叉树的右视图]] — 手搓不建树版：四参区间递归 + `leftSize` 先算再递归右子树
- [[二叉树的镜像]] — 手搓单参递归 `swap(left,right)`；不要把 `val` 当成交换对象
- [[验证二叉搜索树]] — 手搓 `check(node, low, high)`；边界用 `LLONG_MIN/LLONG_MAX`（Windows `long` 是 32 位）
- [[完全二叉树]] — 手搓 `isComplete` BFS；注意故意把 `nullptr` 入队标记断层
- [[用vector实现栈]] — 手搓模板 `template<typename T> class MyStack`；`T& top()` 返回引用 + 异常安全版 `T pop()`
- [[判断完全二叉树]] — 手搓 `seenEmpty` 状态机 + `queue<pair<TreeNode*,long long>>` 编号法
- [[平衡二叉树]] — 旋转只改结构不改中序；四型旋转细节见 AVL 篇
- [[判断平衡二叉树]] — 手搓 `getHeight` 返回 -1 当双职责哨兵；短路不能省
- [[二叉搜索树的最近公共祖先]] — 手搓三支判断；`else return cur` 兜底「分叉」与「自身是祖先」
- [[二叉树的最近公共祖先]] — 手搓 8 行后序；`if (left && right) return root;` 是核心一句
- [[二叉树的序列化与反序列化]] — 手搓 string 缓冲 + `new char[]` + strcpy；栈上对象返回必悬空
- [[重建二叉树]] — 手搓四参区间递归 `build(pre,pl,pr,vin,il,ir)`；`pl > pr` 判空
- [[滑动窗口的最大值]] — 手搓 `deque<int>` 存下标；三行核心：弹尾 / push_back / 弹头；下标比较代替值比较
- [[数位加减凑倍数]] — 位运算枚举 `mask`，`1 << k` 组合；注意 `val > 0` 与 `LLONG_MAX` 哨兵
- [[ACM模式输入输出]] — 手写 main：ios 提速 / 多组 T / while(cin>>n) 读到 EOF / getline + istringstream 解析；endl 会 flush 致 TLE
- [[字符串变形]] — 原地 `reverse(begin, begin+n)` + 逐词 `reverse`；`toupper((unsigned char)c)` 防 UTF-8 负值 UB
- [[最长公共前缀]] — 手写纵向扫描；`strs[0].substr(0, j)` 的第二参是长度；`size()` 与 `int` 比较要强转
- [[验证IP地址]] — 手写 split + 逐段校验；`v = v*10 + (c-'0')` 不是 `+=`；`isdigit((unsigned char)c)`
- [[进制转换]] — `v = v*base + digit(c)`；`0x` 前缀先剥掉不要循环跳过；`stoi(…,16)` 认前缀但 FFFFFFFF 会 out_of_range；`cout << hex` 粘性要 `dec` 关
- [[栈的输出序列种数]] — 卡特兰三实现（公式/递推/DP）+ `__int128` 输出；`res*(n+i)/i` 每步精确整除

## Stats

| Tag | Count |
|-----|-------|
| unity | 40 |
| 算法 | 37 |
| 性能优化 | 10 |
| unity面试 | 16 |
| 数据结构 | 32 |
| 二叉树 | 17 |
| cpp | 27 |
| 每日学习 | 2 |
| DOTS | 1 |
| ECS | 1 |
| Profiler | 1 |
| ScriptableObject | 1 |
| 数据驱动 | 1 |
| InputSystem | 1 |
| 输入系统 | 1 |
| 网络 | 3 |
| 网络同步 | 1 |
| 心跳与重连 | 1 |
| 工业仿真 | 1 |
| URP | 1 |
| 渲染 | 6 |
| 描边 | 1 |
| 平面反射 | 1 |
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
| 架构 | 4 |
| AI | 3 |
| 行为树 | 2 |
| 类层级 | 1 |
| scripting | 4 |
| Canvas | 2 |
| 屏幕适配 | 1 |
| 事件系统 | 2 |
| 射线检测 | 2 |
| 拖拽 | 1 |
| 物理检测 | 2 |
| DrawCall | 3 |
