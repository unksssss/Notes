---
title: "Unity 类继承体系 Object-Component-Behaviour-MonoBehaviour"
type: resource
tags: [unity, 生命周期, 架构, 面试, scripting, 类层级]
created: "2026-09-11"
updated: "2026-09-11"
status: active
summary: "System.Object → UnityEngine.Object 四大分支；Component 两条腿——走 Behaviour（MonoBehaviour/StateMachineBehaviour + 大部分内置组件）vs 直接继承（Transform/Renderer/Collider3D/Rigidbody）；3D 与 2D 不对称（PhysX vs Box2D）；BehaviourManager 派发与 enabled/isActiveAndEnabled 区别；Unity 不允许继承 Component/Behaviour 的硬约束"
---

# Unity 类继承体系 Object-Component-Behaviour-MonoBehaviour

（Day 39 知识点）

> 知识点 + 面试/实战常见追问一次说清。先讲体系，再讲两条关键腿（Behaviour vs 直接继承），最后讲为什么这么设计。

---

## 一、整体结构（一句话版本）

**`System.Object` → `UnityEngine.Object` → 四大分支（场景容器 / Component 系 / 资源资产 / 运行时概念）**，其中 Component 系再分两条腿：**走 Behaviour 的（你的脚本 + 大部分内置组件）** 和 **直接继承 Component 的（Transform / Renderer / Collider3D / Rigidbody）**。

---

## 二、`UnityEngine.Object` —— 整个体系的根

继承自 `System.Object`，但**重写了 `==`/`!=` 运算符**和**实现了 `IDisposable` 风格的销毁机制**。这是 Unity 与 .NET 体系最不一样的地方——Object 实例的"存活"由 C++ 端 native 对象托管，C# 引用变成"包装壳"。详见 [[UnityEngine.Object 判空与销毁机制]]。

它提供四大类能力：

| 能力 | 关键成员 | 说明 |
|---|---|---|
| **生命周期** | `Destroy` / `DestroyImmediate` / `DontDestroyOnLoad` / `Instantiate` | native 对象销毁/克隆由引擎控制 |
| **空检查** | 重写 `==` `!=` `bool` | `obj == null` 能识别 native 已死但 C# 引用还在的情况（fake null）|
| **查找** | `FindObjectOfType` / `FindObjectsOfType`（新版 `FindAnyObjectByType` / `FindFirstObjectByType`） | 按类型查活跃对象 |
| **元数据** | `name` / `hideFlags` / `GetInstanceID()` | 调试与 Editor 用 |

---

## 三、Object 四大分支

### 分支 1：场景/容器（2 个）

- **`GameObject`**：`sealed` 类，**不能继承**。场景里所有可见实体的根。组件的宿主，本身不是组件。提供 `AddComponent<T>()` / `GetComponent<T>()` / `SetActive(bool)` / `transform` / 父子层级管理。
- **`ScriptableObject`**：数据容器。**不是 Component，不能挂场景**，只能在工程里保存成 `.asset` 文件。适合配置/配方/存档等"独立于场景的数据"。详见 [[scriptableobject数据驱动设计]]。

### 分支 2：Component 系（核心）

见第四节专讲。

### 分支 3：资源/数据/工具（与 GameObject 平级）

`Material` / `Mesh` / `Texture`（子类 `Texture2D` `Cubemap` `RenderTexture` `Texture3D`）/ `Sprite` / `Shader` / `AudioClip` / `AnimationClip` / `AssetBundle` / `Font` / `PhysicsMaterial` / `RuntimeAnimatorController` / `ComputeBuffer` / `GraphicsBuffer` ...

> 共同点：可继承 `Object`，但**不是 Component**，无法挂到 GameObject 上，也不能用 `AddComponent`。

### 分支 4：运行时/静态（容易忽略）

- **`Coroutine`**：协程句柄，由 `StartCoroutine` 返回，继承 Object。详见 [[协程原理与unitask]]。
- **`AsyncOperation`**：异步操作基类（`Scene.LoadSceneAsync` / `Resources.LoadAsync` / `AssetBundle.LoadAssetAsync`）。
- **`YieldInstruction`** + 子类 `WaitForSeconds` / `WaitForFixedUpdate` / `WaitForEndOfFrame` / `WaitUntil` / `WaitWhile`：协程的 `yield return` 产物，**都从 Object 派生**。
- **静态工具类**：`Time` / `Physics` / `Debug` / `Application` / `Input` / `Mathf` / `Random` / `Resources` / `SceneManager` / `PlayerPrefs` / `Graphics` / `GL` …… 这些虽以静态方式调用，**类定义本身仍然继承 Object**——这给它们带来了"fake null 检测"的能力（`Application.isPlaying` 这种属性访问走 native）。

