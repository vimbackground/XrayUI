---
id: DOC-DIST-VHARNESS-TEMPLATE-SYSTEM-MEMORY-README-MD
title: "memory Directory"
document_type: readme
status: active
version: 1.0.0
project: vHarness
owner: "vHarness maintainers"
audience:
  - developer
  - agent
scope: _System/memory
agent_access: read_write_with_approval
created_at: 2026-09-06T17:54:48+08:00
updated_at: 2026-09-06T17:54:48+08:00
tags:
  - documentation
---
# memory Directory

跨会话状态与恢复游标。

## 内容边界

- 只存放与本目录职责直接相关的内容。
- 业务、系统治理和开发者私有材料不得跨边界混放。
- 新增、移动或删除内容时遵循根目录 Agent.md 和相关 SOP。

## Agent 使用

- 访问策略：read_write_with_approval。
- 中长程或高风险操作前必须建立正式方案和恢复检查点。
- 目录职责变化时必须同步更新本 README。

## 状态索引

- `current_state.md`：当前准确状态、最近验证、恢复路径和下一步。
- `retrospective_log.md`：按时间追加的会话或 Work Item 复盘事实与 Harness 改进线索。
