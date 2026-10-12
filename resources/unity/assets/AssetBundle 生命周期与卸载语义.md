---
title: "AssetBundle 生命周期与卸载语义"
type: resource
tags: [unity, AssetBundle, 资源管理, 内存, 真实面经]
created: "2026-09-07"
updated: "2026-10-12"
status: active
summary: "AB 包全流程（标记/打包生成 Manifest/依赖/下发/加载/实例化/卸载）+ 打包粒度策略；Unload(false) 只卸包头保留实例、Unload(true) 连资源实例全销毁（场景在用 → 粉红 Missing）；依赖加载先父后子、卸载先子后父；裸 AB 打包-加载最小闭环（Editor 打包脚本 / 产物四件套与 manifest 字段 / LoadFromFile 与资产名语义 / Resources 双份包体坑 / 压缩格式三选）；Addressables 引用计数 = 这套底层的人肉记账自动化"
related:
  - "[[Addressables资源生命周期]]"
  - "[[Resources.Load 加载与 as 转型]]"
---

# AssetBundle 生命周期与卸载语义

来源：CSDN 面经《Unity 面试官视角：10 大高频考点》——AB 卸载两档位是资源管理方向必问。
补充（2026-10-09）：灯皇面试题「讲一下 AB 包」→ 补全流程总览一节，本笔记从"卸载语义专题"升级为"AB 包完整回答"。
补充（2026-10-12）：学习项目 `E:\unityproject\HybirdCLR` 手打裸 AB —— 新增「裸 AB 打包与加载最小闭环」一节，把上表②⑤⑦⑧四步落到可运行代码 + 真实 manifest 字段。

## AB 包整体流程（面试开场版）

被问「讲一下 AssetBundle」时，**先给全流程骨架，再挑一个点深入**，不要一上来就钻细节。

| 阶段 | 做什么 | 关键点 |
| --- | --- | --- |
| ① 标记 | 在 Inspector 底部的 AssetBundle 栏给资源打 `包名/变体名` | 名字决定归属；同包不可重复依赖彼此 |
| ② 打包 | 编辑器脚本调 `BuildPipeline.BuildAssetBundles` | 产出 `.bundle`（含序列化资源）+ **Manifest 清单**（依赖关系、Hash、CRC） |
| ③ 清单 | 读 `AssetBundleManifest` 拿到包间依赖 | **依赖关系在打包期就算好了**，运行时靠它决定加载顺序 |
| ④ 下发 | 上传 CDN + 版本清单（MD5/大小） | 客户端启动比对 → **只下差异包**（热更的资源侧） |
| ⑤ 加载 | `AssetBundle.LoadFromFile`（本地，推荐）/ `LoadFromMemory` / `UnityWebRequestAssetBundle`（远端） | 加载包 ≠ 加载资源，还要 `LoadAsset` |
| ⑥ 依赖加载 | 先用 Manifest 加载依赖包，再加载目标包 | **加载先父后子**；不先加载依赖 → 引用悬空 → 粉红 |
| ⑦ 实例化 | `Instantiate(prefab)` | `LoadAsset` 只是拿到资产对象，实例化才进场景 |
| ⑧ 卸载 | `Unload(false/true)`，按引用计数决定时机 | 见下面两节 |

**打包粒度策略**（几乎必被追问）：

- **按生命周期分**：常驻资源（UI 通用图集）一组、关卡资源一组 —— 粒度策略的核心原则；
- **按引用关系分**：被大量资源引用的**共享资源单独成包**（否则会被重复打进多个包，包体暴涨）；
- **不要一个资源一个包**：包太小则依赖关系爆炸、加载次数过多；
- **不要一个大包**：改一个图要整包重下。

口诀：**"共享的抽出来、同生命周期的放一起"**。

## 裸 AB 打包与加载最小闭环（2026-10-12 实战）

把上面流程表的 ②⑤⑦⑧ 四步落到可运行代码 —— 不手打一遍，就永远看不懂 YooAssets / Addressables 封装掉了什么。

### ① Editor 打包脚本（对应上表②）

```csharp
// 必须放在 Assets/Editor/ 下
using System.IO;
using UnityEditor;
using UnityEngine;

public class CreateAB
{
    [MenuItem("Assets/CreateAB")]
    public static void BuildAllAB()
    {
        string dir = "AssetsBundles";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        BuildPipeline.BuildAssetBundles(dir, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows64);
    }
}
```

四个要点：