---

## 四、Component 派生树 —— 这条线最容易踩认知误区

### 4.1 Component 自己继承 Object，是**所有能挂到 GameObject 上的东西的基类**。提供：

- `gameObject` / `transform` / `tag` —— 自反访问宿主
- `GetComponent` / `GetComponents` / `GetComponentInChildren` / `GetComponentsInParent` / `TryGetComponent` —— 邻接查找
- `BroadcastMessage` / `SendMessage` / `SendMessageUpwards` —— 反射式消息分发（性能差，非必要不用）
- `CompareTag` / `GetInstanceID`

### 4.2 Component 的两大子类（关键区分）

| 路线 | 典型子类 | 关键能力 | 代价 |
|---|---|---|---|
| **Behaviour → MonoBehaviour** | 你的脚本 + StateMachineBehaviour + Collider2D + Light + AudioSource + Animator + Canvas + Cloth + Joint + NavMeshAgent + ParticleSystem + Terrain + WindZone + ConstantForce…… | `enabled` / **`isActiveAndEnabled`**（同时考虑 enabled + GameObject.activeInHierarchy）；会被引擎加入 **BehaviourManager** 参与 Update/FixedUpdate/LateUpdate 派发 | 派发开销、消息机制开销 |
| **直接继承 Component** | **Transform** / **Renderer**（MeshRenderer / SkinnedMeshRenderer / LineRenderer / TrailRenderer / SpriteRenderer / ParticleSystemRenderer）/ **Collider3D**（Box / Sphere / Capsule / Mesh / Wheel / Terrain）/ **Rigidbody** / **MeshFilter** | 轻量、按需实现 `enabled`，**不进 BehaviourManager** | 无 `isActiveAndEnabled`、无 `enabled` 概念（除 Renderer 自己实现的）|

### 4.3 为什么 Transform/Renderer/Collider3D/Rigidbody 跳过 Behaviour？

Unity 工程师 Karl Jones 在 2016 年论坛的原话：

> "Behaviour 会被加进 BehaviourManager 参与 Update/FixedUpdate/LateUpdate 派发。**Renderer 没有这种需求**，所以直接继承 Component。"

具体到每个组件的理由：

- **Transform**：位置数据，每帧由引擎写，不需要派发回调
- **Renderer**：绘制由渲染管线在 native 端组织（详见 [[Unity 渲染批处理体系]]），不进脚本派发系统
- **Collider3D / Rigidbody**：3D 物理（PhysX）由 C++ 端的 native 系统管生命周期，Unity 不需要 BehaviourManager 介入

物理组件同理：
- **Rigidbody** 不可"禁用"（要么启用要么 kinematic），没有 enabled 概念
- **Renderer / Collider3D** 的 `enabled` 是各自实现的，跟 Behaviour 的 enabled 是两套东西——`renderer.enabled = false` 关掉的是该 renderer 的绘制参与，**不是** BehaviourManager 派发

### 4.4 3D vs 2D 的设计不对称（很反直觉）

| | 3D | 2D |
|---|---|---|
| **Collider** | `Collider` → **直接继承 Component** | `Collider2D` → **走 Behaviour** |
| **Rigidbody** | `Rigidbody` → **直接继承 Component** | `Rigidbody2D` → **走 Behaviour** |

为什么？因为 3D 物理（PhysX）和 2D 物理（Box2D）在引擎内部是**两套独立的 native 系统**——3D PhysX 由 C++ 端的 native 系统管生命周期，Unity 不需要 BehaviourManager 介入；2D Box2D 走 Unity 自己的轻量系统，反而走 Behaviour 走标准派发。这种不对称是历史包袱，不是设计错误。

### 4.5 代码层面后果（写代码时要注意）

