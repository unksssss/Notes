---
title: "ACM模式输入输出"
type: resource
tags: [算法, cpp, 面试]
category: 工具与模板
created: "2026-10-22"
updated: "2026-10-22"
status: active
summary: "ACM 模式 = IO 壳 + 算法 + 输出格式化三层，只有中间那层与核心代码模式相同。五种输入形态：单组读 n、先给 T 组、不给 T 读到 EOF（while (cin >> n)）、整行含空格（必须 getline，且先 getline 吃掉 cin>> 残留的换行）、逗号分隔（getline(iss, tok, ',')）。★ 最坑：cin >> x 之后直接 getline 会读到空串，导致整组数据错位一行、最后一组丢失（已实测）。输出三条：末尾别多空格、组间空行看题面、大数据量用 '\\n' 不用 endl。IO 提速 ios::sync_with_stdio(false) + cin.tie(nullptr)。本地用 < in.txt 重定向调试，别把 freopen 交上去"
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

## 相关

- [[用vector实现栈]] —— **同样偏工程细节**：那篇讲容器 API 的语义坑（`pop_back` 返回 void、空容器 UB），本篇讲 IO 层的语义坑，两者都是「思路对了但细节翻车」的类型
- [[滑动窗口的最大值]] —— **练 ACM 模式的推荐载体**：它的输入正好是「先读 n、再读 n 个数、可能多组」，是最典型的 ACM 输入形态，适合拿来练手写外壳
