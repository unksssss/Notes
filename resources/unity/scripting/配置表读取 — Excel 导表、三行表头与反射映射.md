---
title: "配置表读取 — Excel 导表、三行表头与反射映射"
type: resource
tags: [unity, 配置表, 反射, 数据驱动, 项目实战]
created: "2026-11-02"
updated: "2026-11-03"
status: active
summary: "策划表 → 中间格式 → 运行时的完整链路；三行表头（类型/注释/数据）必须跳过；Excel 存的 SpreadsheetML 不是通用 XML；XmlDocument 五个必踩坑（FirstChild 是声明节点、GetFields 拿不到属性、SetValue 不做类型转换、Add 位置错、Load 路径只在 Editor 成立）；JsonUtility 三条硬限制；JSON 路线 JsonParser<T>+JsonManager<T> 两层实现与 8 条边界；反射按名字逐字匹配，Bulletname vs BulletName 静默失败"
---

# 配置表读取 — Excel 导表、三行表头与反射映射

## 为什么配表

数值（射速、射程、弹夹容量、伤害）不写死在代码里，策划改 Excel 就能改游戏，不用等程序编译打包。

链路固定三段：

```
策划 Excel（游戏配表/WeaponsGun.xlsx）
  ↓ 导表工具（Excel 另存 / 自写脚本）
中间格式（CSV / XML / JSON / 二进制）
  ↓ 丢进 Assets/StreamingAssets/
运行时解析（XMLReader / JsonUtility / Newtonsoft）
  ↓ 反射映射到 C# 类
List<WeaponConfig>
```

**中间格式选型**：CSV 最简单但没法表达嵌套；JSON 生态最好、能表达嵌套；二进制（Protobuf/自研）最快但不可读、没法手改。手游通常选 **二进制 + 一份 JSON 供调试**。

---

## 坑 1：三行表头不是数据

导表工具最普遍的产物结构是 **前三行都是表头**：

| 行 | 内容 | 用途 |
|---|---|---|
| 第 1 行 | `ID` / `WeaponName` / `FireRate` … | **字段名**（反射按它匹配） |
| 第 2 行 | `int` / `string` / `int` … | **类型**（导表工具用来生成强类型代码） |
| 第 3 行 | `武器ID` / `武器名称` / `射速` … | **中文注释**（给策划看） |
| 第 4 行起 | `1001` / `AK47` / `300` … | 真数据 |

实际导出的 JSON 把三行都当数据塞进数组：

```json
{"Sheet1":[
  {"ID":"int","WeaponName":"string", ...},
  {"ID":"武器ID","WeaponName":"武器名称", ...},
  {"ID":1001,"WeaponName":"AK47","FireRate":300, ...}
]}
```

**解析时必须跳过前两条**，否则 `"int"` 会被当成一个 ID。而且第一行第二行里所有值都是字符串，第三行开始才混入数字 → **同一个字段在不同行是不同类型**，强类型反序列化必然炸。

> 更稳的导表：**表头单独抽出去**，导出的数据文件只含数据行；或者导出成 `{"schema":[...],"rows":[...]}` 两段式。让运行时不用猜。

---

## 坑 2：Excel 存的 XML 不是你想的那种 XML

Excel「另存为 XML 表格 2003」产出的是 **SpreadsheetML**，结构长这样：

```xml
<?xml version="1.0"?>
<?mso-application progid="Excel.Sheet"?>
<Workbook xmlns="urn:schemas-microsoft-com:office:spreadsheet" ...>
  <DocumentProperties>...</DocumentProperties>
  <Styles>...</Styles>
  <Worksheet ss:Name="Sheet1">
    <Table>
      <Row>
        <Cell><Data ss:Type="String">ID</Data></Cell>
        <Cell><Data ss:Type="String">WeaponName</Data></Cell>
      </Row>
      ...
    </Table>
  </Worksheet>
</Workbook>
```

两个致命差异：

1. **字段名不是元素名**。字段名在 `<Data>` 的**文本内容**里，元素名永远是 `Cell` / `Data`。所以「拿 `content.Name` 和字段名比」永远不相等。
2. **根元素下挂的是元数据**。`Workbook` 的直接子节点是 `DocumentProperties` / `OfficeDocumentSettings` / `ExcelWorkbook` / `Styles` / `Worksheet` —— 真正的表格藏在 `Worksheet/Table/Row` 里，深度是 4 层。