| 要点 | 说明 |
| --- | --- |
| 放 `Assets/Editor/` | `Editor` 是**编译约定目录**，脚本只编进 `Assembly-CSharp-Editor`，**打 Player 时整体剔除**；`using UnityEditor` 也只在这里合法（放普通目录 → 构建 Player 报错） |
| 输出目录**不能放 `Assets/` 内** | 否则 ① 被 Unity 当资源导入并生成 `.meta`；② 下一轮打包把上轮的 `.bundle` 再当资源打一遍，**包体滚雪球**；③ 可能自锁成循环依赖 |
| `BuildAssetBundleOptions.None` | = **LZMA 全流压缩**：包体最小，但加载需整体解压 → **加载最慢、解压内存峰值最高** |
| `BuildTarget` 硬编码 | AB **与平台强绑定**，换平台必须重打；工程化应取 `EditorUserBuildSettings.activeBuildTarget` |

### ② 产物四件套与 manifest 字段（对应上表③）

打包输出**两两配对**：

| 文件 | 是什么 |
| --- | --- |
| `main` | **AB 本体**（真正资源包），包名来自资源上指定的 AssetBundle 名 |
| `main.manifest` | 该包的**清单**：CRC / 内含资源列表 / 依赖列表 / 类型树 Hash |
| `AssetsBundles` | **总 Manifest bundle**（与输出目录同名），存**所有包之间的依赖关系** |
| `AssetsBundles.manifest` | 总清单文本形式 = 运行时 `AssetBundleManifest` 的序列化来源 |

`main.manifest` 真实内容（2026-10-12 实测）：

```yaml
ManifestFileVersion: 0
CRC: 3128260053
Hashes:
  AssetFileHash: { Hash: 9938ccf0c6be19970a02ee82fa672e46 }
  TypeTreeHash:  { Hash: bfc629bcbc5d327c442afdb2922bd5c7 }
HashAppended: 0
ClassTypes:
- Class: 1     # GameObject
- Class: 4     # Transform
- Class: 114   # MonoBehaviour ×2
- Class: 115   # Prefab
Assets:
- Assets/Resources/prefab/GameObject.prefab
Dependencies: []
```

- **`CRC`** → 完整性校验：远端下载后比对，**清单 Hash 判断"要不要下"、CRC 判断"下下来对不对"**，这对组合拳是资源热更的标准做法。
- **`Dependencies: []`** → 本包不依赖其他包，所以能直接 `LoadAsset`；**一旦非空，加载顺序必须先父后子**（否则引用悬空 → 粉红）。
- **`ClassTypes`** → 包里出现哪些原生类型（按 Unity 内置 ClassID），反序列化时按 ID 建对象。

### ③ 加载与实例化（对应上表⑤⑦⑧）

```csharp
void Start()
{
    AssetBundle ab = AssetBundle.LoadFromFile("AssetsBundles/main");
    var go = ab.LoadAsset<GameObject>("GameObject");
    Instantiate(go);
    ab.Unload(false);   // ← 补上这一步
}
```

**加载包 ≠ 加载资源 ≠ 实例化**，三步分离是高频考点。三个必踩的坑：

1. **相对路径只在 Editor 成立**：`LoadFromFile` 的相对路径按 `Application.dataPath`（`<项目>/Assets`）解析，而产物在项目根 `AssetsBundles/` → **Editor 能跑、构建 Player 必失败**。正确落点只有 `Application.streamingAssetsPath`（随包）或 `Application.persistentDataPath`（远端下载后落盘）。
2. **`LoadAsset<T>(name)` 传的是资产名不是路径**：资产名 = **资源文件名去扩展名**（`GameObject.prefab` → `"GameObject"`）。**找不到时不抛异常、只返回 null** → 报错点（`Instantiate(null)`）离根因很远，必须自己判空。
3. **`Unload` 时机**：顺序必须是 **先 `Instantiate` 再 `Unload(false)`**。`false` = 卸包头（清单元数据）保实例；`true` = 连内存中的资源实例一起销毁 → 场景里正在用的东西变粉红。

### ④ ⚠️ 双份包体坑：`Resources/` 与 AB 不能同时收同一个资源

上面 manifest 里资源路径是 `Assets/Resources/prefab/GameObject.prefab` —— 这个 prefab **既躺在 `Resources/` 里、又被指定了 AssetBundle 名** → 会被打**两份**：一份进 `resources.assets`（Resources 构建流程），一份进 `main`（AB 构建流程），**包体直接翻倍**。

