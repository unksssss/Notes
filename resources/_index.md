---
title: resources index
type: index
updated: 2026-09-04
---

# Resources

按知识域分目录归档（Unity / C# / 算法 / 归档 / meta），Unity 内再按主题细分。双链按文件名解析，跨目录跳转不受影响。

## Unity / 引擎技术

### animation（3）

| File | Summary |
| --- | --- |
| [[Animator参数与性能优化]] | Animator 参数四类型、Trigger vs Bool 区别、字符串参数哈希查找的性能坑、StringToHash 预计算 int 优化、HasParameter 防静默失效 |
| [[DOTween与协程选型]] | 协程（状态机/流程控制）vs DOTween（时间轴补间/可中断可控），选择口诀：'动'用 DOTween、'流程'用协程，可混用 |
| [[dotween详解]] | Unity DOTween 动画补间库完整指南：基本用法、回调系统、Sequence序列、生命週期控制、常用模式与性能注意事项 |

### architecture（4）

| File | Summary |
| --- | --- |
| [[dots详解]] | Unity DOTS (ECS + Jobs + Burst) 完整讲解 |
| [[scriptableobject数据驱动设计]] | 从面试题出发，由浅入深讲解 ScriptableObject 的原理与应用；运行时创建必须 CreateInstance（new 只有 C# 壳无原生侧）；CreateInstance 实例不随场景/GC 卸载需手动 Destroy |
| [[对象池 OnEnable OnDisable 最佳实践]] | 利用 OnEnable/OnDisable 配合对象池实现自动重置，避免手动重置状态的耦合问题 |
| [[对象池实现]] | Unity 对象池原理与 IObjectPool<T> 实现（踩坑修正版），含面试三连问：池空 Get=Instantiate 兜底 / 池满 Release=Destroy 截断 / 初始化放 OnEnable 原因 |

### assets（2）

| File | Summary |
| --- | --- |
| [[Addressables资源生命周期]] | Addressables 引用计数机制：Load/Release 成对、计数累加、只 Load 不 Release = 内存泄漏；LoadAssetAsync/Release 与 InstantiateAsync/ReleaseInstance 配对规则与错误后果；与 Resources 对比 |
| [[Resources.Load 加载与 as 转型]] | Resources.Load<T> 泛型 vs 非泛型+as 的机制、引用类型 as 无装箱误解、加载失败返回 null 不抛异常、判空习惯、卸载时机 |

### digest（2）

| File | Summary |
| --- | --- |
| [[unity知识点-2026-04-28]] | 今日学习：DOTS + 协程/UniTask + 对象池 + URP优化 + Profiler采样 |
| [[unity知识点-2026-04-29]] | 今日知识点：工业仿真网络同步方案 — 状态同步 vs 帧同步 |

### editor（3）

| File | Summary |
| --- | --- |
| [[Unity创建编辑器窗体EditorWindow]] | EditorWindow 生命周期/OnGUI 立即模式/刷新三兄弟/EditorPrefs 持久化，配场景体检窗口完整示例 |
| [[Unity编辑器扩展开发入门]] | 编辑器扩展知识体系地图 + 入门篇核心（文件夹规范/菜单入口四兄弟/Selection），来源 CSDN 虚拟喵系列（10 篇） |
| [[Unity编辑器相关特性]] | 编辑器特性四分类：属性特性/方法特性/类特性/自定义特性，序列化三兄弟易混点，PropertyDrawer 三步套路 |

### input（2）

| File | Summary |
| --- | --- |
| [[Unity Input System 使用指南]] | Unity 新版 Input System 完整使用指南，从安装、创建Action、绑定按键到代码读取输入。 |
| [[unity-inputsystem详解]] | 从面试题出发，由浅入深讲解 Unity Input System 的原理与应用 |

### performance（1）

| File | Summary |
| --- | --- |
| [[profiler自定义采样]] | Unity Profiler 自定义采样标记定位性能瓶颈 |

### physics（2）

| File | Summary |
| --- | --- |
| [[CharacterController移动 — Move vs SimpleMove]] | Move（位移/帧+手动重力）vs SimpleMove（速度/秒+自动重力）对比、斜坡 Slope Limit 与法线投影处理、选型建议 |
| [[Physics Raycast 与 NonAlloc]] | Physics.Raycast 性能要点：LayerMask 过滤减少检测数量、同步立即返回；RaycastAll 每次返回新数组有 GC，RaycastNonAlloc 写入预分配数组零分配（VR 首选） |

### rendering（3）

