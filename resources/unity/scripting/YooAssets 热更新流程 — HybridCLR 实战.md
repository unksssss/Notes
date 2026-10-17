---
title: "YooAssets 热更新流程 — HybridCLR 实战"
type: resource
tags: [unity, 热更新, HybridCLR, YooAssets, AssetBundle, 学习项目]
created: "2026-10-09"
updated: "2026-10-17"
status: active
summary: "实战项目 E:\\unityproject\\HybirdCLR 的完整热更链路：YooAssets.Initialize → CreatePackage → 自定义 IRemoteService + HostPlayModeOptions 文件系统 → RequestPackageVersion → LoadPackageManifest → ResourceDownloader 差异下载 → LoadAssetAsync<TextAsset>(dll.bytes) → Assembly.Load → 反射调用热更类；附 YooAsset 3.0.6 官方示例（YooAssets_HybirdCLR）首次打包实证与打包配置项语义、补丁 FSM 八节点及各节点实现细节、HostPlayMode 首次跑通的两个独立坑（BundledCopyOption 默认 None 缺 hash/BuiltinCatalog、打包后未同步 CDN 导致版本错位）与「hash → bytes → version 对齐」三层排查法"
related:
  - "[[热更新 HybridCLR — AOT 泛型元数据补全]]"
  - "[[AssetBundle 生命周期与卸载语义]]"
  - "[[Addressables资源生命周期]]"
---

# YooAssets 热更新流程 — HybridCLR 实战

对应学习项目：**`E:\unityproject\HybirdCLR`**（2026-10 起的学习主线，替代已下线的消防项目）。

> **两条腿**：`YooAssets` 管**资源**（AB 的打包/更新/下载/加载），`HybridCLR` 管**代码**（热更 DLL 的解释执行）。这套组合是当前国产 Unity 商业项目最主流的热更方案。

## 一、项目结构（先建立全局观）

```
E:\unityproject\HybirdCLR\
├── Assets\
│   ├── AOT\                         ← 主包程序集（Assembly-CSharp）
│   │   ├── LoadDllScripts.cs        ← 热更总入口
│   │   └── RemoteService.cs         ← 自定义 IRemoteService
│   ├── HotUpdate\                   ← 热更程序集（编译成 HotUpdate.dll）
│   │   └── Hello.cs                 ← 会被热更的业务逻辑
│   ├── HybridCLRGenerate\
│   │   └── AOTGenericReferences.cs  ← HybridCLR 生成的 AOT 泛型引用清单
│   ├── HotUpdateDlls\
│   │   └── HotUpdate.dll.bytes      ← 热更 DLL（改名 .bytes 以便当 TextAsset）
│   ├── StreamingAssets\yoo\         ← YooAssets 内置清单（随包发布）
│   └── BundleCollectorSetting.asset ← YooAssets 资源收集器配置
├── Bundles\StandaloneWindows64\     ← 打包产物
└── HybridCLRData\                   ← HybridCLR 工作数据（含补全 DLL）
```

**为什么分 `AOT` / `HotUpdate` 两个目录**：这是全部热更方案的地基 —— 主包只 AOT 编译引擎 + 少量启动代码，业务逻辑全进 `HotUpdate.dll`，运行时动态加载。详见 📎 [[热更新 HybridCLR — AOT 泛型元数据补全]]

## 二、完整热更链路（`LoadDllScripts.cs` 逐段拆解）

### 第 0 步：关键配置字段

```csharp
private string packName        = "HotUpdateDlls";                              // 资源包名
private string dllFullPath     = "Assets/HotUpdateDlls/HotUpdate.dll.bytes";   // DLL 的可寻址地址
private string defaultHostServer = "http://127.0.0.1:8080/CDN/PC/";            // 远端 CDN 根
private string defaultVersion    = "v1.0";                                     // 版本号一级目录
```

⚠️ **`dllFullPath` 必须是「可寻址地址（location）」** —— YooAssets 里加载资源用的是**收集器里配的地址**，不是磁盘物理路径。DLL 放在 `Assets/HotUpdateDlls/` 下并被 Bundle Collector 收集，才能用这个路径 `LoadAssetAsync`。

### 第 1 步：初始化资源系统 + 创建资源包

```csharp
YooAssets.Initialize();                                     // 全局只初始化一次
var package = YooAssets.TryGetPackage(packName, out var pkg)
    ? pkg
    : YooAssets.CreatePackage(packName);                     // 幂等获取/创建
```

**要点**：YooAssets 是**多包（multi-package）**设计 —— 一个游戏可以按模块拆成多个 Package（如 `Base` / `HotUpdateDlls` / `Level1`），各自独立更新、独立版本。这里 `TryGetPackage` 先尝试复用，避免重复创建。

### 第 2 步：运行模式与文件系统（本次用的 HostPlayMode）

