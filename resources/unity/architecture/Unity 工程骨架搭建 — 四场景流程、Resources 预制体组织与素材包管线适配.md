---
title: "Unity 工程骨架搭建 — 四场景流程、Resources 预制体组织与素材包管线适配"
type: resource
tags: [unity, 项目架构, 资源管理, 项目实战]
created: "2026-10-27"
updated: "2026-10-27"
status: active
summary: "新项目从零搭骨架的四件事：四场景流程（Init/Login/Main/Fighting）职责与 Build Settings 顺序、Resources 目录按类型分层的加载与常驻代价、环境整包烘成 Scene.prefab 的收益与三个坑、BIRP 项目导入 URP 素材的洋红成因与 InputSystem 切换；附搭建检查清单"
---

# Unity 工程骨架搭建 — 四场景流程、Resources 预制体组织与素材包管线适配

> 实证来源：2026-10-27 `E:\unityproject\Empty`（Unity 2022.3.62f3c1 / Built-in / InputSystem 1.14.0）与 `WildernessElite` 的工程初始化。当天全域 **0 个 .cs 改动**，动的全是「工程结构」——场景、预制体、目录、烘焙设置。

## 一、四场景流程骨架

```
Init  →  Login  →  Main  →  Fighting
引导/热更   登录     大厅/选人    战斗
```

**职责划分**

| 场景 | 职责 | 关键点 |
| --- | --- | --- |
| `Init` | 配置表、音频/输入初始化、热更检查、版本比对 | **必须是 Build Settings 的 index 0**；这里做的耗时决定冷启动体验 |
| `Login` | 账号、协议、区服列表 | 通常只有 UI，可以做成叠加加载（additive） |
| `Main` | 大厅、角色展示、匹配入口 | 常驻内容多，适合放「预加载池」 |
| `Fighting` | 战斗 | 会反复进出，**切场景会丢全部运行时状态** |

**Build Settings 顺序是硬约束**：`File → Build Settings → Scenes In Build`，列表第一项就是启动场景。工程搭好但没把 `Init` 加进去，按 Play/打包跑的仍是素材包自带的 demo 场景——这是最容易「看起来做了但没生效」的一步。

**切场景代码骨架**

```cs
// 异步切场景 + 进度条
using UnityEngine.SceneManagement;

IEnumerator LoadFightingAsync()
{
    var op = SceneManager.LoadSceneAsync("Fighting", LoadSceneMode.Single);
    op.allowSceneActivation = false;          // 先卡住，等进度条走完
    while (op.progress < 0.9f)                // 0.9 就是「加载完成但未激活」的上限
    {
        slider.value = op.progress / 0.9f;
        yield return null;
    }
    slider.value = 1f;
    op.allowSceneActivation = true;           // 放行
}
```

**跨场景必须存活的东西要显式声明**：`DontDestroyOnLoad(gameObject)`。否则「常驻管理器」在 `Main → Fighting` 时被销毁，单例变 null（这正是伪单例在切场景后失效的根因之一）。

**四场景 vs 单场景 + UI 栈**：四场景的好处是「每个场景的层级干净、光照/后处理可以各配一套」；代价是「状态全丢 + 加载黑屏」。FPS 这类要反复进出的战斗场景，常见折中是「`Main` 与 `Fighting` 合成一个场景，用 UI 栈 + 状态机切界面」。

---

## 二、Resources 目录怎么组织

今日实证的布局：

```
Assets/Resources/
├── Prefab/
│   ├── Scene.prefab          ← 整个环境烘成一个预制体（4.0 MB，5 个 GameObject）
│   ├── Role/SK_TP_CH_Default.prefab   ← 人物，带 1 个 Animator，骨骼层级完整
│   ├── Effect/               ← 空
│   └── Weapon/               ← 空
└── Animators/
    ├── PlayerAnimator.controller
    └── HandGunAnimator.controller
```

**三种组织法的取舍**

