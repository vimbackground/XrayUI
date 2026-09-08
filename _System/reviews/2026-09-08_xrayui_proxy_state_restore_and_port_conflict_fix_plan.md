---
id: PLAN-XRAYUI-PROXY-STATE-RESTORE-PORT-CONFLICT-FIX-20260908
title: XrayUI 代理状态启动恢复与端口冲突判定优化方案
document_type: plan
status: verified
version: 1.0.0
project: XrayUI
owner: XrayUI maintainers
audience:
  - developer
  - agent
scope: _System/reviews
created_at: 2026-09-08T18:45:00+08:00
updated_at: 2026-09-08T18:45:00+08:00
tags:
  - xrayui
  - reliability
  - startup-restore
  - port-conflict
  - user-experience
---

# XrayUI 代理状态启动恢复与端口冲突判定优化方案

## 1. 事实基线

- **启动恢复现状**：当前应用仅在开机自启（带 `--startup-minimized` 参数）且勾选开机自连（`IsAutoConnect`）时，才会自动连接 `LastAutoConnectServerId`；常规手动启动或重启后代理默认均为停止状态，且辅助代理的运行状态无法随软件启动自动恢复。
- **端口冲突误判现状**：在主代理关闭后重新启用或快速切换主代理时，旧 Xray 进程虽已结束，但系统网络协议栈对刚断开的连接保留短暂 `TIME_WAIT` / `CLOSE_WAIT` 状态。
- **检测机制限制**：`PortHelper.IsPortAvailable` 检查了 `GetActiveTcpConnections()`，只要发现有本地端口等于目标端口的残留连接即判定不可用，且绑定测试 Socket 时使用了 `ReuseAddress = false`；在等待 2 秒超时且无活动进程占用时，直接触发 `ShowPortConflictPromptAsync` 弹窗建议改端口，给用户造成强烈困扰。
- **多代理与单代理差异**：单代理模式下系统所有请求集中在主端口，残留连接多且易踩中检测；多代理模式下辅代理端口流量相对分散，复现概率相对较低。

## 2. 目标与非目标

### 目标

1. **新增启动恢复开关**：在设置页面中提供「启动时恢复上次代理状态」独立开关（默认关闭），由用户自主选择。
2. **准确记录与恢复状态**：开启该功能后，软件退出时自动记录主代理运行状态、当前主节点 ID 及激活的辅助代理节点列表；下次启动软件时（无论开机还是手动打开），自动恢复主代理和辅助代理的工作状态。
3. **消除端口冲突误报**：重构端口可用性判定与启动冲突检查逻辑，允许地址复用（`SO_REUSEADDR`），区分“正在监听的活动进程占用”与“已关闭连接的内核 `TIME_WAIT` 残留”，杜绝无外部程序占用时的改端口弹窗。
4. **保留真实冲突兜底**：若确实有第三方外部程序正在监听占用端口且无法安全清理，保留端口冲突弹窗与修改端口建议。

### 非目标

- 不改变现有单一节点手动切换流程和多代理模式的基本业务逻辑。
- 不强制开启状态恢复（默认关闭，保持既有用户习惯）。
- 不强行终止未经识别的第三方进程。

## 3. 架构与流程设计

### 3.1 代理状态保存与恢复流

```text
【软件正常退出 / 关闭】
  │
  ├─ 检查 settings.RestoreProxyStateOnStartup 是否启用
  │     ├─ 开启：
  │     │    ├─ 主代理是否运行？是 → 记录 LastRunningServerId；否 → 记录 null
  │     │    └─ 辅助代理哪些处于激活？记录 LastRunningAuxiliaryServerIds 列表
  │     └─ 关闭：清除记录
  └─ 保存配置并执行正常退出清理

【软件下次启动】
  │
  ├─ 加载 settings.json 与 servers.json
  ├─ 检查 settings.RestoreProxyStateOnStartup 是否启用 且 存在上次运行记录
  │     ├─ 是：
  │     │    ├─ 恢复辅助代理激活标记 (IsDedicatedPortActive)
  │     │    └─ 若存在 LastRunningServerId 对应节点 → 自动触发 ConnectToServerAsync(server)
  │     └─ 否：保持现有启动逻辑（默认停止，若为开机自启且开启 IsAutoConnect 则走原有自连）
  └─ 进入主界面
```

### 3.2 端口可用性与冲突判定流

```text
【启动 / 切换主代理】
  │
  ├─ 端口是否正在被活动进程监听？(检查 GetTcpListenerProcessIds)
  │     ├─ 否 (无进程监听)：
  │     │    └─ 即使存在 TIME_WAIT 残留，Xray 核心原生支持 SO_REUSEADDR 正常启动 → 直接启动成功！
  │     └─ 是 (有进程监听)：
  │          ├─ 属于本程序遗留的 xray.exe？
  │          │    ├─ 是 → TryRecoverOrphanedCoreOnPortAsync 清理残留 → 恢复启动
  │          │    └─ 否 (第三方程序真实占用) → 弹出 ShowPortConflictPromptAsync 提示改端口
  └─ 结束
```

## 4. 变更清单

### [MODIFY]

