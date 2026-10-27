---
title: resources index
type: index
updated: 2026-10-27
---

# Resources

按知识域分目录归档（Unity / C# / 算法 / 归档 / meta），Unity 内再按主题细分。双链按文件名解析，跨目录跳转不受影响。

## Unity / 引擎技术

### ai（2）

| File | Summary |
| --- | --- |
| [[巡逻保安AI — FSM与行为树的机制差异与选型]] | FSM 与行为树六维差异（心智模型 / 执行方式 / 扩展成本 / 复用 / 打断 / 调试）；三个易错机制——**Running 是记忆不是状态**、**打断来自 Reactive Selector 而非行为树本身**、**FSM 病根是状态间两两耦合而非状态多**；巡逻保安选 FSM 的完整实现（4 状态 + 感知降频 10Hz + Enter/Exit 配对 + 幂等切换 + 追击双退出条件）；换行为树的四个信号与「BT 选、FSM 执行」的混用实践 |
| [[行为树工程实现 — 黑板、RUNNING 中断与节点生命周期]] | 手写 BT 的工程层三件套：**黑板**（`GetData` 沿父链查找 + `SetData` 用索引器而非 `Add` + 「null 值 vs key 不存在」三态 + 泛型黑板以类型为 key + **字符串 key 是编译期零保护**，写 `player` 读 `Target` 的实证）、**RUNNING 中断语义**（Sequence 遇 RUNNING 必须立刻 return，只 break switch 会让后续 Action 被误执行；记忆式 vs 响应式；顺序即优先级；**持续型动作必须自带让位条件否则兄弟分支饿死**）、**节点依赖注入**（Node 非 MonoBehaviour，Transform/NavMeshAgent 靠构造传入；**缺 OnEnter/OnExit 导致计时器跨切换残留**；动作叶依赖 `IDamageable` 而非具体类）。含 2026-10-22~23 实战 16 个 bug 的根因与修复：提前 return 让父链查找成死代码、`parent.parent` 硬编码深度、`static LayerMask` 恒为 0、`using UnityEditor` 混进运行时程序集、`Attach` 私有导致树建不起来、`SetUpTree` 缺 return 的 CS0161、`TaskPatrol`/`TaskGoToTarget` 绕过 NavMeshAgent、基类消息方法被子类隐藏的静默失效 |

### animation（3）

| File | Summary |
| --- | --- |
| [[Animator参数与性能优化]] | Animator 参数四类型、Trigger vs Bool 区别、字符串参数哈希查找的性能坑、StringToHash 预计算 int 优化、HasParameter 防静默失效；状态机过渡期间状态归属 + Has Exit Time（Day 36 面经） |
| [[DOTween与协程选型]] | 协程（状态机/流程控制）vs DOTween（时间轴补间/可中断可控），选择口诀：'动'用 DOTween、'流程'用协程，可混用 |
| [[dotween详解]] | Unity DOTween 动画补间库完整指南：基本用法、回调系统、Sequence序列、生命週期控制、常用模式与性能注意事项 |

### architecture（7）

| File | Summary |
| --- | --- |
| [[dots详解]] | Unity DOTS (ECS + Jobs + Burst) 完整讲解；含 Burst SIMD 向量化三要素（blittable/连续/对齐/__managed 阻断） |
| [[有限状态机FSM — 手写实现与状态切换边界]] | 手写 FSM 三层骨架（IState / 状态基类持 owner / 状态机 Dictionary 注册 + 幂等切换）；Exit→换ID→Enter 的顺序理由、非 MonoBehaviour 状态机开不了协程、状态切换边界五要素、FSM vs 行为树 vs GOAP；怪物 AI 实战 6 坑（每帧 SetDestination / 冷却只读不写 / 球形判定不判朝向 / int 存距离 / 单例伪 null） |
| [[scriptableobject数据驱动设计]] | 从面试题出发，由浅入深讲解 ScriptableObject 的原理与应用；运行时创建必须 CreateInstance（new 只有 C# 壳无原生侧）；CreateInstance 实例不随场景/GC 卸载需手动 Destroy |
| [[对象池 OnEnable OnDisable 最佳实践]] | 利用 OnEnable/OnDisable 配合对象池实现自动重置，避免手动重置状态的耦合问题 |
| [[对象池实现]] | Unity 对象池原理与 IObjectPool<T> 实现（踩坑修正版），含面试三连问：池空 Get=Instantiate 兜底 / 池满 Release=Destroy 截断 / 初始化放 OnEnable 原因；池化反面判据 + 粒子池同构（maxParticles/Prewarm，Day 36 拔高） |
| [[单例模式与静态类选型]] | 单例 vs 静态类：要对象用单例、只要函数用静态类；静态类做不到四件事（继承接口/当参数/生命周期/序列化）；单例四缺点（Day 37 面经） |
| [[UI-逻辑-数据分层与事件驱动]] | UI/逻辑/数据三层职责 + 事件驱动（TaskStateManager 范式）：状态变更走事件 UI 订阅；优点（解耦/可单测）+ 缺点（事件流难追踪/过度设计）（Day 37 面经） |

### assets（3）

| File | Summary |
| --- | --- |
| [[Addressables资源生命周期]] | Addressables 引用计数机制：Load/Release 成对、计数累加、只 Load 不 Release = 内存泄漏；LoadAssetAsync/Release 与 InstantiateAsync/ReleaseInstance 配对规则与错误后果；与 Resources 对比 |
| [[Resources.Load 加载与 as 转型]] | Resources.Load<T> 泛型 vs 非泛型+as 的机制、引用类型 as 无装箱误解、加载失败返回 null 不抛异常、判空习惯、卸载时机 |
| [[AssetBundle 生命周期与卸载语义]] | **AB 包全流程**（标记 → 打包生成 Manifest → 依赖 → 下发 → 加载 → 依赖加载 → 实例化 → 卸载）+ 打包粒度两原则「共享的抽出来、同生命周期的放一起」；Unload(false) 卸包头保实例 vs Unload(true) 连资源全销毁 → 粉红 Missing；依赖加载先父后子、卸载先子后父；**裸 AB 打包-加载最小闭环**（Editor 打包脚本四要点 / 产物四件套与 manifest 字段解读 / LoadFromFile 路径与资产名语义 / Resources 双份包体坑 / 压缩格式三选 LZMA·LZ4·Uncompressed / 加载 API 选型 / 与 YooAssets 对照）；Addressables 引用计数 = 底层 AB 的人肉记账自动化（Day 36 面经 + 2026-10-09 补全流程 + 2026-10-12 实战） |

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

### network（3）

