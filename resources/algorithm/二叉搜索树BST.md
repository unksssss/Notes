---
title: "二叉搜索树BST"
type: resource
tags: [算法, 数据结构, 面试, 二叉树, cpp]
created: "2026-08-21"
updated: "2026-10-15"
status: active
summary: "BST 性质（中序遍历=升序）、递归插入/查找/删除三情况（双子用中序后继值替换）、三个实现踩坑、退化 O(n) 引出 AVL/红黑树"
related: ["[[红黑树]]", "[[DFS与BFS遍历算法]]", "[[递归与迭代转换]]"]
---

# 二叉搜索树 BST

## 一、性质

对任意节点，满足 **左子树所有值 < 根 < 右子树所有值**（递归成立）。

由此得到最重要的推论：

> **BST 的中序遍历结果 = 升序序列。**

这条是 BST 相关算法题的万能钥匙 —— 判定是否 BST、求第 k 小、恢复错误 BST、转成有序双向链表，全靠它（📎 [[DFS与BFS遍历算法]]）。

## 二、查找

```cpp
BSTNode* search(BSTNode* root, int key)
{
    if (!root || root->val == key) return root;
    return key < root->val ? search(root->left, key)
                           : search(root->right, key);
}
```

每个节点只比一次，走一条从根到叶的路径 → 时间复杂度 **O(h)**（h = 树高）。

## 三、插入

```cpp
BSTNode* insert(BSTNode* root, int val)
{
    if (root == nullptr)
        return new BSTNode(val);              // ★ 空位即插入点
    if (val > root->val)
        root->right = insert(root->right, val);
    else if (val < root->val)
        root->left  = insert(root->left,  val);
    return root;                              // ★ 每层必须 return
}
```

**要点**：
- 新节点**一定插在叶子位置**（先查到 nullptr，再 `new`）；
- `root->left = insert(...)` 这种**接住返回值**的写法是关键 —— 不接就丢子树；
- 重复值策略：忽略 / 计数 / 统一放右（需全局一致，否则查找会漏）。

## 四、删除（三种情况）

```cpp
BSTNode* remove(BSTNode* root, int key)
{
    if (!root) return nullptr;

    if (key < root->val)      root->left  = remove(root->left, key);
    else if (key > root->val) root->right = remove(root->right, key);
    else {                                   // 命中，分三种情况
        // ① 叶子 / ② 单孩子：用非空的那支顶替（叶子时两支都空 → 返回 nullptr）
        if (!root->left)  { BSTNode* r = root->right; delete root; return r; }
        if (!root->right) { BSTNode* l = root->left;  delete root; return l; }

        // ③ 双子：找【中序后继】（右子树最小节点）值替换，再递归删掉它
        BSTNode* succ = root->right;
        while (succ->left) succ = succ->left;
        root->val   = succ->val;                        // 只换值，不动结构
        root->right = remove(root->right, succ->val);   // 归约成 ①②
    }
    return root;                              // ★ 每层必须 return root
}
```

**三种情况归纳**：

| 情况 | 处理 |
|---|---|
| ① 叶子 | 直接删，返回 `nullptr` |
| ② 单孩子 | 用那支孩子顶替自己 |
| ③ 双子 | **中序后继值替换 + 递归删后继** —— 把 ③ 归约成 ①② |

**为什么用中序后继而不是前驱？** 两者都能保持 BST 性质（后继是右子树最小，替换后仍 > 左子树全部；前驱同理）。习惯上取**右子树最左节点**。

## 五、三个实现踩坑（真实踩过）

```cpp
// ① 野指针：声明未初始化 → 解引用输出垃圾值（0/-152758512）
BSTNode* root = nullptr;          // ★ 必须显式初始化

// ② remove 递归返回值没接住 → 子树丢失 / 悬垂指针
root->right = remove(root->right, key);   // 漏掉赋值 = 删完节点整支丢失

// ③ if / else if 分支缺 return root → 编译警告 + 返回值垃圾
```

> **铁律**：递归函数**每一条路径都要 `return`**，且调用处必须**接住返回值**。这条和 [[递归与迭代转换]] 里的经验一致 —— 凡是"可能改变子树头"的操作，都要 `xxx = f(xxx, ...)`。
> 编译警告（`-Wall`）提示 missing return 时别忽略。

## 六、退化问题（为什么需要平衡树）

BST 的形态**完全取决于插入顺序**：

```
按 1,2,3,4,5 递增插入  →  退化成单链表，h = n，查找 O(n)
随机顺序插入            →  树高约 O(log n)
```

所以**普通 BST 不保证 O(log n)** —— 这就是 AVL 树和红黑树存在的理由（📎 [[红黑树]]）。

## 七、复杂度

| 操作 | 平均 / 平衡时 | 最坏（退化成链） |
|---|---|---|
| 查找 | O(log n) | **O(n)** |
| 插入 | O(log n) | **O(n)** |
| 删除 | O(log n) | **O(n)** |
| 中序遍历 | O(n) | O(n) |

空间 O(n) 存节点，递归栈 O(h)。

## 八、面试常考

| 题 | 思路 |
|---|---|
| 验证二叉搜索树 | 中序遍历应为**严格升序**（⚠️ 不能只比较父子，要带上下界递归） |
| BST 第 k 小元素 | 中序遍历第 k 个 |
| BST 的最近公共祖先 | 利用有序性：都在左 → 往左；都在右 → 往右；分叉 → 当前节点 |
| 二叉搜索树转双向链表 | 中序遍历 + 调整指针 |
| 恢复错误的 BST（两节点交换） | 中序序列找两处逆序对 |

---

**一句话记忆**：**BST = 中序有序**；插入查空位、删除三情况、双子用后继；递归**每层都要 return 且调用处要接住**；顺序插入会退化成链 → 需要平衡树。
