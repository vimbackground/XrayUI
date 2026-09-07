---
id: SOP-VHARNESS-TEMPLATE-PLAN
title: 正式变更方案
document_type: sop
status: active
version: 1.2.0
project: vHarness
owner: project maintainers
audience:
  - developer
  - agent
scope: _Dev/SOP
created_at: 2026-09-06T17:54:48+08:00
updated_at: 2026-09-07T03:15:00+08:00
tags:
  - planning
  - approval
source_of_truth: true
---

# 正式变更方案

架构、跨模块、批量、删除覆盖、发布、迁移及中长程变更必须先在 `_System/reviews/` 创建正式方案。方案包含 Frontmatter、事实基线、目标与非目标、变更清单、阶段、验收、风险和回滚。开发者明确批准前不得实施。

## Vibe Coding 需求确认

开发者可以只用日常语言描述想实现的效果。Agent 应先读取仓库并主动推导技术范围，再通过简短对话确认：想解决的问题、期望看到的结果、不能破坏的行为和怎样算满意。能够从项目确认的文件、命令和架构不得反问开发者。

Agent 负责把对话转换为正式方案中的 `[NEW]`、`[MODIFY]`、`[DELETE]`、验收、风险、回滚和 Decision Item，并用白话摘要需要开发者决定的事项。开发者批准的是正式方案的明确范围，而不是对未知扩展的授权。

## 来源与修订一致性

- 引用开发者提供的附件或外部资料时，只记录来源角色、必要文件名和证据摘要；普通治理文档不得固化盘符、用户目录、临时挂载或其他本地绝对路径。
- 方案经过范围增补或开发者改变决定后，必须按相关术语检查事实基线、目标、文件清单、阶段、验收和全部 Decision Item。
- 后续决定覆盖旧决定时，保留旧决定的历史语义并明确标记为 `superseded`，再记录当前 preserve、adopt、merge、relocate 或 defer action；不得让相反 action 同时保持有效。

## 批准前预验证

1. 新方案在请求开发者批准前，先读取 Standards Registry，确认 Frontmatter 的 `status`、`document_type`、逻辑 ID、scope 和术语均为合法值；等待批准使用 `draft`，实施阶段使用 `in_progress`，验收完成使用 `verified`。
2. 在批准前运行适用的 Host Architecture、Host Documentation 和 `git diff --check`；方案自身包含真实本地绝对路径、异常编码、无效状态或结构错误时，先修正再展示给开发者。
3. 预验证只证明方案文档可执行，不等于开发者已经批准，也不得提前实施方案中的业务或治理修改。
4. 批准后的输入状态或范围发生变化时仍需停止并增补方案；不得用批准前预验证替代实施阶段验证。
