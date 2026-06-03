---
title: "ParticleSystem详解"
type: resource
tags: [unity, ParticleSystem, 粒子系统, 特效]
created: "2026-06-04"
updated: "2026-06-04"
status: active
summary: "Unity ParticleSystem 全部模块属性详解，按开发频率排序，附常见参数调优方案。"
source: ""
related: []
---

# ParticleSystem 详解

Particle System 由多个独立模块组成，每个模块控制粒子生命周期的某个方面。

---

## 模块概览（按使用频率排序）

```
⭐⭐⭐ 每次都调    → Main、Emission、Shape、Renderer、Color over Lifetime
⭐⭐ 经常调       → Size over Lifetime、Noise、Collision、Gravity
⭐ 偶尔调        → Velocity over Lifetime、Rotation、Texture Sheet、Sub Emitters、Lights、Trails
```

---

## Main（主模块）⭐⭐⭐

粒子的"出生设定"：活多久、多大、多快、什么颜色。

| 属性 | 说明 | 常用值 |
|---|---|---|
| **Duration** | 一次循环的时长（秒） | 不循环时设为期望播放长度 |
| **Looping** | 是否循环播放 | 火焰/烟雾开启，一次性特效关闭 |
| **Prewarm** | 启动时预先模拟一周期（仅 Looping 可用） | 火焰场景勾上，避免冷启动 |
| **Start Lifetime** | 粒子存活时间（秒） | 火球 1~2s，烟雾 3~8s，泡沫 0.5~1s |
| **Start Speed** | 发射初速度 | 火球 5~8，烟雾 0.5~2，泡沫 2~4 |
| **Start Size** | 初始大小 | 火球 0.3~1，烟雾 0.1~0.5，火星 0.02 |
| **Start Color** | 初始颜色 | 火焰橙红，烟雾灰，泡沫白 |
| **Start Rotation** | 初始旋转角 | Billboard 模式无效，Mesh 粒子可用 |
| **Gravity Modifier** | 重力倍率 | 0=无重力，负值=上飘，火球-0.1，烟雾-0.3~0.5 |
| **Simulation Space** | 坐标系 | Local(跟发射器) / World(独立存在) |
| **Simulation Speed** | 模拟速度 | 1=正常，0.5=慢动作，2=快进 |
| **Scaling Mode** | 缩放模式 | Hierarchy(继承父级) / Local(仅自身) |
| **Max Particles** | 粒子数上限 | 默认 1000，超过不再发射。性能关键参数 |

### Start Speed 增大导致稀疏

速度↑ → 粒子间距拉大 → 视觉稀疏。补救：
- `Emission → Rate over Time` 同比翻倍
- `Start Size` 调大

公式：`视觉密度 ≈ (粒子数 × 粒子大小²) / 速度`

---

## Emission（发射模块）⭐⭐⭐

控制每秒/每次发多少粒子。

| 属性 | 说明 |
|---|---|
| **Rate over Time** | 每秒发射粒子数。火焰 20~50，烟雾 5~15 |
| **Rate over Distance** | 每移动单位距离发射数。拖尾/扬尘专用 |
| **Bursts** | 定点爆发，0s 爆发 30 个 = 一次性喷射 |

```cs
// 代码控制
var emission = ps.emission;
emission.rateOverTime = 10f;
emission.SetBursts(new ParticleSystem.Burst[] {
    new ParticleSystem.Burst(0f, 30)  // 0秒时爆发30个
});
```

---

## Shape（形状模块）⭐⭐⭐

定义粒子从哪里发射、朝哪个方向。

| Shape Type | 适用场景 |
|---|---|
| **Cone** | 灭火器泡沫、火球喷射、水枪 |
| **Sphere** | 爆炸、四面八方扩散、烟雾 |
| **Box** | 地面火、区域烟雾 |
| **Circle / Edge** | 圆形光环 |
| **Mesh** | 从模型表面发射 |

### Cone 关键参数

| 参数 | 说明 |
|---|---|
| **Angle** | 锥角（度）。灭火器 10~15°，宽喷 30~45° |
| **Radius** | 发射口半径。越小越集中 |
| **Radius Thickness** | 0=从中心点发射，1=从整个圆面发射 |
| **Rotation** | 发射方向（不受发射器旋转影响时可写死） |

---

