---
id: DOC-XRAYUI-AGENT
title: XrayUI Agent 协同规则
document_type: guide
status: active
version: 1.0.0
project: XrayUI
owner: XrayUI maintainers
audience:
  - developer
  - agent
scope: repository
created_at: 2026-09-06T23:40:00+08:00
updated_at: 2026-09-06T23:40:00+08:00
tags:
  - governance
  - host-repository
---

# XrayUI Agent 协同规则

## 1. 项目定位

XrayUI 是采用 WinUI 3 与 .NET 10 的 Windows 桌面应用，并包含独立的 Rust updater。vHarness 作为本仓库的 Agent 治理、状态、迁移和验证框架，不改变 Project Content 的业务目录布局。

## 2. 目录权限与边界

- `Controls/`、`Converters/`、`Helpers/`、`Models/`、`Services/`、`Styles/`、`ViewModels/`、`Views/`、`Assets/`、`Strings/`、`Properties/`、`XrayUI.Tests/` 和 `updater-rs/` 属于 Project Content。
- `_System/` 是 Framework Infrastructure；`_System/standards/` 是术语、命名、兼容别名和文档字段的唯一机器标准源。
- `_Dev/` 是 Developer Resources；`_Dev/box/` 是开发者人工管理边界，Agent 默认只读，未经当前任务明确授权不得修改。
- `plugins/` 是与核心治理解耦的可选能力。
- `_wip/` 保存本地快照、迁移备份和其他可恢复制品，默认不提交。
- `Tasks/`、`Projects/` 和 `in/review/out` 不是本项目或 vHarness 的必需结构，不得自行创建。
- 不得在受管源码或文档中写入真实本地绝对路径；构建工具生成的 `bin/obj/target` 内容除外。

## 3. 变更门禁

- 涉及结构调整、跨模块或批量修改、删除覆盖、发布、迁移、核心功能或系统性 debug 时，必须先按 `_Dev/SOP/SOP_change_planning.md` 在 `_System/reviews/` 创建正式方案。
- 正式方案必须包含事实基线、目标与非目标、带 `[NEW]`、`[MODIFY]`、`[DELETE]` 标签的文件清单、实施阶段、验收、风险、回滚和 Decision Item。
- 获得开发者明确批准前不得实施；批准不扩展到方案外事项。

## 4. 状态与交接

- 会话开始先读取 `_System/memory/current_state.md` 和 `_System/architecture/task_breakdown.md`。
- 完成一个合理业务单元后更新 Work Item 状态；需要交接时记录准确进度、变更文件、验证结果和下一步。
- 连续两轮修改未命中目标或偏离架构时立即停止，记录问题并请求开发者裁决。

## 5. 平台、恢复与验证

- 开始任务前确认平台、项目根、权限和所需工具能力。
- 中长程或高风险变更前创建并验证 `_wip/` 快照。
- 常规验收至少包括相关 vHarness 验证、`dotnet test XrayUI.Tests/XrayUI.Tests.csproj -c Release`，以及变更影响发布链时执行 README 规定的 `dotnet publish`。
- Rust updater 变更还需执行对应 Cargo 验证。

## 6. 交付路径

XrayUI 使用 `.github/workflows/`、GitHub Releases 和 README 规定的 `dotnet publish` 作为现有交付流程。Host Repository 不要求复制 vHarness Framework Source 的 `_Dist` 目录；未来如需仓库内统一交付目录，必须另行决策。
