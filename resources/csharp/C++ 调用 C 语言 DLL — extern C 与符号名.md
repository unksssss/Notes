---
title: "C++ 调用 C 语言 DLL — extern C 与符号名"
type: resource
tags: [cpp, csharp, 链接机制, DLL, extern-C]
created: "2026-11-07"
updated: "2026-11-07"
status: active
summary: "C++ 调 C DLL 的核心是 name mangling：C 生成符号 Add，C++ 生成 ?Add@@YAHHH@Z，DLL 导出表只有前者，所以必须 extern \"C\"（它管名字、不管调用约定）。三种写法（有头文件 / 头文件自带守卫 / 无头文件手声明）；__declspec(dllimport) 对函数非必需、对全局变量必需；32 位 __stdcall 的 @8 后缀与 64 位无调用约定差异；隐式链接 vs LoadLibrary；dumpbin /exports 排查；C ABI 跨编译器兼容而 C++ 接口不可行；Unity DllImport 的同源要求。"
source: "面试八股整理（2026-11-07）"
related: ["[[C# IL 中间语言与编译执行模型]]", "[[C# 闭包与委托 — 隐藏类与 GC 陷阱]]"]
---

# C++ 调用 C 语言 DLL — extern "C" 与符号名

## 一、最短答案

```cpp
extern "C" {
#include "clib.h"
}
```

无头文件时手写声明：

```cpp
extern "C" __declspec(dllimport) int Add(int a, int b);
```

> **`extern "C"` 不是「按 C 的语法解析」，而是「按 C 的规则生成符号名」。**

---

## 二、为什么需要它：名称修饰

同一个声明，两个编译器生成的符号完全不同：

| 编译器 | 生成的符号 |
| --- | --- |
| C | `Add` （32 位可能带前导下划线 `_Add`） |
| C++ | `?Add@@YAHHH@Z`（MSVC）/ `_Z3Addii`（Itanium ABI） |

**名字里编码了返回类型、参数类型、命名空间、类名**——这是 C++ 支持函数重载的基础。

但 C 的 DLL 导出表里只有 `Add`。C++ 按修饰名去查 → 查不到 → **LNK2019 unresolved external symbol**。

`extern "C"` 就是让 C++ 编译器「这条路不要修饰」。

---

## 三、三种写法

### ① 有 C 头文件（最常见）

```cpp
extern "C" {
#include "clib.h"
}
```

注意是**包住 `#include`**，不是改写头文件。

### ② 头文件自带 C/C++ 守卫（C 库作者该这么写）

```c
// clib.h
#ifdef __cplusplus
extern "C" {
#endif

int Add(int a, int b);

#ifdef __cplusplus
}
#endif
```

这样 C 和 C++ 调用方都能直接 `#include`，调用方不用再包一层。**自己写库时务必加上**。

### ③ 没有头文件 —— 手动声明

```cpp
extern "C" __declspec(dllimport) int Add(int a, int b);
```

必须与 DLL 真实导出签名逐字一致：返回类型、参数类型、**调用约定**。

---

## 四、一个头文件同时服务导出与导入

```c
#ifdef MYLIB_EXPORTS
#define MYLIB_API __declspec(dllexport)
#else
#define MYLIB_API __declspec(dllimport)
#endif

#ifdef __cplusplus
extern "C" {
#endif

MYLIB_API int Add(int a, int b);
MYLIB_API extern int g_counter;          // 导出的全局变量

#ifdef __cplusplus
}
#endif
```

### `__declspec(dllimport)` 该不该加

| 对象 | 是否必需 | 说明 |
| --- | --- | --- |
| **函数** | 非必需 | 不加也能链接（链接器生成 thunk 转发），但失去一次间接跳转的优化机会 |
| **全局变量** | **必需** | 不加会访问到错误地址，直接崩 |

---

## 五、最容易漏的坑：调用约定

**`extern "C"` 只管名字，不管调用约定。** 两者不匹配 → 名字找到了，但调用时栈不平衡 → 运行时崩溃（且往往是「跑几次才崩」的诡异现象）。

| 平台 | 情况 |
| --- | --- |
| **32 位 Windows** | C 默认 `__cdecl`；若库用 `__stdcall`，导出符号是 `_Add@8`（`@` 后是参数字节数），声明必须写 `__stdcall` |
| **64 位 Windows** | 只有一种调用约定，不再区分 `__cdecl`/`__stdcall`，也不加下划线前缀 |

```cpp
// 32 位库用 __stdcall 导出时
extern "C" __declspec(dllimport) int __stdcall Add(int a, int b);
```

---

## 六、两种链接方式

| 方式 | 需要 | 特点 |
| --- | --- | --- |
| **隐式链接**（加载时） | `.lib` 导入库 + `.dll` | 启动时自动加载；缺 DLL 直接启动失败 |
| **显式链接**（运行时） | 只要 `.dll` | `LoadLibrary` + `GetProcAddress`，不需要 `extern "C"` 声明，但要自己管加载/卸载 |

