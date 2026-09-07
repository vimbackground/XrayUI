---
id: ARCH-VHARNESS-NAMING-TERMINOLOGY
title: vHarness 命名与术语架构
document_type: architecture
status: active
version: 1.0.0
project: vHarness
owner: vHarness maintainers
audience:
  - developer
  - agent
scope: _System/architecture
created_at: 2026-09-06T21:20:00+08:00
updated_at: 2026-09-06T21:20:00+08:00
tags:
  - terminology
  - naming
  - standards
---

# vHarness 命名与术语架构

机器可读的唯一标准源是 `_System/standards/`。本文件只解释其架构含义，不建立平行词表。

## 认知边界

- **Framework Infrastructure**：`Agent.md` 与 `_System` 中的治理、状态、迁移和验证能力。
- **Developer Resources**：`_Dev` 中的 SOP、手册、参考和人工材料。
- **Project Content**：宿主仓库自己的代码和业务目录，其布局不由 vHarness 强制规定。
- **Distribution Artifact**：由 Framework Source 生成或验证的 `_Dist` 交付物。
- **Work Item**：系统治理进度记录，不等同于名为 `Tasks` 的业务目录。

`Tasks/`、`Projects/` 和 `in/review/out/_wip` 不再是 vHarness 核心要求。宿主仓库若已有这些目录，应按 Project Content 原地保留。

## 消费规则

Agent 和工具先读取 `vocabulary.json`、`naming_rules.json` 与 `compatibility_aliases.json`，再解释迁移任务。旧名称只有在兼容注册表明确登记且置信度足够时才能自动处理；否则形成 Decision Item。
