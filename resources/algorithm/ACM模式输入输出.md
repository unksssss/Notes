---
title: "ACM模式输入输出"
type: resource
tags: [算法, cpp, 面试]
category: 工具与模板
created: "2026-10-22"
updated: "2026-11-17"
status: active
summary: "ACM 模式 = IO 壳 + 算法 + 输出格式化三层，只有中间那层与核心代码模式相同。五种输入形态：单组读 n、先给 T 组、不给 T 读到 EOF（while (cin >> n)）、整行含空格（必须 getline，且先 getline 吃掉 cin>> 残留的换行）、逗号分隔（getline(iss, tok, ',')）。★ 最坑：cin >> x 之后直接 getline 会读到空串，导致整组数据错位一行、最后一组丢失（已实测）。输出三条：末尾别多空格、组间空行看题面、大数据量用 '\\n' 不用 endl。IO 提速 ios::sync_with_stdio(false) + cin.tie(nullptr)。本地用 < in.txt 重定向调试，别把 freopen 交上去。★ 加速实测（1e6 个 int）：默认 473 ms → 只关 sync 87 ms（5.4×）→ 两个都关 85 ms；只关 tie 468 ms（几乎无差别），因为 tie 省的是「读前 flush cout」，无真实输出等待时没用；关同步后的 cin 比 scanf（240 ms）还快一倍多。代价：不能与 scanf/printf 混用、交互题必须手动 flush。真正更常见的 TLE 元凶是 endl（每次强制 flush）"
source: "牛客 ACM 模式刷题（灯皇 2026-10-22 提问）"
related:
  - "[[滑动窗口的最大值]]"
  - "[[用vector实现栈]]"
---

# ACM 模式输入输出

> **背景**：核心代码模式（LeetCode / 牛客的 `class Solution`）只写函数，输入输出平台代劳；ACM 模式要自己写完整程序 —— `#include`、`main`、读数据、调算法、按格式打印。

## 一、核心认知：它是三层，只有中间那层是你在练的

| 层 | 职责 | 易错程度 |
| --- | --- | --- |
| ① **IO 壳** | `cin` / `getline` / 多组循环 / 解析 | ★★★ 最高 |
| ② **算法** | 和核心代码模式里那段完全一致 | ★ 低（你练的就是这个） |
| ③ **输出格式化** | 换行、空行、精度、末尾空格 | ★★ 高 |

**交题失败绝大多数出在 ① 和 ③**。所以从核心代码模式切过去时，**算法函数原样搬，只补外壳** —— 别重写。

## 二、万能骨架

```cpp
#include <iostream>
#include <vector>
#include <string>
#include <sstream>
using namespace std;

void solve() {                     // 处理「一组」数据
    int n;
    cin >> n;
    vector<int> a(n);
    for (int i = 0; i < n; ++i) cin >> a[i];

    // ==== 算法：把 Solution 里那段搬进来 ====
    long long ans = 0;
    for (int x : a) ans += x;

    cout << ans << '\n';
}

int main() {
    ios::sync_with_stdio(false);   // ★ IO 提速（1e6 级输入必备）
    cin.tie(nullptr);

    int T;
    cin >> T;
    while (T--) solve();           // 若题面不给 T，改成 while (cin >> n) 形式
    return 0;
}
```

**把「读一组 + 算一组 + 打一组」封成 `solve()`**，多组循环放在 `main` —— 这是 ACM 模式最省心的结构，改输入形态时只动 `main`。

## 三、五种输入形态（全部实测）

### 形态 1：单组，先读 n

```cpp
int n; cin >> n;
vector<int> a(n);
for (int i = 0; i < n; ++i) cin >> a[i];
```

`cin >>` 自动跳过空格和换行，**不需要关心数据是分行还是同行**。

#### 一行两个整数：`cin >> a >> b` 就够了

```cpp
int a, b;
cin >> a >> b;
cout << a + b << '\n';
```

**不要用 `getline` 再自己 split** —— `>>` 本身就是「按任意空白切分」，空格、多个空格、tab、换行在它眼里完全等价。

实测（同一份代码，三种输入）：

| 输入 | 输出 | 说明 |
| --- | --- | --- |
| `1 2` | `3` | 标准情形 |
| `3    4`（多个空格） | `7` | 自动跳过任意多空白 |
| `5` 换行 `6`（分两行） | `11` | **「一行两个数」和「两行各一个数」读法完全一样** |

