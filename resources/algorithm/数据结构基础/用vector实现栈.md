---
title: "用vector实现栈"
type: resource
tags: [算法, 数据结构, cpp, 面试]
category: 数据结构基础
created: "2026-10-22"
updated: "2026-10-22"
status: active
summary: "栈只用到 vector 的尾部三件套：push_back / back / pop_back，接口映射几乎 1:1。核心坑：pop_back() 返回 void，不能 return data_.pop_back()，必须 back() 取值再 pop_back() 删除（与 stack.top()/pop() 同源，顺序反了就丢元素 + 空栈 UB）；空栈 back()/pop_back() 是 UB 不是异常；pop_back 不缩容；扩容后此前取到的 top 引用会失效。复杂度：push 摊还 O(1)（GCC 容量 1→2→4→8→16 翻倍，触发时整体搬家），pop/top O(1)。std::stack<T, vector<T>> 可直接换底层，默认是 deque 因为 deque 扩容不搬家"
source: "面试题：用 vector / 数组手搓栈（承接「两栈实现队列」）"
related:
  - "[[栈与队列的相互实现]]"
  - "[[哈希表冲突解决与Dictionary底层]]"
  - "[[完全二叉树]]"
---

# 用 vector 实现栈

> **题目**：不借助 `std::stack`，用 `std::vector` 作为底层容器实现一个栈，支持 `push` / `pop` / `top` / `empty` / `size`。

## 一、核心认知

栈的语义是「**只在同一端进出**」（LIFO）。而 `vector` 的尾部恰好支持「摊还 O(1) 追加」和「O(1) 删除末尾」——**栈需要的操作，正好是 vector 最擅长的三个**。

所以这题没有算法难度，全部难点都在**接口语义的细节**上：

- `vector::pop_back()` **返回 void**，不像 `std::stack::pop()` 那样至少让你以为能拿值
- 空容器上 `back()` / `pop_back()` 是**未定义行为**，不会抛异常
- `size` 和 `capacity` 是两个概念，`pop_back` 只改前者

## 二、接口映射表

| 栈操作 | vector 实现 | 说明 |
| --- | --- | --- |
| `push(v)` | `data_.push_back(v)` | 摊还 `O(1)`，容量不够时整体搬家 |
| `pop()` | `data_.pop_back()` | `O(1)`，**返回 void** |
| `top()` | `data_.back()` | `O(1)`，返回**引用** |
| `empty()` | `data_.empty()` | `O(1)` |
| `size()` | `data_.size()` | `O(1)` |

栈顶 = **vector 的末尾**（`size()-1`），不是开头。这一点决定了所有实现都是「尾部三件套」。

## 三、版1：最小实现（对齐 `std::stack` 的接口）

```cpp
template <typename T>
class MyStack {
public:
    void push(const T& v) { data_.push_back(v); }
    void pop()            { data_.pop_back(); }        // 与 std::stack 一致：不返回值
    T& top()              { return data_.back(); }
    const T& top() const  { return data_.back(); }
    bool empty() const    { return data_.empty(); }
    size_t size() const   { return data_.size(); }

private:
    std::vector<T> data_;
};
```

`std::stack` 本身就这么设计的 —— `pop()` 不返回值，就是为了**避免在返回值拷贝时抛异常导致元素丢失**（异常安全）。想要值就先 `top()` 再 `pop()`，这是 C++ 容器的通用惯例。

## 四、版2：`pop()` 同时返回被弹的值（面试常要求）

```cpp
T pop() {
    if (data_.empty()) throw std::out_of_range("pop from empty stack");
    T v = data_.back();        // ★ 先取值
    data_.pop_back();          // ★ 再删除
    return v;
}
```

**这两行的顺序不能换** —— 换一下就是上一题（两栈实现队列）里那个错：

```cpp
data_.pop_back();              // ❌ 元素已经没了
return data_.back();           //    再 back()：要么是"下一个"元素，要么空栈 UB
```

## 五、五个坑

**① `pop_back()` 返回 `void`，不能直接 return**

```cpp
return data_.pop_back();       // ❌ 编译错误：void 无法转换为 T
T v = data_.pop_back();        // ❌ 同上
```

必须**两步**：`back()` 取值 → `pop_back()` 删除。

**② 空栈上 `back()` / `pop_back()` 是 UB，不是异常**

```cpp
MyStack<int> s;
s.pop();                       // ❌ 未定义行为，不抛异常，可能静默跑过去
```

`std::vector` 和 `std::stack` 都**不做空检查**（为了性能）。要安全就自己加 `if (empty()) throw ...`。Java 的 `EmptyStackException` 在这里不存在。

**③ 别用 `erase(end() - 1)` 代替 `pop_back`**

```cpp
data_.erase(data_.end() - 1);  // ❌ 语义相同但可能退化成 O(n)，且多一次迭代器构造
```

`pop_back()` 是编译器能内联的「size-- + 析构末尾元素」，没有理由绕路。

**④ `pop_back` 不会缩容，内存一直占着**

`size` 减 1，`capacity` 不变。一个曾经装过 100 万个元素的栈，即使全部弹空，内存仍然占着 —— 需要的话用 `data_.shrink_to_fit()`（C++11，且不保证一定缩）。

**⑤ 扩容会让之前取到的 `top()` 引用失效**

