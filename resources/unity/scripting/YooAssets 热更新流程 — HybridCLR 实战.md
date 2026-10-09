---
title: "YooAssets 热更新流程 — HybridCLR 实战"
type: resource
tags: [unity, 热更新, HybridCLR, YooAssets, AssetBundle, 学习项目]
created: "2026-10-09"
updated: "2026-10-09"
status: active
summary: "实战项目 E:\\unityproject\\HybirdCLR 的完整热更链路：YooAssets.Initialize → CreatePackage → 自定义 IRemoteService + HostPlayModeOptions 文件系统 → RequestPackageVersion → LoadPackageManifest → ResourceDownloader 差异下载 → LoadAssetAsync<TextAsset>(dll.bytes) → Assembly.Load → 反射调用热更类"
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

## 五、面试答题框架

① 一句话定性：**YooAssets 管资源、HybridCLR 管代码，两条链路各自做差异更新** → ② 资源侧八步：Initialize → CreatePackage → 选运行模式（Editor/Offline/Host）+ 文件系统 → 请求版本 → 加载清单 → 差异下载（`TotalDownloadCount` 即差异数）→ 加载 → Release → ③ 代码侧：DLL 改名 `.bytes` 打进 AB → `LoadAssetAsync<TextAsset>` → `Assembly.Load(bytes)` → 反射调用 → ④ 补上 AOT 泛型元数据这个最大的坑。

## 相关

- [[热更新 HybridCLR — AOT 泛型元数据补全]]
- [[AssetBundle 生命周期与卸载语义]]
- [[Addressables资源生命周期]]
