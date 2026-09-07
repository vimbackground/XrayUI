---
id: DOC-DIST-VHARNESS-TEMPLATE-DEV-SOP-README-MD
title: "SOP Directory"
document_type: readme
status: active
version: 1.0.0
project: vHarness
owner: "vHarness maintainers"
audience:
  - developer
  - agent
scope: _Dev/SOP
agent_access: read_write_with_approval
created_at: 2026-09-06T17:54:48+08:00
updated_at: 2026-09-06T17:54:48+08:00
tags:
  - documentation
---
# SOP Directory

具有约束力的标准操作流程。

## 内容边界

- 只存放与本目录职责直接相关的内容。
- 业务、系统治理和开发者私有材料不得跨边界混放。
- 新增、移动或删除内容时遵循根目录 Agent.md 和相关 SOP。

## Agent 使用

- 访问策略：read_write_with_approval。
- 中长程或高风险操作前必须建立正式方案和恢复检查点。
- 目录职责变化时必须同步更新本 README。

## SOP 索引

| SOP | 适用场景 |
|---|---|
| `SOP_agent_platform_adaptation.md` | 平台、能力层级、项目根、权限与沙箱预检 |
| `SOP_change_planning.md` | 结构、跨模块、批量、发布、迁移和高风险变更的正式方案 |
| `SOP_task_execution_and_verification.md` | 低风险任务、常规实现、测试与局部重构 |
| `SOP_systematic_debugging.md` | 故障复现、根因定位、修复门禁与回归 |
| `SOP_independent_review.md` | 只读独立复核、Findings 分级与结论 |
| `SOP_read_only_state_audit.md` | 不修改项目的状态与物理事实核查 |
| `SOP_architecture_verification.md` | 根据变更范围和风险选择验证层级 |
| `SOP_session_handoff.md` | 正常结束当前会话，供同一 Agent 后续会话接续 |
| `SOP_cross_agent_migration.md` | 切换 Agent 工具前的移交与新 Agent 接管 |
| `SOP_development_pause_and_resume.md` | 开发者主动暂停及之后继续 |
| `SOP_interruption_recovery.md` | 死机、断网或 Agent 意外关闭后的检查与继续 |
| `SOP_resilience_and_disaster_recovery.md` | 快照、失败停止、恢复预演与回滚 |
| `SOP_retrospective_and_knowledge_distillation.md` | 完整会话或 Work Item 复盘与经验沉淀 |
| `SOP_workspace_audit_and_cleanup.md` | 绝对路径审计、临时产物分类与受控清理 |

SOP 可以组合使用；出现约束冲突时，采用更严格的审批、停止和验证要求。面向非程序员开发者的可直接复制入口见 `_Dev/manuals/prompt_engineering_collection.md`。
