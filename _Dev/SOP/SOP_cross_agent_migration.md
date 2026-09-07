---
id: SOP-XRAYUI-CROSS-AGENT-MIGRATION
title: 跨 Agent 工具移交与接管
document_type: sop
status: active
version: 2.0.0
project: XrayUI
owner: XrayUI maintainers
audience:
  - developer
  - agent
scope: _Dev/SOP
created_at: 2026-09-06T17:54:48+08:00
updated_at: 2026-09-07T02:10:00+08:00
tags:
  - migration
  - platform
  - handoff
source_of_truth: true
---

# 跨 Agent 工具移交与接管

## 适用场景

从 Codex、Antigravity、Claude Code、Cursor 或其他 Agent 工具切换到不同工具。平台能力必须以新会话实际可见信息为准。

## 原 Agent 移交准备

1. 停止新修改，核对文件、方案、验证、进程、快照和回滚路径。
2. 更新 current state 与 Work Item Registry，明确已验证成果、半成品、未知状态、批准边界和接手后的第一步。
3. 记录原平台可见能力和限制，但不得把权限假设传递为新平台事实。
4. 需要时按 retrospective SOP 提炼经验；不得把敏感数据放入交接记录。
5. 用白话向开发者说明已准备好切换，并提供可直接粘贴给新 Agent 的接管提示词。

## 新 Agent 接管

1. 按 `SOP_agent_platform_adaptation.md` 重新确认平台、能力层级、项目根、读写权限、Shell、网络、连接器、沙箱和审批机制。
2. 读取 Agent、system architecture、current state、Work Item Registry、相关方案和交接记录。
3. 按 `SOP_read_only_state_audit.md` 核对物理文件，只检查与任务有关的 `_wip` 内容。
4. 输出白话接管摘要：原任务是什么、现在到哪里、有什么风险、最安全的下一步。
5. 等待开发者确认后再实施，不自动恢复、覆盖、清理或继承原 Agent 的外部权限。
