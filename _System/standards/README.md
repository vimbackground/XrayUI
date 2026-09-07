---
id: DIR-SYSTEM-STANDARDS
title: vHarness Standards Registry
document_type: readme
status: active
version: 1.0.0
project: vHarness
owner: vHarness maintainers
audience:
  - developer
  - agent
scope: _System/standards
agent_access: read_write_with_approval
created_at: 2026-09-06T21:20:00+08:00
updated_at: 2026-09-06T21:20:00+08:00
tags:
  - standards
  - terminology
  - naming
---

# vHarness Standards Registry

本目录是 vHarness 术语、命名、兼容别名和文档元数据的唯一机器可读标准源。

- `vocabulary.json`：规范术语及弃用别名。
- `naming_rules.json`：名称、路径、字段和 CLI 规则。
- `compatibility_aliases.json`：旧 harness/异构结构到规范标识的映射。
- `schemas/`：注册表和文档元数据的 JSON Schema。

工具必须引用这里的稳定 ID，不得复制另一套词表。`_System/architecture/naming_and_terminology.md` 是面向人和 Agent 的解释视图，不是第二标准源。发布副本只能由打包工具生成。