```csharp
var remoteServer = new RemoteService(defaultHostServer + defaultVersion);
var creatParameters = new HostPlayModeOptions();
// 内置文件系统：随包资源（首次启动的兜底）
creatParameters.BuiltinFileSystemParameters =
    FileSystemParameters.CreateDefaultBuiltinFileSystemParameters();
creatParameters.BuiltinFileSystemParameters.AddParameter(
    EFileSystemParameter.CopyBuiltinPackageManifest, true);   // 把内置清单拷到沙盒
// 缓存文件系统：远端下载 + 本地缓存
creatParameters.CacheFileSystemParameters =
    FileSystemParameters.CreateDefaultSandboxFileSystemParameters(remoteServer);
creatParameters.CacheFileSystemParameters.AddParameter(EFileSystemParameter.DownloadMaxConcurrency, 5);
creatParameters.CacheFileSystemParameters.AddParameter(EFileSystemParameter.DownloadMaxRequestPerFrame, 1);
creatParameters.CacheFileSystemParameters.AddParameter(EFileSystemParameter.DownloadWatchdogTimeout, 10);
```

**三档运行模式对比**（YooAssets 的核心选型）：

| 模式 | 资源来源 | 用途 |
| --- | --- | --- |
| `EditorSimulateMode` | 直接读 AssetDatabase | **编辑器开发**，免打包，改完即见 |
| `OfflinePlayMode` | 只读 StreamingAssets 内置资源 | 单机包，**无网络更新** |
| `HostPlayMode` | 内置兜底 + **远端 CDN** + 沙盒缓存 | **联机热更**（本项目用的就是它） |

**三个下载参数的含义**：

| 参数 | 值 | 含义 |
| --- | --- | --- |
| `DownloadMaxConcurrency` | 5 | 同时下载的文件数上限（并发太高会打爆 CDN / 弱网更慢） |
| `DownloadMaxRequestPerFrame` | 1 | 每帧最多发起几个请求（**防主线程卡顿**的关键参数） |
| `DownloadWatchdogTimeout` | 10 | 单次下载看门狗超时（秒），超时判失败可重试 |

> 💡 `DownloadMaxRequestPerFrame` 是最容易被忽略、又最能决定"下载时不掉帧"的参数。

### 第 3 步：请求版本号

```csharp
var versionOp = package.RequestPackageVersionAsync();
yield return versionOp;   // 协程等异步 Operation
```

YooAssets 的异步 API 全部返回 **Operation 对象**（可 `yield return`，也可 await 或传回调），统一的 `Status` / `Error` / `Progress` 三件套。

**版本号的来源**：远端 `CDN/PC/v1.0/` 目录下的版本文件（如 `PackageVersion_HotUpdateDlls.version`）。它告诉客户端"最新清单是哪个"。

### 第 4 步：加载远端清单

```csharp
var options  = new LoadPackageManifestOptions(versionOp.PackageVersion, 60); // 60 = 超时秒数
var manifest = package.LoadPackageManifestAsync(options);
yield return manifest;
```

**清单（Manifest）里有什么**：每个资源的 **Hash / CRC / 大小 / 依赖关系 / Bundle 归属**。第 5 步的差异比对全靠它。

### 第 5 步：差异下载

```csharp
var downloadOptions = new ResourceDownloaderOptions(10, 3);   // 重试 10 次 / 每次间隔 3 秒
var downloadLoader  = package.CreateResourceDownloader(downloadOptions);

if (downloadLoader.TotalDownloadCount == 0)
{
    Debug.Log("资源包已经是最新版本");     // 清单无差异，直接跳过
}
else
{
    downloadLoader.StartDownload();
    yield return downloadLoader;           // 等整个下载器完成
}
```

**这就是热更的核心收益**：客户端不必知道"哪些文件改了"—— 它拿本地清单和远端清单做 **Hash 比对**，`TotalDownloadCount` 就是差异文件数，**只下差异部分**。

> 📌 这与 HybridCLR 的代码热更形成两条平行链路：**资源差异**走这里，**代码差异**走 `HotUpdate.dll` 这一个文件（改了逻辑只需重下一份 DLL）。

### 第 6 步：加载 DLL

```csharp
var dllHandle = package.LoadAssetAsync<TextAsset>(dllFullPath);
yield return dllHandle;
TextAsset dllText = dllHandle.AssetObject as TextAsset;
```

⚠️ **为什么 DLL 要改名 `.bytes`**：Unity 只把**已知文本/二进制扩展名**的资源当 `TextAsset` 处理，`.dll` 会被 Unity 当成托管程序集特殊对待（不参与 AB 收集、或加载方式不同）。改名 `.bytes` 后它就是普通二进制文本资源，可以正常打进 AB、正常 `LoadAssetAsync<TextAsset>` 读出 `bytes` 数组。

### 第 7 步：动态加载程序集 + 反射调用