```csharp
// ❌ 这样写抓不到 Renderer / Collider3D / Rigidbody
var bs = go.GetComponents<Behaviour>();
// → 只返回你的脚本 + Light + Animator + ...

// ✅ 用 Component 才能全抓
var all = go.GetComponents<Component>();
```

```csharp
// ❌ 编译报错：Renderer 没有这个属性
go.GetComponent<Renderer>().isActiveAndEnabled = true;

// ✅ Renderer 自己的 enabled（不是 Behaviour 的 enabled）
go.GetComponent<Renderer>().enabled = false;
```

---

## 五、Behaviour 子类 —— 你的脚本的家

### 5.1 唯一可继承的是 `MonoBehaviour`

**Unity 不允许你继承 Component / Behaviour / Object**，只能从 MonoBehaviour 派下去。这是引擎硬约束——MonoBehaviour 是**唯一能让 Unity 把你的类识别为"可挂载脚本"**的入口。

```csharp
// ✅ 合法
public class PlayerController : MonoBehaviour { }

// ❌ 编译能过但运行时 Unity 不识别
public class MyComponent : Component { }
public class MyBehaviour : Behaviour { }
```

> 补充：脚本侧另有限制 `sealed` 还能用，但同一脚本挂多个实例的限制看类名/组件名机制（不展开）。

### 5.2 MonoBehaviour 给你的能力

| 类别 | 内容 |
|---|---|
| **生命周期消息** | `Awake` / `OnEnable` / `Start` / `FixedUpdate` / `Update` / `LateUpdate` / `OnDisable` / `OnDestroy` / `OnGUI` / `OnApplicationPause` / `OnApplicationQuit` 详见 [[MonoBehaviour生命周期与SetActive的坑]] / [[Update-FixedUpdate-LateUpdate执行时机]] |
| **协程** | `StartCoroutine` / `StopCoroutine` / `StopAllCoroutines` 详见 [[协程原理与unitask]] |
| **延迟调用** | `Invoke` / `InvokeRepeating` / `CancelInvoke` / `IsInvoking` |
| **编辑器属性** | `[SerializeField]` / `[HideInInspector]` / `[Header]` / `[Tooltip]` / `[Range]` / `[ContextMenu]` / ...（编辑器扩展用 `[CustomEditor]` / `Editor`）详见 [[Unity 序列化机制]] |
| **状态** | `enabled` `isActiveAndEnabled` `gameObject` `transform` `tag` `name`（继承自 Behaviour + Component + Object）|
| **销毁** | `Destroy(this)` / `Destroy(gameObject)`（继承自 Object 的静态方法）详见 [[UnityEngine.Object 判空与销毁机制]] |
| **调试** | `print(...)`（静态类方法，等价 `Debug.Log`）|

### 5.3 唯一另一个继承 Behaviour 的引擎类：`StateMachineBehaviour`

Animator Controller 的 State 节点回调基类：

```csharp
public class AttackState : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) { }
    public override void OnStateUpdate(...) { }
    public override void OnStateExit(...) { }
    public override void OnStateIK(...) { }
}
```

它**不是 MonoBehaviour**，不能挂 GameObject——只能作为 `.asset`（AnimatorController）里的子节点被引擎回调。

---

## 六、完整继承图（精简 ASCII 版）

```
System.Object
└─ UnityEngine.Object
 ├─ GameObject (sealed)
 ├─ ScriptableObject
 │
 ├─ Component ─┬─ Behaviour ─→ MonoBehaviour ─→ 你的脚本
 │            │           └─ StateMachineBehaviour (Animator)
 │            ├─ Transform
 │            ├─ Renderer ─→ MeshRenderer / SkinnedMeshRenderer / LineRenderer
 │            │            └ TrailRenderer / SpriteRenderer / ParticleSystemRenderer
 │            ├─ Collider3D ─→ BoxCollider / SphereCollider / CapsuleCollider
 │            │              └ MeshCollider / WheelCollider / TerrainCollider
 │            ├─ Rigidbody3D
 │            └─ MeshFilter
 │
 ├─ (走 Behaviour 的内置组件：Light / Camera / AudioSource / AudioListener
 │   / Collider2D / Rigidbody2D / Animator / Animation / Canvas
 │   / Cloth / Joint / Joint2D / NavMeshAgent / NavMeshObstacle
 │   / ParticleSystem / Terrain / WindZone / ConstantForce ...)
 │
 ├─ 资源：Material / Mesh / Sprite / Shader / Texture(/Texture2D/Cubemap/RenderTexture)
 │ / AudioClip / AnimationClip / Font / AssetBundle / PhysicsMaterial
 │
 └─ 运行时：Coroutine / AsyncOperation / YieldInstruction(/WaitForSeconds/...)
              + 静态类：Time / Physics / Debug / Application / Input / ...
```