所以题面说「一行两个整数」不用特意「读一行」，直接 `cin >> a >> b` 最稳。

**多组（每行两个数，不给 T）**：

```cpp
int a, b;
while (cin >> a >> b) cout << a + b << '\n';   // 链式判断：任一读失败即为 false
```

#### ⚠️ 但如果两个数是**逗号**隔开的，`>> a >> b` 会静默算错

实测输入 `1,2`：

| 写法 | 结果 |
| --- | --- |
| `cin >> a >> b` | `a=1, b=0`，`cin` 进入失败状态 → **算出 `1+0=1`，不报错** |
| `cin >> a >> comma >> b` | `3` ✅ |

`>>` 卡在 `,` 上读不到 `b`，而 C++11 起**读取失败会把目标变量置 0**，于是得到一个看起来正常的错误答案。所以：

```cpp
int a, b; char comma;
cin >> a >> comma >> b;          // 逗号分隔，显式吃掉分隔符
```

**判据**：分隔符是**空白**（空格/tab/换行）→ `>>` 连读；分隔符是**可见字符**（逗号、冒号、`[` `]`）→ 必须先把它读进一个变量吃掉，或者 `getline` + `istringstream` 切分。

### 形态 2：先给 T 组

```cpp
int T; cin >> T;
while (T--) solve();
```

### 形态 3：不给 T，读到 EOF

```cpp
int n;
while (cin >> n) {          // ★ cin 能当条件用：读失败（EOF）即转 false
    vector<int> a(n);
    for (int i = 0; i < n; ++i) cin >> a[i];
    ...
}
```

**这是牛客上最常见、也最容易漏的形式** —— 题面写「多组数据，处理到文件末尾」。别硬猜组数。

### 形态 4：整行含空格，必须 `getline`

```cpp
int T; cin >> T;
string line;
getline(cin, line);          // ★ 吃掉 cin>>T 留下的换行！不写这句，第一组读到的是空串
while (T--) {
    getline(cin, line);
    istringstream iss(line);
    long long s = 0; int x;
    while (iss >> x) s += x;   // 不定长数字，读到行尾
    cout << "[" << line << "] sum=" << s << '\n';
}
```

### 形态 5：逗号分隔 / 带方括号（`[1,2,3]`）

```cpp
string line; cin >> line;                       // 先读整块
for (char& c : line) if (c == '[' || c == ']') c = ' ';
istringstream iss(line);
string tok; long long s = 0;
while (getline(iss, tok, ',')) s += stoi(tok);   // ★ 按逗号切
```

`getline(流, 变量, 分隔符)` 是解析「带自定义分隔符」的利器，比自己手写 `split` 可靠。

## 四、三个最坑（按踩坑概率排序）

### 坑 1：`cin >> x` 之后直接 `getline` → 读到空串

`cin >>` 遇到换行符就停，但**换行符留在缓冲区里**；紧接着的 `getline` 一进去就看到这个换行，于是返回空串。

实测（输入 `2` + 两行 `hello 1 2` / `  10 20 30  `）：

| 写法 | 输出 |
| --- | --- |
| 正确（先 `getline` 吃掉换行） | `[hello 1 2] sum=0` / `[  10 20 30  ] sum=60` ✅ |
| **忘写** | `[] sum=0` / `[hello 1 2] sum=0` ❌ |

后果不只是"第一组空"，而是**整体错位一行**：第一组读到空串，第二组读到原本的第一行，**最后一组数据被直接丢掉**（`T` 已经用完）。这种错位在判题时表现为「答案少一行」或「全部 Wrong Answer」，很难从报错反推原因。

**修法三选一**：
```cpp
getline(cin, line);                    // ① 先吃掉换行（最简单）
cin.ignore(numeric_limits<streamsize>::max(), '\n');   // ② 更彻底
// ③ 干脆全用 getline，再 istringstream 拆（推荐，见形态 4）
```

### 坑 2：输出格式（判题是逐字节比对）

