---
title: "TMP Text 零分配更新"
type: resource
tags: [UGUI, 性能优化, 项目实战]
created: "2026-08-06"
updated: "2026-08-06"
status: active
summary: "TextMeshPro 零 GC 分配更新文本：SetText 格式化重载直接写入内部字符缓冲、SetCharArray；避开 string 拼接与 Format 的堆分配"
---

# TMP Text 零分配更新

## 问题根源

`text.text = "血量: " + hp;` 每次赋值都有多处堆分配：

1. `+` 拼接产生新的临时 string（string 不可变，每拼一次 new 一个）
2. TMP 内部把 string 转成字符数组再排版

FPS/计时器每秒/每帧刷新 → 成百上千个垃圾 string → GC 压力 → VR 掉帧头晕。

## 核心方案：SetText 格式化重载

```csharp
text.text = "FPS: " + fps;      // ❌ 有分配
text.SetText("FPS: {0}", fps);   // ✅ 零分配！
```

TMP 提供专为数值设计的重载（float/int/double 等，最多 3 个参数）：

- `SetText(string, float)`
- `SetText(string, int)`
- `SetText(string, float, float, float)` …

**原理**：`{0}` 占位符 + 数值走 TMP 内部自有的数字→字符转换（查表拆字符），**直接写入 TMP 内部字符缓冲区**，再触发网格重建。全程不经过托管堆 string。

## 其他方案

| 方案 | 用法 | 适用 |
|---|---|---|
| SetText 格式化重载 | `SetText("进度:{0}%", pct)` | ⭐ 首选，数值场景 |
| SetCharArray | `SetCharArray(char[], start, len)` | 数据本身就是字符数组 |
| SetText(StringBuilder) | `text.SetText(sb)` | 已有 SB 的场景 |

## ⚠️ 两个坑

1. **格式串必须是编译期常量**：`"FPS: {0}"` 这种字面量常驻字符串常量池，不会每帧分配；如果是动态拼出来的格式串，格式串本身就有分配
2. **float 精度陷阱**：TMP 内部对 float 的自有格式化会截断精度（默认小数位有限）。要显示固定两位小数时，先转 int（如毫秒）再传更可控

## 补充：普通字符串拼接的性能对比

| 写法 | 分配情况 |
|---|---|
| `a + b + c` | 每次 + 都 new 中间 string，n 次拼接 O(n) 次分配 ❌ |
| `string.Format` | 有格式化开销 + 分配 ❌ |
| `StringBuilder` 复用（Clear+Append+ToString） | 只有最后 ToString 一次分配 ✅ |
| 每帧 new StringBuilder | 比不用还差 ❌（白造对象） |

## 相关

- [[UGUI布局系统与强制刷新]]
- [[Unity 渲染批处理体系]]