**纪律**：一个资源要么走 `Resources`、要么走 `AB`，不能两头都收。上线前用打包报告核对资源归属。

### ⑤ 压缩格式三选（面试延伸）

| 选项 | 压缩方式 | 包体 | 加载速度 | 内存峰值 |
| --- | --- | --- | --- | --- |
| `None`（默认） | **LZMA 全流** | 最小 | 最慢（整体解压） | 最高 |
| `ChunkCompression` | **LZ4 128KB 块** | 中 | 快（按块解压） | 低 |
| `UncompressedAssetBundle` | 不压缩（16 字节对齐） | 最大 | 最快 | 最低 |

补充：本地缓存会把 LZMA 包**自动重压成 LZ4** 再存；**WebGL 不支持 LZMA**（无法整体解压）。生产推荐 **LZ4**。

### ⑥ 加载 API 选型

| API | 场景 |
| --- | --- |
| `LoadFromFile` / `LoadFromFileAsync` | 本地文件（`StreamingAssets` / `persistentDataPath`）；Async 版不卡帧、带进度 |
| `LoadFromMemory` / `LoadFromMemoryAsync` | 已拿到 `byte[]`（网络下载后 / 加密包解密后） |
| `LoadFromStream` | 自定义流（加密流、分块读取） |
| `UnityWebRequestAssetBundle` | 远端 URL 直接下（带缓存与 CRC 校验） |

### ⑦ 和 YooAssets / Addressables 的对照

| 环节 | 裸 AB | YooAssets |
| --- | --- | --- |
| 打包 | `BuildPipeline.BuildAssetBundles` + 手动指定 AB 名 | `BundleCollectorSetting` 收集器 + 一键 Build |
| 清单 | `AssetsBundles.manifest` 文本，**自己解析依赖** | `BuiltinCatalog` / 远端 `.bytes`，框架解析 |
| 版本 | **无**（自己造） | `.version` + `.hash` → 自动差异比对 |
| 加载 | `LoadFromFile` 同步、无异步、无进度 | `LoadAssetAsync` + 句柄引用计数 |
| 卸载 | 手动 `Unload(false/true)` | 句柄 `Release`，计数归零才真卸 |

**结论**：框架不是"另一套东西"，就是上表右列对左列的**自动化**。先跑通裸 AB，再看框架才不虚。

## 两个卸载档位

| API | 卸载什么 | 已 LoadAsset 的实例 | 适用场景 |
|---|---|---|---|
| `Unload(false)` | AB 包头/**清单元数据**（目录） | **保留**，照常能用 | 场景还在用资源时 |
| `Unload(true)` | 包头 + **内存中的资源实例一并销毁** | 全灭 → Missing/粉红 | 整块内容确定不用（切场景/卸模块） |

**铁律**："false 保实例、true 全销毁；**用着别 true**"。场景正在显示某 AB 的资源却 Unload(true)（哪怕无依赖问题）= 粉红头号来源。

## 依赖陷阱（粉红材质排查方向）

- **加载顺序**：Bundle B 引用 Bundle A 的材质 → 必须**先 Load(A) 再 Load(B)**，B 才能拿到正确引用；
- **卸载顺序**：**先卸子（B）再卸父（A）**——先 Unload(A) 则 B 的资源还在、引用的 A 资源却没了 → 粉红；
- **使用期误卸**：无依赖问题，但正在用的 AB 被 true 卸载 → 粉红。

口诀："**加载父先、卸载子先、用着别 true**"。

## 与 Addressables 的关系

Addressables 的引用计数（Load/Release 成对）本质就是**包装这套 AB 卸载逻辑**：Release 让计数 -1，**计数归零才真正触发底层 AB 卸载**——省掉手动 AB 时代"谁该卸、什么时候卸"的人肉记账。但理解底层 Unload(false/true) 语义仍是排查 Addressables 泄漏/粉红问题的基础。

## 面试答题框架

① **先给全流程骨架**（标记 → 打包生成 Manifest → 依赖 → 下发 → 加载 → 依赖加载 → 实例化 → 卸载），并点出打包粒度两原则「共享的抽出来、同生命周期的放一起」→ ② 讲两档位语义与"为什么 false 后资源还能用"（实例还活着）→ ③ 展开依赖：加载先父后子、卸载先子后父 → ④ 粉红材质排查三方向（依赖顺序 / 使用期误卸 / 依赖缺失加载）→ ⑤ 可衔接 Addressables 引用计数（加分）。