- `Models/AppSettings.cs`：
  - 新增 `RestoreProxyStateOnStartup`（布尔值，默认 `false`）。
  - 新增 `LastRunningServerId`（字符串，记录退出时正在运行的主节点 ID）。
  - 新增 `LastRunningAuxiliaryServerIds`（字符串列表，记录退出时处于激活状态的辅助节点 ID）。
- `ViewModels/AppSettingsViewModel.cs`：
  - 增加 `RestoreProxyStateOnStartup` 属性绑定、设置加载与持久化逻辑。
- `Views/AppSettingsControl.xaml`：
  - 在常规/启动设置区域增加「启动时恢复上次代理状态」ToggleSwitch 开关及描述文本。
- `Strings/zh-CN/Resources.resw` 与 `Strings/en-US/Resources.resw`：
  - 补充中英文界面文案与提示。
- `MainWindow.xaml.cs`：
  - 在 `StopBackgroundServicesOnExit` 中增加退出前代理状态捕获与保存。
- `ViewModels/ControlPanelViewModel.cs`：
  - 暴露当前活动节点或 ID 属性供状态捕获使用。
  - 优化 `StartSelectedServerAsync` 中的端口忙检查，避免因 `TIME_WAIT` 误报冲突弹窗。
- `ViewModels/MainViewModel.cs`：
  - 在 `InitializeAsync` 初始化阶段，增加对 `RestoreProxyStateOnStartup` 的评估与自动恢复流程。
- `Helpers/PortHelper.cs`：
  - 优化 `IsPortAvailable` 与端口等待逻辑，Socket 测试绑定启用 `ReuseAddress = true`，忽略单纯处于 `TIME_WAIT` / `CLOSE_WAIT` 的非监听连接，仅将真正的活动 Listener 视为冲突。
- `XrayUI.Tests/PortHelperTests.cs`：
  - 同步补充并更新端口检测相关单元测试，确保高并发或断开重用场景下的断言正确性。

## 5. 实施阶段

- **阶段 1：端口检测与防误判优化（解决问题 2）**
  - 重构 `PortHelper.IsPortAvailable`，允许地址复用，排除 `TIME_WAIT` 残留干扰。
  - 优化 `ControlPanelViewModel.cs` 端口判定：只有在实测存在第三方活动监听进程且清理失败时才弹窗。
  - 运行定向单元测试验证。
- **阶段 2：数据模型与设置界面（解决问题 1 基础）**
  - 在 `AppSettings`、`AppSettingsViewModel` 及 `AppSettingsControl.xaml` 中增加配置项与 UI 控件。
  - 补充多语言资源文件。
- **阶段 3：退出捕获与启动恢复逻辑（解决问题 1 闭环）**
  - 在退出服务前记录运行中的主节点与辅代理节点。
  - 在 `MainViewModel.InitializeAsync` 中接入启动恢复编排。
- **阶段 4：回归验证与测试**
  - 运行 `dotnet test XrayUI.Tests/XrayUI.Tests.csproj -c Release`。
  - 运行 Host Architecture 与 Host Documentation 门禁验证。

## 6. 验收标准

1. **单元测试验收**：`dotnet test XrayUI.Tests/XrayUI.Tests.csproj -c Release` 全部通过（包含新增与既有端口检测用例）。
2. **端口防误报验证**：
   - 连续快速切换不同主节点，主代理正常无感切换，不弹出“本地代理端口冲突提示”弹窗。
   - 停止主代理后立即点击启动，主代理秒级启动成功，不弹出修改端口窗口。
3. **状态恢复验证**：
   - 关闭开关时：退出软件后再次启动，代理处于关闭状态（符合默认预期）。
   - 开启开关时：
     - 在主代理开启时退出软件，重新启动软件后主代理自动恢复连接。
     - 在主代理与辅助代理同时开启时退出软件，重新启动后主辅代理均恢复连接。
     - 在代理完全关闭时退出软件，重新启动后代理保持关闭。
4. **工程门禁**：Host 架构验证、文档验证及 `git diff --check` 无错误通过。

## 7. 风险与缓解

- **风险 1**：如果节点配置本身已失效（如远程服务器不可用或配置错误），启动时自动恢复可能导致核心报错。
  - **缓解措施**：自动恢复调用既有的 `ConnectToServerAsync` 流程，如果启动失败会触发既有的统一错误处理与安全降级，不会导致程序卡死或崩溃。
- **风险 2**：操作系统在端口清理极度迟滞时的偶发异常。
  - **缓解措施**：Xray 核心自身具有强鲁棒性的重试和绑定能力，配合严格的活动进程监听检查，保证真实冲突不漏报、虚假冲突不误报。

## 8. 回滚计划

- 若实施后遇到非预期阻断，由于本方案为增量式优化，可通过 Git 快速回滚至方案实施前的 `HEAD` 提交（`973d7b7`）。

## 9. Decision Items

- [x] **D-01**：设置开关默认值设为 `false`（默认关闭，保持既有体验，由用户按需启用）。
- [x] **D-02**：端口冲突判定以“是否存在活动进程正在监听”作为核心判据，支持安全地址复用。