```csharp
Assembly hotupdateAss = Assembly.Load(dllText.bytes);   // 关键：把字节流变成程序集
Type helloType = hotupdateAss.GetType("Hello");         // 拿到热更代码里的类
MethodInfo helloMethod = helloType.GetMethod("Run");    // 拿到方法
helloMethod.Invoke(null, null);                        // 静态方法，参数为空
```

- `Assembly.Load(byte[])` 是 **HybridCLR 接管解释执行的入口** —— 标准 .NET 里这行在 IL2CPP 下会失败（AOT 不认运行时加载的程序集），HybridCLR 把它变成了可用；
- `GetType("Hello")` 里的名字**必须是全名**（含命名空间），本项目 `Hello` 类在全局命名空间所以直接写 `"Hello"`；
- ⚠️ 反射调用**没有编译检查**：类名/方法名写错要到运行时才炸，务必加判空与 try/catch。

### 第 8 步：释放句柄

```csharp
dllHandle.Release();    // 句柄用完即释放，引用计数归零才会真正卸包
```

📎 引用计数语义见 [[Addressables资源生命周期]]（YooAssets 与 Addressables 思路一致）。

## 三、自定义远程服务 `RemoteService`

```csharp
public class RemoteService : IRemoteService
{
    private string _defaultUrl;
    public IReadOnlyList<string> GetRemoteUrls(string fileName)
    {
        List<string> result = new List<string>();
        result.Add($"{_defaultUrl}/{fileName}");
        return result;
    }
    public RemoteService(string defaultUrl) { _defaultUrl = defaultUrl; }
}
```

**作用**：告诉 YooAssets「某个文件该去哪个 URL 下」。返回的是**URL 列表**而不是单个 URL —— 这是为**多 CDN 容灾**预留的：可以返回主站 + 备站，主站挂了自动切。

**扩展点**：真实项目通常在这里做 —— 多 CDN 轮询、按平台/渠道拼不同路径、加签名的临时 URL、灰度分流。

## 四、当前项目状态与待办

- `AOTGenericReferences.cs` **目前是空的**（`PatchedAOTAssemblyList` 与泛型类型列表都没有内容）→ 说明还没执行过 HybridCLR 的「生成 AOT 泛型引用」菜单，或热更代码里暂时没有用到主包没见过的泛型。**一旦热更代码 new 了新泛型，必须先跑生成并让 DLL 随包携带**，否则运行时崩溃。详见 📎 [[热更新 HybridCLR — AOT 泛型元数据补全]]
- `Bridge` 代码目前是「加载 → 反射调用静态方法」的最小闭环，下一步通常会演进为：加载完成后调用 `Hello` 的入口方法去启动整个热更业务（而不是散落反射）。

## 五、2026-10-11 实战推进：从「加载即调用」到「常驻驱动」+ 首次打包

### 5.1 职责拆分：加载归 Bridge，调用归常驻 MonoBehaviour

`LoadDllScripts.cs` 里那段反射调用被**注释掉、移交给 `GameManager`**：

```diff
  HotUpdateDll = Assembly.Load(dllText.bytes);
- Type helloType = hotupdateAss.GetType("Hello");
- MethodInfo helloMethod = helloType.GetMethod("Run");
- helloMethod.Invoke(null, null);
  dllHandle.Release();
+ yield return new WaitForSeconds(3);
+ SceneManager.LoadScene("GameScene");
```

```csharp
public class GameManager : MonoBehaviour
{
    void Update()
    {
        Type helloType = LoadDllScripts.Instance.HotUpdateDll.GetType("Hello");
        MethodInfo helloMethod = helloType.GetMethod("Run");
        helloMethod.Invoke(null, null);
    }
}
```

**拆分的理由**：`LoadDllScripts` 收敛为「**把 DLL 加载进内存**」（加载完即 `Release()` 释放资源句柄、切场景），「**调用热更代码**」是持续行为，交给常驻对象。这样 Assembly 的持有生命周期与业务驱动解耦。

配套两点：

- `LoadDllScripts.Start()` 里 `Instance = this; DontDestroyOnLoad(gameObject);` —— **必须跨场景常驻**，否则 `LoadScene` 一执行，Assembly 的持有者就随场景卸载没了；
- `public Assembly HotUpdateDll { get; private set; }` —— 把 Assembly 暴露给 AOT 侧其它脚本（这是 AOT ↔ 热更的唯一"接口面"）。

### 5.2 每帧反射是热路径反模式

`Update` 里每帧 `GetType` + `GetMethod` + `Invoke` = **每帧 3 次反射**（`Invoke` 含装箱 + 参数数组 + 反射栈帧，`GetMethod` 还要字符串匹配）。优化按性价比排序：

| 档位 | 做法 | 说明 |
| --- | --- | --- |
| ① 缓存元数据 | `Start()` 里把 `Type` / `MethodInfo` 存字段 | 改动最小，干掉 2/3 反射 |
| ② 转委托 | `MethodInfo.CreateDelegate<Action>()` | 调用开销接近直接调用（**推荐**） |
| ③ 改架构 | 热更 DLL 暴露统一入口（如 `IHotUpdateEntry.Start()`），AOT 侧只调一次 | 从根上避免"每帧过反射" |