| 细节 | 说明 |
| --- | --- |
| **行末多余空格** | 有的 OJ 严格比对（Presentation Error）；循环里 `cout << a[i] << " "` 会在末尾留空格 |
| **组间空行** | 有的题要求每组之间空一行，且**最后一组之后不能多空**（经典陷阱，见 POJ 常见格式题） |
| **浮点精度** | `cout << fixed << setprecision(2)`；别用 `double` 判等 |
| **布尔输出** | 有的题要 `true/false`，有的要 `Yes/No`，**照题面抄** |
| 数组输出格式 | `[1,2,3]` 还是 `1 2 3`，两种差别巨大 |

**安全写法**：
```cpp
for (int i = 0; i < n; ++i) {
    if (i) cout << ' ';        // ★ 空格放前面，天然没有尾随空格
    cout << a[i];
}
cout << '\n';
```

### 坑 3：用 `endl` 代替 `'\n'`

`endl` 会**强制 flush**（每次都把缓冲区推到 stdout）。输出 10^6 行时，这个 flush 的代价足以把 1 秒的题 timeout。**ACM 模式统一用 `'\n'`。**

## 五、本地调试：重定向，不要 `freopen`

```bash
g++ -std=c++17 -o a.exe a.cpp
./a.exe < in.txt          # ★ 标准输入重定向，最干净
./a.exe < in.txt > out.txt && cat out.txt
```

如果习惯在代码里写 `freopen("in.txt", "r", stdin);` —— **提交前必须注释掉**，否则线上读不到文件直接 RE。用 shell 重定向就完全没这个问题。

## 六、从核心代码模式迁移：三步

1. **搬算法**：`class Solution` 里的函数，去掉 `class` 包装或原样保留，改成自由函数 / 直接内联进 `solve()`
2. **写 `solve()`**：读参数 → 调算法 → 打结果
3. **写 `main()`**：测速两行 + 多组循环（按题面选形态 1/2/3）

**注意**：核心代码模式的函数签名常带 `vector<int>&`（引用），ACM 模式下自己构造的局部变量传进去完全没问题；但如果算法**修改了入参**，多组数据要记得每组重新构造，别复用同一个 vector。

## 七、环境注意（灯皇本机）

- g++ 8.1.0 下 `#include <bits/stdc++.h>` + `-std=c++17` **会编译失败**（`fs_path.h` 的 `operator!=`）→ **拆成具体头文件**
- 编译命令：`g++ -std=c++17 -finput-charset=UTF-8 -o x.exe x.cpp`
- 中文输出乱码：`main` 里加 `SetConsoleOutputCP(CP_UTF8)`（Windows 控制台），但**提交给 OJ 时删掉**（OJ 是 Linux，`windows.h` 编译不过）

## 八、实测记录

六种形态全部用 g++ 编译跑通（`-Wall -O2` 零警告），输入文件与输出如下：

| 形态 | 输入 | 输出 |
| --- | --- | --- |
| 单组 `n + 数组` | `5` / `1 3 -1 -3 5` | `5` ✅ |
| 先给 `T=3` | 三组数据 | `6` / `12` / `22` ✅ |
| 读到 EOF | 两组数据（无 T） | `6` / `12` ✅ |
| `getline` 正确版 | 两行含空格 | `[hello 1 2] sum=0` / `[  10 20 30  ] sum=60` ✅ |
| `getline` 忘 ignore | 同上 | `[] sum=0` / `[hello 1 2] sum=0` ❌（错位 + 丢组） |
| 逗号分隔 `[1,2,3,4]` | 一行 | `sum=10` ✅ |

## 九、读加速：`sync_with_stdio(false)` 和 `tie(nullptr)` 到底在干嘛

到处能见到这段，写在 `main` 外面（全局作用域）：

```cpp
static const auto io_sync_off = []() {
    std::ios::sync_with_stdio(false);   // ① 断开与 C 标准库的同步
    std::cin.tie(nullptr);              // ② 解开 cin 对 cout 的绑定
    return nullptr;
}();
```

### 为什么要写成「赋给一个全局变量」

它本质是**想在 `main` 之前执行一次**。C++ 里"在 `main` 之前跑一段代码"最省事的办法就是**全局变量初始化** —— 于是用「立即调用的 lambda」把两条语句塞进一个初始化表达式里。

拆开看：

| 部件 | 作用 |
|---|---|
| `[](){...}()` | 立即调用（IIFE），执行完就丢 |
| `return nullptr` | lambda 必须有个返回值才能被赋给变量；返回 `void` 的话 `auto` 推不出类型，`auto x = void` 非法 |
| `static` | 内部链接，多个 `.cpp` 各有一份互不冲突；且只初始化一次 |
| `const` | 只是防止后续误改 |

