---
title: URP 卡通描边 — 法线外扩与 ZOffset
type: resource
tags:
  - unity
  - URP
  - 渲染
  - 描边
created: 2026-10-20
updated: 2026-10-20
status: active
summary: 两 Pass 描边法（Cull Front + 沿法线外扩）的完整拆解：屏幕恒定线宽的 FOV/距离补偿公式、ZOffset 只改 clipPos.z 解决脸部脏描边、顶点着色器采样必须 tex2Dlod，以及与屏幕后处理 Sobel 方案的取舍
source: "E:/unityproject/UnityGroupWorking/UnityGroupWorking-main/Assets/shader/（NiloToon URP Toon Lit Outline Example）"
related:
  - "[[Unity 渲染批处理体系]]"
  - "[[urp移动优化]]"
---

# URP 卡通描边 — 法线外扩与 ZOffset

> 素材来源：`UnityGroupWorking` 项目引入的 NiloToon 示例着色器
> `Assets/shader/SimpleURPToonLitOutlineExample.shader` + `NiloOutlineUtil.hlsl` + `NiloZOffset.hlsl` + `SimpleURPToonLitOutlineExample_Shared.hlsl`
> 运行环境：Unity 2022.3.62f3c1 + URP 14.0.12

## 一、核心认知：描边 = 多画一遍背面

两 Pass 描边法（Shell / Inverted Hull）的全部思想只有两步：

1. **再画一遍模型**，顶点沿法线方向往外推一点点；
2. **只保留背面**（`Cull Front`），正面被剔除。

结果就是：模型本体盖住中间，四周露出一圈被放大的背面 —— 看起来就是描边。

```hlsl
// Outline Pass 的固定搭配
ZWrite On
Cull Front        // ← 必须，不是 Cull Off
ZTest LEqual
```

> ⚠️ 为什么必须是 `Cull Front`：如果 Cull Off，外扩后的**正面**会盖在本体前面（正面法线朝外，外扩后更靠前），整只角色会被描边色糊住。

### 五个 Pass 的分工

| Pass | Cull | ZWrite | 作用 |
| --- | --- | --- | --- |
| `ForwardLit` | Off | On | 本体着色（卡通光照 Ramp） |
| `Outline` | **Front** | On | 描边，必须写深度 |
| `ShadowCaster` | Off | On | 投影，带 `ToonShaderApplyShadowBiasFix` |
| `DepthOnly` | Off | On | 深度图 |
| `DepthNormalsOnly` | Off | On | 深度+法线（SSAO / 后处理描边用） |

后三个 Pass 里也 `#define ToonShaderIsOutline`，让**描边区域同样写进深度/法线**；否则描边在 SSAO、深度雾、屏幕后处理里会「不存在」，出现本体有雾描边没雾的割裂。

---

## 二、线宽必须做「屏幕恒定」补偿

最直觉的写法是 `positionWS += normalWS * _OutlineWidth`，但这样会得到一个致命问题：

**世界空间固定偏移，在屏幕上不是固定宽度。** 相机拉远 → 描边变细到看不见；FOV 变大 → 同样变细。卡通渲染要求线宽像手绘一样稳定，所以必须补偿。

### 补偿公式（NiloOutlineUtil.hlsl）

```hlsl
float3 TransformPositionWSToOutlinePositionWS(float3 positionWS, float positionVS_Z, float3 normalWS)
{
    float outlineExpandAmount = _OutlineWidth * GetOutlineCameraFovAndDistanceFixMultiplier(positionVS_Z);

    #if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED) || defined(UNITY_STEREO_DOUBLE_WIDE_ENABLED)
    outlineExpandAmount *= 0.5;   // VR 单眼半宽
    #endif

    return positionWS + normalWS * outlineExpandAmount;
}
```

`GetOutlineCameraFovAndDistanceFixMultiplier` 的三段逻辑：

```hlsl
float GetOutlineCameraFovAndDistanceFixMultiplier(float positionVS_Z)
{
    float cameraMulFix;
    if (unity_OrthoParams.w == 0)          // 透视相机
    {
        cameraMulFix = abs(positionVS_Z);              // ① 距离越远，世界空间偏移越大
        cameraMulFix = saturate(cameraMulFix);         // ② 距离淡出（太近时别爆粗）
        cameraMulFix *= GetCameraFOV();                // ③ FOV 越大，偏移越大
    }
    else                                   // 正交相机
    {
        float orthoSize = abs(unity_OrthoParams.y);
        orthoSize = ApplyOutlineDistanceFadeOut(orthoSize);
        cameraMulFix = orthoSize * 50;                 // 50 是对齐透视观感的魔法数
    }
    return cameraMulFix * 0.00005;         // 缩到「默认外扩量」量级
}
```