```cpp
T& r = s.top();                // 拿到的是 data_[size-1] 的引用
s.push(x);                     // 若触发扩容 → 整体搬到新内存
r = 100;                       // ❌ r 仍指向已释放的旧内存 → UB
```

这是 vector 的「**扩容后所有迭代器/引用/指针失效**」规则在栈上的表现。写 `auto&` 的时候要留意后续有没有 `push`。

## 六、复杂度：摊还 `O(1)` 从哪来

单次 `push_back` 在容量满时是 `O(n)`（申请新块 + 拷贝全部元素 + 释放旧块），但**扩容后容量翻倍**，所以连续 n 次 push 的总搬家量是 `1+2+4+…+n < 2n` → **摊还 `O(1)`**。

GCC 实测（每次 push 后打印 capacity）：

| push 次数 | size | capacity | 是否触发搬家 |
| --- | --- | --- | --- |
| 1 | 1 | 1 | ✅ |
| 2 | 2 | 2 | ✅ |
| 3 | 3 | 4 | ✅ |
| 5 | 5 | 8 | ✅ |
| 9 | 9 | 16 | ✅ |
| 17 | 17 | 32 | ✅ |

**注意增长倍率是「实现定义」的**：GCC 是 2 倍，MSVC 是 1.5 倍。1.5 倍的好处是能复用之前释放的内存块（`1.5^n` 增长慢于 `2^n`，旧块总和不超过新块），2 倍的好处是常数更小。

如果知道栈的最大深度，`data_.reserve(n)` 可以**一次性省掉所有搬家** —— 这是刷题/工程里最实用的优化。

## 七、四种底层实现的对比

| 底层 | push | pop | 空间 | 特点 |
| --- | --- | --- | --- | --- |
| **`std::vector`** | 摊还 `O(1)` | `O(1)` | 最省（连续、无额外结构） | 缓存友好；**扩容要搬家**，引用失效 |
| **`std::deque`** | `O(1)` | `O(1)` | 略大（分段 + 中控数组） | **扩容不搬家**；`std::stack` 的默认选择 |
| 静态数组 + 栈顶指针 | `O(1)` | `O(1)` | 固定 | 最快，但**容量写死**、需自己判满 |
| `std::list` | `O(1)` | `O(1)` | 最大（每节点两个指针 + 无缓存局部性） | 完全没必要用于栈 |

**面试常问：`std::stack` 默认为什么是 `deque` 而不是 `vector`？**

因为 `deque` 的 `push_back` **不需要把已有元素搬走**，所以「取到引用/迭代器后再 push」不会失效，也不会出现一次 `O(n)` 的尖峰。代价是内存略大、随机访问比 vector 慢一点 —— 但栈本来就不做随机访问，所以这个代价可以接受。

## 八、标准库其实早就支持换底层

不用自己手写，`std::stack` 是容器适配器，**第二个模板参数就是底层容器**：

```cpp
std::stack<int, std::vector<int>> s;   // 用 vector 做底层
std::stack<int, std::deque<int>>  d;   // 等价于 std::stack<int>（默认）
```

自己手写的价值在于：**面试要看你知不知道那三行映射、以及 `pop()` 的返回值语义为什么被设计成 void**。工程里直接用 `std::stack<int, std::vector<int>>` 就好。

## 九、面试延伸

- **「用静态数组实现栈」** → 加一个 `int top = -1`，`push` 时 `if (top == cap-1) 报满`，`pop` 时 `--top`。比 vector 快，但容量固定
- **「栈溢出怎么处理」** → 静态数组版要么抛异常、要么扩容（自己实现就变成 vector 的 `push_back` 了）
- **「min 栈怎么实现」** → 双栈（辅助栈存当前最小值）或单栈存 `pair<值, 当前min>`；见 [[栈与队列的相互实现]] 的延伸
- **「`top()` 为什么返回引用」** → 避免拷贝；但要注意引用生命周期（见坑⑤）
- **「`pop()` 为什么不返回值」** → 异常安全：如果用「返回值 + 删除」一步完成，拷贝构造抛异常时元素已经没了。C++ 的惯例是「先看再删」

## 实测

`g++ -std=c++17 -Wall` 零警告，全部通过：

| 项目 | 结果 |
| --- | --- |
| LIFO 顺序（push 1..5，弹出） | `5 4 3 2 1` ✅ |
| capacity 增长（GCC） | `1 → 2 → 4 → 8 → 16 → 32` ✅ |
| 版2 `pop()` 返回被弹值 | `c b a` ✅ |
| 空栈 `pop()`（带检查版） | 捕获 `pop from empty stack` ✅ |
| `std::stack<int, vector<int>>` | `top=3, size=3` ✅ |

## 相关

- [[栈与队列的相互实现]] —— **同族笔记，分工明确**：那篇讲「栈与队列互转」（LIFO↔FIFO 的结构变换），本篇讲「栈本身的容器实现」。两篇共享同一个坑：`top()`/`back()` 取值 与 `pop()`/`pop_back()` 删除的**顺序不能颠倒**
- [[哈希表冲突解决与Dictionary底层]] —— 同样是「手搓容器」：那边的 `负载因子 → rehash 阈值`，与本篇的 `size → capacity 扩容` 是同一种「容量-增长」账本
- [[完全二叉树]] —— 数组连续存储的另一个经典案例：堆用 `2i / 2i+1` 定位父子，和本篇「用尾部下标代替指针」是同一思路的不同应用