| 组织法 | 示例 | 优点 | 缺点 |
| --- | --- | --- | --- |
| **按对象类型** | `Prefab/{Role, Effect, Weapon}` | 运行时按类别加载，路径可拼（`$"Role/{id}"`） | 一个关卡要加载的散件多，加载次数多 |
| **按关卡/模块** | `Prefab/Level_01/{...}` | 一关卡一目录，卸载边界清楚 | 跨关卡复用资产会重复 |
| **混合** | 公共按类型 + 关卡按模块 | 兼顾复用与卸载 | 目录纪律要求高 |

**加载 API 与路径语义**

```cs
// 路径不含 "Resources/" 前缀，不含扩展名
var rolePrefab = Resources.Load<GameObject>("Prefab/Role/SK_TP_CH_Default");
var go = Instantiate(rolePrefab);

// Animator Controller 同样走 Resources
var ctrl = Resources.Load<RuntimeAnimatorController>("Animators/PlayerAnimator");
animator.runtimeAnimatorController = ctrl;
```

**Resources 的硬代价（必须在骨架阶段就想清楚）**

1. **全量进包**：`Resources/` 下所有资产在构建时被打进 `resources.assets` 并写进包体清单，**不管用没用到**，包体直接涨。
2. **无法按需卸载**：只有 `Resources.UnloadAsset(obj)`（针对单个非 GameObject 资产）和 `Resources.UnloadUnusedAssets()`（全量扫描，代价大）两条路，**没有「卸载这一关卡的资源」这种粒度**。
3. **启动即索引**：启动时要建立整个 Resources 索引表，目录越大冷启动越慢。
4. **不适合热更**：Resources 里的东西改不了（不在 AB 里），这是热更项目最终都会迁到 Addressables / YooAssets 的根本原因 → 📎 [[YooAssets 热更新流程 — HybridCLR 实战]]、📎 [[AssetBundle 生命周期与卸载语义]]

**结论**：骨架期用 Resources 换开发速度可以，但要**在目录设计上预留迁移**——按类型分的目录结构，迁到 Addressables 时正好对应「一个 Label 一组」，改动最小。

---

## 三、把整个环境烘成 `Scene.prefab`

今日的 `Scene.prefab` 是「场景转预制体」：根 `Scene` + 三块地形碎片 `SM_Env_Planecrash_Debris_01 (1)(2)(3)` + `Water`，5 个 GameObject，4MB。

**收益**

- **关卡变成可实例化资产**：`Instantiate(scenePrefab)` 就能换关，不用 `LoadSceneAsync`，切换无黑屏、无状态丢失。
- **可复用**：同一套环境可以在一个场景里摆多份（做镜像关、试炼场）。
- **可程序化拼装**：关卡 = 环境 prefab + 刷怪点 + 触发器，做成数据驱动。

**三个坑**

1. **光照贴图会断**：预制体里只保存 `Renderer.lightmapIndex` / `lightmapScaleOffset` 这类**索引**，真正的 lightmap 贴图属于场景的 Lighting Data Asset。预制体被搬到别的场景（或同场景摆第二份）时，索引指向的贴图对不上 → 要么花掉，要么整片错位。跨场景复用必须重新烘焙或走 lightmap 重映射流程。
2. **无法局部卸载**：4MB 单资产，加载时整包进内存；想「只卸掉水面」做不到，只能整个 prefab 卸。
3. **静态合批与遮挡剔除退化**：预制体里的静态物体在运行时合并/剔除的收益不如「场景内直接摆放」——静态合批在构建期完成，运行时 Instantiate 的副本拿不到（除非勾了 `Static` 且走烘焙流程）。

**什么时候该用「场景」而不是「预制体」**：需要光照烘焙 + 遮挡剔除 + 静态合批的大关卡 → 用 Scene（配合 Addressables 的 Scene 加载，可带依赖与卸载）；小模块 / 室内房间 / 程序化拼装件 → 用 prefab。

---

## 四、素材包与渲染管线适配（BIRP 项目导入 URP 素材）

今日 `Empty` 是 **Built-in 管线**（manifest 无 URP/HDRP，只有 postprocessing），但美术包 `PolygonBattleRoyale` 里带了一个 `URP_Extract_Me.unitypackage`——说明这批资源是 URP 版。