---

## 七、Runtime 验证脚本（用反射打印真实继承链）

```csharp
using UnityEngine;
using System;
using System.Linq;

public static class HierarchyDump
{
    [ContextMenu("打印关键组件的真实继承链")]
    public static void Dump()
    {
        var targets = new[]
        {
            typeof(GameObject), typeof(ScriptableObject),
            typeof(Transform), typeof(Renderer), typeof(MonoBehaviour),
            typeof(Collider), typeof(Rigidbody),
            typeof(Collider2D), typeof(Rigidbody2D),
            typeof(Light), typeof(AudioSource), typeof(Canvas),
            typeof(AnimationClip), typeof(Material), typeof(Texture2D),
            typeof(Time), typeof(Physics), typeof(AsyncOperation),
            typeof(WaitForSeconds),
        };

        foreach (var t in targets)
        {
            var chain = new System.Text.StringBuilder();
            for (var cur = t; cur != null; cur = cur.BaseType)
            {
                if (chain.Length > 0) chain.Append(" → ");
                chain.Append(cur.Name);
            }
            Debug.Log($"{t.Name,-18} : {chain}");
        }
    }
}
```

跑一下，控制台会直接打印 19 条真实继承链——比查文档快，也避免 SPA 翻车。

---

## 八、面试/实战常见追问

### 1. Behaviour 存在的意义是什么？BehaviourManager 派发做了什么事？

**BehaviourManager 是 Unity 内部按 Update/FixedUpdate/LateUpdate 分桶的派发表**，每帧遍历桶调函数，避免每帧遍历场景里所有组件决定是否更新。直接继承 Component 的类不进派发表 → 没 Update 回调 → 没协程支持 → 也无 `isActiveAndEnabled`。

### 2. `enabled` 和 `isActiveAndEnabled` 区别？

- `enabled` 只看自己（Behaviour 的 enabled 开关 / Renderer 自己的 enabled / Light 自己的 enabled）。
- `isActiveAndEnabled` = `enabled && gameObject.activeInHierarchy`（**只有 Behaviour / MonoBehaviour 有这属性**）。

实战口诀：**关自己用 enabled，想确认"现在到底跑不跑"用 isActiveAndEnabled**。详见 [[MonoBehaviour生命周期与SetActive的坑]]。

### 3. 为什么 Renderer 不继承 Behaviour，UGUI 的 Graphic 反而继承 Behaviour？

- **Graphic**（继承 Behaviour）：是 Canvas 批渲染系统的一部分，需要 BehaviourManager 派发来每帧检查脏标记并更新几何；属于"逻辑驱动渲染"的组件。
- **Renderer**：走 native 渲染管线，绘制由 C++ 端组织，不需要脚本层介入。

判断口诀：**"我每帧要做事吗？"** → 要，进 Behaviour；不要，直接继承 Component。

### 4. 你能在脚本里写 `class MyCom : Component` 吗？编译报错吗？

- **编译能过**（语法合法，C# 允许继承任何非 sealed 类）。
- **AddComponent 时引擎不识别**：`AddComponent<MyCom>()` 拿到的是空壳（缺 native 侧）。
- **运行时拿不到反射签名**：Unity 的序列化 / Inspector 不识别这种组件。

这就是 Unity 用 MonoBehaviour 作为"唯一可挂载入口"的硬约束。

### 5. Rigidbody 不可禁用，那运行时怎么"暂停"它？

三种姿势：

```csharp
// ① 让 PhysX 接管但不算物理对象（最常用，物体不参与物理但仍能被 Transform 移动）
rb.isKinematic = true;

// ② 销毁组件（彻底下线，重启用 rb = gameObject.AddComponent<Rigidbody>();）
Destroy(rb);

// ③ 关闭 Rigidbody2D / Collider2D 的 enabled（2D 走 Behaviour，所以支持 enabled）
rb2d.simulated = false;  // Rigidbody2D 走 Behaviour，所以支持 enabled
col2d.enabled = false;
```

