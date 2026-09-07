---
id: DOC-SYSTEM-TOOLS-README-MD
title: "tools Directory"
document_type: readme
status: active
version: 1.1.0
project: vHarness
owner: "vHarness maintainers"
audience:
  - developer
  - agent
scope: "_System/tools"
agent_access: read_write_with_approval
created_at: 2026-09-06T17:54:48+08:00
updated_at: 2026-09-06T21:42:07+08:00
tags:
  - documentation
---
# tools Directory

可复用且可验证的自动化工具。

- `common/`：架构、文档、发布、快照和恢复工具。
- `fusion/`：vHarness 深度融合、repair、事务回滚和验证工具。

## 内容边界

- 只存放与本目录职责直接相关的内容。
- 业务、系统治理和开发者私有材料不得跨边界混放。
- 新增、移动或删除内容时遵循根目录 Agent.md 和相关 SOP。

## Agent 使用

- 访问策略：read_write_with_approval。
- 中长程或高风险操作前必须建立正式方案和恢复检查点。
- 目录职责变化时必须同步更新本 README。
