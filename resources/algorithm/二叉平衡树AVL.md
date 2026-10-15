---
title: "二叉平衡树AVL"
type: resource
category: 树与平衡树
tags: [算法, 数据结构, 面试, 二叉树, cpp]
created: "2026-08-31"
updated: "2026-10-15"
status: active
summary: "AVL 严格平衡定义（平衡因子 |bf| ≤ 1）、最小失衡子树、四型旋转 LL/RR/LR/RL（含 rotateLeft/Right 代码与 height 刷新顺序）、插入至多 1 次旋转 vs 删除最坏 O(log n) 次、斐波那契树高上界 1.44log₂n、与 BST/红黑树三方对比"
related: ["[[二叉搜索树BST]]", "[[红黑树]]", "[[递归与迭代转换]]"]
---

# 二叉平衡树 AVL

AVL = 两位发明者 **A**delson-**V**elsky 和 **L**andis（1962）。它是**第一种**自平衡二叉搜索树，也是理解红黑树的必经台阶。

学习顺序：**[[二叉搜索树BST]] → AVL → [[红黑树]]**。BST 会退化成链，AVL 用"严格平衡"堵住退化，红黑树则用"放宽标准 + 颜色记账"换取更少的旋转。

## 一、定义：严格平衡

对每个节点，记录**平衡因子**：

```
bf(node) = height(left) - height(right)      // 也常见 right - left，全局统一即可
AVL 要求：|bf| ≤ 1      即 bf ∈ { -1, 0, 1 }
```

空节点高度约定为 **0**，叶子高度为 **1**（`height = 1 + max(h(l), h(r))`）。

| 树 | 平衡标准 | 树高上界 |
|---|---|---|
| 普通 BST | 无 | O(n)（退化成链） |
| **AVL** | **\|bf\| ≤ 1（严格）** | **≈ 1.44·log₂n** |
| 红黑树 | 最长路径 ≤ 2×最短 | ≈ 2·log₂n |

## 二、旋转的前提：最小失衡子树

插入/删除一个节点后，**可能失衡的只有这条路径上的祖先**（其它子树高度没变）。

> **最小失衡子树** = 从操作点往上，**第一个** `|bf| > 1` 的节点。
> 只要把这棵子树转平，整棵树就重新平衡。

**插入时为什么只需转一次？**（关键论证）

```
插入 → 子树高度 +1（失衡）
旋转 → 子树高度回到"插入前"的值
     → 所有更上面的祖先 bf 恢复原样，不会继续失衡
```

所以 **AVL 插入至多 1 次旋转**（单旋或双旋）。
而**删除**时旋转可能让子树高度 **−1**，反而让祖先失衡 → 需要一路向上转，**最坏 O(log n) 次**。这是 AVL 相对红黑树最大的痛点（红黑树删除 ≤ 3 次）。

## 三、四型旋转（核心）

命名规则：两个字母 = **从失衡节点往下走两步的方向**。

| 型 | 形状 | 操作 | 记忆 |
|---|---|---|---|
| **LL** | 插到**左**孩子的**左**子树 | 对失衡节点 **右旋**（单旋） | 左边太长 → 往右扳 |
| **RR** | 插到**右**孩子的**右**子树 | 对失衡节点 **左旋**（单旋） | 右边太长 → 往左扳 |
| **LR** | 插到**左**孩子的**右**子树 | 先对**左孩子左旋** → 变 LL → 再对失衡节点**右旋** | 之字形，先"掰直" |
| **RL** | 插到**右**孩子的**左**子树 | 先对**右孩子右旋** → 变 RR → 再对失衡节点**左旋** | 之字形，先"掰直" |

**判断方法有两种，别混用**：

1. **插入时用 `val` 比方向**（简单直接，见第五节代码）；
2. **删除时用子节点的 bf 符号**（删除后没有"新插入值"可比，只能看结构）：

```
bf(root) > 1 且 bf(root->left) >= 0  → LL，直接右旋
bf(root) > 1 且 bf(root->left) <  0  → LR，先左旋左孩子再右旋
bf(root) < -1 且 bf(root->right) <= 0 → RR，直接左旋
bf(root) < -1 且 bf(root->right) >  0 → RL，先右旋右孩子再左旋
```

