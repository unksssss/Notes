---
title: "DFS 与 BFS 遍历算法"
type: resource
tags: [算法, 面试, 数据结构]
created: "2026-07-31"
updated: "2026-07-31"
status: active
summary: "DFS（深度优先）与 BFS（广度优先）对比：数据结构、应用场景、复杂度；BFS 无权图最短路径、DFS 环检测"
---

# DFS 与 BFS 遍历算法

## 对比总览

| | DFS（深度优先） | BFS（广度优先） |
|---|---|---|
| 数据结构 | 递归 / 显式栈 | 队列 |
| 遍历方式 | 一头扎到底再回溯 | 一层一层扫 |
| 典型应用 | **环检测**、拓扑排序、连通分量、回溯/迷宫 | **无权图最短路径**、层序遍历、状态扩散 |
| 时间复杂度 | O(V+E) | O(V+E) |
| 空间复杂度 | 最坏 O(V)（递归深度，通常更省） | 最坏 O(V)（队列） |

> 记忆口诀：**BFS = 队列 + 一层一层扫 → 最短路径；DFS = 递归/栈 + 一头扎到底 → 环检测**

## 细节要点（面试）

1. DFS 递归写法（系统栈）和显式栈写法**都行**，递归更常见
2. 有向图环检测需**三色标记**（白=未访问 / 灰=访问中 / 黑=完成）——DFS 回边指向"灰"节点即有环
3. BFS 求无权图最短路径：**第一次到达某节点即为最短**（层数即距离）
4. BFS 可配合 visited 数组防重复入队

## 典型代码骨架

```csharp
// BFS（队列）
void BFS(Graph g, int start)
{
    Queue<int> q = new Queue<int>();
    bool[] visited = new bool[g.N];
    q.Enqueue(start); visited[start] = true;
    while (q.Count > 0)
    {
        int v = q.Dequeue();
        foreach (int w in g.Adj(v))
            if (!visited[w]) { visited[w] = true; q.Enqueue(w); }
    }
}

// DFS（递归）
void DFS(Graph g, int v, bool[] visited)
{
    visited[v] = true;
    foreach (int w in g.Adj(v))
        if (!visited[w]) DFS(g, w, visited);
}
```

## 相关

- [[哈希表冲突解决与Dictionary底层]]