> 注意 ③：**3D Rigidbody 没有 `enabled` 属性**（直接继承 Component，没有走 Behaviour 派发），只有 `isKinematic` 这条"准禁用"路。

### 6. Collider3D 走 Component、Collider2D 走 Behaviour，为什么这么分裂？

历史包袱 + 双物理引擎：3D 用 PhysX（native C++ 系统），2D 用 Box2D（Unity 自己封装的轻量系统）。Unity 没必要为 3D 物理走 BehaviourManager（native 端自己管生命周期），2D 反而需要走标准派发。

### 7. GameObject 是 sealed 类吗？我能继承 GameObject 写子类吗？

**GameObject 是 sealed**，不能继承。Unity 限制你"用 GameObject + 组件组合"而非"继承 GameObject"——这是组合优于继承的设计约束。如果你需要"自己的容器类"，用 MonoBehaviour 挂载空 GameObject，或用 ScriptableObject 做数据容器。

### 8. Object 是引用类型还是值类型？为什么 `==` 重载后跟 null 比较能识别"假空"？

- **引用类型**（class 派生）。
- **`==` 重载**让 C# 编译期 `null` 比较时调用 Unity 重写的 operator，它会查 native 侧对象是否还活着——活着就返回 `false`（"非空"），死了才返回 `true`（"真空"）。
- **fake null 现象**：native 已 Destroy 但 C# 引用还在，`obj == null` 返回 `true`，`obj.Equals(null)` 返回 `false`。

详见 [[UnityEngine.Object 判空与销毁机制]]。

### 9. Coroutine 是 Object 的子类吗？它什么时候被销毁？

- **是 Object 子类**（由 `StartCoroutine` 返回）。
- **生命周期**：协程所在 MonoBehaviour 被 Destroy / Disable 时，Unity 自动终止协程（保险起见仍建议在 `OnDisable` 里 StopAllCoroutines）。
- **yield return null**：下一帧继续；yield return new WaitForSeconds(x)：等待 x 秒。

详见 [[协程原理与unitask]]。

### 10. 在脚本里 `new Component()` 或 `Instantiate(component)` 会发生什么？

- `new Component()` 编译能过，运行时拿到"空壳"（缺 native 侧），**绝大部分 API 调用会抛 MissingReferenceException**。
- `Instantiate(component)` 合法（clone 现有组件连同 native 侧），但实例化的 GameObject 上才有意义；纯 Instantiate 一个 Component 单独拿出来是边缘用法。
- **正解**：`gameObject.AddComponent<T>()` 才是 Unity 推荐路径。

### 11. 静态类（Time/Physics 等）也是 Object 子类，有什么实际意义？

- 类定义继承 Object → 拥有 `name` / `GetInstanceID()` / `==` 重载，但**因为静态类不能实例化**，实际只能用其静态成员。
- 实战意义：**某些静态属性访问走 native**（`Application.isPlaying` / `Time.frameCount`），所以"静态类继承 Object"这个事实平时感受不到，但确实是 Unity 设计的一致性体现。

### 12. `[ExecuteAlways]` / `[ExecuteInEditMode]` 改写了 Behaviour 的什么？

让 MonoBehaviour 在 Editor 非 Play 模式下也能收到 `Update` / `OnEnable` 等回调。本质是 Unity 在 BehaviourManager 派发表里加了一个分支——非 Play 模式也派发到打了标签的类。这跟 Behaviour vs Component 的派发区别正交。

---

## 九、关联阅读

- [[UnityEngine.Object 判空与销毁机制]] —— native 壳与 C# 壳的双层结构、fake null
- [[MonoBehaviour生命周期与SetActive的坑]] —— MonoBehaviour 的生命周期消息全景
- [[Update-FixedUpdate-LateUpdate执行时机]] —— BehaviourManager 派发的三阶段
- [[协程原理与unitask]] —— Coroutine 继承 Object 的设计意义
- [[Unity 序列化机制]] —— MonoBehaviour 的 [SerializeField] 等编辑器属性
- [[UI-逻辑-数据分层与事件驱动]] —— 多组件协作的事件模式（架构层）