**三个关键点**：

1. `positionVS_Z` 取的是**视空间 Z**（相机空间深度），不是 clip 空间 —— clip 空间 z 是非线性的，直接乘会得到畸变。
2. FOV 从投影矩阵反解，不需要 C# 传参：
   ```hlsl
   float t = unity_CameraProjection._m11;              // = 1 / tan(fovY/2)
   float fov = atan(1.0 / t) * 2.0 * (180 / 3.1415);   // 弧度 → 角度
   ```
   `_m11` 是投影矩阵 y 方向缩放，`1/_m11 = tan(fovY/2)`，`atan` 回来再乘 2 就是整 FOV。
   > 注释里明说这是「慢函数」，正式项目可以在 C# 用 RendererFeature 里 `cmd.SetGlobalFloat("_CurrentCameraFOV", fov)` 全局传一次。
3. **正交/透视必须分支**：`unity_OrthoParams.w` 为 0 是透视、1 是正交（`x/y` 存正交尺寸）。正交相机没有距离衰减，用 `orthoSize * 50` 硬凑同一个观感。

---

## 三、ZOffset：只改深度，不改几何

### 问题：脸部的脏描边

角色脸上，鼻子、下巴、眼皮这些部位法线变化剧烈，外扩后的背面会从脸内部「透」出来，在额头/脸颊上糊出黑线。眉毛和头发互相穿插也会 Z-Fighting。

### 解法：把描边顶点在深度上往相机方向推一点

`NiloZOffset.hlsl` 的做法很精妙 —— **只覆盖 `positionCS.z`，xy 完全不动**：

```hlsl
float4 NiloGetNewClipPosWithZOffset(float4 originalPositionCS, float viewSpaceZOffsetAmount)
{
    if (unity_OrthoParams.w == 0)
    {
        // 透视：用一个「假想顶点」的 viewSpaceZ 反算 clipZ
        float2 ProjM_ZRow_ZW = UNITY_MATRIX_P[2].zw;
        float modifiedPositionVS_Z = -originalPositionCS.w + -viewSpaceZOffsetAmount;
        float modifiedPositionCS_Z = modifiedPositionVS_Z * ProjM_ZRow_ZW[0] + ProjM_ZRow_ZW[1];
        originalPositionCS.z = modifiedPositionCS_Z * originalPositionCS.w / (-modifiedPositionVS_Z);
        return originalPositionCS;
    }
    else
    {
        // 正交：z 与 view 空间是线性的，直接线性加
        originalPositionCS.z += -viewSpaceZOffsetAmount / _ProjectionParams.z;
        return originalPositionCS;
    }
}
```

为什么不能直接 `positionCS.z += offset`？因为**透视投影下 clip 空间 z 是非线性的**（`z_clip = A*z_view + B`，再除以 w 才落到 NDC）。所以代码走的是「先构造假想视空间深度 → 用投影矩阵 z 行重算 clipZ → 再乘回 w」这条正路。

> 透视分支里 `-originalPositionCS.w` 就是原始 viewSpaceZ（透视投影下 `w_clip = -z_view`），这是个可以记的小技巧。

### 遮罩与脸部特判

```hlsl
float outlineZOffsetMask = tex2Dlod(_OutlineZOffsetMaskTex,
        float4(input.uv, 0, 0)).r;              // ← 顶点着色器必须 tex2Dlod
outlineZOffsetMask = 1 - outlineZOffsetMask;     // 约定：黑色 = 应用 ZOffset
outlineZOffsetMask = invLerpClamp(_OutlineZOffsetMaskRemapStart, _OutlineZOffsetMaskRemapEnd, outlineZOffsetMask);

output.positionCS = NiloGetNewClipPosWithZOffset(
        output.positionCS,
        _OutlineZOffset * outlineZOffsetMask + 0.03 * _IsFace);   // 脸部强制再推一点
```

三个工程细节：

1. **顶点着色器不能用 `tex2D`** —— 因为 `ddx/ddy` 在光栅化之前不存在，隐式求导无从谈起。必须 `tex2Dlod` 并显式给 mip level（这里给 0）。
2. **遮罩语义反着来**（黑=应用）是美术习惯：遮罩图默认全黑，未涂区域就该生效，省得整张图刷白。
3. `0.03 * _IsFace`：`_IsFace` 是 `[ToggleUI]` 材质开关（脸/眼/嘴专用材质），用魔法数再往前推，专门压掉脸部穿插描边。