| File | Summary |
| --- | --- |
| [[TCP 与 UDP 的区别与游戏选型]] | TCP vs UDP 十维度对比表；**字节流 vs 数据报**的边界差异（收发次数不对应 / 读不完时续读 vs 截断 / 粘包归属与三种划界方案）；**队头阻塞**是实时游戏弃用 TCP 的核心原因（位置数据有时效性，宁可丢包也不等重传）；Nagle 算法坑与 `NoDelay`；业界方案 UDP + 自研可靠（KCP/ENet/UTP） |
| [[可靠 UDP 与 KCP 原理]] | **可靠 UDP = 把序列号 / ACK / 重传 / 滑动窗口搬到用户态重做**（TCP 那套在内核里改不动）；附 **RTT / RTO / SRTT / RTTVAR / MSS / MTU / BDP 术语速查**；**四个机制逐个拆解**：一次丢包完整走查、TCP 字节序号 vs 包序号、累积确认 vs 位图确认、超时重传 vs 快速重传（阈值为何是 3）、`min(rwnd, cwnd)` 与流量控制·拥塞控制分工；**RTO 是算出来的**（Karn 算法 + Jacobson 公式 + 退避 ×2 vs ×1.5）、BDP 与吞吐上限公式、`MSS = MTU − 首部`；KCP 比 TCP 快在哪 = 用带宽换延迟；**选择性可靠**——指令类走可靠通道、状态类走不可靠通道；附自测六问 |
| [[Socket 通讯与心跳重连机制]] | Socket 两端 API 顺序（**`Accept` 返回新 socket**，监听 socket 是前台、新 socket 是服务员）；三次握手与 API 对应 + `backlog` 溢出；**半开连接**与 TCP KeepAlive 四个不够用的理由（默认 2 小时 / 只在空闲发 / 应用层不可见 / NAT 可能先回收）；心跳 Ping-Pong 设计与参数（间隔 3~10s、超时 2~3 倍、**间隔必须小于 NAT 表项超时**）；重连 = 指数退避 + 抖动 + 上限，且重连后要重新鉴权/恢复会话/拉全量快照；Unity 三坑（切后台停心跳、`System.Threading.Timer` 线程、心跳包零分配）；附「自己项目没做心跳」的面试话术 |

### performance（1）

| File | Summary |
| --- | --- |
| [[profiler自定义采样]] | Unity Profiler 自定义采样标记定位性能瓶颈 |

### physics（4）

| File | Summary |
| --- | --- |
| [[CharacterController移动 — Move vs SimpleMove]] | Move（位移/帧+手动重力）vs SimpleMove（速度/秒+自动重力）对比、斜坡 Slope Limit 与法线投影处理、选型建议 |
| [[NavMeshAgent 移动控制 — SetDestination 与 transform 直写的冲突]] | **agent 是位置的唯一所有者**：`updatePosition`/`updateRotation` 默认 true，每帧把内部位置与朝向写回 transform，任何 `transform.position = ...` 都在和它抢所有权（抽搐/回弹/穿墙/`remainingDistance` 与实际位置脱节）。正确姿势 `SetDestination` + `isStopped` + `speed`/`stoppingDistance`；**到达判定必须先判 `pathPending`**（否则 remainingDistance 无意义）+ `pathStatus == PathPartial` 失败分支；`LookAt` 与 `updateRotation` 只能活一个；合法瞬移用 `agent.Warp`；不用寻路（死亡/击飞）先 `enabled = false`。附行为树 `TaskGoToTarget` 双写实战的修复代码与十坑清单 |
| [[Physics Raycast 与 NonAlloc]] | Physics.Raycast 性能要点：LayerMask 过滤减少检测数量、同步立即返回；RaycastAll 每次返回新数组有 GC，RaycastNonAlloc 写入预分配数组零分配（VR 首选）；**2D 版本 Physics2D**：3D 返回 bool vs 2D 直接返回 RaycastHit2D、AI 检测玩家的四条排查清单、LayerMask 传层索引的坑、射线只该用于遮挡检测的成本判断、ContactFilter2D 与 queriesHitTriggers |
| [[Rigidbody 睡眠与 Trigger Collider]] | 睡眠机制（低于 SleepThreshold + 无外力 → 停模拟省 CPU；传送不自动唤醒用 MovePosition/WakeUp）；Trigger 只重叠检测走 OnTrigger、开销低，触发同样需至少一方有刚体（Day 36 面经） |

### rendering（5）

| File | Summary |
| --- | --- |
| [[平面反射 — 镜像相机、斜投影裁剪与反射贴图采样]] | Built-in 管线平面反射五步链路：镜像相机 + **镜像矩阵**（`I - 2nnᵀ`，乘在 view 矩阵左）× **斜投影裁剪**（`CalculateObliqueMatrix` 把近裁剪面扭到镜面，否则倒影里出现地板背面；`clipPlaneOffset` 防镜面自反射递归；正交投影下必退化）→ **`GL.invertCulling` 翻绕序**（镜像行列式为负，必须 try/finally 复位，否则主画面也里外翻）→ **屏幕空间采样**（`ComputeScreenPos` 必须除 w）。含三方案边界（Probe 照不出角色 / SSR 反射不到屏幕外 / Planar DrawCall 翻倍）、RT 与镜像相机生命周期（`[ExecuteAlways]` 下 `Destroy` vs `DestroyImmediate`）、`Screen position out of view frustum` 根因（SceneView 正交相机触发 `OnWillRenderObject`）、10 条坑清单、移植 URP 的 CommandBuffer 对照 |
| [[URP 卡通描边 — 法线外扩与 ZOffset]] | 两 Pass 描边法（Cull Front + 沿法线外扩）：屏幕恒定线宽的「视空间 Z × FOV」补偿（FOV 由 `unity_CameraProjection._m11` 反解）、ZOffset 只改 `positionCS.z` 治脸部脏描边（透视走投影矩阵 z 行重算）、顶点采样必须 `tex2Dlod`；五 Pass 分工、描边要写深度、硬边破面等六坑、与屏幕后处理 Sobel 取舍 |
| [[ParticleSystem详解]] | Unity ParticleSystem 全部模块属性详解，按开发频率排序，附常见参数调优方案。 |
| [[Unity 渲染批处理体系]] | 静态批处理 vs GPU Instancing vs SRP Batcher 对比、条件、陷阱、MPB 易混口诀；SRP Batcher 底层 CBUFFER 常量缓冲复用、破坏三条件、与 Instancing 路径二选一（Day 36 拔高） |
| [[urp移动优化]] | URP 渲染管线在移动端的优化配置与技巧；含 2026 官方战略（BIRP 弃用、URP 唯一管线、HDRP 维护模式） |

### scripting（10）