| File | Summary |
| --- | --- |
| [[ParticleSystem详解]] | Unity ParticleSystem 全部模块属性详解，按开发频率排序，附常见参数调优方案。 |
| [[Unity 渲染批处理体系]] | 静态批处理 vs GPU Instancing vs SRP Batcher 对比、条件、陷阱、MPB 易混口诀 |
| [[urp移动优化]] | URP 渲染管线在移动端的优化配置与技巧 |

### scripting（5）

| File | Summary |
| --- | --- |
| [[IL2CPP 编译原理与陷阱]] | IL2CPP 编译流水线、泛型处理、代码裁剪、反射限制、Mono 对比 |
| [[MonoBehaviour生命周期与SetActive的坑]] | Awake/OnEnable/Start 生命周期；初始 inactive 对象不触发 Awake（首次激活才触发）；协程与 SetActive 的关系；OnDisable vs OnDestroy 触发时机对比 |
| [[UnityEngine.Object 判空与销毁机制]] | Unity 对象双层结构：托管壳 + native 芯；Destroy 只销毁 native；== 重载使销毁即空；MissingReferenceException；假空对象；为什么分两侧（历史/性能/生命周期主权） |
| [[Update-FixedUpdate-LateUpdate执行时机]] | 三 Update 分工：FixedUpdate 固定步长跑物理、Update 帧率相关跑逻辑、LateUpdate 相机跟随；物理与渲染是解耦的两套时钟 |
| [[协程原理与unitask]] | Unity 协程工作原理、IL 层状态机、yield 指令恢复时机表、WaitForEndOfFrame 帧末时机与截屏用途、局限性、UniTask 零 GC 异步方案深度解析 |

### ui（5）

| File | Summary |
| --- | --- |
| [[TMP Text 零分配更新]] | TextMeshPro 零 GC 分配更新文本：SetText 格式化重载直接写入内部字符缓冲、SetCharArray；避开 string 拼接与 Format 的堆分配 |
| [[ToggleGroup底层机制]] | ToggleGroup allowSwitchOff 行为与底层调用链：Toggle.Set() 入口早退拦截 + NotifyToggleOn → SetAllTogglesOff |
| [[UGUI事件接口与EventTrigger]] | UGUI 事件两种写法（接口 vs EventTrigger）、IPointerMoveHandler 不生效的四大原因、验证技巧；C# event vs UnityEvent（序列化与 Inspector 配置、性能差异） |
| [[UGUI多级UI性能优化与Canvas重建]] | 多级 UI 性能优化：Canvas Rebuild 根因、动静分离拆 Canvas、浅平化层级、图集合批、代码层优化、面试回答框架 |
| [[UGUI布局系统与强制刷新]] | LayoutGroup + ContentSizeFitter 动态加子项尺寸不刷新的解法：LayoutRebuilder.ForceRebuildLayoutImmediate / Canvas.ForceUpdateCanvases；Start 时机读 UI 尺寸用 GetWorldCorners；ScrollRect 三种 MovementType |

## C# / .NET / 语言机制