**洋红（Magenta）的成因**：材质引用的 Shader 在当前管线下不可用（`Shader.Find` 返回 null / Shader 与管线不兼容），Unity 用内置的 error shader 兜底渲染 → 整片洋红。**不是贴图丢了，是 Shader 没找到**。

**两条出路**

| 路线 | 做法 | 代价 |
| --- | --- | --- |
| **转 URP** | 装 `com.unity.render-pipelines.universal` → 创建 URP Asset → `Graphics`/`Quality` 里切管线 → 跑 URP 升级器（`Window → Rendering → Render Pipeline Converter`） | 项目所有自定义 Shader 要重写（CG → HLSL + URP 的 `UniversalForward` Pass）；后处理栈从 PostProcessing v2 换成 Volume |
| **留 BIRP** | 解包 URP 资源后，把材质 Shader 批量换成 `Standard`，重新指认贴图 | URP 材质的贴图槽名不同（`_BaseMap` vs `_MainTex`），要写批量替换脚本；失去 URP 的 SRP Batcher 收益 |

⚠️ 关键判断：**先定管线，再导素材**。管线在中途换，等于所有材质/Shader 重做一遍。

---

## 五、InputSystem 与 Active Input Handling

装了 `com.unity.inputsystem` **不等于**输入就能用：

1. `Project Settings → Player → Active Input Handling` 必须设为 `Input System Package (New)` 或 `Both`。留 `Old` 时新 API 直接不工作；设 `New` 时 `Input.GetAxis` 会抛异常。
2. **UI 事件会静默失效**：场景里 `EventSystem` 上的 `StandaloneInputModule` 属于旧输入系统，切到 New 之后必须换成 `InputSystemUIInputModule`，否则按钮点不动、拖拽全废 → 📎 [[UGUI 事件系统与射线检测链路]]
3. 运行时切模式需要重启 Editor（会弹窗提示），这是正常行为。

---

## 六、新项目搭建检查清单

- [ ] 场景流程定名并**加进 Build Settings**，`Init` 在 index 0，删掉素材包自带的 demo 场景
- [ ] 定渲染管线（BIRP / URP / HDRP）——**在导入美术素材之前**
- [ ] 素材包自检：有没有 `*_Extract_Me.unitypackage`、`Readme`、`How to Access Documentation.pdf` 这类「需要额外步骤」的提示
- [ ] `Active Input Handling` + `EventSystem` 的 InputModule 对齐
- [ ] 目录骨架：`Scripts/`、`Resources/`（或 Addressables 组）、`Scenes/`、`Settings/`、`Editor/`
- [ ] 程序集划分：是否需要 asmdef（热更项目必备，AOT 与 HotUpdate 必须分开）→ 📎 [[热更新 HybridCLR — AOT 泛型元数据补全]]
- [ ] 常驻管理器清单：哪些要 `DontDestroyOnLoad`，哪些随场景销毁
- [ ] 版本管理：`.gitignore` 排除 `Library/`、`Temp/`、`Logs/`、`obj/`；`ProjectSettings/` 要提交
- [ ] 首帧跑通：空场景能进、能切到 `Fighting`、UI 能点

---

## 相关

- 📎 [[Resources.Load 加载与 as 转型]] — Resources 加载的 API 语义与判空
- 📎 [[AssetBundle 生命周期与卸载语义]] — 为什么最终要离开 Resources
- 📎 [[Addressables资源生命周期]] — 引用计数式的加载/卸载边界
- 📎 [[YooAssets 热更新流程 — HybridCLR 实战]] — 热更项目的资源分层实践
- 📎 [[UGUI Canvas 渲染模式与屏幕适配]] — 场景切好后 UI 层的适配
- 📎 [[UGUI 事件系统与射线检测链路]] — InputSystem 切换后 UI 点不动的原因
- 📎 [[Unity Input System 使用指南]] — 新输入系统的配置与读取
- 📎 [[Animator参数与性能优化]] — 手臂/身体双 Animator 的参数与开销
- 📎 [[Unity 序列化机制]] — 预制体与场景的序列化边界