结论：**SpreadsheetML 只能当「能打开」用，不能当数据交换格式**。要 XML 就自己按 `<root><item><field>v</field></item></root>` 的结构导。

---

## 坑 3：XmlDocument 五个必踩点

```csharp
private XmlDocument xd = new XmlDocument();

public List<T> Read<T>(string path) where T : new()
{
    xd.Load(path);

    Type type = typeof(T);
    var fields = type.GetFields();          // ❌ 坑 2

    XmlNode rootNode = xd.FirstChild;        // ❌ 坑 1
    XmlNodeList childNodes = rootNode.ChildNodes;

    List<T> TList = new List<T>();
    foreach (XmlNode node in childNodes)
    {
        T obj = new T();
        foreach (XmlNode content in node.ChildNodes)
        {
            foreach (var fieldInfo in fields)
            {
                if (fieldInfo.Name == content.Name)
                    fieldInfo.SetValue(obj, content.InnerText.ToString());  // ❌ 坑 3
            }
            TList.Add(obj);                  // ❌ 坑 4
        }
    }
    return TList;
}
```

### 坑 1：`FirstChild` 是 XML 声明，不是根元素

文件第一行有 `<?xml version="1.0"?>` 时，`XmlDocument.FirstChild` 返回的是 **XmlDeclaration 节点**（它也是 XmlNode 的一种），它的 `ChildNodes` 是**空的** → 循环一次都不进 → **静默返回空 List**，不报错。

```csharp
// ✅ 三种正确写法
XmlNode root = xd.DocumentElement;                 // 最直接
XmlNode root = xd.SelectSingleNode("/Workbook");   // 按名取
XmlNode root = xd.LastChild;                       // 能跑但语义错，别用
```

### 坑 2：`GetFields()` 拿不到自动属性

`WeaponConfig` 里全是 `{get;set;}` 自动属性：

```csharp
public class WeaponConfig
{
    public int ID { get; set; }          // 属性（Property）
    public string WeaponName { get; set; }
}
```

`Type.GetFields()` 默认只返回 **public 字段（Field）**，属性是 `GetProperties()`。→ `fields` 长度 0 → **一个值都填不上**，而且不报错。

```csharp
// ✅ 属性用 GetProperties
var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
// ✅ 字段用 GetFields（注意要加 Instance，默认有，但显式写更安全）
var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
```

> 想一套代码同时兼容字段和属性，就两个都查，或者**统一规定配置类只用 public 字段**。

### 坑 3：`SetValue` 不做类型转换

`content.InnerText` 永远是 `string`。往 `int` 属性里塞字符串 → `ArgumentException: Object of type 'System.String' cannot be converted to type 'System.Int32'`。

```csharp
// ✅ 先转成目标类型
object raw = content.InnerText;
Type t = prop.PropertyType;
object value = (t == typeof(string))
    ? raw
    : Convert.ChangeType(raw, t);        // int / float / bool / enum 都吃
prop.SetValue(obj, value);

// enum 要单独处理（Convert.ChangeType 不吃字符串→enum）
if (t.IsEnum) value = Enum.Parse(t, raw.ToString());
```

### 坑 4：`Add` 放错层

`TList.Add(obj)` 写在**遍历子节点的内层循环**里 → 一个对象被 Add 了「子节点个数」次（本例 11 个字段 = 11 份重复）。必须提到 `foreach (content)` **外面**、`foreach (node)` 里面。

这是典型的「缩进对了、层级错了」——**编译通过、运行不报错、数据量悄悄 ×11**。

### 坑 5：`Load(path)` 只在 Editor 成立

```csharp
xd.Load("Assets/StreamingAssets/WeaponGun.xml");   // 仅 Editor 相对路径能跑
```

- **Android**：StreamingAssets 在 apk 的 assets 里，是 **jar 内路径**，`File.ReadAllText` 读不到；
- **WebGL**：是 URL，必须走 HTTP；
- **iOS/macOS**：路径也不同。