> ⚠️ 与红黑树同理：**旋转永远看结构（一字 / 之字），不看"记账字段"**。AVL 的记账字段是 `int height`，红黑树是 `bool color`，它们只负责"告诉你要不要转、转完要不要再往上"，不决定怎么转。

## 四、旋转代码

```cpp
struct AVLNode {
    int val, height;
    AVLNode *left, *right;
    AVLNode(int v) : val(v), height(1), left(nullptr), right(nullptr) {}
};

int h(AVLNode* n)  { return n ? n->height : 0; }              // 空节点高 0
int bf(AVLNode* n) { return n ? h(n->left) - h(n->right) : 0; }

void update(AVLNode* n) {                                     // 刷新高度
    n->height = 1 + std::max(h(n->left), h(n->right));
}

// LL：y 失衡（左重），左孩子 x 上位，x 的原右孩子 t2 过继给 y 当左孩子
AVLNode* rotateRight(AVLNode* y) {
    AVLNode* x  = y->left;
    AVLNode* t2 = x->right;
    x->right = y;
    y->left  = t2;
    update(y);  update(x);        // ★ 先更新儿子，再更新新根（顺序反了高度就错）
    return x;                     // ★ 返回新的子树根
}

// RR：镜像
AVLNode* rotateLeft(AVLNode* x) {
    AVLNode* y  = x->right;
    AVLNode* t2 = y->left;
    y->left  = x;
    x->right = t2;
    update(x);  update(y);
    return y;
}
```

**旋转的三条铁律**：

1. 只动**三个指针**（`x->right`、`y->left` 与父节点指向新根），**不动 val**；
2. `update` 顺序必须**从下往上**（先旧根，再新根）；
3. **必须 `return` 新根**，且调用处接住（`root = rotateRight(root)`）—— 与 BST 那条铁律完全一致（📎 [[递归与迭代转换]]）。

## 五、插入（BST 插入 + 回溯修平衡）

```cpp
AVLNode* insert(AVLNode* root, int val) {
    if (!root) return new AVLNode(val);                 // ① 标准 BST 插入
    if      (val < root->val) root->left  = insert(root->left,  val);
    else if (val > root->val) root->right = insert(root->right, val);
    else                      return root;              // 重复值：忽略（策略需全局一致）

    update(root);                                       // ② 回溯时刷新高度
    int b = bf(root);                                   // ③ 算平衡因子

    if (b >  1 && val < root->left->val)  return rotateRight(root);            // LL
    if (b < -1 && val > root->right->val) return rotateLeft(root);             // RR
    if (b >  1 && val > root->left->val) {                                     // LR
        root->left = rotateLeft(root->left);
        return rotateRight(root);
    }
    if (b < -1 && val < root->right->val) {                                    // RL
        root->right = rotateRight(root->right);
        return rotateLeft(root);
    }
    return root;                                        // ④ 没失衡原样返回
}
```

**执行顺序永远是：插入 → 更新高度 → 算 bf → 分型旋转**。高度没刷新就判 bf，等于拿旧账算新账。

## 六、删除（比插入麻烦）

```cpp
AVLNode* remove(AVLNode* root, int key) {
    if (!root) return nullptr;
    if      (key < root->val) root->left  = remove(root->left,  key);
    else if (key > root->val) root->right = remove(root->right, key);
    else {                                        // 命中：同 BST 三情况
        if (!root->left)  { AVLNode* r = root->right; delete root; return r; }
        if (!root->right) { AVLNode* l = root->left;  delete root; return l; }
        AVLNode* succ = root->right;              // 双子：中序后继值替换
        while (succ->left) succ = succ->left;
        root->val   = succ->val;
        root->right = remove(root->right, succ->val);
    }

    update(root);
    int b = bf(root);
    if (b > 1) {                                  // 左重：看左孩子的 bf 判 LL / LR
        if (bf(root->left) < 0) root->left = rotateLeft(root->left);
        return rotateRight(root);
    }
    if (b < -1) {                                 // 右重：镜像判 RR / RL
        if (bf(root->right) > 0) root->right = rotateRight(root->right);
        return rotateLeft(root);
    }
    return root;                                  // ★ 注意：这里仍然可能要往上继续转
}
```

> ⚠️ **与插入的本质差异**：删除后旋转可能使子树高度 −1，**更上面的祖先会接着失衡** —— 所以每层 `remove` 返回前都要再判再转，**最坏要转 O(log n) 次**。这就是"AVL 查快改慢"的根源。

