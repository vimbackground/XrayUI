---
id: TEMPLATE-DOC-VHARNESS-PLATFORM-MATRIX
title: Agent 平台适配矩阵
document_type: reference
status: active
version: 1.0.0
project: vHarness
owner: vHarness maintainers
audience:
  - developer
  - agent
scope: _Dev/platforms
created_at: 2026-09-06T17:54:48+08:00
updated_at: 2026-09-06T17:54:48+08:00
tags:
  - platform
  - adaptation
---

# Agent 平台适配矩阵

此表是启动检查清单，不代表永久能力承诺；Agent 必须以当前会话实际暴露的能力为准。

| 平台族 | 优先确认 | 执行优化 |
|---|---|---|
| Codex | 工作区根、沙箱、审批、可用工具与技能 | 先读仓库指令；长任务使用检查点；按任务风险建议模型层级 |
| Antigravity | 文件系统映射、命令代理、上下文迁移范围 | 运行跨 Agent 迁移探针；不要沿用上一平台的权限假设 |
| Claude Code | 项目指令文件、工具许可、上下文预算 | 将长任务拆成可验证阶段；交接前固化状态与下一步 |
| Cursor | Agent/编辑器模式、终端权限、规则文件范围 | 区分编辑器索引与真实文件访问；用项目命令验证修改 |
| 未知平台 | 平台名称、读写、Shell、网络、审批机制 | 暂停写入，先向开发者确认平台并运行环境探针 |

推荐模型只使用 `fast/economical`、`balanced coding`、`deep reasoning`、`independent strong review` 能力层级。具体型号应在当前平台中核验后再推荐。