```csharp
// ✅ 跨平台唯一正解：UnityWebRequest
string url = Path.Combine(Application.streamingAssetsPath, "WeaponGun.xml");
using var req = UnityWebRequest.Get(url);
await req.SendWebRequest();
if (req.result != UnityWebRequest.Result.Success) { /* 报错分支必须有 */ }
xd.LoadXml(req.downloadHandler.text);   // 注意是 LoadXml 不是 Load
```

> `Application.streamingAssetsPath` 在 Android 上返回的是 `jar:file://...` 形式，**只有 UnityWebRequest 能处理**。

---

## 坑 4：JsonUtility 的三条硬限制

JSON 路线比 XML 舒服，但 Unity 自带的 `JsonUtility` 限制很硬：

| 限制 | 表现 | 绕过 |
|---|---|---|
| **顶层必须是对象**，不能是数组 | `[{...},{...}]` 直接失败 | 包一层 `{"rows":[...]}` 或 `{"Sheet1":[...]}` |
| **不支持 Dictionary** | 字段被忽略 | 改成 `List<KV>` 或换 Newtonsoft |
| **类型必须严格匹配** | `"WeaponType":"RF"` 往 `int` 字段塞 → 整条失败/字段为 0 | 字段类型跟表里实际类型对齐，或自定义 `ISerializationCallbackReceiver` |
| **字段名逐字匹配** | `Bulletname` vs `BulletName` → 静默为 null | 见下节 |
| **只序列化 public 字段** | 属性不参与 | 用 public 字段或 `[SerializeField]` |

本项目实际数据里 `WeaponType` 是 `"RF"`（字符串），但表头类型行写的是 `int` → **表本身自相矛盾**。这种时候要么改表（`WeaponType` 定义成 string 或 enum），要么字段用 string 再自己解析。

> 需要 Dictionary / 混合类型 / 顶层数组 → 上 **Newtonsoft.Json**（`com.unity.nuget.newtonsoft-json`，Unity 官方包），别跟 JsonUtility 死磕。

---

## 坑 5：反射按名字**逐字**匹配，大小写不同就静默失败

```csharp
// 配置表里叫 Bulletname（小写 n）
public string BulletName { get; set; }   // 代码里大写 N
```

反射用 `==` 比字符串 → **大小写敏感** → 该字段永远拿不到值，**不报错、不警告**，你只会看到「子弹名字是空的」。

防御三件套：

```csharp
// 1. 不区分大小写匹配（容错但不治本，可能撞名）
if (string.Equals(name, prop.Name, StringComparison.OrdinalIgnoreCase))

// 2. 启动时校验覆盖率：表头字段必须全被消费，否则打 Error
var mapped = props.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
var missing = headers.Where(h => !mapped.Contains(h)).ToList();
if (missing.Count > 0) Debug.LogError($"配置表有未映射字段: {string.Join(",", missing)}");

// 3. 终极方案：导表时直接生成 C# 类 / 常量
```

**第 3 条才是正解**：导表工具顺手生成

```csharp
public static class WeaponTable
{
    public const int AK47_ID = 1001;
    public const int AK47_FireRate = 300;
    ...
}
```

或者生成二进制 + 生成的 Reader 代码。**运行时零反射**——反射慢、裁剪（IL2CPP/AOT）下还可能被裁掉、字段改名编译期抓不到。反射只适合做**调试期工具**，不适合做正式数据管线。

---

## JSON 路线：`JsonParser<T>` + `JsonManager<T>` 索引层

XML 走不通（五个坑见上）之后改的路线，实测一天跑通到 UI。分两层：**解析层**（JSON → `List<T>`）和**索引层**（`List<T>` → 按 ID 查）。

### 解析层