## 七、复杂度与树高上界

令 `N(h)` = 高度为 h 的 AVL 树的**最少节点数**。为了最省节点，一棵子树高 h−1、另一棵高 h−2（两边差 1，刚好踩在平衡边界上）：

```
N(h) = 1 + N(h-1) + N(h-2)，  N(0)=1， N(1)=2
```

这正是**斐波那契递推**，解得 `N(h) = F(h+3) − 1`，而 `F(k) ≈ φ^k / √5`（φ ≈ 1.618）：

```
n ≥ N(h) ≈ φ^h / √5   ⟹   h ≤ 1.44·log₂(n+2) − 0.328   ≈ 1.44·log₂n
```

**结论**：AVL 树高 ≤ 约 1.44 log₂n，比红黑树的 2 log₂n 更矮 → **查询更快**，但维护更贵。

| 操作 | AVL | 说明 |
|---|---|---|
| 查找 | **O(log n)**，常数比红黑树小 | 树更矮 |
| 插入 | O(log n)，**至多 1 次旋转** | 但沿途要刷新 height |
| 删除 | O(log n)，**最坏 O(log n) 次旋转** ⚠️ | 痛点 |
| 空间 | 每节点多一个 `int height` | 红黑树只需 1 bit color |

## 八、三者对比（纠错备忘）

| | 普通 BST | **AVL** | 红黑树 |
|---|---|---|---|
| 平衡标准 | 无 | **\|bf\| ≤ 1** | 最长 ≤ 2×最短 |
| 树高上界 | O(n) | **1.44 log₂n** | 2 log₂n |
| 记账字段 | 无 | `int height` | `bool color` |
| 插入旋转 | 0 | **至多 1 次** | 0~2 次 |
| 删除旋转 | 0 | **最坏 O(log n) 次** ⚠️ | **≤ 3 次** |
| 查询 | O(n) 最坏 | **最快** | 略慢 |
| 适用场景 | 教学 | **查多改少**（如字典/索引，一次建好反复查） | **改多查少**（通用容器首选） |

> ⚠️ **常见误传纠正**："AVL 插入要 O(log n) 次旋转"是错的 —— O(log n) 那部分是**沿途 height 的刷新**，不是旋转次数。**AVL 插入至多 1 次旋转**；真正的痛点在**删除**。
> 反过来说，红黑树的收益是：**无违规时插入零维护**（父黑直接结束）+ 旋转次数封顶。

## 九、面试常考

| 题 | 思路 |
|---|---|
| 判断一棵树是否为 AVL | **后序**求高度 + 判 bf，自底向上 O(n)；⚠️ 不能只查根，必须每个节点都判 |
| AVL 树高上界推导 | 斐波那契递推 `N(h)=1+N(h-1)+N(h-2)` → 1.44 log₂n |
| 手写 LL / RR / LR / RL | 记住"**之字先掰直**：LR = 左旋左孩子 + 右旋自己" |
| AVL vs 红黑树怎么选 | 查多改少 → AVL；改多查少 → 红黑树（`std::map` 就是后者） |
| 插入后旋转次数 | 至多 1 次（旋转后子树高度复原，祖先不再失衡） |

## 十、TODO

- **`AVL.cpp` 重搓**：原 `D:\CppTest\hello\Tree\AVL.cpp` 已随环境清理丢失，需重建（`AVLNode` + `h/bf/update` + `rotateLeft/Right` + `insert` 四型 + `remove`）
  - 验证方式：按 `1,2,3,4,5,6,7` 递增插入（最坏场景）→ 中序遍历应为升序，且树高应为 3 而非 7
  - 实现顺序：`insert` 递归骨架 → `rotateRight/Left` → 四型判定 → `remove`

---

**一句话记忆**：**BST 会退化 → AVL 要求 |bf| ≤ 1 → 失衡找最小那棵 → 一字单旋、之字双旋（先掰直）→ 旋转后先刷儿子再刷新根、必须 return 新根 → 插入至多转 1 次，删除最坏转 O(log n) 次。**

**理解捷径**：把 AVL 想成"**强迫症版 BST**"—— 只要左右差超过 1 层就立刻扳平，代价是每次改动都要沿路重新量身高（`update`）。红黑树则是"**差不多就行版**"，允许 2 倍差距，换来少转几次。