---

## 四、代价与坑

| 问题 | 说明 |
| --- | --- |
| **DrawCall 翻倍** | 每个材质多一个 Pass；角色多时是硬开销。合批只能靠 SRP Batcher 复用 CBUFFER，但注释明确说这个教学版 Outline Pass **不是 SRP Batcher 友好**（没有 C# 侧按材质正确设置常量缓冲），正式项目要自己补 |
| **硬边模型破面** | 外扩依赖**平滑法线**。低模硬边（法线断裂、UV 接缝处顶点分裂）外扩后会在接缝处裂开口子。解决：导入时开 Smooth Normals / 加平滑法线烘焙，或改用「沿顶点色存好的外扩方向」 |
| **顶点级外扩会棱角化** | 外扩发生在顶点上，低模的圆轮廓会被放大成多边形。高模或曲面细分才顺滑 |
| **线宽不随分辨率变** | 公式补偿的是距离/FOV，不是屏幕像素。不同分辨率下观感会有差异，需要时再按 `_ScreenParams` 补一档 |
| **Cull Front 与双面材质冲突** | 裙子、披风这类双面物件，本体要 `Cull Off`，描边再 `Cull Front` 会出现内侧描边缺失，通常要单独做背面壳 |
| **ZOffset 会破坏深度排序** | 推得太狠，描边会盖到更近的物体前面；`Range(0,1)` 默认 `0.0001`，别乱调大 |

---

## 五、另一条路：屏幕后处理描边（Sobel）

| 维度 | 两 Pass 法线外扩 | 屏幕后处理（深度+法线 Sobel） |
| --- | --- | --- |
| 开销 | 每材质多 1 个 Pass（几何量 ×2） | 全屏 1 次，与物体数量无关 |
| 线宽控制 | 逐材质 `_OutlineWidth`，可单独调 | 全屏统一，要按物体区分得写额外 ID 缓冲 |
| 线宽稳定性 | 靠 FOV/距离补偿，边缘会抖动 | 天然屏幕空间恒定 |
| 内描边 | 做不到（只有外轮廓） | 可以（法线/深度突变都能检出） |
| 依赖 | 只要模型法线 | 需要 `_CameraDepthTexture` + `_CameraNormalsTexture`，URP 里要开 DepthNormals |
| 适合 | 少量主角、风格化强烈、要精细控制 | 大量角色、全局统一描边、卡通场景 |

> 选择口诀：**角色少、要精细 → 两 Pass；角色多、要统一 → 屏幕后处理。**

---

## 六、必须记住的点

1. 描边 = **再画一遍 + 沿法线外扩 + `Cull Front`**。
2. 世界空间固定偏移 → 屏幕上宽度不稳定，必须乘 **`|viewSpaceZ| × FOV`** 补偿。
3. FOV 可从 `unity_CameraProjection._m11` 反解：`atan(1/_m11) * 2`。
4. ZOffset **只改 `positionCS.z`**，xy 不动 —— 解决脸部脏描边而不动几何。
5. 透视下 clipZ 非线性，重算要走**投影矩阵 z 行**；正交可直接线性加。
6. 顶点着色器采样**必须 `tex2Dlod`**（无 ddx/ddy）。
7. 描边 Pass 也要参与 `DepthOnly` / `DepthNormalsOnly` / `ShadowCaster`，否则深度/法线类效果里描边会消失。

## 七、易错点

- ❌ `Cull Off` 当描边 Pass —— 整只模型被描边色盖住。
- ❌ 直接 `positionCS.z += offset` —— 透视下深度错乱，近处正常远处穿帮。
- ❌ 用 clip 空间 z 当距离乘子 —— 应取视空间 z。
- ❌ 顶点着色器写 `tex2D` —— 编译报错或结果未定义。
- ❌ 忘给 DepthNormals Pass 定义 `ToonShaderIsOutline` —— SSAO 在描边区不生效。
- ❌ 硬边低模直接上法线外扩 —— 接缝开裂。

## 相关

- [[Unity 渲染批处理体系]] —— 多 Pass 会打断 SRP Batcher，描边 Pass 的合批代价在这里
- [[urp移动优化]] —— URP 管线配置与移动端开销取舍