📎 反射 / IL / 委托的底层开销见 [[C# IL 中间语言与编译执行模型]]

### 5.3 热更侧同样别做「每帧查找」

```csharp
// 反面写法：每帧两次 GameObject.Find（全场景层级遍历 O(n)）
public static void Run() {
    var g1 = GameObject.Find("Square (1)");
    g1.transform.Rotate(0, 0.1f, 0.1f);
}
```

正确姿势是热更入口初始化时缓存引用，`Run` 里只做 `Rotate`。**「每帧做查找」一律改成「一次查找 + 缓存引用」** —— 这条在 AOT 侧（反射）和热更侧（Find）是同一条原则。

### 5.4 打包产物分层（包版本 v1.1.5）

```
Bundles/StandaloneWindows64/HotUpdateDlls/v1.1.5/     ← 要上传 CDN 的那份
├── 97e2d83ce3cf8abe85e5c929a3916f85.bundle          ← 实际资源包（Hash 命名）
├── HotUpdateDlls_v1.1.5.bytes                       ← 远端清单
├── HotUpdateDlls_v1.1.5.hash                        ← 清单 Hash（比对是否需要更新）
├── HotUpdateDlls.version                            ← 版本文件（指向最新清单名）
├── HotUpdateDlls_v1.1.5.report                      ← 打包报告（查资源归属）
└── link.xml                                         ← 裁剪保护

Assets/StreamingAssets/yoo/HotUpdateDlls/             ← 随包发布的内置清单（兜底）
├── BuiltinCatalog.json / .bytes                     ← 对应 CopyBuiltinPackageManifest 参数
├── HotUpdateDlls.version
└── HotUpdateDlls_v1.1.5.bytes / .hash
```

- `OutputCache/`（`buildInfo.log` / `buildlogtep.json`）是**打包中间缓存，不进包**；
- **两层版本概念别混淆**：代码里 `defaultVersion = "v1.0"` 是**远端 URL 的一级目录**（`http://127.0.0.1:8080/CDN/PC/v1.0/`），构建产出的 `v1.1.5` 是 **YooAssets 包版本**。CDN 目录按哪个组织必须和 `RemoteService` 拼出的 URL 对齐，否则 `RequestPackageVersionAsync` 取不到版本文件 —— 这是热更"首跑失败"的高频原因。

## 六、面试答题框架

① 一句话定性：**YooAssets 管资源、HybridCLR 管代码，两条链路各自做差异更新** → ② 资源侧八步：Initialize → CreatePackage → 选运行模式（Editor/Offline/Host）+ 文件系统 → 请求版本 → 加载清单 → 差异下载（`TotalDownloadCount` 即差异数）→ 加载 → Release → ③ 代码侧：DLL 改名 `.bytes` 打进 AB → `LoadAssetAsync<TextAsset>` → `Assembly.Load(bytes)` → 反射调用 → ④ 补上 AOT 泛型元数据这个最大的坑。

## 七、2026-10-13 实战推进：YooAsset 3.0.6 官方示例首次打包 + 打包配置项语义

**第二个学习项目**：`E:\unityproject\YooAssets_HybirdCLR`（导入 YooAsset 3.0.6 官方 **Space Shooter** 示例），10-13 16:04 跑通**首次 YooAssets 打包**，包版本 `2026-10-13-962`。示例的 `PatchLogic/` 就是上面八步链路的**官方参考实现**，且比手写版本更完整。

### 7.1 打包产物三份，用途各不相同

```
Bundles/StandaloneWindows64/DefaultPackage/
├── 2026-10-13-962/     ← 上传 CDN 的那份（22 个 .bundle + 清单四件套）
├── OutputCache/        ← 打包中间缓存（buildInfo.log / buildlogtep.json / link.xml），不进包
└── Simulate/           ← EditorSimulateMode 用的虚拟清单（DefaultPackage_Simulate.bytes）

Assets/StreamingAssets/yoo/DefaultPackage/   ← 随包发布的内置清单（兜底）
├── DefaultPackage.version = 2026-10-13-962
├── DefaultPackage_2026-10-13-962.bytes / .hash / .report
└── link.xml
```

### 7.2 `.report` = 一份可审计的构建快照（打包配置项语义）

`DefaultPackage_<版本>.report` 的 `Summary` 段会把**收集器全部配置原样落盘**，是回答「这个配置项决定什么」最硬的证据来源：

| 字段 | 值 | 语义 |
| --- | --- | --- |
| `BuildPipeline` | `ScriptableBuildPipeline` | 走 SBP：支持增量/缓存，可拿构建耗时 trace |
| `BuildBundleType` | 2 | `RawAssetBundle`（真实 AB，区别于虚拟包） |
| `CompressOption` | 2 | **LZ4 分块压缩**（128KB 块）—— 兼顾包体与加载速度，热更资源默认优选 |
| `EnableAddressable` | true | 资源可用**可寻址地址**（如 `music_background`）加载，不必写全路径 |
| `SupportExtensionless` | true | 地址可省略扩展名 |
| `UniqueBundleName` | false | 包名不加包前缀 → **多包工程遇同名资源会冲突，须改 true** |
| `EnableSharePackRule` | true | **共享资源（被 ≥2 处引用）自动抽成独立包** |
| `SingleReferencedPackAlone` | true | **仅被单个资源引用的资源单独成包**（避免连带下载） |
| `FileNameStyle` | 0 | Bundle 文件名用 **Hash 命名**（`ae0ced….bundle`），天然支持 CDN 强缓存 |
| `AutoCollectShaders` | true | 自动收集 Shader 到 `unityshaders.bundle`（由 `BuiltinShadersBundleName` 指定） |
| `WriteLinkXML` | true | 输出 `link.xml` 裁剪保护清单 |
| `ClearBuildCacheFiles` / `UseAssetDependencyDB` | false | 未清缓存 / 未启用依赖数据库 |
| `EncryptionServicesClassName` | `EncryptionNone` | 未加密（加密包数 0） |

**规模**：`AssetFileTotalCount: 95` / `MainAssetTotalCount: 25` / `AllBundleTotalCount: 43` / `AllBundleTotalSize: 7 555 909 B`（≈7.2 MB）。

**`IndependentAssets` 段**（单引用独立存放）：`vehicle_enemyShip_red_dff.tif`（Texture2D, 721 KB）、`UIFont/LICENSE.txt`（TextAsset, 11 KB）—— 印证 `SingleReferencedPackAlone` 的作用。

**`BundleInfos` 段**：43 个包，每包含 `FileHash` / `FileCrc` / `FileSize` / `DependBundles` / `ReferenceBundles` / `BundleContents`。**包名 = 资源物理路径转下划线**（`assets_samples_yooasset_3_0_6_space_shooter_gameres_…`），只有 `unityshaders.bundle` 是引擎自动收集的特殊包（`ReferenceBundles` 被多个特效包引用 = 共享包典型）。

### 7.3 两条「正常但有用」的打包警告

```
[WARN] [ErrorCode201] Found unreferenced asset and removed it: '…vehicle_enemyShip_red_dff.tif'
[WARN] [ErrorCode601] Found stray bundle. Bundle ID: 42, bundle name: 'unityshaders.bundle'
```

- **ErrorCode201**：资源进了清单但**无人引用** → 打包器主动剔除（与 `IndependentAssets` 是同一批文件，说明「独立成包」和「未被引用」是两件事）；
- **ErrorCode601**：`unityshaders.bundle` 由引擎自动生成、**不在收集器配置里** → 报 stray 属正常，不必修。

### 7.4 构建耗时拆解（`buildlogtep.json`，Chrome Trace 格式）

| 阶段 | 耗时 |
| --- | --- |
| `BuildPlayerScripts` | **8.79 s**（占大头 = 脚本编译/程序集重编译） |
| `WriteSerializedFiles` | 3.59 s |
| `Process Entries` | 3.52 s |
| `Writing SceneBundleWriteOperation` | 1.91 s |
| `CalculateAssetDependencyData` | 1.70 s |
| `ArchiveAndCompressBundles` | 1.20 s |

`buildInfo.log` 里 `TaskBuilding` 共 **15 s**、`BuildSeconds: 16` → **首次打包的瓶颈是脚本编译，不是资源归档压缩**。`buildlogtep.json` 可直接拖进 `chrome://tracing` 看构建瀑布图。

### 7.5 补丁流程 FSM 八节点（官方实现）

```
FsmInitializePackage → FsmRequestPackageVersion → FsmUpdatePackageManifest
→ FsmCreateDownloader → FsmDownloadPackageFiles → FsmDownloadPackageOver
→ FsmClearCacheBundle → FsmStartGame
```

`PatchManager` = **静态状态机 + 事件驱动**：`Create(packageName, playMode)` 建节点 → `Start()` 从首节点跑 → `Update()` 由 `Boot.Update()` 每帧驱动；下载确认/重试这类「等用户、等结果」的环节靠 `UserTryXxxEvent` 事件在 `OnHandleEventMessage` 里 `ChangeState` 切回状态机，**而不是阻塞死等**。

| 节点 | 关键 API / 参数 | 要点 |
| --- | --- | --- |
| `FsmInitializePackage` | `YooAssets.TryGetPackage` / `CreatePackage` → `package.InitializePackageAsync(options)` | 按 `EPlayMode` 分四套 Options |
| `FsmRequestPackageVersion` | `RequestPackageVersionAsync()` → `operation.PackageVersion` | 版本号写回黑板给下一步用 |
| `FsmUpdatePackageManifest` | `new LoadPackageManifestOptions(version, 60)` + `LoadPackageManifestAsync` | 第二参数 = 超时秒数 |
| `FsmCreateDownloader` | `new ResourceDownloaderOptions(10, 3)` + `CreateResourceDownloader` | `TotalDownloadCount == 0` → **直接跳 `FsmStartGame`** |
| `FsmDownloadPackageFiles` | 下载器进度/完成事件 | 由 `UserBeginDownloadWebFilesEvent` 进入 |
| `FsmDownloadPackageOver` | — | 下载完成收尾 |
| `FsmClearCacheBundle` | `new ClearCacheOptions(ClearCacheMethods.ClearUnusedBundleFiles)` + `ClearCacheAsync` | 清的是「**未使用**」缓存，不是清全部 |
| `FsmStartGame` | `GameManager.SetGamePackage(YooAssets.GetPackage("DefaultPackage"))` → 切场景 | 资源系统就绪后才启动业务 |

**四档 `EPlayMode` 的初始化参数差异**（选型题标准素材）：

| PlayMode | Options | 文件系统 |
| --- | --- | --- |
| `EditorSimulateMode` | `EditorSimulateModeOptions` | `EditorSimulateBuildInvoker.Build(packageName, (int)EBundleType.VirtualAssetBundle)` 产虚拟包根目录 + `CreateDefaultEditorFileSystemParameters`，并注入 `VirtualDownloadMode / VirtualDownloadSpeed(1024*1000) / AsyncSimulateMinFrame(5) / AsyncSimulateMaxFrame(10)` —— **在编辑器里模拟异步时序与假下载速度**，可提前暴露掉帧/时序竞争 |
| `OfflinePlayMode` | `OfflinePlayModeOptions` | `CreateDefaultBuiltinFileSystemParameters()`（只读 StreamingAssets） |
| `HostPlayMode` | `HostPlayModeOptions` | Builtin（+`CopyBuiltinPackageManifest=true`）+ `CreateDefaultSandboxFileSystemParameters(remoteService)`（+`DownloadMaxConcurrency=5` / `DownloadMaxRequestPerFrame=1` / `DownloadWatchdogTimeout=10`） |
| `WebPlayMode` | `WebPlayModeOptions` | WebGL 用 `CreateDefaultWebServerFileSystemParameters`；微信小游戏用 `WechatFileSystemCreater` + `WX.env.USER_DATA_PATH`（`#if UNITY_WEBGL && WEIXINMINIGAME`） |

> 💡 `Boot.cs` 默认 `EPlayMode.EditorSimulateMode`；`Awake` 只做 `targetFrameRate=60 / runInBackground=true / DontDestroyOnLoad`，`Start` 里 `YooAssets.Initialize()`（**全局只调一次**）→ `PatchManager.Create("DefaultPackage", PlayMode)` → `Start()`。

### 7.6 「包版本号」≠「远端 URL 版本目录」（易混点）

- 10-11 `HotUpdateDlls` 包版本 = `v1.1.5`（自定义命名）；
- 10-13 `DefaultPackage` 包版本 = `2026-10-13-962`（**日期 + 自增序号**，YooAsset 默认按打包日期生成）；
- `FsmInitializePackage.GetHostServerURL()` 拼的是 `http://127.0.0.1/CDN/PC/v1.0` —— 这个 `v1.0` 是**代码里硬编码的 URL 一级目录**，与包版本无关。

→ 上传 CDN 时必须让 **URL 目录组织** 与 **`RemoteService` 拼出的路径** 对齐，否则 `RequestPackageVersionAsync` 取不到 `.version` 文件。**这是热更「首跑失败」的高频原因。**

## 八、2026-10-14 实战推进：补丁 FSM 各节点实现细节

第七节只记了八节点的**顺序**，这里补上各节点源码里的实现细节（`PatchLogic/FsmNode/`）。

| 文件 | 关键实现 |
| --- | --- |
| `FsmCreateDownloader.cs` | `int downloadingMaxNum = 10; int failedTryAgain = 3;` → `new ResourceDownloaderOptions(10, 3)` → `package.CreateResourceDownloader(options)`；`TotalDownloadCount == 0` 直接 `ChangeState<FsmStartGame>()`，否则发 `PatchFoundUpdateFilesEvent(count, bytes)` **暂停流程、让玩家确认是否更新** |
| `FsmDownloadPackageFiles.cs` | 先挂 `downloader.DownloadError += …` / `DownloadProgressChanged += …`，再 `downloader.StartDownload(); yield return downloader;` → 恢复后判 `Status != EOperationStatus.Succeeded` 才决定是否推进 |
| `FsmDownloadPackageOver.cs` | 纯日志节点，直接 `ChangeState<FsmClearCacheBundle>()` |
| `FsmClearCacheBundle.cs` | `new ClearCacheOptions(ClearCacheMethods.ClearUnusedBundleFiles)` → `package.ClearCacheAsync(options)`；**排在下载完成之后**执行 |
| `PatchManager.cs` | `EventGroup` 监听 5 个 User 事件（InitializePackage / BeginDownloadWebFiles / RequestPackageVersion / UpdatePackageManifest / DownloadWebFiles）；`StateMachine(null)` + `AddNode<…>()` × 8；`IsValidPlayMode` 白名单含 `WebPlayMode`（四档全支持） |

### 8.1 `ResourceDownloaderOptions(10, 3)` 两个参数

`downloadingMaxNum`（最大并发下载数）= 10、`failedTryAgain`（单文件失败重试次数）= 3。**重试次数不是重试间隔** —— 间隔由下载器内部按次数退避，别把第二个参数当"秒数"。

### 8.2 `yield return downloader;` 为什么合法

YooAsset 的 `Operation` **直接实现了 `IEnumerator`**（`MoveNext` 里返回 `IsDone`、`Current` 返回自身），所以协程可以 `yield return` 一个异步操作对象，语义 = 「等到操作完成再恢复」。

```csharp
downloader.StartDownload();
yield return downloader;                      // 等价于 while(!op.IsDone) yield return null;
if (downloader.Status != EOperationStatus.Succeeded) { /* 处理失败 */ }
```

这就是 YooAsset 全异步 API 能**无缝塞进协程**（也能 `await`、也能传回调）的原因 —— 一套 Operation 三处复用。对照 📎 [[协程原理与unitask]]：原生协程只认 `YieldInstruction` / `IEnumerator` / `null`，YooAsset 走的正是「自定义 `IEnumerator`」这条路。

### 8.3 `ClearCacheMethods` 的两个枚举与执行时机

| 枚举 | 清什么 |
| --- | --- |
| `ClearUnusedBundleFiles` | **只清未被任何清单引用**的缓存文件（官方示例用这个） |
| `ClearAllBundleFiles` | 清掉全部缓存文件（连当前清单引用的也清） |

**为什么排在 `FsmDownloadPackageOver` 之后**：下载过程中缓存目录里既有旧版本文件、也有正在写入的新文件，此时清缓存有误删风险；等下载全部结束、清单已生效，再清「没被引用的」才是安全的。

## 九、2026-10-17 实战推进：HostPlayMode 全链路首次跑通（两个独立坑）

`YooAssets_HybirdCLR` 项目今日首次跑通 HostPlayMode 完整链路（含真实增量热更）。本节记录**两个独立故障**的根因链与修复，以及排查方法论 —— 这是「官方示例照抄也会挂」的典型样本。

### 9.1 前置事实：这个项目其实没有 HybridCLR

| 项 | 实测 |
| --- | --- |
| 引擎 | Unity **2022.3.62f3c1**（中国版），目标 Windows Standalone |
| 资源框架 | YooAsset **3.0.6**（openupm scopedRegistry `com.tuyoogame.yooasset`） |
| 代码热更 | ⚠️ **未安装** —— manifest 无 `com.code-philosophy.hybridclr`、`Assets/` 下无 HybridCLR 目录、`Build/*_Data/Managed` 是纯 Mono 输出 |
| 版本控制 | **无 git 仓库**（连 diff 都看不了） |
| 场景 | `SampleScene.unity` 只有 Camera + Light，**没有挂启动器**；入口在示例的 `Boot.unity` |

**结论**：目录名叫 `HybirdCLR`（Hybrid 的拼写错误）但能力上**只有资源热更，没有代码热更**。名字骗人，判断能力要看 manifest 与产物。

### 9.2 坑一：`BundledCopyOption` 默认 `None` → 内置包缺三件套

**现象**

```
file:///.../Assets/StreamingAssets/yoo/DefaultPackage/DefaultPackage_2026-10-13-962.hash → 404
→ InitializePackageOperation 失败 → FsmInitializePackage.cs:99 只报 warning，流程静默中断
```

**根因链（逐层挖到源码）**

1. `BuiltinFileSystem` 初始化**必须**读到 `<Package>_<Version>.hash` —— `RequestBuiltinPackageHashOperation.cs:44-63`：`File.Exists` 不满足就发 `file://` 请求，404 直接 `SetError`
2. HostPlayMode 下 `CopyBuiltinPackageManifest=true`（`FsmInitializePackage.cs:68` 显式打开），`CopyBuiltinPackageManifestOperation.cs:79-105` 会**无条件**拷贝内置 version / manifest / hash —— 源码注释写明「只负责拷贝文件，不负责校验」，缺一个就 fail
3. **为什么缺**：打包窗口的 `BundledCopyOption` **默认值是 `None`**（`Editor/BundleBuilder/BuildParameters.cs:95`）
   - `TaskCopyBundledFiles_SBP.cs:19` → `None` 时整段跳过，不往 StreamingAssets 拷 version / manifest / hash
   - `TaskCreateCatalog_SBP.cs:18` → `None` 时不生成 `BuiltinCatalog.json / .bytes`
   - StreamingAssets 里现存的 version + bytes 是**当时手动拷的**，正好漏了 hash

**关键：补 hash 只是第一层。** `BuiltinFileSystem.OnCreate`（`BuiltinFileSystem.cs:348-356`）会创建 `BuiltinBundleCache`，其初始化 `BBCInitializeOperation` → `LoadBuiltinCatalogOperation` **强依赖 `BuiltinCatalog.bytes`**，缺文件同样走 `file://` 请求失败 → 只补 hash 能过初始化，**加载资源时还会挂**。

**正解**：AssetBundle Builder 窗口把「内置资源拷贝选项 / Bundled Copy Option」改为 **`ClearAndCopyAll`** 后重新构建 → 自动拷全 `version + <ver>.bytes + <ver>.hash + 43 个 bundle + BuiltinCatalog.json/bytes`。

### 9.3 坑二：打包后没同步 CDN → 版本错位

- 16:34 重打包后：内置包版本 `2026-10-17-994`；CDN（`E:\net\CDN\PC\v1.0\`）仍是 `2026-10-17-988`
- **表现很怪**：客户端会「降到旧清单」→ 按 988 去找 bundle → 找不到 / 加载失败。看起来像网络问题或资源丢失，实际是**两边版本号不一致**
- **修复**：994 全套 `cp -f` 到 CDN（只覆盖/新增，没删任何文件；988 的清单文件留在原地成为**孤儿**，无害）
- **纪律**：**「打包」和「同步 CDN」必须当成一个原子操作**，漏一步必踩。建议后续写 `Tools/` 一键同步脚本（robocopy + 版本号校验）

### 9.4 为什么「三档验证顺序」是排查的前提

本次能快速定位，靠的是**先缩范围**：

```
EditorSimulateMode  →  绕开所有文件系统，确认示例本身正常
        ↓
OfflinePlayMode     →  验证内置包完整（本次先跑通 → 问题被压到 host 侧）
        ↓
HostPlayMode        →  只剩「下载链路 + 远端版本」两个变量
```

Offline 能跑、Host 不能跑 → **内置包没问题，问题一定在 host 侧的文件系统或远端**。这一步省掉了大量瞎猜。

### 9.5 关键路径速查（调试先看这里）

| 用途 | 路径 |
| --- | --- |
| 编辑器沙盒缓存 | `Library/yoo/DefaultPackage/`（`ManifestFiles/` 存 `<Pkg>_<ver>.bytes` + `.hash` + `ApplicationFootprint.bytes`；`SimulateCache/` 存模拟缓存） |
| 打包产物 | `Bundles/StandaloneWindows64/DefaultPackage/<版本号>/` |
| 内置资源（首包） | `Assets/StreamingAssets/yoo/DefaultPackage/` |
| 本地 CDN 服务器 | `E:\net\`（`python -m http.server 80`，启动脚本 `开启服务器.bat`）→ URL `http://127.0.0.1/CDN/PC/v1.0` |
| 播放器沙盒 | `%USERPROFILE%\AppData\LocalLow\DefaultCompany\YooAssets_HybirdCLR\` |

**行为异常时第一招**：清掉 `Library/yoo/DefaultPackage/`（里面常残留多版清单，比如 985 / 988），整目录重来。

### 9.6 排查方法论：初始化失败先分三层

| 层 | 缺什么会挂 | 对应文件 |
| --- | --- | --- |
| ① 文件系统 | hash 读不到 | `<Pkg>_<Ver>.hash` |
| ② 清单 | manifest / catalog 缺失 | `<Pkg>_<Ver>.bytes` + `BuiltinCatalog.bytes` |
| ③ 远端 | 版本目录 / 版本号对不上 | `DefaultPackage.version` + URL 里的 `<appVersion>` 目录 |

**一句话**：初始化 404 不要先怀疑网络，按「hash → bytes → version 对齐」三层逐个 `curl` / `ls` 一遍，八成当场定位。

### 9.7 顺带的生命周期实验（`Assets/Scripts/`）

同日在项目里写了两个小脚本验证 `Instantiate` 的生命周期：

```csharp
// CreateGo.cs
void Start() {
    var go = Instantiate(prefab);
    //awake 在实例化时就会调用一次
    go.enabled = false;
}

// BeCreatedGo.cs：Awake / Start / OnEnable 各打一条日志 + gameObject.activeSelf
```

**结论**：`Instantiate` **同步执行 Awake**（`Instantiate` 返回前 Awake 已跑完）；紧接着的 `enabled = false` 只能拦住 `Start`（延后到启用那一刻），**拦不住已经发生的 Awake 与 OnEnable**。完整执行顺序与 `active` / `enabled` 两个开关的分界见 📎 [[MonoBehaviour生命周期与SetActive的坑]]。

## 相关

- [[热更新 HybridCLR — AOT 泛型元数据补全]]
- [[AssetBundle 生命周期与卸载语义]]
- [[Addressables资源生命周期]]
- [[MonoBehaviour生命周期与SetActive的坑]]