```cs
public interface IJsonData { int GetID(); }

public class JsonParser<T> where T : IJsonData, new()
{
    public static List<T> ParseTabel(string jsonPath)
    {
        string jsonText;
        using (var sr = new System.IO.StreamReader(jsonPath))
            jsonText = sr.ReadToEnd();

        JObject root = JObject.Parse(jsonText);
        JArray rows = root["Sheet1"] as JArray;
        if (rows == null || rows.Count < 3)
        {
            Debug.LogError("[JsonPath]表结构异常,需要填充数据");
            return null;                     // ← 报错必须中断
        }

        Type t = typeof(T);
        var tLst = new List<T>();

        for (int i = 2; i < rows.Count; i++)  // 跳过 2 行表头
        {
            T item = new T();
            foreach (var cell in (JObject)rows[i])
            {
                PropertyInfo prop = t.GetProperty(cell.Key,
                    BindingFlags.Public | BindingFlags.Instance);
                if (prop == null || !prop.CanWrite)
                {
                    Debug.LogWarning($"[JsonPath]{t.Name}找不到属性{cell.Key}");
                    continue;
                }

                object raw = ((JValue)cell.Value).Value;
                object v = prop.PropertyType == typeof(string)
                    ? raw?.ToString()
                    : Convert.ChangeType(raw, prop.PropertyType);
                prop.SetValue(item, v);
            }
            tLst.Add(item);                      // ← Add 在外层，一个对象一次
        }
        return tLst;
    }
}
```

### 为什么 JSON 路线只跳 2 行（不是 3 行）

| xlsx 位置 | 内容 | 导出 JSON 后 |
|---|---|---|
| 第 1 行 | 字段名 `ID / WeaponName / …` | **变成对象 key**，不再占数组元素 |
| 第 2 行 | 类型 `int / string / …` | `Sheet1[0]` |
| 第 3 行 | 中文注释 `武器ID / …` | `Sheet1[1]` |
| 第 4 行起 | 数据 `1001 / AK47 / …` | `Sheet1[2]` 起 |

**「三行表头」跳 3 行、「两行表头」跳 2 行，取决于中间格式保不保留字段名行。** 这个数字跟导表格式绑死，别硬编码在循环头里 —— 提成常量或导表配置，换工具时只改一处。

### 索引层（泛型基类）

```cs
public class JsonManager<T> where T : class, IJsonData, new()
{
    private readonly Dictionary<int, T> dict = new Dictionary<int, T>();

    public JsonManager(string jsonPath)
    {
        foreach (var item in JsonParser<T>.ParseTabel(jsonPath))
        {
            if (dict.ContainsKey(item.GetID()))
                Debug.LogError($"[JsonPath]重复 ID {item.GetID()}");   // 别静默覆盖
            dict[item.GetID()] = item;
        }
    }

    public T GetConfig(int id) => dict.TryGetValue(id, out var t) ? t : null;
    public List<T> GetAllConfig() => new List<T>(dict.Values);
}

// 加一张新表 = 一行
public class WeaponGunManager : JsonManager<WeaponConfig>
{
    public WeaponGunManager(string jsonPath) : base(jsonPath) { }
}
```

泛型约束 `class, IJsonData, new()` 保证「有 ID、能 new」，读表逻辑与「表是哪张」彻底解耦。

### 进阶：自引用泛型 + 懒加载单例（`JsonManager<T,U>`）

路径不通过构造参数传，而是抽成抽象属性，让每个表一个子类：

```cs
public abstract class JsonManager<T, U>
    where T : class, IJsonData, new()
    where U : JsonManager<T, U>, new()          // ← 自引用泛型（CRTP）
{
    private static JsonManager<T, U> instance;

    public static JsonManager<T, U> Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new U();             // 用 U 造自己，才能调到子类覆盖的 JsonPath
                instance.LoadConfig();
            }
            return instance;
        }
        set => instance = value;
    }

    protected abstract string JsonPath { get; }

    private void LoadConfig()
    {
        var tList = JsonParser<T>.ParseTabel(JsonPath);
        foreach (var item in tList) TConfigDict[item.GetID()] = item;
    }
}

// 加一张新表 = 一个 5 行子类
public class WeaponConfigManager : JsonManager<WeaponConfig, WeaponConfigManager>
{
    protected override string JsonPath
        => Path.Combine(Application.streamingAssetsPath, "WeaponGun.json");
}
```

**为什么要两个泛型参数**：C# 的静态字段属于**闭合泛型类型**，`JsonManager<WeaponConfig>` 与 `JsonManager<SkillConfig>` 天然各有一份 `instance`（这正好是我们要的「每表一单例」）。但如果只写一个参数 `JsonManager<T>`，`new()` 造出来的是**基类自己**，`JsonPath` 是抽象的、没法实例化。把 `this` 类型也当泛型参数传进来，才能做到「单例是子类实例 + 路径由子类覆盖 + 读表逻辑只写一遍」。