| File | Summary |
| --- | --- |
| [[Boehm GC 保守式垃圾回收原理]] | Unity Mono 使用的 Boehm GC 是保守式(Conservative)GC，不精确追踪引用，而是扫描堆栈内存判断指针；内存碎片危害与缓解（分代/对象池/LOH） |
| [[C# foreach 与枚举器零分配]] | foreach 的 GC 真相：数组 for 展开零分配、List<T> struct 枚举器不走接口零分配、接口接收枚举器即装箱、yield 状态机分配 |
| [[UniTask 异步编程指南]] | UniTask 零 GC 异步编程指南：以 struct 替代 class 实现零分配，Unity 官方 Task 与协程的最佳替代方案，配套异步模式与生命周期管理 |
| [[struct 装箱陷阱与值类型原理]] | 值类型装箱/拆箱机制、性能影响、GC 压力来源、避免装箱的三种方法、栈/堆内存布局与生命周期、继承与内存布局分离、装箱内存布局 |
| [[内存管理方案对比]] | 各大语言内存管理策略对比：完全手动 → 编译期静态分析 → ARC → 追踪式 GC → 引用计数+GC |

## 算法与数据结构

| File | Summary |
| --- | --- |
| [[DFS与BFS遍历算法]] | DFS（深度优先）与 BFS（广度优先）对比：数据结构、应用场景、复杂度；BFS 无权图最短路径、DFS 环检测；二叉树四种遍历（前/中/后序+层序） |
| [[KMP字符串匹配]] | KMP 核心思想：主串指针不回退，利用 next 数组（最长相等前后缀）计算模式串滑动距离，复杂度从 O(n×m) 降到 O(n+m) |
| [[二分查找]] | 二分查找原理、最少/最多比较次数推导（⌈log₂n⌉）、O(log n) 时间复杂度、C# 内置 BinarySearch |
| [[动态规划入门]] | DP 两大核心（最优子结构、重叠子问题）+ 三板斧（定义状态、转移方程、初始值）+ 爬楼梯变体例题 |
| [[哈希表冲突解决与Dictionary底层]] | 链地址法/开放地址法对比（信箱 vs 停车场比喻）、开放地址删除需墓碑标记、C# Dictionary 分离链接结构与其扩容机制 |
| [[排序算法-快排归并堆排]] | 三大排序对比：快速排序（平均之王/不稳定/最坏 O(n²)）、归并排序（稳定/费内存）、堆排序（省内存/常数大），复杂度与记忆口诀 |
| [[栈与队列的相互实现]] | 两栈实现队列 vs 单队列实现栈的对称思路与复杂度；核心是牺牲一次 O(n) 转圈调序；易错点：单队列 push 转圈方向、双队列 push O(1) 版本 |
| [[贪心算法入门]] | 贪心=每步局部最优；找零钱翻车案例证明贪心不是万能；适用条件（贪心选择性质+最优子结构）；与动态规划的边界 |
| [[递归与迭代转换]] | 递归的栈溢出与性能开销、尾递归与 TCO（C# 默认不做）、一般递归用栈/队列显式保存状态（DFS 用栈、BFS 用队列）、二叉树前序迭代模板；账本比喻：不欠账→循环，欠账要回溯→显式栈 |
| [[链表反转]] | 链表反转两种写法：三指针迭代法（O(n)/O(1)）与递归法（O(n)/O(n)），先保存 next 防断链 |

## 项目归档（消防仿真 / XR-VR，已停出题）

| File | Summary |
| --- | --- |
| [[Ignite-灭火机制分析]] | Firefighting Simulator Ignite 灭火机制拆解：火灾分类（Class A/B/C）与对应灭火介质、配电房/厨房等场景的 VR 消防机制借鉴 |
| [[Obi Rope粒子约束体系]] | Obi Rope 粒子 + 约束架构：Distance/Bend/Pin/Attachment 约束、初始爆炸根因（粒子堆叠自碰撞 + Pin 高空拉回）、Blueprint 局部坐标系与整体平移 |
| [[VR物理冲击力 — AddExplosionForce vs 自定义定向力]] | AddExplosionForce 三坑（方向不可控/必须全挂 RB/质心不可预测）与自定义定向力方案（dir+衰减+Impulse），VR 冲击波首选自定义 |
| [[VR门交互 — Hinge Joint + XR Grab Interactable]] | 用 Hinge Joint 实现VR里真实的开关门交互，结合 XR Grab Interactable 让手抓住门把手推开拉开关门。 |
| [[XR Grab Interactable 完整参考手册]] | XR Grab Interactable 每个属性的作用、取值范围、使用场景和VR消防项目实际配置。 |
| [[XR Interaction Toolkit 配置指南]] | XR Interaction Toolkit 从安装到交互的完整配置流程，覆盖拿物体、UI点击、射线交互、开门等消防VR项目的全部需求。 |
| [[XR射线交互与World Space Canvas]] | VR 手柄射线点击 World Space UI 三件套：TrackedDeviceGraphicRaycaster 替换 GraphicRaycaster、Event Camera 指定 XR 相机、XRRayInteractor UI interaction；3D 物理射线 vs UI Raycaster 两套命中系统；排查思路 |
| [[unity网络同步方案-状态同步vs帧同步]] | 工业数字孪生场景中两种网络同步方案的对比与选型 |
| [[单圆柱多折点水带实现解析]] | 双 Mesh 多折点水带方案：BakedHoseMesh 固定段低频重建 + ActiveHoseMesh 末端实时段每帧重建；对应 SingleCylinderHoseFromFireTruck / MultiPointBentCylinder 脚本 |

## 学习路线 / Agent 协作

| File | Summary |
| --- | --- |
| [[AI-Agent-提示词-日记问答同步]] | AI Agent 每日任务（知识问答 + 技术笔记 + GitHub 同步）的早期提示词存档，已被 automation 内部 prompt 取代 |
| [[Unity主程学习路线]] | Unity程序从一年经验到主程的完整学习路线，分五个阶段：基础→引擎深入→架构→专项突破→主程能力。 |

共 58 篇资源笔记。
