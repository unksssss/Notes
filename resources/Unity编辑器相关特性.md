---
title: Unity编辑器相关特性
type: resource
tags: [Unity, 编辑器扩展, Attribute, 特性, PropertyDrawer]
created: 2026-08-07
updated: 2026-08-07
status: 学习中
summary: 编辑器特性四分类：属性特性/方法特性/类特性/自定义特性，序列化三兄弟易混点，PropertyDrawer 三步套路
---

# Unity 编辑器相关特性

> 学习来源：CSDN《Unity 编辑器扩展总结》系列第 2 篇（作者：虚拟喵）
> 上一篇：[[Unity编辑器扩展开发入门]]

## 特性四分类口诀
**属性管显示、方法管入口、类管行为、自定义管画控件**

## ① 属性特性（字段级，控制 Inspector 显示）
- `[Range(0,100)]` 数值滑块限制范围
- `[Multiline(3)]` / `[TextArea(2,4)]` 多行文本
- `[SerializeField]` 私有字段序列化+显示
- `[HideInInspector]` public 隐藏但**仍序列化**
- `[NonSerialized]` 不序列化不显示
- `[FormerlySerializedAs("旧名")]` 字段改名保数据
- `[Header("标题")]` / `[Space(10)]` / `[Tooltip("提示")]` / `[ColorUsage(true)]` 面板装饰

### 序列化三兄弟（易混必考）
| 特性 | 序列化 | 显示 | 场景 |
|------|:---:|:---:|------|
| SerializeField | ✅ | ✅ | 私有字段要可调 |
| HideInInspector | ✅ | ❌ | 不想被改但值要存 |
| NonSerialized | ❌ | ❌ | 运行时临时变量 |

## ② 方法特性（编辑器入口）
- `[MenuItem]` 菜单栏按钮（静态方法）
- `[DrawGizmo]` Gizmos 绘制，逻辑与调试代码分离
- `[ContextMenu]` / `[ContextMenuItem]` 齿轮菜单/属性右键菜单

## ③ 类特性（组件行为控制）
- `[Serializable]` 类序列化为子属性显示
- `[RequireComponent(typeof(Animator))]` 自动补依赖组件
- `[DisallowMultipleComponent]` 防重复挂载
- `[ExecuteInEditMode]` 编辑器不运行时也执行（⚠️ 双刃剑：Update 改数据会污染场景，用 `if (!Application.isPlaying) return;` 保护）
- `[CanEditMultipleObjects]` 多选统一改值（配 CustomEditor）
- `[AddComponentMenu("路径")]` Component 菜单归类
- `[CustomEditor]` 自定义 Inspector 入口
- `[CustomPropertyDrawer]` 自定义属性绘制器
- `[SelectionBase]` 点子物体自动选根节点，防误选

P.S. 多特性逗号隔开：`[SerializeField, Range(0,5)]`

## ④ 自定义特性（PropertyDrawer 三步套路）
```
① 定义数据：class ShowTimeAttribute : PropertyAttribute { 构造函数存参数 }
② 写绘制器：class TimeDrawer : PropertyDrawer  // 必须放 Editor 文件夹！
     [CustomPropertyDrawer(typeof(ShowTimeAttribute))]
     重写 OnGUI(Rect, SerializedProperty, GUIContent)
     重写 GetPropertyHeight() 控制绘制高度
③ 挂字段：  [ShowTime(true)] public int time = 3605;
```
- Drawer 中通过 `attribute as ShowTimeAttribute` 取参数
- `property.propertyType` 判断字段类型，非预期类型用 `EditorGUI.HelpBox` 报错

## 相关笔记
- [[Unity编辑器扩展开发入门]]
- [[scriptableobject数据驱动设计]]