它和下面这句**完全等价**，后者更容易看懂：

```cpp
static const int io_sync_off = (std::ios::sync_with_stdio(false), std::cin.tie(nullptr), 0);
```

### ① `sync_with_stdio(false)`：断开 C/C++ 两套缓冲的同步

默认情况下，`cin`/`cout` 和 `scanf`/`printf` **共享同一个底层缓冲区**，这样 `cout << 1; printf("2");` 的输出顺序才有保证。代价是**每次 IO 都要过一层同步检查**。

关掉后 `cin` 用自己的缓冲一次读一大块，速度差一个量级。

**代价：不能混用。** 关掉之后 `cin` 和 `scanf` 各读各的缓冲，`cout` 和 `printf` 各写各的，**输出顺序会乱、输入可能重复消费**。要么全用 C++ 流，要么全用 C 函数。

### ② `tie(nullptr)`：解开 `cin` 对 `cout` 的绑定

默认 `cin` 绑着 `cout`，**每次 `cin >>` 之前都会先 flush 一次 `cout`** —— 这是为了交互式程序里"先打出提示语、再等输入"能正常工作。

解除绑定后这个"读前先写"的动作没了。

**代价：交互题必须自己 flush**（`cout << ... << endl;` 或 `cout.flush()`），否则程序可能卡在等输入、而提示语还在缓冲里没发出去。

### 实测（本机 g++ 8.1.0 `-O2`，1e6 个整数）

| 配置 | 纯读 1e6 个 int | 边读边写 20 万轮（输出重定向） |
|---|---|---|
| 默认 | 473 ms | 527 ms |
| 只 `tie(nullptr)` | 468 ms | 535 ms |
| 只 `sync_with_stdio(false)` | **87 ms** | — |
| 两个都关 | 85 ms | **24.7 ms** |
| `scanf`（对照） | 240 ms | — |

**结论很反直觉**：

1. **收益几乎全部来自 `sync_with_stdio(false)`**（5.4 倍）。`tie(nullptr)` 在纯读、输出重定向到文件时**测不出差别**（473 vs 468），因为它省的是"flush cout"——没有真实输出等待时 flush 本来就便宜。
2. **`cin` 关同步后比 `scanf` 还快一倍多**（87 vs 240）。「`scanf` 比 `cin` 快」这句话在关掉同步后不成立。
3. `tie(nullptr)` 真正值钱的是**交互题**和**输出到终端**的场景，不是"读得慢"的解法。

所以这两行是"一起写没坏处"的习惯，但**别以为写了 `tie` 就能救 TLE**。

### 什么时候别写这段

- **数据量小**（几千个）—— 没意义，还多一层心智负担
- **交互题** —— `tie(nullptr)` 要还回来，或者每步手动 `flush`
- **要混用 `printf` / `scanf`** —— 关同步后会错序，只能二选一
- **多线程里** —— `ios_base::sync_with_stdio` 不是线程安全的

### 比它更快的

数据到 1e7 级别时，`cin` 关同步也顶不住，此时要**自己搞缓冲**：

```cpp
static char buf[1 << 25];
int n = fread(buf, 1, sizeof(buf), stdin);   // 一次读进来，再手写解析
```

或者 Linux 上的 `getchar_unlocked()`（无锁版 `getchar`，比 `getchar` 快 2~3 倍，但**不可重入、非标准**，Windows 上没有）。

### 还有个比它更常见的 TLE 元凶

```cpp
cout << ans << endl;    // ❌ endl = '\n' + flush()，1e5 行就是 1e5 次 flush
cout << ans << '\n';    // ✅
```

**`endl` 会强制刷缓冲**，这才是刷题 TLE 的头号原因。`tie` 和它比是小巫见大巫。

## 相关

- [[用vector实现栈]] —— **同样偏工程细节**：那篇讲容器 API 的语义坑（`pop_back` 返回 void、空容器 UB），本篇讲 IO 层的语义坑，两者都是「思路对了但细节翻车」的类型
- [[滑动窗口的最大值]] —— **练 ACM 模式的推荐载体**：它的输入正好是「先读 n、再读 n 个数、可能多组」，是最典型的 ACM 输入形态，适合拿来练手写外壳