## Color over Lifetime（颜色随时间变化）⭐⭐⭐

粒子从生到死的颜色渐变。

```
火焰：    橙红(0%) → 暗红(60%) → 透明黑(100%)
烟雾：    灰白(0%) → 灰色(50%) → 完全透明(100%)
泡沫：    白色(0%) → 淡蓝白(50%) → 透明(100%)
电火花：  亮白(0%) → 亮蓝(30%) → 暗蓝(70%) → 透明(100%)
```

```cs
var col = ps.colorOverLifetime;
Gradient grad = new Gradient();
grad.SetKeys(
    new GradientColorKey[] {
        new GradientColorKey(Color.white, 0f),
        new GradientColorKey(Color.gray, 0.5f),
        new GradientColorKey(new Color(0.3f, 0.3f, 0.3f, 0f), 1f)
    },
    new GradientAlphaKey[] {
        new GradientAlphaKey(1f, 0f),
        new GradientAlphaKey(0.5f, 0.5f),
        new GradientAlphaKey(0f, 1f)
    }
);
col.color = new ParticleSystem.MinMaxGradient(grad);
```

### Gradient 的 MinMaxGradient

- `MinMaxGradient(Color)` — 固定颜色
- `MinMaxGradient(Color, Color)` — 两个颜色之间随机
- `MinMaxGradient(Gradient)` — 按生命周期渐变

---

## Size over Lifetime（大小随时间变化）⭐⭐

| 典型曲线 | 效果 |
|---|---|
| 0.3 → 1.5 逐渐变大 | 烟雾扩散、火球膨胀 |
| 1 → 0.01 逐渐缩小 | 灭火中火焰缩小 |
| 0.5 → 1 → 0.5 先大后小 | 爆炸闪光 |

```cs
var size = ps.sizeOverLifetime;
AnimationCurve curve = new AnimationCurve(
    new Keyframe(0, 0.3f),
    new Keyframe(1, 1.5f)
);
size.size = new ParticleSystem.MinMaxCurve(1f, curve);
```

---

## Noise（噪声模块）⭐⭐

给粒子添加随机抖动，让烟雾/火焰看起来自然不规则。

| 参数 | 说明 | 烟雾推荐值 |
|---|---|---|
| **Strength** | 抖动强度 | 0.5~1.5 |
| **Frequency** | 抖动频率（低=大波浪，高=细碎抖） | 0.1~0.3 |
| **Scroll Speed** | 噪声纹理滚动速度 | 0.1~0.3 |
| **Damping** | 噪声衰减（勾上后粒子越老抖越小） | ✅ 勾上 |
| **Octaves** | 噪声叠加层数 | 1~2 |
| **Quality** | 质量 | Medium 够用 |

---

## Collision（碰撞模块）⭐⭐

### Type: World vs Planes

| | World（碰撞体） | Planes（平面） |
|---|---|---|
| 碰撞对象 | 场景里的 Box/Sphere/Mesh Collider | 一个数学平面（无限大） |
| 精度 | 取决于碰撞体建模精度 | 绝对精准 |
| 性能开销 | 较高（需遍历场景碰撞体） | **极低**（纯数学计算） |
| 设置方式 | 拖场景 GameObject | 拖一个 Transform，以其 XY 平面为碰撞面 |
| 天花板 | 需建 Box Collider | ✅ 拖天花板 Transform 即可 |

### Planes 模式详解

```cs
Collision 模块:
  Type: Planes
  Planes: 拖入天花板 Transform
  
  Dampen: 0.7~0.9       // 碰顶后慢下来
  Bounce: 0              // 不反弹，停住平铺
  Lifetime Loss: 0~0.2   // 碰顶后加速消失
```

- 平面方向 = Transform 的 XY 平面，天花板要水平就把 Transform 的 Z 轴朝上
- **无限大**，不关心物体大小，只要粒子飞到平面高度就碰撞
- 拖多个 Transform = 多个独立平面

### 选型

```
天花板  → Planes（一个 Transform 搞定）
地面    → Planes
墙壁    → World（需要多面 Box Collider 围房间）
复杂障碍 → World
```

### World 模式参数