三处坑：

| 坑 | 说明 |
|---|---|
| **`Instance` 返回的是基类类型** | 签名写的是 `JsonManager<T,U>`，子类想加 `Reload()` / `GetByWeaponType()` 就调不到 → 返回类型改成 `U` |
| **懒加载把异常吞成空表** | `instance = new U();` 先赋值、`LoadConfig()` 后执行，后者抛异常时 `instance` 已非 null → 下次访问直接返回**空字典**，且不再报第二次错。改成 `var tmp = new U(); tmp.LoadConfig(); instance = tmp;` |
| **`set` 对外开放** | `set => instance = value;` 让任何代码都能替换单例，测试注入的便利与「被意外覆盖」的风险同时存在，要写注释说明 |

**再记一条**：抽象属性把路径「抽出来」了，**不等于**路径就对了。`protected override string JsonPath => @"E:\unityproject\...\WeaponGun.json";` 一样是绝对路径 —— 配置化的收益是「改一处」，不是「自动正确」。而且同步 `string` 属性这个签名在 Android 上根本站不住（见下节）。

### JSON 路线的边界

| 边界 | 说明 |
|---|---|
| **`Convert.ChangeType` 只吃 `IConvertible`** | `enum` / `Nullable<T>` / `List<T>` 全不支持，得单独分支 |
| **`(JValue)value` 强转** | 值是数组或对象时抛 `InvalidCastException`，先判 `value.Type == JTokenType.Object` |
| **`JObject.Parse` 要求根是对象** | 根是数组得用 `JArray.Parse` / `JToken.Parse` |
| **JSON 有、类没有 → 只 LogWarning** | 静默跳过，加字段忘了加属性不崩，只是数据缺失 |
| **类有、JSON 没有 → 保持 default** | `int` 静默变 0，比异常更难查 |
| **`Dictionary` 遍历序不是契约** | 配表顺序有语义时另存 `List<T>` 保序 |
| **`GetAllConfig()` 每次 new List** | 高频调用会成 GC 源，缓存成 `IReadOnlyList<T>` |
| **报错后不 `return`** | `rows == null` 时 `LogError` 完继续跑，下一行 `rows.Count` 直接 NRE，把真正的错误信息盖掉 |

### 路径：`StreamingAssets` 的唯一正确写法

```cs
// ✗ 绝对路径，换机器 / 打包必炸
private string jsonPath = @"E:\unityproject\Empty\Assets\StreamingAssets\WeaponGun.json";

// ✓
string path = Path.Combine(Application.streamingAssetsPath, "WeaponGun.json");
```

**Android 例外**：`StreamingAssets` 被打进 apk（未压缩），是 `jar:file://` 路径，`StreamReader` / `File.ReadAllText` **读不到**，必须 `UnityWebRequest`（或用 Addressables 把配表打成 TextAsset）。桌面平台 `StreamReader` 可用。

---

## 校验清单（写完配表读取先过一遍）

- [ ] 表头几行？跳过逻辑写了吗？跳过的行数是不是硬编码在解析器里（应该跟导表工具约定，别散落）
- [ ] 根节点取的是 `DocumentElement` 还是 `FirstChild`？
- [ ] 配置类是字段还是属性？`GetFields` / `GetProperties` 对得上吗？
- [ ] 有没有 `Convert.ChangeType`？enum 单独处理了吗？
- [ ] `List.Add` 在内层还是外层？
- [ ] `Load` 换 `UnityWebRequest` 了吗？失败分支有吗？
- [ ] 表头字段 ↔ C# 成员 **逐个大小写核对**了吗？（`Bulletname` / `BulletName`）
- [ ] 表头类型行 ↔ C# 字段类型一致吗？（`WeaponType` 是 int 还是 string）
- [ ] 空表 / 缺文件 / 字段缺失 / 数值越界 四个异常分支都有兜底吗

---

## 相关

- [[scriptableobject数据驱动设计]]
- [[Resources.Load 加载与 as 转型]]
- [[C# IL 中间语言与编译执行模型]]
- [[IL2CPP 编译原理与陷阱]]