| File | Summary |
| --- | --- |
| [[IL2CPP 编译原理与陷阱]] | IL2CPP 编译流水线、泛型处理、代码裁剪、反射限制、Mono 对比；2026 官方路线 Mono → CoreCLR 四阶段（6.5~6.8） |
| [[Unity 类继承体系 Object-Component-Behaviour-MonoBehaviour]] | System.Object → UnityEngine.Object 四大分支；Component 两条腿（走 Behaviour 的 MonoBehaviour/StateMachineBehaviour/Collider2D/Rigidbody2D vs 直接继承的 Transform/Renderer/Collider3D/Rigidbody）；BehaviourManager 派发与 enabled/isActiveAndEnabled 区别；3D 与 2D 不对称；12 道面试追问 |
| [[Unity 序列化机制]] | Unity 序列化器范围与限制、Dictionary 三绕路；Unity 6.6 官方新特性：Inspector 原生 Dictionary 序列化（两列 key-value + 编译期校验）（Day 37 官方新特性） |
| [[MonoBehaviour生命周期与SetActive的坑]] | Awake/OnEnable/Start 生命周期；**执行顺序分阶段**（所有 Awake → 所有 OnEnable → 所有 Start，这是 Start 的依赖保证）；**active 与 enabled 两个开关的分界**（Awake 只看 active，enabled 从 OnEnable 起生效）；三种激活情况实测对照表；`Instantiate` 同步执行 Awake；对象池重置必须放 OnEnable 的机制解释；协程与 SetActive；OnDisable vs OnDestroy |
| [[UnityEngine.Object 判空与销毁机制]] | Unity 对象双层结构：托管壳 + native 芯；Destroy 只销毁 native；== 重载使销毁即空；MissingReferenceException；假空对象；为什么分两侧（历史/性能/生命周期主权） |
| [[Update-FixedUpdate-LateUpdate执行时机]] | 三 Update 分工：FixedUpdate 固定步长跑物理、Update 帧率相关跑逻辑、LateUpdate 相机跟随；物理与渲染是解耦的两套时钟 |
| [[协程原理与unitask]] | Unity 协程工作原理、IL 层状态机、yield 指令恢复时机表、WaitForEndOfFrame 帧末时机与截屏用途、局限性、UniTask 零 GC 异步方案深度解析 |
| [[Unity线程模型 — 子线程为什么不能碰Transform]] | Unity API 线程不安全的底层真相：渲染帧首快照 Transform 给 GPU、子线程写入=数据竞争→偶发错位；帧内数据静止；Job 算数据主线程提交（真实面经） |
| [[热更新 HybridCLR — AOT 泛型元数据补全]] | 热更四步流程：程序集剥离 → AB+清单 MD5 下发 → 启动差异更新 → AOT 泛型元数据补全（DHE）；泛型=按需实例化、主包没见过的组合机器码没有；AOT vs JIT 精讲（Day 36 面经） |
| [[YooAssets 热更新流程 — HybridCLR 实战]] | **学习项目实战**（`E:\unityproject\HybirdCLR`）：YooAssets 多包设计 + 三档运行模式（EditorSimulate/OfflinePlay/HostPlay）+ 八步链路（Initialize → CreatePackage → 文件系统与下载参数 → 请求版本 → 加载清单 → 差异下载 → LoadAssetAsync<TextAsset> → Assembly.Load + 反射调用 → Release）；DLL 为何改名 `.bytes`；自定义 IRemoteService 的多 CDN 容灾；**YooAsset 3.0.6 官方示例（YooAssets_HybirdCLR）首次打包实证**：产物三份（CDN/OutputCache/Simulate）+ `.report` 配置项语义（EnableSharePackRule / SingleReferencedPackAlone / FileNameStyle / CompressOption）+ ErrorCode201·601 警告 + buildlogtep 耗时拆解 + 补丁 FSM 八节点 + 四档 PlayMode 初始化参数 + 包版本号 vs URL 版本目录；**补丁 FSM 各节点实现细节**（ResourceDownloaderOptions(10,3) 参数语义 / `yield return downloader` = Operation 实现 IEnumerator / ClearCacheMethods 两枚举与清理时机）；**HostPlayMode 首次跑通的两个独立坑**（`BundledCopyOption` 默认 `None` → 缺 hash/BuiltinCatalog 导致初始化 404；打包后未同步 CDN → 内置 994 vs 远端 988 版本错位）+「hash → bytes → version 对齐」三层排查法（2026-10-13 · 2026-10-14 · 2026-10-17 补强） |

### ui（11）

| File | Summary |
| --- | --- |
| [[TMP Text 零分配更新]] | TextMeshPro 零 GC 分配更新文本：SetText 格式化重载直接写入内部字符缓冲、SetCharArray；避开 string 拼接与 Format 的堆分配 |
| [[UGUI 图集原理与合批]] | 图集共享纹理免切换→合批；代价：尺寸上限/整张常驻内存/按界面分组；Mask(模板缓冲打断合批) vs RectMask2D(shader 裁剪)；动静分离三档分层（Day 37 面经） |
| [[ToggleGroup底层机制]] | ToggleGroup allowSwitchOff 行为与底层调用链：Toggle.Set() 入口早退拦截 + NotifyToggleOn → SetAllTogglesOff |
| [[UGUI事件接口与EventTrigger]] | UGUI 事件两种写法（接口 vs EventTrigger）、IPointerMoveHandler 不生效的四大原因、验证技巧；C# event vs UnityEvent（序列化与 Inspector 配置、性能差异） |
| [[UGUI多级UI性能优化与Canvas重建]] | 多级 UI 性能优化：Canvas Rebuild 根因、动静分离拆 Canvas、浅平化层级、图集合批、代码层优化、面试回答框架 |
| [[UGUI布局系统与强制刷新]] | LayoutGroup + ContentSizeFitter 动态加子项尺寸不刷新的解法：LayoutRebuilder.ForceRebuildLayoutImmediate / Canvas.ForceUpdateCanvases；Start 时机读 UI 尺寸用 GetWorldCorners；ScrollRect 三种 MovementType |
| [[UGUI Canvas 渲染模式与屏幕适配]] | Canvas 三模式（Overlay / Screen Space-Camera / World Space）渲染时机与遮挡差异、Event Camera 坑；Canvas Scaler 三模式与 Match 公式；刘海屏 SafeArea 适配完整代码与五条坑 |
| [[UGUI 事件系统与射线检测链路]] | UI 事件完整链路（EventSystem→InputModule→Raycaster→ExecuteEvents 冒泡）、GraphicRaycaster 源码级射线流程、点击双条件、OnDrop 时序；Raycast Target 过多的真实代价 + 「不影响 DrawCall」辟谣 + 七条优化 |
| [[UGUI 拖拽实现与拖放检测]] | 背包拖拽四接口完整骨架（幽灵 CanvasGroup / 坐标转换三兄弟 / RaycastNonAlloc 探测）；落点判定三方案；「拖拽中碰撞检测失效」八条根因（UI 无 Collider / Kinematic vs Static / ghost 挡射线 / blockingObjects） |
| [[UGUI DrawCall 与性能优化]] | UGUI DrawCall 生成机制与合批五条件、破批十大元凶、DrawCall vs Overdraw、Frame Debugger/Profiler 诊断链路、四层优化清单与面试框架 |
| [[字体与文本合批]] | 「字体能合批吗」：**字体不决定合批**，仍看同材质同贴图 + 深度连续；Legacy Text 同 Font 共一张动态图集可合批（扩容会打断）；TMP 同 FontAsset 共享材质可合批，**fallback 会拆 submesh 产生额外 DC**；最常见破批其实是「文本与 Image 交错」（材质不同）+ 六条优化 |

## C# / .NET / 语言机制

