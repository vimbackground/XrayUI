---
id: DOC-DIST-VHARNESS-TEMPLATE-DEV-MANUALS-README-MD
title: "manuals Directory"
document_type: readme
status: active
version: 1.0.0
project: vHarness
owner: "vHarness maintainers"
audience:
  - developer
  - agent
scope: _Dev/manuals
agent_access: read_write_with_approval
created_at: 2026-09-06T17:54:48+08:00
updated_at: 2026-09-06T17:54:48+08:00
tags:
  - documentation
---
# manuals Directory

面向开发者的操作手册与检查表。

## 内容边界

- 只存放与本目录职责直接相关的内容。
- 业务、系统治理和开发者私有材料不得跨边界混放。
- 新增、移动或删除内容时遵循根目录 Agent.md 和相关 SOP。

## Agent 使用

- 访问策略：read_write_with_approval。
- 中长程或高风险操作前必须建立正式方案和恢复检查点。
- 目录职责变化时必须同步更新本 README。

## 手册索引

- `prompt_engineering_collection.md`：面向非程序员开发者、可原样复制的 21 个 XrayUI Vibe Coding 提示词，由 Agent 主动通过口语化对话确认需求，并覆盖开发、检查、验收、会话生命周期、复盘沉淀、受控清理和 Git 本地恢复。
