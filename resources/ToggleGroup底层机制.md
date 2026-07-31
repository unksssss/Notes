---
title: "ToggleGroup 底层机制"
type: resource
tags: [unity, ui, toggle, 面试]
created: "2026-07-31"
updated: "2026-07-31"
status: active
summary: "ToggleGroup allowSwitchOff 行为与底层调用链：Toggle.Set() 入口早退拦截 + NotifyToggleOn → SetAllTogglesOff"
---

# ToggleGroup 底层机制

## allowSwitchOff 行为

- `allowSwitchOff = true`：允许"全不选"——玩家点击已选中的 Toggle 可将其关闭
- `allowSwitchOff = false`（默认）：同组始终至少一个选中；玩家点击已选中的 Toggle 不会关闭它

## 底层调用链（面试重点）

### 关闭路径（allowSwitchOff=false 时的"驳回"）

```
selectedToggle.isOn = false
  → isOn setter → Set(false) → Set(false, sendCallback: true)
      → 入口前置检查：
        m_Group != null && m_Group.isActiveAndEnabled
        && !m_Group.allowSwitchOff && !value(即 false)
      → 命中 → 直接 return！
      → 状态不变、onValueChanged 不触发
```

> ⚠️ 驳回是 **early return 早退机制**，不是"改了再弹回来"！拦截发生在 **Toggle 自己的 Set() 方法入口**，不是 ToggleGroup 层。

### 选中路径

```
toggle.isOn = true
  → Set() 确认 m_IsOn == true
  → 调用 m_Group.NotifyToggleOn(this)
  → ToggleGroup.SetAllTogglesOff() 遍历关掉同组其他 Toggle
```

## 项目实践（轮播背包 RotationBackPack）

- `allowSwitchOff = true`，`SelectItem` 直接设 `m_Items[index].m_Toggle.isOn = true`
- 其他 item 自动熄掉，靠的就是"选中路径"这条链
- 可见性：只显示 ScaleTimes 最大的 3 个，其余隐藏；选中 = PosId 等于 `m_Items.Count/2` 的 item

## 相关

- [[Unity 渲染批处理体系]]