| File | Summary |
| --- | --- |
| [[Boehm GC 保守式垃圾回收原理]] | Unity Mono 使用的 Boehm GC 是保守式(Conservative)GC，不精确追踪引用，而是扫描堆栈内存判断指针；内存碎片危害与缓解（分代/对象池/LOH） |
| [[C# foreach 与枚举器零分配]] | foreach 的 GC 真相：数组 for 展开零分配、List<T> struct 枚举器不走接口零分配、接口接收枚举器即装箱、yield 状态机分配 |
| [[C# 闭包与委托 — 隐藏类与 GC 陷阱]] | 闭包=编译器为捕获变量的匿名方法生成隐藏类（c__DisplayClassX），捕获变量搬字段、存活期=委托引用存活期 → 静态/事件订阅拴住 this=泄漏；每帧 new 捕获型 lambda=持续 GC Alloc（Day 36 面经） |
| [[UniTask 异步编程指南]] | UniTask 零 GC 异步编程指南：以 struct 替代 class 实现零分配，Unity 官方 Task 与协程的最佳替代方案，配套异步模式与生命周期管理 |
| [[struct 装箱陷阱与值类型原理]] | 值类型装箱/拆箱机制、性能影响、GC 压力来源、避免装箱的三种方法、栈/堆内存布局与生命周期、继承与内存布局分离、装箱内存布局；参数传递 ref/out 与值拷贝（Day 36） |
| [[内存管理方案对比]] | 各大语言内存管理策略对比：完全手动 → 编译期静态分析 → ARC → 追踪式 GC → 引用计数+GC |
| [[C# IL 中间语言与编译执行模型]] | IL/CIL = .NET 通用中间语言：两段论（csc 编译一次、JIT/AOT 各自翻译）；栈机模型（ldarg 压栈/add 弹栈/ret）；box/newobj/隐藏类指令 ↔ 装箱/闭包/协程状态机；Unity 四条消费路线 Mono/IL2CPP/CoreCLR/HybridCLR |
| [[委托与事件]] | delegate = 类型安全的函数指针 + target（实例方法隐式持 `this`）→ 多播委托链按序执行、异常截断、`-=` 只移除一个；**event = 编译器对 delegate 字段的封装**（私有字段 + add/remove 访问器，外部只能 `+=`/`-=`，不能赋 null 覆盖、不能 Invoke）→ 为什么需要它；与 UnityEvent 取舍；订阅不取消 = GC 泄漏，静态事件最危险 |

## 操作系统（1）

> 面向客户端面试的最小集：只覆盖「会被问到的那几个点」，不做教材式铺开。

| File | Summary |
| --- | --- |
| [[进程与线程 — 基本概念与关系]] | 进程 = 资源分配单位（独立地址空间）、线程 = CPU 调度单位（共享地址空间）；**共享代码段/数据段/堆、独享栈/寄存器/TID**（「栈是私有的，堆是大家的」）；四个关系要点——线程是轻量级进程、一个线程崩会带走整个进程（Chrome 多进程 vs Unity 单进程）、共享内存=通信免费但要锁（`count++` 非原子）、切换开销差在换页表与 TLB；IPC vs 线程通信；进程/线程/协程三层对比；Unity 主线程-子线程-回主线程映射；线程池与线程数经验值 |

## 算法与数据结构（39）

> 分区导航：[[algorithm/_index|算法区导航]] —— 本区已按 **6 个子目录物理分类**（目录即分类），附建议刷题顺序。

### 数据结构基础（5）— `algorithm/数据结构基础`

| File | Summary |
| --- | --- |
| [[栈与队列的相互实现]] | 两栈实现队列 vs 单队列实现栈的对称思路与复杂度；核心是牺牲一次 O(n) 转圈调序；易错点：单队列 push 转圈方向、双队列 push O(1) 版本；栈的应用延伸：括号匹配（平分法误区、三条件+两边界） |
| [[哈希表冲突解决与Dictionary底层]] | 链地址法/开放地址法对比（信箱 vs 停车场比喻）、开放地址删除需墓碑标记、C# Dictionary 分离链接结构与其扩容机制；哈希应用两数之和 O(n)（Day 36） |
| [[完全二叉树]] | 概念笔记。**定义**：除最后一层外每层填满、最后一层节点**全部靠左连续** ⇔ 层序编号 1..n 后位置恰好占满、中间无空洞。**三个近义概念别混**：满二叉树 Perfect（每层填满，`2^h-1` 个）⊂ 完全；Full Binary Tree（每节点 0 或 2 个孩子）与完全**无包含关系**。**最大价值 = 数组连续存储**（1-based 左 `2i` / 右 `2i+1` / 父 `i/2`）→ 堆与线段树都靠它、不存指针；反之 **BST 不能这样存**（可退化成链、满是空洞）。**判定**：层序 BFS，出现空节点后不能再有非空（★ 故意把 `nullptr` 入队，与常规 BFS 相反）。**核心对比：与 BST 是两个正交维度** —— 完全二叉树管**形状**不关心值，BST 管**值序**不管形状，四象限均可举实例（完全+非BST 的 `9/5,8` 就是**大顶堆**）。**最大误解：堆不是 BST** —— 堆只保证「父≥子」弱序，不保证右子树全 > 根，故无法范围查询与有序遍历。**延伸**：维持「完全」插入删除代价极高（需整层平移补洞），故平衡树退而只保证**高度有界** |
| [[用vector实现栈]] | 面试题。栈只需 vector 的「尾部三件套」：`push`→`push_back`、`top`→`back`、`pop`→`pop_back`。★ **最大坑**：`pop_back()` 返回 `void`，不能 `return data_.pop_back()`，必须 `back()` 取值后再 `pop_back()` 删除 —— **顺序反了丢元素 + 空栈 UB**（与《栈与队列的相互实现》的 `top`/`pop` 同源）。次要：空栈 `back()` 是 UB 不抛异常 / `pop_back` 不缩容 / **扩容后旧 `top()` 引用失效**。`push` 摊还 `O(1)`（GCC 容量翻倍 `1→2→4→8→16`，触发时整体搬家；MSVC 是 1.5 倍；`reserve` 可免搬家），`pop`/`top` 为 `O(1)`。★ `std::stack<T, vector<T>>` 可直接换底层，默认用 `deque` 正是因为扩容不搬家。四底层对比：vector（最省内存但会搬家）/ deque（默认，不搬家但内存略大）/ 静态数组（最快但容量写死）/ list（完全没必要）|
| [[栈的输出序列种数]] | 经典组合数学。**结论**：`n` 个元素依次入栈，可能的出栈序列种数 = **第 n 个卡特兰数** `C(n) = C(2n,n)/(n+1) = (2n)!/(n!(n+1)!)`；前几项 `1,1,2,5,14,42,132,429,1430,4862`。**为什么是卡特兰**：把「入栈」记成左括号、「出栈」记成右括号，2n 步构成括号序列，约束是「任何前缀里出栈次数不超过入栈次数」→ 正是合法括号序列的计数。**n=3 的枚举**：`3! = 6` 种排列里**只有 `312` 不可能** —— 要出 3 必须先把 1、2、3 全压进去，弹掉 3 后栈顶是 2，下一个只能是 2（推广：能由栈产生的排列不含 `312` 模式）。**三种算法**：① 公式法 `res = res*(n+i)/i` 最后除 `n+1`（每步精确整除、无浮点误差）② 递推 `C(n+1) = C(n)*2(2n+1)/(n+2)` ③ DP `C(n) = Σ C(i)·C(n-1-i)`（O(n²)，只有加乘最稳）。★ **溢出实测（重要）**：公式法用 `long long` **最多只能算到 n = 30** —— 中间量 `res*(n+i)` 先溢出，即使**最终结果到 n = 35 还装得下**（`C(35) ≈ 3.1e18`、`C(36) ≈ 1.2e19` 才超 9.2e18）；要算到 35 必须 `__int128`。★ **别搞混两种数**：出栈**序列**种数 = `Catalan(n)`（n=3 时 5），而入出栈**操作方案**数（含栈空仍出栈的非法情形）= `C(2n,n)`（n=3 时 20）。★ **一题多用**：n 个节点的不同二叉树形态数、n 对括号合法序列、n+2 边形三角划分数、n×n 网格不越对角线的路径数，全是卡特兰。识别判据：**两种操作交替，任何前缀里 A 的次数不少于 B**。暴力枚举（真模拟栈）+ 三种公式，n=1..7 全部一致；大值用 Python 独立核对到 n=37 |

### 链表（3）— `algorithm/链表`

| File | Summary |
| --- | --- |
| [[链表反转]] | 链表反转两种写法：三指针迭代法（O(n)/O(1)）与递归法（O(n)/O(n)），先保存 next 防断链；附快慢指针 Floyd 判环 |
| [[两个链表的第一个公共结点]] | 单链表相交必为 **Y 形**（共用尾部，不可能 X 形）；三种解法：长度差对齐法 / 双指针「走完自己走对方」（路程都是 m+n，代码最短，面试首选）/ 哈希表；易错点：比引用不比值、无交点返 null（剑指 Offer 52 / LC160） |
| [[链表中环的入口结点]] | 快慢指针找相遇点 → fast 重置 head、两指针同速再走，再遇即入口；核心推导 `a = (n-1)L + c`，即「头→入口 = 相遇点→入口（+整数圈）」；易错：第二步必须改回 1 步、不能直接返回相遇点（剑指 Offer 23 / LC142） |

### 树与平衡树（17）★ 主线 — `algorithm/树与平衡树`

| File | Summary |
| --- | --- |
| [[DFS与BFS遍历算法]] | DFS（深度优先）与 BFS（广度优先）对比：数据结构、应用场景、复杂度；BFS 无权图最短路径、DFS 环检测；二叉树四种遍历（前/中/后序+层序） |
| [[二叉搜索树BST]] | BST 性质（左<根<右，**中序遍历 = 升序**）；递归查找 / 插入（查到空位才 new）/ 删除三情况（叶子 / 单孩子顶替 / 双子用**中序后继值替换+递归删后继**）；三个实现坑：根指针未初始化野指针、remove 递归返回值没接住丢子树、分支缺 return；顺序插入退化成链 O(n) → 引出平衡树 |
| [[平衡二叉树]] | 概念笔记。**核心认知：不是具体树，而是家族概念** —— 本质是给 BST **附加「高度约束」**防退化：`平衡二叉搜索树 = BST（值序） + 自平衡机制（高度）`。**术语歧义**：国内教材（严蔚敏）直接把 AVL 定义为「平衡二叉树」，面试说「平衡二叉树」大概率指 AVL，但学术/工程语境是家族统称，答题先界定。**指标**：`bf = height(左) − height(右)`，★ 是**高度差不是节点数差**；AVL 要求 `\|bf\| ≤ 1`。**强度是一条谱系**：严格（AVL，树高 `1.44log₂n`）→ 弱平衡（红黑树「黑高 + 禁红红」，`2log₂(n+1)`）→ 随机化（Treap，期望 `O(log n)`）→ 均摊（Splay，单次可达 `O(n)`）；**越严格查询越快、写时转得越多**，故读多写少选 AVL、读写均衡选红黑树（工程上红黑树最多：`map/set`、`TreeMap`、Linux `rbtree`）。**维持手段统一是旋转**：只改局部结构、**中序不变** → BST 性质自动保持。**三个正交维度**：完全二叉树管**形状**、BST 管**值序**、平衡二叉树管**高度**；可两两组合（完全+平衡 = **堆**；BST+平衡 = AVL/红黑树）。**五个误区**：家族当具体树 / 按节点数算平衡 / 与完全二叉树混淆 / 以为 AVL 全面优于红黑树（删除 AVL 最坏转 `O(log n)` 次、RBT 仅 ≤3 次）/ 以为平衡树永不退化（Splay 单次 `O(n)`） |
| [[判断平衡二叉树]] | 剑指 Offer 55-2 / LC 110。**定义**：对树中**每一个节点**，`abs(height(左) − height(右)) <= 1` —— 两个关键词正对应两个坑：**「每一个节点」**（必须递归到所有节点）与 **「≤ 1」**（差 1 也合法，不是相等）。❌ 初版两错方向相反：① 只判根不递归子树 → **漏判**（反例：根左右各 3 层相等，但节点 2 左右差 2）；② 用 `height(left) == height(right)` → **误判**（根高度差 1 本来合法）；③ `height` 重复调用 → 平衡树 `O(n log n)`、链状树 **`O(n²)`**。**解法一 自顶向下**（最小修正：`==` 改 `abs(...)<=1` + 补两行递归子树），直观但慢。**解法二 后序 + `-1` 哨兵（首选）**：`getHeight` 返回高度、失衡返回 `-1`，判断在**后序位置**；★ **两处 `-1` 短路不能省**（左、右各一句），少写就退化成 `O(n²)`；有短路则每节点只访问一次 → **`O(n)`**。**解法三 `pair<bool,int>`** 语义更清晰。**与《验证二叉搜索树》同构**：两题都是「每个节点都要满足」的**全树性质**，都易犯「只看根」的错，解药都是「递归到每个节点 + 后序剪枝」→ 自查口诀「**子树我判了吗？**」。易错补充：漏 `abs()` 只拦一边。时间 `O(n)` / 空间 `O(h)` |
| [[二叉搜索树的最近公共祖先]] | 剑指 Offer 68-I / LC 235。**核心认知：LCA = p 与 q 第一次被「分到两边」的节点** —— 三支判断：都 `< cur` 走左、都 `> cur` 走右、**其余情况 `cur` 即答案**（「其余」刚好覆盖「一左一右」与「p 或 q 本身就是 cur」，符合「节点可以是自己的祖先」）。★ **不需要存路径**：有序性让每步都能预判方向，**分叉即停**。**解法一 BST 分流（首选）**：迭代 `while(cur)` 三支，**`O(h)` / `O(1)` 空间**；递归版更短但多递归栈。**解法二 路径法**（初版思路，方向正确）：`getPath` 存两条根路径取「最后一个公共节点」，`O(h)` 空间、~20 行，但**能泛化到普通二叉树**。⚠️ **表述精修**：不是「求**值最接近**的交集」（反例 `p=3,q=5` 值最接近的是 3、5 本身，而 LCA 是 4），而是「**两条根路径公共前缀里最深 / 最后一个**的节点」——「交集」说法准确，补上「取最深那个」即可。**路径法三坑**：用 `val` 而非指针比较、忘记取 `min(size)`、循环必须「遇第一个不等即 break」。**延伸 LC236 普通二叉树**：无有序性只能**后序自底向上** `O(n)`，与 BST 版「自顶向下单向下降」正好是两种方向对照。易错：分叉判断写成 `<=`/`>=`、只写「都左/都右」忘了 `else` 兜底 |
| [[二叉树的最近公共祖先]] | 剑指 Offer 68-II / LC 236。**普通二叉树没有有序性** → BST 的「按值分流」彻底失效，只能**后序自底向上汇合** `O(n)`。**返回值是混合语义**（理解这题的关键）：`dfs(root)` 返回「以 root 为根的子树里找到的那个 p 或 q；若两者已找到且 LCA 已确定则返回 LCA；都没有则 `nullptr`」——★ **返回值不一定是 LCA**，只是「命中的节点」，真正的判定在**上层**完成。**三种情况**：左右都非空 → `root` 是分叉点；只有一侧非空 → 往上送；都空 → `nullptr`。**为什么「命中即返回自己」对**：因为「节点可以是自己的祖先」，p 是 q 祖先时走到 p 直接返回 p 就是 LCA，不必再往子树找 q。**必须后序**（先拿到两侧返回值才能判断）；命中判断必须放**终止条件**而非「先递归再看」。**备选路径法**：普通二叉树求路径不能按值定方向，要全树 DFS + **`pop_back()` 回溯**（不回溯会让未命中分支污染路径）。**与 BST 版对照**：自顶向下 `O(h)/O(1)` vs 自底向上 `O(n)/O(h)`；**为什么降不到 `O(h)`** —— 无序则到达某节点前无法排除任何一侧，必须两边搜完。延伸：LC1676 k 个节点 LCA 用「子树命中计数 == k」 |
| [[二叉树的序列化与反序列化]] | 剑指 Offer 37 / LC 297。**序列化三个必须**：① **值转十进制文本**（`char` 只有 1 字节 −128~127，`r[i]=cur->val` 必截断，须 `to_string`）② **逗号分隔**（否则 `123` 分不清 `1,2,3` 还是 `12,3`）③ **`'\0'` 结尾**（`string` / `strcpy` 自带）。**内存铁律**：所有要活过函数的东西**必须 `new`**。❌ 初版 8 个问题，**4 个必崩**：`char r[100]` 是栈上局部数组 `return r` → **悬空指针**；`char` 存 `int` 截断；无分隔符；无 `'\0'`；`TreeNode root(str[0])` + `return &root` → **悬空**；`TreeNode left/right` 定义在 **while 循环体内**每轮析构 → `cur->left` 指向已销毁对象；`i` 起点应 1（根已单独建）；`cur->right` 误用 `str[i]`（应为 `str[i+1]`）。⚠️ 这类 bug **有时能跑通**（栈还没被覆盖），最难查。**解法一 BFS**（直观）：`string` 缓冲 + `to_string` + `,`，反序列化「每弹出一个非空节点正好消耗两个 token」；**解法二 递归前序**（最短）：`getline(in, tok, ',')` 一步完成切分读取，格式 `1,2,#,#,3,4,#,#,5,#,#`。★ **必须带 `#` 占位**，只存非空节点的前序序列无法唯一还原形状。`O(n)/O(n)`，两版均已 g++ 实测 round-trip 通过（含空树 / 单节点负值边界） |
| [[重建二叉树]] | 剑指 Offer 7 / LC 105。**核心思路就三步**：① **前序第一个一定是根**（唯一入口）② 拿根去**中序里找位置**，它把中序切成两半（左＝左子树、右＝右子树）③ 左半边节点数 `leftSize` 决定前序怎么切，然后**递归两段**。**下标推算（唯一难点）**：设前序 `pre[pl..pr]`、中序 `vin[il..ir]`、根在中序下标 `k`，则 **`leftSize = k - il`**（★ 不是 `k`！`il` 在子树层不为 0）；左子树 `pre[pl+1, pl+leftSize]` / `vin[il, k-1]`，右子树 `pre[pl+leftSize+1, pr]` / `vin[k+1, ir]`（★ `+1` 是跳过根，最易漏）。**代码要点**：`if (pl > pr) return nullptr`（★ 写成 `>=` 会丢掉叶子）；哈希表存「值→中序下标」把线性查找的 `O(n²)` 降到 **`O(n)`**。**变体**：后序+中序（LC106）用「**后序最后一个**」当根，且算的是 `rightSize = ir - k`（后序是「左-右-根」，根左边紧邻右子树）。**组合可行性**：中序+前序 / 中序+后序 / 中序+层序均可；**前序+后序不行**（无法区分单孩子节点的左右，反例 `pre=[1,2] post=[2,1]`）。手推示例与六个易错点见笔记 |
| [[二叉树的右视图]] | 牛客 BM41 / LC 199。**核心认知：右视图 = 每层最右节点**，两条视角对应两条路。**解法一 重建 + BFS 层序**（初版思路，正确）：★ **必须先存 `sz = q.size()`** 再开 `for`，写成 `i < q.size()` 会因 push 而动态变大 → 层边界失效、结果混入别层节点。**解法二 DFS「根-右-左」+ 深度首现（推荐）**：`if (depth == res.size()) res.push_back(node->val)` —— `res` 严格按深度 0,1,2… append，故 `size()` 恒等于「已记录最大深度 + 1」，`==` 即「该深度第一个」；★ **顺序写成「根-左-右」就得到左视图**；`O(n)/O(h)`，省掉队列。**解法三 不建树**（只要求视图、不要求返回树时最优）：直接在前序/中序区间上按根-右-左递归，右子树前序 `[pl+1+leftSize, pr]`、中序 `[k+1, ir]`（先算 `leftSize` 再递归），彻底省掉 `new TreeNode`。**三版对比**：BFS `O(n)/O(n)` 最直观 / DFS `O(n)/O(h)` / 不建树 `O(n)/O(h)` 另加哈希表。**顺带性质：右视图长度 = 树高**，可作自检。三版均 g++ `-Wall` 零警告、5 例（含全左斜链 / 空树）一致通过 |
| [[二叉平衡树AVL]] | **严格平衡 \|bf\| ≤ 1**：平衡因子 + **最小失衡子树** + 四型旋转 **LL/RR/LR/RL**（之字先掰直，rotate 后先更新儿子再更新新根）；插入**至多 1 次旋转** vs 删除**最坏 O(log n) 次**；斐波那契树高上界 **1.44·log₂n**；与 BST / 红黑树三方对比 |
| [[红黑树]] | 五性质（根黑 / NIL 黑 / 禁红红 / 黑高相等）；黑高推导树高 ≤ 2·log₂(n+1)；插入默认红、父红才修；**插入三 case**（叔红→变色上推 / 叔黑之字→绕 P 掰直 / 叔黑一字→绕 G 旋转+换色）；**2-3-4 树同构**（黑=占层格子、红=格内挂件、禁红红=每格≤3 key）；纠错：旋转看结构不靠颜色、AVL 插入至多 1 次旋（痛点是删除 O(log n) 次）；C++ map/set 底层 |
| [[二叉搜索树转双向链表]] | 剑指 Offer 36 / LC 426。**核心认知：BST 中序 = 升序，本题是「遍历题」不是「结构变形题」** —— 旋转拉直树是错误方向（旋转是平衡树调高度的工具、一次只挪一条边、需 O(n) 次且无法收敛）。解法：中序递归 + `prev` 串联（`node->left=prev`；`if(prev) prev->right=node`；`prev=node`），链头链尾原本就是 nullptr 无需清理、不用 dummy（禁用新建节点）。**两大坑**：`prev` 必须 `TreeNode*&` 引用传参（值传递时副本随栈帧销毁 → 链头丢失，编译通过但返回空指针）、`node->left` 必须在递归左子树「之后」才改（否则丢左子树）。另附迭代栈版与分治 `pair<head,tail>` 版；时间 O(n) / 空间 O(h) |
| [[对称的二叉树]] | 剑指 Offer 28 / LC 101。**核心认知：对称是「结构镜像」，不是「序列回文」** —— 对称 ⇒ 中序回文，反之**不成立**（反例：`1/左2(左3) / 右3(左2)`，中序 `3,2,1,2,3` 回文但左右子树值不等）。对称的递归定义：`a->val == b->val` 且 `isMirror(a->left, b->right)` 且 `isMirror(a->right, b->left)` —— **配对交叉**（`a->left` 比 `b->right`）。**三类终止条件顺序不能换**：都空→true、一空一非空→false、值不等→false（`(!a && !b)` 放第一同时挡掉解引用）。解法：递归镜像（首选）/ 队列成对入队迭代（`if (!a && !b) continue;` 不能省）。❌ 初版「中序存 vector 再逆序比」有 6 处问题：`i` 恒等于 `size()` 导致 `result[i]` **越界 UB**、左子树为空时必然越界、`i--` 到负数、不剪枝、`stack<TreeNode*> stack;` 遮蔽模板名、状态机过绕 —— 且**即使全修对，判据本身仍会误判**。时间 O(n) / 空间 O(h) |
| [[合并二叉树]] | LeetCode 617。**核心认知：配对是「同侧」** —— `a->left` 配 `b->left`、`a->right` 配 `b->right`，**左右各自对齐**，与《对称的二叉树》的**交叉配对**正好相反（两题递归骨架几乎一样，唯一分水岭就是这一处）。规则：重叠节点**值相加**、不重叠节点**连同整棵子树直接接管**。**最大坑**：两个 base case 必须 `return root2` / `return root1`，写成 `return nullptr` 会丢掉所有不重叠子树；且一侧为空时 `O(1)` 挂上、不用继续遍历。原地版 `root1->val += root2->val` 会修改入参（LC 允许，需主动说明）；另有不破坏输入的 new 版与队列存「成对节点」的迭代版（注意 `else if (!a->left) a->left = b->left;` 方向）。时间 **O(min(m,n))** / 空间 O(min(h₁,h₂)) |
| [[二叉树的镜像]] | 剑指 Offer 27 / LC 226。**核心认知：镜像改的是「结构」不是「值」** —— 本质只有一句话：**对每个节点做一次 `swap(left, right)`**（交换左右孩子**指针**）。❌ 初版两处根本性错误：① 交换 `a->val`/`b->val` 而不是指针 → 树形一点没变、只是换标签（单链树 `1→左2` 更是**无值可换但结构仍需翻转**）；② **误借「判对称」的双参成对骨架**（`push(left); push(right);` 成对 pop）→ 本题是**单参**、每节点独立操作，**不需要配对**；连带崩掉的是 `aLast`/`bLast` 初值 `nullptr` 直接解引用（单链树第一步即崩）、把节点挂回上一节点会**覆盖原子树**、`if(!a&&!b) continue;` 是死代码、`queue<TreeNode*> queue;` 遮蔽模板名。解法：递归 `swap` + 左右递归（首选，swap 放前放后都行）/ 队列·栈迭代（**与遍历顺序无关**，因不需要回溯状态）。时间 O(n) / 空间 O(h)。延伸：镜像是对合运算（做两次即还原）、镜像后中序为原中序的逆序（对称树中序即回文） |
| [[验证二叉搜索树]] | 剑指 Offer 98 / LC 98。**BST 约束是「整棵子树」级，不是「父子」级** —— `10` 的右子树里所有节点都必须 `> 10`。**经典反例**：`10 / 左5, 右15(左6, 右20)`，每条父子边都合法却非 BST（6 < 10）。❌ 初版 `if(root) return true;` 少写 `!` → 非空恒真、空则崩，且后面两行在 `return` 之后是死代码；只比父子也必然误判。**解法**：① 上下界递归 `check(node, low, high)`，往左收紧上界、往右收紧下界（边界须 `LLONG_MIN/LLONG_MAX`，**Windows `long` 只有 32 位**）② 中序严格升序，`prev` 须 `TreeNode*&` 引用传参（与《二叉搜索树转双向链表》同坑）③ 迭代中序防爆栈。O(n) / O(h) |
| [[判断完全二叉树]] | LeetCode 958。**核心认知**：完全二叉树 ⇔ 层序编号 1..n 后位置恰好占满，故有两条等价判定 —— ① **断层视角**：层序遍历中一旦出现空位，后面不允许再有非空；② **编号视角**：`maxIdx == 节点数`。**解法一 BFS + 空节点标记**（首选）：★ **必须把 `nullptr` 也入队**当断层标记（别判空再入队）—— 这是全库唯一「故意让空指针入队」的 BFS。**最大坑**：写成 `if (node->left) q.push(...)` → 队列永无空节点 → `seenEmpty` 永不触发 → **函数恒返回 true**，与《验证二叉搜索树》迭代版 `cur && !st.empty()` 同属**静默失效**。次要：空节点用 `break` 会提前跳出。**解法二 层序编号**：左 `2i` 右 `2i+1`，判 `maxIdx == count`；判据更本质、无需 `seenEmpty`，但 `idx` 须 `long long` 且起点为 1。**不推荐递归**：需同时知道「是否已缺」+「层级」，比 BFS 绕 —— 少数「迭代优于递归」的树形题。时间 O(n) / 空间 **O(n)**（非 O(h)，空指针也占位） |

### 查找与排序（2）— `algorithm/查找与排序`

| File | Summary |
| --- | --- |
| [[二分查找]] | 二分查找原理、最少/最多比较次数推导（⌈log₂n⌉）、O(log n) 时间复杂度、C# 内置 BinarySearch |
| [[排序算法-快排归并堆排]] | 三大排序对比 + **快排四种写法全解**：**Hoare（推荐直接背）**——`i=l-1`/`j=r+1` 配 `do-while` 是「pivot 取自区间内部」换来的**天然哨兵**，递归边界必须用 `j`/`j+1`（用 `i` 死循环）、`a[i] < p` 不能写 `<=`（全相同越界）、pivot 取**中间位置**即抗有序退化（1e6 升序 8ms），**不必随机化**；**Lomuto** 最好懂但 pivot 取最右在有序/全相同退 `O(n²)`（16k 有序 77.46ms vs Hoare 1.01ms，n 翻倍 ×4）；**三路划分** 等值段不递归（1e6 全相同 0ms）；**迭代版** 短半递归/长半循环，栈深恒 `O(log n)`。1e6 四场景实测表（随机 66/75/77/66ms）证明**随机化不免费、加了反而慢 15%**；附五个坑速查 |

### 算法思想（5）— `algorithm/算法思想`

| File | Summary |
| --- | --- |
| [[递归与迭代转换]] | 递归的栈溢出与性能开销、尾递归与 TCO（C# 默认不做）、一般递归用栈/队列显式保存状态（DFS 用栈、BFS 用队列）、二叉树前序迭代模板；账本比喻：不欠账→循环，欠账要回溯→显式栈 |
| [[贪心算法入门]] | 贪心=每步局部最优；找零钱翻车案例证明贪心不是万能；适用条件（贪心选择性质+最优子结构）；与动态规划的边界 |
| [[动态规划入门]] | DP 两大核心（最优子结构、重叠子问题）+ 三板斧（定义状态、转移方程、初始值）+ 爬楼梯变体例题 |
| [[滑动窗口的最大值]] | 剑指 59-I / LC 239 / 牛客 BM45。**核心认知**：窗口每滑一步要同时「加右 + 丢左」，三个动作作用在**两端** → 必须用**双端单调队列**（`push_back` / `pop_front` / 读 `front`），单调栈只能动一端做不了。三个动作：① 队尾所有 `<= nums[i]` 的弹掉（更小且更早过期，永无机会当最大值）② 新下标入队 ③ 队首 `<= i-k` 弹掉过期；`i >= k-1` 时输出 `nums[front]`。★ **必须存下标不存值** —— 存值无法判断队首是否已滑出窗口（实测 `[7,2,4] k=2` 输出 `[7,7]`，正确应为 `[7,4]`）。易错：出队写 `< i-k`（应 `<=`）/ 记录写 `i > k-1`（应 `>=`）/ push 前读 `front` 可能空队列 UB / 忘处理 `k<=0`、`k>n`。复杂度 **`O(n)` / `O(k)`** —— 每个下标一生只进队一次、出队一次，**内层 while 不是 `O(n²)`**。★ 判据：**看到「窗口 + 极值」→ 单调队列；看到「下一个更大/更小」→ 单调栈**，区别在「要不要按位置淘汰」。附 8 步完整推演表 + 单调栈/单调队列对照表 + DP 优化延伸（`LC 1425`）。实测 7 例 + 3000 组随机对拍全一致 |
| [[数位加减凑倍数]] | 牛客《小红的数位操作》（京东 23 届春招）。★ **原题三个条件别漏**：每位**最多**操作一次（可不操作）+ 9 不能加 / 0 不能减 + **前导零要一并输出** → `3^k` 枚举且必须按字符串拼（样例 `101 123456789 → 000`）。本文主体讲收紧后的简化版（每位必须 ±1），价值在于核心认知 **第 i 位 ±1 ≡ 整体 ±10ⁱ**，进位借位全自动，题目降级为在 `2^k` 个 `n + Σδᵢ·10ⁱ` 里找 p 的正倍数；⚠️ 简化版在原题上会答错（同一例答 `-1`）。初版四个硬伤：递归到个位直接 `return false` / 改数组不回溯致分支污染 / `if(n<p) return false` 砍掉一半答案 / 判据放在递归中途。 |

### 图与字符串（5）— `algorithm/图与字符串`

| File | Summary |
| --- | --- |
| [[KMP字符串匹配]] | KMP 核心思想：主串指针不回退，利用 next 数组（最长相等前后缀）计算模式串滑动距离，复杂度从 O(n×m) 降到 O(n+m) |
| [[A星寻路算法]] | A* = Dijkstra + 启发式 f(n)=g(n)+h(n)；h 可采纳性保证最优、高估错过最优解；h=0 退化为 Dijkstra；网格启发函数选择；NavMesh 底层思想（Day 37 面经） |
| [[字符串变形]] | 牛客 BM83。**核心认知**：这题是两件事 —— ① 单词顺序反序（词内字符顺序不能变）② 每个字符大小写反转。★ 初版唯一错：`for (i = n-1; i > 0; i--)` 写成 `> 0` **漏掉 `s[0]`** → 反序后它属于最后一个单词，表现为「最后一个单词缺首字母」（`Hello World` → `wORLD ELLO`；`This is a sample` → `SAMPLE A IS HIS`；**单字符 `a` 直接返回空串**）；7 个用例 6 个错，只有空串那个「对」。**栈思路很巧**：从右往左压栈、遇空格弹栈 → LIFO 把「逆序压入」变成「正序弹出」，一个栈同时搞定单词反序 + 词内正序。修法一只需 `i >= 0`。**修法二（推荐，`O(1)` 空间）**：原地两遍反转 —— 整体 `reverse`（单词顺序对调、词内也反了）→ 逐词 `reverse` 恢复。★ 坑：`toupper/tolower` 参数转 `unsigned char`（UTF-8 高位字节传负数是 UB）/ 首尾与连续空格必须原样保留（`" hello world "` → `" WORLD HELLO "`）/ 不能 trim。修法一二随机对拍 5000 组一致 |
| [[最长公共前缀]] | LC 14 / 牛客 BM84。**核心认知**：「前缀」必须从下标 0 开始（≠ 最长公共**子串**），答案只需确定长度 `j`。**解法一 纵向扫描（首选）**：字符矩阵一列一列往下比，第一个不一致的列立即返回 `strs[0].substr(0, j)`。优点：最坏也是最优 `O(S)`（S = 字符总数）、空间 `O(1)`、能提前退出。★ **最关键的一行**：`if (j >= strs[i].size() \|\| strs[i][j] != c)` —— **越界判断必须放在 `\|\|` 左边**，靠短路求值保护右边的 `strs[i][j]`；顺序反了就是 UB（和《栈与队列的相互实现》里括号匹配「`st.empty() \|\|` 在前」完全同源）。**解法二 横向扫描**：以首串为候选前缀，两两求 LCP 逐步收缩，`prefix` 单调变短。**解法三 排序比首尾**（代码最短）：字典序性质 —— 最小串与最大串前 j 位相同，则任意中间串前 j 位也必相同（反证：否则它会小于最小或大于最大）；代价是 `O(n·m·log n)`。易错：空数组直接 `strs[0]` 崩 / `substr(0, j)` 第二参是**长度** / 忘 `substr` 会把整条首串返回（`["abc"]` 恰好对、`["abc","abd"]` 就错）/ `size()` 是 `size_t` 会有符号警告。三版实测 10/10 + 随机对拍 5000 组一致。延伸：Trie 多次查询、后缀数组 height 数组、二分长度+哈希 |
| [[验证IP地址]] | 牛客 BM85 / LC 468。**IPv4 四道关卡**：段数 = 4、每段 1~3 位纯数字、**无前导零**（单个 `0` 合法）、值 ≤ 255；**IPv6 三道关卡**：段数 = 8、每段 1~4 位、纯十六进制（**允许前导零**），并且**不支持 `::` 压缩**（必须写满 8 段）。★ **核心思路**：不要「先猜类型再验证」，而是 `isIPv4()` 不行就试 `isIPv6()`，两个都不行才 Neither —— 否则「既没有点也没有冒号」的串会走错分支（初版 `isIPv4` 初值 `false`，`abc` 被判成 IPv6）。★ **初版 12 处错、23 个用例只对 11 个**：`toNum` 写成 `sum += sum*10`（应为 `= `，`123` 算成 **146**、`240` 算成 **286**）；阈值 `>= 255`（应 `> 255`，255 合法）；前导零判断漏 `size() > 1`（把 `0.0.0.0` 误杀）；**完全没检查段数**（`1.1.1`、5 段全放过）；**循环结束忘校验最后一段**（`1.1.1.999` 返回 IPv4）；IPv6 完全不校验字符集（`gggg` 也能过）。★ `split` 实现必须补 `parts.push_back(cur)` 收尾，漏了就丢最后一段（`1.1.1.` 只切出 3 段从而误判合法）。其他：`isdigit`/`isxdigit` 参数要转 `unsigned char`；空段上访问 `s[0]` 是 UB。正确版实测 23/23 |

### 工具与模板（2）— `algorithm/`（根目录）

| File | Summary |
| --- | --- |
| [[ACM模式输入输出]] | 工具页。**ACM = IO 壳 + 算法 + 输出格式化三层**，只有中间那层与核心代码模式相同 → 切模式时算法原样搬，只补外壳（`solve()` 读一组算一组，`main()` 管多组循环 + IO 提速）。**五种输入形态**：单组读 n / 先给 T / **不给 T 读到 EOF**（`while (cin >> n)`，牛客最常见）/ 整行含空格 / 逗号分隔。★ **最大坑**：`cin >> x` 后直接 `getline` 读到空串 → **整体错位一行、最后一组丢失**（实测）。输出三坑：行末空格 / 组间空行 / `endl` 强制 flush 致 TLE。本地调试用 `< in.txt` 重定向。含 g++ 8.1.0 下 `bits/stdc++.h` 报错的环境提示 |
| [[进制转换]] | 通用工具页。**N 进制转十进制只有一条式子**：`v = v * base + digit(c)` —— 十六进制就是 `base = 16`，和十进制版 `v = v*10 + (c-'0')` 只差基数（注意别写成 `v += v*base`）。`digit(c)` 用「`c` 按位或 `0x20`」折小写，只判一段范围就能同时覆盖大小写字母。★ **`0x` 里的 `x` 怎么办**：它是**前缀标记不是数字**，必须先剥掉（`s.substr(2)`）；**不要**在循环里无条件 `continue` 跳过 x —— 那样 `"1x2"` 这种非法输入也会被当成合法。**标准库四选**：`stoi(s, nullptr, 16)`（认 `0x`/`0X`，非法抛 `invalid_argument`，溢出抛 `out_of_range`）/ `stoul` / `stoll`（`FFFFFFFF` 超 int，必须换）/ `strtol`（把停在哪写进 endptr，能校验是否整串消费）。★ **反向输出** `cout << hex` 是**粘性**标志，用完必须 `<< dec` 关掉，否则后面所有输出都变十六进制。通用版 `toDec(s, base)` 记得加 `d >= base` 检查（`toDec("2", 2)` 应非法）。手写版与 stoi 实测 9 例全部一致 |

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

> **每日面试题与算法题不在本目录**：每天问答过的题目记录在**日记**里（`journal/YYYY-MM/YYYY-MM-DD.md` 的「每日知识问答」节），那里才是「我每天学了什么」的载体。本目录只沉淀**已消化的知识讲解**。

共 115 篇资源笔记（unity 56 / os 1 / csharp 8 / algorithm 39 / archive 9 / meta 2）。算法区 39 篇已按 6 个子目录物理分类；`resources/algorithm/_index.md` 为分区导航（不计入）。