| 参数 | 说明 | 建筑物火推荐 |
|---|---|---|
| **Dampen** | 碰撞后速度衰减（0=不减速，1=完全停） | 火焰 0.3~0.5，烟雾 0.7~0.9 |
| **Bounce** | 反弹系数 | 火焰 0.2~0.4，烟雾 0 |
| **Lifetime Loss** | 碰撞后生命缩短比例 | 火焰 0.1~0.2，烟雾 0 |
| **Min Kill Speed** | 低于此速度的粒子销毁 | 0.1 |
| **Radius Scale** | 碰撞体半径倍率 | 1~1.5 |
| **Collision Quality** | 精度 | 复杂墙体 High，天花板 Medium |
| **Send Collision Messages** | 触发 OnParticleCollision 回调 | 需事件时开启 |

---

## Velocity over Lifetime（速度随时间变化）⭐

给粒子施加持续的加速度。

| 模式 | 说明 |
|---|---|
| **Linear** | X/Y/Z 直线加速度 |
| **Orbital** | 绕中心旋转 |
| **Speed Modifier** | 沿当前速度方向加减速 |

⚠️ **World Simulation Space 下慎用**：速度在局部坐标系计算，发射器旋转后方向会歪。

---

## Gravity Modifier（重力）⭐

在 Main 模块里，但值得单独说：

| 值 | 效果 |
|---|---|
| 0 | 无重力 |
| 1 | 正常下落 |
| -0.3~-0.5 | 烟雾向上飘 |
| -0.1 | 火焰轻微上升 |

---

## Renderer（渲染模块）⭐⭐⭐

| 参数 | 说明 |
|---|---|
| **Render Mode** | Billboard(面朝相机) / Stretched Billboard(速度方向拉伸) / Mesh(3D模型) |
| **Material** | 粒子材质 |
| **Sort Mode** | 排序方式 |
| **Sorting Fudge** | 排序偏移（正=靠前渲染，负=靠后） |
| **Min/Max Particle Size** | 屏幕空间粒子大小的最小/最大值 |
| **Render Alignment** | Billboard 的对齐轴 |
| **Enable GPU Instancing** | 同材质粒子合批，减少 Draw Call ✅ |

---

## Texture Sheet Animation（纹理序列帧）⭐

用一张含多帧的 Flipbook 纹理做序列帧动画（NatureManufacture 的火焰就是这种方法）。

| 参数 | 说明 |
|---|---|
| **Tiles X / Y** | 纹理切成几行几列（如 4×4 = 16 帧） |
| **Animation** | Whole Sheet(全张) / Single Row(单行) |
| **Frame over Time** | 随时间切换帧的曲线 |
| **Cycles** | 循环次数 |

---

## Sub Emitters（子发射器）⭐

粒子出生/死亡/碰撞时生成新的子粒子。

| 触发条件 | 用途 |
|---|---|
| **Birth** | 火焰出生时生成烟雾 |
| **Death** | 粒子死亡时生成火花 |
| **Collision** | 碰到天花板生成平铺烟雾 |

---

## Trails（拖尾）⭐

粒子留下拖尾效果（刀光、流星）。

| 参数 | 说明 |
|---|---|
| **Trail Material** | 拖尾材质 |
| **Lifetime** | 拖尾长度 |
| **Minimum Vertex Distance** | 拖尾顶点间距（越小越平滑） |

---

## Lights（光照）⭐

粒子附加点光源/聚光灯。

| 参数 | 说明 |
|---|---|
| **Light** | Prefab 光源 |
| **Ratio** | 多少比例的粒子挂光源（0~1） |
| **Intensity Multiplier** | 光照强度倍数 |
| **Range Multiplier** | 光照范围倍数 |

---

## Rotation（旋转）⭐

控制粒子自转。Billboard 模式下无效。

```cs
var rotation = ps.rotationOverLifetime;
rotation.z = new ParticleSystem.MinMaxCurve(0f, 360f); // Z轴旋转
```

---

## 性能优化要点

| 手段 | 效果 |
|---|---|
| `Max Particles` 设上限 | 防止粒子数爆炸 |
| `Enable GPU Instancing` (Renderer) | 减少 Draw Call |
| 关掉不用的模块 | 每个模块都有 CPU 开销 |
| `Collision Quality` 降为 Low/Medium | 减少碰撞计算 |
| 减少 `Rate over Time` + 增大 `Start Size` | 同样视觉效果，更少粒子 |
| Sub Emitters 层级不超过 2 层 | 子粒子开销指数增长 |

---

