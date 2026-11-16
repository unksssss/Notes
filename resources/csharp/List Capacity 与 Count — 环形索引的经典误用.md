---
title: "List Capacity 与 Count — 环形索引的经典误用"
type: resource
tags: [csharp, 集合, 面试, 陷阱, unity]
category: csharp
created: "2026-11-16"
updated: "2026-11-16"
status: active
summary: "`List<T>.Capacity` 是内部数组长度，`Count` 才是元素个数，两者只在「刚好填满」时相等。环形索引 `(i + dir + n) % n` 里的 n 必须是 Count —— 用 Capacity 当模数，在 Count < Capacity（Add 过又 Remove 过）时算出越界下标，直接 ArgumentOutOfRangeException。另附：扩容 2 倍复制、RemoveAt(0) 是 O(n) 的搬移、Contains 是 O(n) 线性扫描、`new List<T>(2)` 只预设容量不预设元素。"
---

# List Capacity 与 Count — 环形索引的经典误用

## 一、两个数字，含义完全不同

```csharp
var list = new List<int>(2);   // Count = 0, Capacity = 2
list.Add(10);                  // Count = 1, Capacity = 2
list.Add(20);                  // Count = 2, Capacity = 2
list.Add(30);                  // Count = 3, Capacity = 4（内部数组翻倍）
list.RemoveAt(0);              // Count = 2, Capacity = 4  ← 容量不缩
```

| 属性 | 含义 | 谁在用 |
| --- | --- | --- |
| `Count` | 实际元素个数，**逻辑长度** | 所有业务代码 |
| `Capacity` | 内部 `T[] _items` 的数组长度 | 只有 `EnsureCapacity` / 序列化 |

`Capacity` 只会增不会自动减（`TrimExcess()` 才会收缩），所以它是个**只增不减的分配水位**，和「现在有几个元素」没有关系。二者仅在「数组刚好填满」时数值相等 —— 这种巧合让 bug 能通过手工测试。

---

## 二、环形索引：模数只能是 Count

切武器 / 切页签 / 轮播图都长这样：

```csharp
// ❌ 今日踩的写法（Player.SwitchPlayerWeapon）
currWeaponIndex = (currWeaponIndex + value + currWeaponLst.Capacity) % currWeaponLst.Capacity;
EquipWeapon(currWeaponLst[currWeaponIndex]);
```

`currWeaponLst` 是 `new List<WeaponItem>(2)`，玩家只捡了 1 把枪时 `Count == 1`、`Capacity == 2`：

```
index = (0 + 1 + 2) % 2 = 1
currWeaponLst[1] → ArgumentOutOfRangeException
```

**捡一把枪后按滚轮切枪，必崩。** 全程没有数组越界的「可疑感」，因为 2 这个数字看着就是「武器槽数量」—— 它其实是构造函数的容量提示，不是槽位数。

```csharp
// ✅ 正确
int n = currWeaponLst.Count;
if (n == 0) return;                      // 空表必须先挡，% 0 会抛 DivideByZeroException
currWeaponIndex = (currWeaponIndex + value % n + n) % n;   // value 可能 > n，先取一次模
EquipWeapon(currWeaponLst[currWeaponIndex]);
```

三个细节：

1. **`% n` 只对负数不友好**：C# 的 `%` 保留被除数符号，`(-1) % 3 == -1`，所以要先 `+ n` 再 `% n`（或 `((x % n) + n) % n`）。
2. **`value` 要先归一**：`value = 5`、`n = 2` 时 `(0 + 5 + 2) % 2 = 1` 是对的，但 `value` 是 `-7` 时 `(0 - 7 + 2) % 2 = -1` 又负了 → 先 `value %= n` 更稳。
3. **`n == 0` 必须单独挡**，`% 0` 抛的是 `DivideByZeroException` 而不是下标异常，堆栈会指向一行看不出问题的表达式。

---

## 三、`Capacity` 相关的另外三个坑

### 1. `new List<T>(2)` 不创建元素

