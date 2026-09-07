---
id: SOP-XRAYUI-ARCHITECTURE-VERIFICATION
title: 风险相称的验证
document_type: sop
status: active
version: 1.1.0
project: XrayUI
owner: XrayUI maintainers
audience:
  - developer
  - agent
scope: _Dev/SOP
created_at: 2026-09-07T02:00:00+08:00
updated_at: 2026-09-07T03:15:00+08:00
tags:
  - architecture
  - verification
  - risk
source_of_truth: true
---

# 风险相称的验证

## 原则

验证范围由实际变更和风险决定，不要求非程序员开发者选择命令。Agent 应从项目文件、CI、README 和相关 SOP 找到当前有效验证入口，并用白话解释为什么需要这些验证。

## 验证层级

1. 文档或 Developer Resources：Host Documentation Validation、Host Architecture Validation 和相关 vHarness 测试。
2. 局部 C# 逻辑：最贴近变更的测试，再运行 `dotnet test XrayUI.Tests/XrayUI.Tests.csproj -c Release`。
3. UI、项目文件、依赖或发布链：在相关测试基础上执行 README/CI 对应的 `dotnet publish`。
4. `updater-rs/`：执行相关 Cargo 测试或构建，并在影响发布时检查与 MSBuild 的集成。
5. 高风险迁移、恢复或发布：验证快照、输入状态、回滚路径和独立复核。

## 流程

1. 从变更文件和行为确定受影响范围，不沿用过时命令。
2. 先运行快速、定向验证；通过后再运行更广回归。
3. 记录命令、退出码、通过/失败/跳过、警告和产物位置。
4. 失败立即停止，不用降低门禁或删除检查制造通过。
5. 最终用白话说明功能是否达到目标、有哪些警告、哪些范围没有验证。

## 独立复核

核心功能、迁移、恢复和发布完成后，优先按 `SOP_independent_review.md` 进行独立复核；同一实施上下文不能用主观判断替代证据。

## Migration Run 后的宿主语义验收

Migration Run 的哈希和 action 验证只证明计划被一致执行。首次接入或 repair 完成后还必须检查：Host Repository 身份与 Agent 规则、current state 是否反映真实项目、架构和 scope 是否匹配 Canonical Path、验证器是否尊重 Project Content 边界、版本管理与构建排除是否正确，以及项目原有验证命令是否仍通过。两阶段均通过后才能声称改造完成。