```cpp
// 隐式
#pragma comment(lib, "clib.lib")

// 显式
auto h  = LoadLibrary(L"clib.dll");
auto fn = reinterpret_cast<int(*)(int,int)>(GetProcAddress(h, "Add"));
if (!fn) { /* 名字不对 / 导出名被修饰 */ }
```

**显式链接的坑**：`GetProcAddress` 传的必须是导出表里的**原始名字**。如果那个 DLL 是用 C++ 编译导出的，`"Add"` 返回 nullptr，实际符号是 `?Add@@YAHHH@Z`——所以 `extern "C"` 的价值在两种链接方式下是一致的：**保证符号名是你能猜到的那一个**。

---

## 七、排查手段：dumpbin

拿不准符号名、调用约定时，直接看导出表：

```bat
dumpbin /exports clib.dll
```

MinGW 环境用：

```bash
objdump -p clib.dll | grep -A 20 "Export Address Table"
# 或
nm -D clib.dll
```

输出里 `_Add@8` 就说明是 32 位 `__stdcall`；`Add` 说明是 C 且 64 位或 `__cdecl`。

---

## 八、错误对照表

| 现象 | 原因 |
| --- | --- |
| `LNK2019 unresolved external symbol "int __cdecl Add(int,int)"` | 缺 `extern "C"`，链接器在按修饰名找 |
| 名字解析成功，但运行崩溃 / 栈损坏 | 调用约定不匹配（`__cdecl` vs `__stdcall`） |
| `LoadLibrary` 成功但 `GetProcAddress` 返回 nullptr | 查找的名字不是导出表里的真实名字 |
| 程序启动报 0xc000007b | 32/64 位不匹配（64 位进程加载了 32 位 DLL） |
| 访问 DLL 里的全局变量崩溃 | 缺 `__declspec(dllimport)` |
| MinGW 编译的 exe 加载 MSVC 的 C++ DLL 失败 | C++ ABI 不兼容（见下一节） |

---

## 九、C ABI 跨编译器兼容，C++ ABI 不兼容

**C 接口是跨编译器最稳的**：MSVC 编的 DLL，MinGW / Clang 调用基本没问题，因为 C 的调用约定与符号命名是标准化的。

**C++ 接口跨编译器基本不可行**：

- 各家 name mangling 规则不同
- `std::string`、`std::vector` 的布局与实现不保证一致
- 异常传播、RTTI 机制不同

→ 给别的编译器/别的语言用的 DLL，**导出接口必须收窄到 C 子集**：用 `char*`、`int`、裸指针、POD 结构体，不暴露 STL 类型。

这就是为什么绝大多数跨语言 SDK 的 API 长得像 C。

---

## 十、Unity 的 DllImport 同源要求

Unity 的 `[DllImport]` 走 P/Invoke，默认按 `Cdecl` 查符号，所以**导出函数必须加 `extern "C"`**：

```cpp
// 原生插件侧
extern "C" __declspec(dllexport) int Add(int a, int b) { return a + b; }
```

```csharp
// C# 侧
[DllImport("clib")] static extern int Add(int a, int b);
```

同一个规则，同一个原因。

---

## 十一、编译命令示例

MinGW（g++）：

```bash
# 生成 DLL + 导入库
gcc -shared -o clib.dll clib.c -Wl,--out-implib,libclib.a

# 编译调用方
g++ main.cpp -L. -lclib -o main.exe
```

MSVC：

```bat
cl /LD clib.c                :: 生成 clib.dll + clib.lib
cl main.cpp clib.lib
```

---

## 十二、面试话术

> 「核心是 `extern "C"`。C++ 有名称修饰，同一个函数编译出来的符号是带参数信息的修饰名，而 C 的 DLL 导出表里只有原始名字，所以不加 `extern "C"` 就会链接失败、报 unresolved external symbol。
>
> 另外两个容易漏的点：一是 `__declspec(dllimport)` 对函数不是必需的，但对导出的全局变量是必需的；二是 `extern "C"` 只管名字不管调用约定，32 位下如果库是 `__stdcall` 导出的，声明里必须写 `__stdcall`，否则名字找到了但调用时栈不平衡会崩。拿不准就用 `dumpbin /exports` 看真实符号名。」

---

## 相关

- [[C# IL 中间语言与编译执行模型]] —— 同样是「编译产物与运行时契约」的问题，IL 是 .NET 侧的中间表示，本篇是原生侧的符号与 ABI
- [[C# 闭包与委托 — 隐藏类与 GC 陷阱]] —— 编译器自动生成隐藏结构的另一例，与 name mangling 同属「编译器在你不知情时改了名字」