```csharp
var l = new List<int>(2);
Debug.Log(l.Count);      // 0，不是 2
// l[0] = 5;             // ArgumentOutOfRangeException
```

预设容量只是「避免第一次扩容」，元素还是要 `Add`。想把「固定 2 个槽位」表达成数据结构，应该用数组 `WeaponItem[2]` 或自己包一个 `WeaponSlot` 结构，而不是靠 `List` 的 Capacity 暗示。

### 2. `RemoveAt(0)` 是 O(n)

`List` 内部是连续数组，删头部要把后面全部元素左移一位。对象池用 `bullsts.RemoveAt(0)` 取元素，池子越大越慢 —— 换 `Stack<T>` / `Queue<T>`，或维护一个 `_cursor` 索引（把「已取走」的挪到尾部再 `RemoveAt(Count-1)`）。

```csharp
// ❌ O(n) 搬移
GameObject go = pool[0];
pool.RemoveAt(0);

// ✅ 尾部交换后弹尾，O(1)
int last = pool.Count - 1;
GameObject go = pool[last];
pool[last] = pool[0];
pool.RemoveAt(last);
```

### 3. `Contains` 也是 O(n)，且在循环里用会变 O(n²)

```csharp
// 每帧对每个武器做一次线性查找 → n 把枪 = n² 次比较
if (!AreaWeaponItems.Contains(item)) AreaWeaponItems.Add(item);
```

元素少（< 10）时无所谓；上百个就该换 `HashSet<T>`，或让 `WeaponItem` 自己持一个 `bool inArea` 标志位做 O(1) 判定。

---

## 四、今日 bug 完整链路（2026-11-16）

| 位置 | 写法 | 触发条件 | 现象 |
| --- | --- | --- | --- |
| `Player.SwitchPlayerWeapon` | `% currWeaponLst.Capacity` | 只捡 1 把枪时按滚轮 | `ArgumentOutOfRangeException`，切枪必崩 |
| `Player.AddCurrWeapon` | `Count == 2` 才替换 | 正常（这条是对的） | — |
| `Player.GetWeapon` | `switch (currWeaponIndex) { case 0: currWeaponIndex = 0; }` | 任何时候 | 自赋值死代码，看着像漏了逻辑 |

同一段代码里 `Count`（`AddCurrWeapon` 的判据）和 `Capacity`（取模的模数）混用，是「一个集合两种长度」最典型的翻车现场。

---

## 五、面试延伸

1. **`List<T>` 扩容为什么是 2 倍？** 摊还 O(1)：容量翻倍时总复制代价 `1+2+4+…+n < 2n`，均摊到每次 `Add` 是常数；若每次 +1 就退化成 O(n²)。
2. **`Capacity` 能不能设小？** `Capacity = 0` 合法，会把内部数组换成 `Array.Empty<T>()`；但 `Capacity` 不能小于 `Count`（抛 `ArgumentOutOfRangeException`）。
3. **`RemoveAt` 与 `Remove` 的复杂度？** 都是 O(n)：`Remove` 先 `IndexOf`（O(n)）再搬移（O(n)）；`RemoveAt(Count-1)` 是唯一 O(1) 的删除。
4. **`List<T>` 和 `T[]` 怎么选？** 长度固定用数组（无扩容、可 `stackalloc`、能 `fixed`）；长度变化用 `List`。Unity 里每帧 `new List<T>()` 会持续产生 GC，热路径应复用同一个实例 + `Clear()`。

---

## 相关

- [[C# foreach 与枚举器零分配]] —— `List<T>.Enumerator` 是 struct，`foreach` 不装箱；`Capacity` 变化会让枚举器失效
- [[哈希表冲突解决与Dictionary底层]] —— 需要 O(1) 查找时的替代方案
- [[对象池实现]] —— 池子的取/还结构选择（`Stack` vs `List.RemoveAt(0)`）
- [[struct 装箱陷阱与值类型原理]] —— 另一个「看着是值、实际是引用语义」的坑
