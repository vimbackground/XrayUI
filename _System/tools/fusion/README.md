---
id: DIR-SYSTEM-TOOLS-FUSION
title: vHarness Fusion System
document_type: readme
status: active
version: 1.0.0
project: vHarness
owner: vHarness maintainers
audience:
  - developer
  - agent
scope: _System/tools/fusion
agent_access: read_write_with_approval
created_at: 2026-09-06T21:28:00+08:00
updated_at: 2026-09-06T21:28:00+08:00
tags:
  - fusion
  - migration
  - repair
---

# vHarness Fusion System

统一执行首次深度融合和旧版升级补救。规范词表与命名来自 `_System/standards/`，本目录只拥有迁移动作策略和实现。

```text
python vharness_fusion.py plan --assets <assets> --target <host> --output <plan.json>
python vharness_fusion.py repair --assets <assets> --target <host> --output <plan.json>
python vharness_fusion.py apply --plan <plan.json>
python vharness_fusion.py verify --plan <plan.json>
python vharness_fusion.py rollback --plan <plan.json>
```

`plan` 与 `repair` 只生成计划。`apply` 拒绝未解决的冲突和输入哈希漂移，并在目标 `_wip/vharness-migrations/<migration_id>/` 建立备份、manifest 和报告。`Tasks/`、`Projects/` 始终作为 Project Content 保留。
